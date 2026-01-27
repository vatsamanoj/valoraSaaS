import React, { useState } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi } from '../../utils/api';
import { Save, AlertCircle, Lock } from 'lucide-react';

const EmployeePayrollForm = () => {
    const { global, styles } = useTheme();
    const [employeeId, setEmployeeId] = useState('');
    const [baseSalary, setBaseSalary] = useState(0);
    const [currency, setCurrency] = useState('USD');
    const [iban, setIban] = useState('');
    const [bankCountry, setBankCountry] = useState('US');
    const [bankKey, setBankKey] = useState(''); // Routing/IFSC
    const [bankAccount, setBankAccount] = useState('');
    const [message, setMessage] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            await fetchApi(`/api/hcm/employees/${employeeId}/payroll`, {
                method: 'POST',
                body: JSON.stringify({
                    baseSalary,
                    currency,
                    iban,
                    bankCountry,
                    bankKey,
                    bankAccountNumber: bankAccount,
                    effectiveDate: new Date().toISOString()
                }),
            });
            setMessage('Payroll Updated Successfully (Secure)');
        } catch (error) {
            setMessage('Error updating payroll. Ensure Employee ID is valid.');
        }
    };

    return (
        <div className={`p-6 max-w-2xl mx-auto`}>
            <div className={`flex items-center justify-between mb-6`}>
                <h1 className={`text-2xl font-bold ${global.text}`}>Employee Payroll (Secure)</h1>
                <Lock className="text-green-600" size={24} />
            </div>

            <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                {message && (
                    <div className={`p-3 mb-4 rounded-lg text-sm flex items-center gap-2 ${message.includes('Error') ? 'bg-red-50 text-red-600' : 'bg-green-50 text-green-600'}`}>
                        <AlertCircle size={16} />
                        {message}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Employee ID (GUID)</label>
                        <input
                            type="text"
                            value={employeeId}
                            onChange={(e) => setEmployeeId(e.target.value)}
                            className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            required
                        />
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Base Salary</label>
                            <input
                                type="number"
                                value={baseSalary}
                                onChange={(e) => setBaseSalary(parseFloat(e.target.value))}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Currency</label>
                            <select
                                value={currency}
                                onChange={(e) => setCurrency(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            >
                                <option value="USD">USD</option>
                                <option value="EUR">EUR</option>
                                <option value="INR">INR</option>
                                <option value="GBP">GBP</option>
                            </select>
                        </div>
                    </div>

                    <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                        <h3 className={`text-sm font-semibold mb-3 ${global.text}`}>Banking Information</h3>
                        
                        <div className="grid grid-cols-2 gap-4 mb-4">
                            <div>
                                <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Bank Country</label>
                                <input
                                    type="text"
                                    value={bankCountry}
                                    onChange={(e) => setBankCountry(e.target.value)}
                                    maxLength={2}
                                    className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                />
                            </div>
                            <div>
                                <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Bank Key (IFSC/Routing)</label>
                                <input
                                    type="text"
                                    value={bankKey}
                                    onChange={(e) => setBankKey(e.target.value)}
                                    className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                />
                            </div>
                        </div>

                        <div className="mb-4">
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Account Number</label>
                            <input
                                type="text"
                                value={bankAccount}
                                onChange={(e) => setBankAccount(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            />
                        </div>

                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>IBAN (Optional)</label>
                            <input
                                type="text"
                                value={iban}
                                onChange={(e) => setIban(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            />
                        </div>
                    </div>

                    <div className="flex justify-end pt-4">
                        <button
                            type="submit"
                            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium text-white transition-colors ${styles.bg} ${styles.hover}`}
                        >
                            <Save size={16} />
                            Update Secure Payroll
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default EmployeePayrollForm;
