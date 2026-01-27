import React, { useState, useEffect } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi, unwrapResult } from '../../utils/api';
import { Plus, Save, AlertCircle, RefreshCw } from 'lucide-react';

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
        try {
            await fetchApi('/api/finance/gl-accounts', {
                method: 'POST',
                body: JSON.stringify({ accountCode, name, type }),
            });
            setMessage('GL Account Created Successfully');
            setAccountCode('');
            setName('');
            fetchAccounts(); // Refresh list
        } catch (error) {
            setMessage('Error creating GL Account');
        }
    };

    return (
        <div className={`p-6 max-w-6xl mx-auto`}>
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

                {/* Account List */}
                <div className={`lg:col-span-2 p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                    <h2 className={`text-lg font-medium mb-4 ${global.text}`}>Chart of Accounts</h2>
                    
                    {isLoading ? (
                        <div className={`text-center py-8 ${global.textSecondary}`}>Loading accounts...</div>
                    ) : accounts.length === 0 ? (
                        <div className={`text-center py-8 ${global.textSecondary}`}>No accounts found. Create one to get started.</div>
                    ) : (
                        <div className="overflow-x-auto">
                            <table className="w-full text-left text-sm">
                                <thead>
                                    <tr className={`border-b ${global.border}`}>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Code</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Name</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Type</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Status</th>
                                    </tr>
                                </thead>
                                <tbody className={`divide-y ${global.border}`}>
                                    {accounts.map((account) => (
                                        <tr key={account.id} className={`group hover:${global.bgSecondary} transition-colors`}>
                                            <td className={`py-3 font-medium ${global.text}`}>{account.accountCode}</td>
                                            <td className={`py-3 ${global.text}`}>{account.name}</td>
                                            <td className={`py-3`}>
                                                <span className={`px-2 py-1 rounded-full text-xs font-medium 
                                                    ${account.type === 'Asset' ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300' :
                                                      account.type === 'Liability' ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300' :
                                                      account.type === 'Equity' ? 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300' :
                                                      account.type === 'Revenue' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300' :
                                                      'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
                                                    }`}>
                                                    {account.type}
                                                </span>
                                            </td>
                                            <td className={`py-3`}>
                                                <span className={`flex items-center gap-1.5 ${account.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                                                    <span className={`w-1.5 h-1.5 rounded-full ${account.isActive ? 'bg-green-600' : 'bg-gray-400'}`}></span>
                                                    {account.isActive ? 'Active' : 'Inactive'}
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default GLAccountList;
