import React, { useState, useEffect } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi, unwrapResult } from '../../utils/api';
import { Plus, Save, AlertCircle, RefreshCw, Edit2, X, Check } from 'lucide-react';
import { ConsoleSimulator, SimulationLog } from '../ConsoleSimulator';

interface GLAccount {
    id: string;
    accountCode: string;
    name: string;
    type: string;
    isActive: boolean;
}

const GLAccountList = () => {
    const { global, styles, t } = useTheme();
    const [accounts, setAccounts] = useState<GLAccount[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [accountCode, setAccountCode] = useState('');
    const [name, setName] = useState('');
    const [type, setType] = useState('Asset');
    const [message, setMessage] = useState('');

    // Editing State
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editForm, setEditForm] = useState<Partial<GLAccount>>({});

    // Simulation State
    const [logs, setLogs] = useState<SimulationLog[]>([]);
    const [showSimulation, setShowSimulation] = useState(false);

    const addLog = (message: string, type: 'info' | 'success' | 'error' | 'data', data?: any) => {
        setLogs(prev => [...prev, {
            id: Date.now() + Math.random(),
            timestamp: new Date().toLocaleTimeString(),
            message,
            type,
            data
        }]);
    };

    const fetchAccounts = async () => {
        setIsLoading(true);
        try {
            const response = await fetchApi('/api/finance/gl-accounts');
            const data = await unwrapResult(response);
            setAccounts(data);
        } catch (error) {
            console.error('Failed to fetch GL Accounts', error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchAccounts();
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setMessage('');
        setLogs([]);
        setShowSimulation(true);
        
        addLog('Starting Creation Process...', 'info');

        try {
            const payload = { accountCode, name, type };
            addLog('Step 1: Sending Create Command', 'info', payload);

            const response = await fetchApi('/api/finance/gl-accounts', {
                method: 'POST',
                body: JSON.stringify(payload),
            });
            const result = await unwrapResult(response);
            
            addLog('Step 1: Saved to SQL & Published Event', 'success', result);
            setMessage('GL Account Created Successfully');
            
            // Poll for update
            addLog('Step 2: Polling for Projection...', 'info');
            await pollForAccount(result.id);

            setAccountCode('');
            setName('');
            fetchAccounts(); 
        } catch (error: any) {
            setMessage('Error creating GL Account');
            addLog(`Failed: ${error.message}`, 'error');
        }
    };

    const startEditing = (account: GLAccount) => {
        setEditingId(account.id);
        setEditForm({ ...account });
    };

    const cancelEditing = () => {
        setEditingId(null);
        setEditForm({});
    };

    const handleUpdate = async () => {
        if (!editingId || !editForm.name) return;
        
        setLogs([]);
        setShowSimulation(true);
        addLog(`Starting Update for ${editForm.accountCode}...`, 'info');

        try {
            // NOTE: Assuming PUT endpoint exists or using same POST logic for now if UPSERT supported
            // If backend doesn't have explicit update, we might need to adjust.
            // For now, let's assume we call a hypothetical update endpoint or reuse POST if it handles upsert?
            // Usually CQRS has separate command. Let's try standard PUT convention.
            
            const payload = { 
                id: editingId,
                name: editForm.name,
                type: editForm.type,
                isActive: editForm.isActive 
            };
            
            addLog('Step 1: Sending Update Command', 'info', payload);

            // Backend likely needs a specific Update command. 
            // If not implemented, this might fail. We will see in simulation.
            const response = await fetchApi(`/api/finance/gl-accounts/${editingId}`, {
                method: 'PUT',
                body: JSON.stringify(payload),
            });
            
            if (!response.ok) throw new Error('Update failed or endpoint not found');
            
            addLog('Step 1: Update Command Accepted', 'success');
            
            // Poll for projection update
            addLog('Step 2: Polling for Projection Update...', 'info');
            await pollForAccount(editingId, editForm.name); // Pass expected name to verify

            setEditingId(null);
            fetchAccounts();
        } catch (error: any) {
            addLog(`Update Failed: ${error.message}`, 'error');
        }
    };

    const pollForAccount = async (id: string, expectedName?: string) => {
        let attempts = 0;
        const maxAttempts = 10;
        
        while (attempts < maxAttempts) {
            await new Promise(r => setTimeout(r, 1000));
            try {
                const response = await fetchApi('/api/finance/gl-accounts');
                const data: GLAccount[] = await unwrapResult(response);
                const account = data.find(a => a.id === id);
                
                if (account) {
                    if (expectedName && account.name !== expectedName) {
                        addLog(`Attempt ${attempts + 1}: Found account, but name mismatch (Projection lag?)`, 'info');
                    } else {
                        addLog('Step 2: Projection Verified in MongoDB', 'success', account);
                        return;
                    }
                } else {
                     addLog(`Attempt ${attempts + 1}: Account not found yet...`, 'info');
                }
            } catch (e) {
                console.log('Poll failed', e);
            }
            attempts++;
        }
        addLog('Step 2: Projection Timeout', 'error');
    };

    return (
        <div className={`p-6 max-w-6xl mx-auto`}>
            <ConsoleSimulator 
                logs={logs} 
                isVisible={showSimulation} 
                isLoading={isLoading} 
                title="GL ACCOUNT SIMULATOR"
            />

            <div className={`flex items-center justify-between mb-6`}>
                <h1 className={`text-2xl font-bold ${global.text}`}>General Ledger Accounts</h1>
                <button 
                    onClick={fetchAccounts}
                    className={`p-2 rounded-full hover:${global.bgSecondary} transition-colors ${global.textSecondary}`}
                    title="Refresh List"
                >
                    <RefreshCw size={20} />
                </button>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Create Form */}
                <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm h-fit`}>
                    <h2 className={`text-lg font-medium mb-4 ${global.text}`}>Create New Account</h2>
                    
                    {message && (
                        <div className={`p-3 mb-4 rounded-lg text-sm flex items-center gap-2 ${message.includes('Error') ? 'bg-red-50 text-red-600' : 'bg-green-50 text-green-600'}`}>
                            <AlertCircle size={16} />
                            {message}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Account Code</label>
                            <input
                                type="text"
                                value={accountCode}
                                onChange={(e) => setAccountCode(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Account Type</label>
                            <select
                                value={type}
                                onChange={(e) => setType(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            >
                                <option value="Asset">Asset</option>
                                <option value="Liability">Liability</option>
                                <option value="Equity">Equity</option>
                                <option value="Revenue">Revenue</option>
                                <option value="Expense">Expense</option>
                            </select>
                        </div>
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Account Name</label>
                            <input
                                type="text"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>

                        <div className="flex justify-end pt-2">
                            <button
                                type="submit"
                                className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium text-white transition-colors ${styles.bg} ${styles.hover}`}
                            >
                                <Save size={16} />
                                Save Account
                            </button>
                        </div>
                    </form>
                </div>

                {/* Accounts List */}
                <div className={`lg:col-span-2 rounded-xl border ${global.border} ${global.bg} shadow-sm overflow-hidden`}>
                    <table className="w-full text-left">
                        <thead className={`bg-gray-50/50 border-b ${global.border}`}>
                            <tr>
                                <th className={`p-4 font-medium ${global.textSecondary}`}>Code</th>
                                <th className={`p-4 font-medium ${global.textSecondary}`}>Name</th>
                                <th className={`p-4 font-medium ${global.textSecondary}`}>Type</th>
                                <th className={`p-4 font-medium ${global.textSecondary}`}>Status</th>
                                <th className={`p-4 font-medium ${global.textSecondary}`}>Actions</th>
                            </tr>
                        </thead>
                        <tbody className={`divide-y ${global.border}`}>
                            {accounts.map(acc => (
                                <tr key={acc.id} className="hover:bg-gray-50/50 transition-colors">
                                    <td className={`p-4 font-mono text-sm ${global.text}`}>{acc.accountCode}</td>
                                    
                                    <td className={`p-4 ${global.text}`}>
                                        {editingId === acc.id ? (
                                            <input 
                                                type="text" 
                                                value={editForm.name || ''} 
                                                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                                                className={`w-full p-1 border rounded ${global.bg} ${global.text}`}
                                                autoFocus
                                            />
                                        ) : (
                                            acc.name
                                        )}
                                    </td>

                                    <td className={`p-4 ${global.textSecondary}`}>
                                        {editingId === acc.id ? (
                                            <select
                                                value={editForm.type}
                                                onChange={(e) => setEditForm({ ...editForm, type: e.target.value })}
                                                className={`p-1 border rounded ${global.bg} ${global.text}`}
                                            >
                                                <option value="Asset">Asset</option>
                                                <option value="Liability">Liability</option>
                                                <option value="Equity">Equity</option>
                                                <option value="Revenue">Revenue</option>
                                                <option value="Expense">Expense</option>
                                            </select>
                                        ) : (
                                            acc.type
                                        )}
                                    </td>
                                    
                                    <td className="p-4">
                                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${acc.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                                            {acc.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>

                                    <td className="p-4 flex items-center gap-2">
                                        {editingId === acc.id ? (
                                            <>
                                                <button onClick={handleUpdate} className="p-1 text-green-600 hover:bg-green-50 rounded">
                                                    <Check size={16} />
                                                </button>
                                                <button onClick={cancelEditing} className="p-1 text-red-600 hover:bg-red-50 rounded">
                                                    <X size={16} />
                                                </button>
                                            </>
                                        ) : (
                                            <button onClick={() => startEditing(acc)} className={`p-1 ${global.textSecondary} hover:text-blue-600 hover:bg-blue-50 rounded transition-colors`}>
                                                <Edit2 size={16} />
                                            </button>
                                        )}
                                    </td>
                                </tr>
                            ))}
                            {accounts.length === 0 && !isLoading && (
                                <tr>
                                    <td colSpan={5} className={`p-8 text-center ${global.textSecondary}`}>
                                        No accounts found. Create one to get started.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

export default GLAccountList;
