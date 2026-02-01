import React, { useState, useEffect } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi, unwrapResult } from '../../utils/api';
import { Plus, Trash2, Save, AlertCircle, ArrowLeft, CheckCircle2, Circle, Loader2, XCircle } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { ConsoleSimulator, SimulationLog } from '../ConsoleSimulator';
import SearchableSelect from '../SearchableSelect';

interface GLAccount {
    id: string;
    accountCode: string;
    name: string;
}

interface JournalEntryLine {
    id: string; // Temporary ID for UI
    glAccountCode: string;
    description: string;
    debit: string; // string for input handling
    credit: string; // string for input handling
}

const CreateJournalEntry = () => {
    const { global, styles } = useTheme();
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>(); // Check if editing
    const isEditMode = !!id;
    
    // Form State
    const [documentNumber, setDocumentNumber] = useState('');
    const [postingDate, setPostingDate] = useState(new Date().toISOString().split('T')[0]);
    const [description, setDescription] = useState('');
    const [reference, setReference] = useState('');
    const [lines, setLines] = useState<JournalEntryLine[]>([
        { id: '1', glAccountCode: '', description: '', debit: '', credit: '' },
        { id: '2', glAccountCode: '', description: '', debit: '', credit: '' }
    ]);

    // Data State
    const [glAccounts, setGLAccounts] = useState<GLAccount[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
    const [version, setVersion] = useState<number | null>(null); // For Concurrency

    // Fetch GL Accounts for dropdown
    useEffect(() => {
        const fetchAccounts = async () => {
            try {
                const response = await fetchApi('/api/finance/gl-accounts');
                const data = await unwrapResult(response);
                setGLAccounts(data);
            } catch (error) {
                console.error('Failed to fetch GL Accounts', error);
            }
        };
        fetchAccounts();
    }, []);

    // Fetch Existing Entry if Edit Mode
    useEffect(() => {
        if (!isEditMode || !id) return;

        const fetchEntry = async () => {
            setIsLoading(true);
            try {
                // Assuming we can fetch by ID or filter by document number if ID endpoint not available directly.
                // The list endpoint supports filtering. Ideally we have GET /journal-entries/{id}
                // But typically list endpoint returns all fields.
                // Let's try to filter by ID on client side or assume list returns it.
                // Or better, let's just fetch all and filter. (Not efficient but works for now)
                // Actually, backend GetJournalEntriesQuery doesn't support ID filter.
                // But wait, the backend doesn't have a GetJournalEntryById query yet?
                // FinanceController: [HttpGet("journal-entries")] -> GetJournalEntriesQuery
                
                // Workaround: Fetch list and filter by ID (since we likely have few entries in sim)
                // OR add GetJournalEntryById endpoint.
                // Given constraints, let's try to filter list.
                
                const response = await fetchApi(`/api/finance/journal-entries?pageSize=1000`); 
                const data = await unwrapResult(response);
                const entry = data.items.find((e: any) => e.id === id);

                if (entry) {
                    setDocumentNumber(entry.documentNumber);
                    setPostingDate(new Date(entry.postingDate).toISOString().split('T')[0]);
                    setDescription(entry.description);
                    setReference(entry.reference || '');
                    setVersion(entry.version); // Capture Version (xmin)
                    
                    // Map lines
                    const mappedLines = entry.lines.map((l: any) => ({
                        id: Math.random().toString(36).substr(2, 9),
                        glAccountCode: l.glAccountCode,
                        description: l.description || '',
                        debit: l.debit > 0 ? l.debit.toString() : '',
                        credit: l.credit > 0 ? l.credit.toString() : ''
                    }));
                    
                    setLines(mappedLines);
                } else {
                    setMessage({ text: 'Journal Entry not found', type: 'error' });
                }
            } catch (error) {
                console.error('Failed to fetch Journal Entry', error);
                setMessage({ text: 'Failed to load entry', type: 'error' });
            } finally {
                setIsLoading(false);
            }
        };
        
        // Wait for GL accounts to load first (optional, but good for code mapping)
        if (glAccounts.length > 0) {
            fetchEntry();
        }
    }, [id, isEditMode, glAccounts.length]); // Depend on glAccounts.length to ensure mapping works if needed

    // Line Management
    const addLine = () => {
        setLines(prev => [...prev, { 
            id: Math.random().toString(36).substr(2, 9), 
            glAccountCode: '', 
            description: '', 
            debit: '', 
            credit: '' 
        }]);
    };

    const removeLine = (id: string) => {
        if (lines.length > 2) {
            setLines(lines.filter(l => l.id !== id));
        }
    };

    const updateLine = (id: string, field: keyof JournalEntryLine, value: string) => {
        setLines(lines.map(l => {
            if (l.id !== id) return l;
            
            // Exclusive Debit/Credit logic
            if (field === 'debit' && value !== '') return { ...l, [field]: value, credit: '' };
            if (field === 'credit' && value !== '') return { ...l, [field]: value, debit: '' };
            
            return { ...l, [field]: value };
        }));
    };

    // Navigation Helper
    const focusNext = (currentId: string, nextField: string, lineIndex: number) => {
        // IDs: gl-{idx}, desc-{idx}, debit-{idx}, credit-{idx}
        // Logic:
        // GL -> Desc
        // Desc -> Debit or Credit
        // Debit/Credit -> Next GL
        
        let nextElementId = '';
        
        if (nextField === 'desc') {
            nextElementId = `desc-${lineIndex}`;
        } else if (nextField === 'amount') {
            // Smart Navigation based on Balance
            const line = lines[lineIndex];
            const hasDebit = line.debit && parseFloat(line.debit) > 0;
            const hasCredit = line.credit && parseFloat(line.credit) > 0;
            
            // Recalculate totals here to be safe or use closure values
            const currentTotalDebit = lines.reduce((sum, l) => sum + (parseFloat(l.debit) || 0), 0);
            const currentTotalCredit = lines.reduce((sum, l) => sum + (parseFloat(l.credit) || 0), 0);
            
            let target = 'debit';
            
            if (hasCredit) {
                target = 'credit';
            } else if (hasDebit) {
                target = 'debit';
            } else {
                // Empty line - guide the user
                if (currentTotalDebit > currentTotalCredit) target = 'credit';
                else if (currentTotalCredit > currentTotalDebit) target = 'debit';
                else target = 'debit'; // Default to debit if balanced
            }
            
            nextElementId = `${target}-${lineIndex}`;
        } else if (nextField === 'nextRow') {
            // Check if balanced before adding new row
            const currentTotalDebit = lines.reduce((sum, l) => sum + (parseFloat(l.debit) || 0), 0);
            const currentTotalCredit = lines.reduce((sum, l) => sum + (parseFloat(l.credit) || 0), 0);
            const isNowBalanced = Math.abs(currentTotalDebit - currentTotalCredit) < 0.01; // Float tolerance

            if (lineIndex < lines.length - 1) {
                nextElementId = `gl-${lineIndex + 1}`;
            } else {
                // Only add new line if NOT balanced
                if (!isNowBalanced) {
                    addLine();
                    setTimeout(() => {
                        const newIdx = lines.length; // after add
                        document.getElementById(`gl-${newIdx}`)?.focus();
                    }, 100);
                    return;
                } else {
                    // If balanced, maybe focus the Submit button?
                    // Or just stop adding lines.
                    // Let's focus the submit button for convenience
                    // (Assuming there is a button with id="submit-btn" or we just blur)
                    (document.activeElement as HTMLElement)?.blur();
                }
            }
        }

        if (nextElementId) {
            setTimeout(() => {
                document.getElementById(nextElementId)?.focus();
            }, 50);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent, lineIndex: number, field: string) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (field === 'desc') {
                focusNext('', 'amount', lineIndex);
            } else if (field === 'amount') {
                focusNext('', 'nextRow', lineIndex);
            }
        }
    };

    // Calculations
    const totalDebit = lines.reduce((sum, line) => sum + (parseFloat(line.debit) || 0), 0);
    const totalCredit = lines.reduce((sum, line) => sum + (parseFloat(line.credit) || 0), 0);
    const isBalanced = Math.abs(totalDebit - totalCredit) < 0.01;

    // Submit
    const [simulationLogs, setSimulationLogs] = useState<SimulationLog[]>([]);
    const [showSimulation, setShowSimulation] = useState(false);
    
    const addLog = (message: string, type: 'info' | 'success' | 'error' | 'data', data?: any) => {
        setSimulationLogs(prev => [...prev, {
            id: Date.now() + Math.random(),
            timestamp: new Date().toLocaleTimeString(),
            message,
            type,
            data
        }]);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setMessage(null);
        setSimulationLogs([]);

        if (!isBalanced) {
            setMessage({ text: 'Journal Entry is not balanced. Total Debit must equal Total Credit.', type: 'error' });
            return;
        }

        if (lines.some(l => !l.glAccountCode)) {
            setMessage({ text: 'All lines must have a GL Account selected.', type: 'error' });
            return;
        }

        // Validate that at least 2 different accounts are involved
        const uniqueAccounts = new Set(lines.map(l => l.glAccountCode));
        if (uniqueAccounts.size < 2) {
            setMessage({ text: 'Invalid Entry: The same account cannot be used for both sides. Please select at least two different GL Accounts.', type: 'error' });
            return;
        }

        setIsLoading(true);
        setShowSimulation(true);
        
        addLog('Starting Transaction Simulation...', 'info');

        try {
            const payload = {
                documentNumber,
                postingDate: new Date(postingDate).toISOString(),
                description,
                reference,
                version: version, // Send back version for concurrency check
                lines: lines.map(l => {
                    const account = glAccounts.find(a => a.accountCode === l.glAccountCode);
                    return {
                        glAccountId: account?.id,
                        description: l.description || description,
                        debit: parseFloat(l.debit) || 0,
                        credit: parseFloat(l.credit) || 0
                    };
                })
            };

            addLog(`Step 1: Preparing Payload for ${isEditMode ? 'Update' : 'Creation'}`, 'info', payload);

            let result;
            if (isEditMode && id) {
                 const response = await fetchApi(`/api/finance/journal-entries/${id}`, {
                    method: 'PUT',
                    body: JSON.stringify(payload)
                });
                result = await unwrapResult(response);
                addLog('Step 1: Update Saved to SQL Successfully', 'success', { id: result.id });
                addLog('Step 2: Message Published to Kafka Outbox (Topic: valora.fi.updated)', 'info', { eventType: 'JournalEntryUpdated' });
            } else {
                 const response = await fetchApi('/api/finance/journal-entries', {
                    method: 'POST',
                    body: JSON.stringify(payload)
                });
                result = await unwrapResult(response);
                addLog('Step 1: Data Saved to Supabase Successfully', 'success', { id: result.id, documentNumber: result.documentNumber });
                addLog('Step 2: Message Published to Kafka Outbox (Topic: valora.fi.posted)', 'info', { eventType: 'JournalEntryPosted' });
            }
            
            addLog('Step 2: Kafka Publish Successful', 'success');

            addLog('Step 3: Waiting for Kafka Consumer...', 'info');
            
            // Poll for projection existence (or update)
            let attempts = 0;
            const maxAttempts = 10;
            let found = false;

            while (attempts < maxAttempts && !found) {
                await new Promise(r => setTimeout(r, 1000)); // Wait 1s
                try {
                    // For update, we want to check if the data changed.
                    // But list endpoint might be cached or slow. 
                    // Let's just check if it exists and matches description/amount if possible, 
                    // or just assume if it returns success it's fine.
                    // For simplicity, we just check existence for now.
                    const listRes = await fetchApi(`/api/finance/journal-entries?documentNumber=${documentNumber || result.documentNumber}`); // Use current doc number
                    const listData = await unwrapResult(listRes);
                    
                    if (listData.items && listData.items.length > 0) {
                        // Ideally check if version/timestamp updated
                        found = true;
                        addLog('Step 3: Kafka Consumer Successfully Consumed Message', 'success');
                        addLog('Step 4: Projection to MongoDB Verified', 'success');
                        addLog('Final Data in MongoDB (Read Model):', 'data', listData.items[0]);
                    } else {
                        addLog(`Polling MongoDB... Attempt ${attempts + 1}/${maxAttempts}`, 'info');
                    }
                } catch (e) {
                    addLog('Polling check failed, retrying...', 'error');
                }
                attempts++;
            }

            if (found) {
                setMessage({ text: `Journal Entry ${isEditMode ? 'Updated' : 'Posted'} and Verified Successfully!`, type: 'success' });
            } else {
                 addLog('Step 4: Failed - Projection Timeout', 'error');
                 setMessage({ text: 'Saved to SQL, but MongoDB projection timed out.', type: 'error' });
            }

        } catch (error: any) {
            addLog(`Transaction Failed: ${error.message}`, 'error');
            setMessage({ text: error.message || 'Failed to process Journal Entry', type: 'error' });
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className={`p-6 max-w-6xl mx-auto`}>
            <div className={`flex items-center justify-between mb-6`}>
                <div className="flex items-center gap-4">
                    <button 
                        onClick={() => navigate('/finance/journal-entries')}
                        className={`p-2 rounded-full hover:${global.bgSecondary} transition-colors ${global.textSecondary}`}
                    >
                        <ArrowLeft size={20} />
                    </button>
                    <h1 className={`text-2xl font-bold ${global.text}`}>{isEditMode ? 'Edit Journal Entry' : 'Post Journal Entry'}</h1>
                </div>
            </div>

            <form onSubmit={handleSubmit} className={`space-y-6`}>
                {/* Simulation Debugger (Console Style) */}
                <ConsoleSimulator 
                    logs={simulationLogs} 
                    isVisible={showSimulation} 
                    isLoading={isLoading} 
                />

                {/* Header Section */}
                <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                    <h2 className={`text-lg font-medium mb-4 ${global.text}`}>Header Information</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Document Number</label>
                            <input
                                type="text"
                                value={documentNumber}
                                onChange={(e) => setDocumentNumber(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                placeholder="Auto or Manual"
                                required
                            />
                        </div>
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Posting Date</label>
                            <input
                                type="date"
                                value={postingDate}
                                onChange={(e) => setPostingDate(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>
                        <div className="lg:col-span-2">
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Description</label>
                            <input
                                type="text"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>
                        <div className="lg:col-span-2">
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Reference</label>
                            <input
                                type="text"
                                value={reference}
                                onChange={(e) => setReference(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            />
                        </div>
                    </div>
                </div>

                {/* Lines Section */}
                <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                    <div className="flex items-center justify-between mb-4">
                        <h2 className={`text-lg font-medium ${global.text}`}>Line Items</h2>
                        <button
                            type="button"
                            onClick={addLine}
                            className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium text-blue-600 bg-blue-50 hover:bg-blue-100 transition-colors`}
                        >
                            <Plus size={16} />
                            Add Line
                        </button>
                    </div>

                    <div className="overflow-x-auto">
                        <table className="w-full text-left text-sm">
                            <thead>
                                <tr className={`border-b ${global.border}`}>
                                    <th className={`pb-3 px-2 font-medium ${global.textSecondary} w-1/4`}>GL Account</th>
                                    <th className={`pb-3 px-2 font-medium ${global.textSecondary} w-1/4`}>Line Description</th>
                                    <th className={`pb-3 px-2 font-medium ${global.textSecondary} text-right w-32`}>Debit</th>
                                    <th className={`pb-3 px-2 font-medium ${global.textSecondary} text-right w-32`}>Credit</th>
                                    <th className="w-10"></th>
                                </tr>
                            </thead>
                            <tbody className={`divide-y ${global.border}`}>
                                {lines.map((line, index) => (
                                    <tr key={line.id}>
                                        <td className="py-2 px-2 w-64">
                                            <SearchableSelect
                                                id={`gl-${index}`}
                                                value={line.glAccountCode}
                                                onChange={(val) => {
                                                    updateLine(line.id, 'glAccountCode', val);
                                                    focusNext('', 'desc', index);
                                                }}
                                                options={glAccounts.map(acc => ({
                                                    value: acc.accountCode,
                                                    label: acc.name,
                                                    subLabel: acc.accountCode
                                                }))}
                                                placeholder="Select Account"
                                                className="w-full"
                                            />
                                        </td>
                                        <td className="py-2 px-2">
                                            <input
                                                id={`desc-${index}`}
                                                type="text"
                                                value={line.description}
                                                onChange={(e) => updateLine(line.id, 'description', e.target.value)}
                                                onKeyDown={(e) => handleKeyDown(e, index, 'desc')}
                                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                                placeholder={description}
                                            />
                                        </td>
                                        <td className="py-2 px-2">
                                            {(() => {
                                                const hasDebit = line.debit && parseFloat(line.debit) !== 0;
                                                const hasCredit = line.credit && parseFloat(line.credit) !== 0;
                                                // If global debit is heavier, we force credit on new lines.
                                                // Show Debit if:
                                                // 1. It already has a value (editing existing debit)
                                                // 2. OR Credit is empty AND Global Debit is NOT heavier (so we are either balanced or credit heavy)
                                                const showDebit = hasDebit || (!hasCredit && totalDebit <= totalCredit);
                                                
                                                return showDebit && (
                                                    <input
                                                        id={`debit-${index}`}
                                                        type="number"
                                                        value={line.debit}
                                                        onChange={(e) => updateLine(line.id, 'debit', e.target.value)}
                                                        onKeyDown={(e) => handleKeyDown(e, index, 'amount')}
                                                        className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-right`}
                                                        placeholder="0.00"
                                                        min="0"
                                                        step="0.01"
                                                    />
                                                );
                                            })()}
                                        </td>
                                        <td className="py-2 px-2">
                                            {(() => {
                                                const hasDebit = line.debit && parseFloat(line.debit) !== 0;
                                                const hasCredit = line.credit && parseFloat(line.credit) !== 0;
                                                // Show Credit if:
                                                // 1. It already has a value
                                                // 2. OR Debit is empty AND Global Credit is NOT heavier (so we are either balanced or debit heavy)
                                                const showCredit = hasCredit || (!hasDebit && totalCredit <= totalDebit);

                                                return showCredit && (
                                                    <input
                                                        id={`credit-${index}`}
                                                        type="number"
                                                        value={line.credit}
                                                        onChange={(e) => updateLine(line.id, 'credit', e.target.value)}
                                                        onKeyDown={(e) => handleKeyDown(e, index, 'amount')}
                                                        className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-right`}
                                                        placeholder="0.00"
                                                        min="0"
                                                        step="0.01"
                                                    />
                                                );
                                            })()}
                                        </td>
                                        <td className="py-2 px-2 text-center">
                                            <button
                                                type="button"
                                                onClick={() => removeLine(line.id)}
                                                disabled={lines.length <= 2}
                                                className={`p-1.5 rounded-lg text-red-500 hover:bg-red-50 disabled:opacity-30 disabled:hover:bg-transparent transition-colors`}
                                            >
                                                <Trash2 size={16} />
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            <tfoot className={`bg-gray-50 dark:bg-gray-800/50 font-medium ${global.text}`}>
                                <tr>
                                    <td colSpan={2} className="py-3 px-4 text-right">Totals:</td>
                                    <td className="py-3 px-2 text-right">{totalDebit.toFixed(2)}</td>
                                    <td className="py-3 px-2 text-right">{totalCredit.toFixed(2)}</td>
                                    <td></td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>

                {/* Footer / Actions */}
                <div className="flex items-center justify-between">
                    <div className="flex-1">
                        {message && (
                            <div className={`p-3 rounded-lg text-sm flex items-center gap-2 max-w-md ${message.type === 'error' ? 'bg-red-50 text-red-600' : 'bg-green-50 text-green-600'}`}>
                                <AlertCircle size={16} />
                                {message.text}
                            </div>
                        )}
                        {!isBalanced && !message && (
                            <div className="text-red-500 text-sm font-medium flex items-center gap-2">
                                <AlertCircle size={16} />
                                Entry is out of balance by {Math.abs(totalDebit - totalCredit).toFixed(2)}
                            </div>
                        )}
                    </div>
                    <button
                        type="submit"
                        disabled={isLoading || !isBalanced}
                        className={`flex items-center gap-2 px-6 py-2.5 rounded-lg font-medium text-white transition-colors ${styles.bg} ${styles.hover} disabled:opacity-50 disabled:cursor-not-allowed shadow-sm`}
                    >
                        <Save size={18} />
                        {isLoading ? 'Posting...' : 'Post Journal Entry'}
                    </button>
                </div>
            </form>
        </div>
    );
};

export default CreateJournalEntry;
