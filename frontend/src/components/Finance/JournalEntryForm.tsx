import React, { useState } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi } from '../../utils/api';
import { Save, Plus, Trash2, AlertCircle } from 'lucide-react';

interface JournalLine {
    glAccountId: string; // In real app, this would be a GUID selected from a dropdown
    description: string;
    debit: number;
    credit: number;
}

const JournalEntryForm = () => {
    const { global, styles } = useTheme();
    const [description, setDescription] = useState('');
    const [reference, setReference] = useState('');
    const [lines, setLines] = useState<JournalLine[]>([
        { glAccountId: '', description: '', debit: 0, credit: 0 },
        { glAccountId: '', description: '', debit: 0, credit: 0 }
    ]);
    const [message, setMessage] = useState('');

    const addLine = () => {
        setLines([...lines, { glAccountId: '', description: '', debit: 0, credit: 0 }]);
    };

    const removeLine = (index: number) => {
        setLines(lines.filter((_, i) => i !== index));
    };

    const updateLine = (index: number, field: keyof JournalLine, value: any) => {
        const newLines = [...lines];
        newLines[index] = { ...newLines[index], [field]: value };
        setLines(newLines);
    };

    const totalDebit = lines.reduce((sum, line) => sum + Number(line.debit), 0);
    const totalCredit = lines.reduce((sum, line) => sum + Number(line.credit), 0);
    const isBalanced = totalDebit === totalCredit && totalDebit > 0;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!isBalanced) {
            setMessage('Error: Journal Entry is not balanced.');
            return;
        }

        try {
            await fetchApi('/api/finance/journal-entries', {
                method: 'POST',
                body: JSON.stringify({
                    postingDate: new Date().toISOString(),
                    description,
                    reference,
                    lines: lines.map(l => ({
                        ...l,
                        glAccountId: l.glAccountId // In a real app, validate GUID
                    }))
                }),
            });
            setMessage('Journal Entry Posted Successfully');
            setDescription('');
            setReference('');
            setLines([
                { glAccountId: '', description: '', debit: 0, credit: 0 },
                { glAccountId: '', description: '', debit: 0, credit: 0 }
            ]);
        } catch (error) {
            setMessage('Error posting Journal Entry. Ensure Account IDs are valid GUIDs.');
        }
    };

    return (
        <div className={`p-6 max-w-5xl mx-auto`}>
            <div className={`flex items-center justify-between mb-6`}>
                <h1 className={`text-2xl font-bold ${global.text}`}>Post Journal Entry</h1>
            </div>

            <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                {message && (
                    <div className={`p-3 mb-4 rounded-lg text-sm flex items-center gap-2 ${message.includes('Error') ? 'bg-red-50 text-red-600' : 'bg-green-50 text-green-600'}`}>
                        <AlertCircle size={16} />
                        {message}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-6">
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Description</label>
                            <input
                                type="text"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                required
                            />
                        </div>
                        <div>
                            <label className={`block text-sm font-medium mb-1 ${global.textSecondary}`}>Reference</label>
                            <input
                                type="text"
                                value={reference}
                                onChange={(e) => setReference(e.target.value)}
                                className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                            />
                        </div>
                    </div>

                    <div>
                        <div className="flex items-center justify-between mb-2">
                            <label className={`block text-sm font-medium ${global.textSecondary}`}>Lines</label>
                            <button
                                type="button"
                                onClick={addLine}
                                className={`text-xs flex items-center gap-1 ${styles.text}`}
                            >
                                <Plus size={14} /> Add Line
                            </button>
                        </div>
                        
                        <div className="space-y-2">
                            {lines.map((line, index) => (
                                <div key={index} className="flex gap-2 items-start">
                                    <input
                                        type="text"
                                        placeholder="GL Account ID (GUID)"
                                        value={line.glAccountId}
                                        onChange={(e) => updateLine(index, 'glAccountId', e.target.value)}
                                        className={`flex-1 p-2 text-sm rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                        required
                                    />
                                    <input
                                        type="text"
                                        placeholder="Description"
                                        value={line.description}
                                        onChange={(e) => updateLine(index, 'description', e.target.value)}
                                        className={`flex-[2] p-2 text-sm rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                    />
                                    <input
                                        type="number"
                                        placeholder="Debit"
                                        value={line.debit}
                                        onChange={(e) => updateLine(index, 'debit', parseFloat(e.target.value))}
                                        className={`w-24 p-2 text-sm rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                    />
                                    <input
                                        type="number"
                                        placeholder="Credit"
                                        value={line.credit}
                                        onChange={(e) => updateLine(index, 'credit', parseFloat(e.target.value))}
                                        className={`w-24 p-2 text-sm rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                                    />
                                    <button
                                        type="button"
                                        onClick={() => removeLine(index)}
                                        className="p-2 text-red-500 hover:bg-red-50 rounded-lg"
                                    >
                                        <Trash2 size={16} />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className={`flex justify-end items-center gap-6 pt-4 border-t ${global.border}`}>
                        <div className={`text-sm ${global.textSecondary}`}>
                            Total Debit: <span className="font-medium">{totalDebit.toFixed(2)}</span>
                        </div>
                        <div className={`text-sm ${global.textSecondary}`}>
                            Total Credit: <span className="font-medium">{totalCredit.toFixed(2)}</span>
                        </div>
                        <button
                            type="submit"
                            disabled={!isBalanced}
                            className={`flex items-center gap-2 px-6 py-2 rounded-lg font-medium text-white transition-colors ${isBalanced ? `${styles.bg} ${styles.hover}` : 'bg-gray-400 cursor-not-allowed'}`}
                        >
                            <Save size={16} />
                            Post Entry
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default JournalEntryForm;
