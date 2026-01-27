import React, { useState, useEffect } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi, unwrapResult } from '../../utils/api';
import { Plus, Trash2, Save, AlertCircle, ArrowLeft } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

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

    // Line Management
    const addLine = () => {
        setLines([...lines, { 
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

    // Calculations
    const totalDebit = lines.reduce((sum, line) => sum + (parseFloat(line.debit) || 0), 0);
    const totalCredit = lines.reduce((sum, line) => sum + (parseFloat(line.credit) || 0), 0);
    const isBalanced = Math.abs(totalDebit - totalCredit) < 0.01;

    // Submit
    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setMessage(null);

        if (!isBalanced) {
            setMessage({ text: 'Journal Entry is not balanced. Total Debit must equal Total Credit.', type: 'error' });
            return;
        }

        if (lines.some(l => !l.glAccountCode)) {
            setMessage({ text: 'All lines must have a GL Account selected.', type: 'error' });
            return;
        }

        setIsLoading(true);
        try {
            const payload = {
                documentNumber,
                postingDate,
                description,
                reference,
                lines: lines.map(l => ({
                    glAccountCode: l.glAccountCode,
                    description: l.description || description, // Fallback to header description
                    debit: parseFloat(l.debit) || 0,
                    credit: parseFloat(l.credit) || 0
                })).filter(l => l.debit > 0 || l.credit > 0)
            };

            await fetchApi('/api/finance/journal-entries', {
                method: 'POST',
                body: JSON.stringify(payload),
            });

            setMessage({ text: 'Journal Entry Posted Successfully', type: 'success' });
            
            // Reset form
            setTimeout(() => {
                navigate('/finance/journal-entries');
            }, 1500);

        } catch (error: any) {
            setMessage({ text: error.message || 'Error posting journal entry', type: 'error' });
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
                    <h1 className={`text-2xl font-bold ${global.text}`}>Post Journal Entry</h1>
                </div>
            </div>

            <form onSubmit={handleSubmit} className={`space-y-6`}>
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
                                        <td className="py-2 px-2">
                                            <select
                                                value={line.glAccountCode}
                                                onChange={(e) => updateLine(line.id, 'glAccountCode', e.target.value)}
                                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                                required
                                            >
                                                <option value="">Select Account</option>
                                                {glAccounts.map(acc => (
                                                    <option key={acc.id} value={acc.accountCode}>
                                                        {acc.accountCode} - {acc.name}
                                                    </option>
                                                ))}
                                            </select>
                                        </td>
                                        <td className="py-2 px-2">
                                            <input
                                                type="text"
                                                value={line.description}
                                                onChange={(e) => updateLine(line.id, 'description', e.target.value)}
                                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                                placeholder={description}
                                            />
                                        </td>
                                        <td className="py-2 px-2">
                                            <input
                                                type="number"
                                                value={line.debit}
                                                onChange={(e) => updateLine(line.id, 'debit', e.target.value)}
                                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-right`}
                                                placeholder="0.00"
                                                min="0"
                                                step="0.01"
                                            />
                                        </td>
                                        <td className="py-2 px-2">
                                            <input
                                                type="number"
                                                value={line.credit}
                                                onChange={(e) => updateLine(line.id, 'credit', e.target.value)}
                                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-right`}
                                                placeholder="0.00"
                                                min="0"
                                                step="0.01"
                                            />
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
