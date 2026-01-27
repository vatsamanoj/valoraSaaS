import React, { useState, useEffect } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { fetchApi, unwrapResult } from '../../utils/api';
import { RefreshCw, FileText, ChevronDown, ChevronRight, ChevronLeft, Plus, Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

interface JournalEntryLine {
    id: string;
    description: string;
    debit: number;
    credit: number;
    glAccountName: string;
    glAccountCode: string;
}

interface JournalEntry {
    id: string;
    documentNumber: string;
    postingDate: string;
    description: string;
    status: number;
    lines: JournalEntryLine[];
}

const JournalEntryList = () => {
    const { global, formatCurrency, styles } = useTheme();
    const navigate = useNavigate();
    const [entries, setEntries] = useState<JournalEntry[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [expandedIds, setExpandedIds] = useState<string[]>([]);
    
    // Pagination State
    const [page, setPage] = useState(1);
    const [pageSize] = useState(20);
    const [totalCount, setTotalCount] = useState(0);

    // Filter State
    const [searchDocNum, setSearchDocNum] = useState('');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');

    const fetchEntries = async () => {
        setIsLoading(true);
        try {
            const query = new URLSearchParams({
                page: page.toString(),
                pageSize: pageSize.toString(),
            });

            if (searchDocNum) query.append('documentNumber', searchDocNum);
            if (startDate) query.append('startDate', startDate);
            if (endDate) query.append('endDate', endDate);

            const response = await fetchApi(`/api/finance/journal-entries?${query.toString()}`);
            const result = await unwrapResult(response);
            
            // Backend now returns { items: [], totalCount: 0, page: 1, pageSize: 20 }
            setEntries(result.items);
            setTotalCount(result.totalCount);
        } catch (error) {
            console.error('Failed to fetch Journal Entries', error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchEntries();
    }, [page]); // Re-fetch when page changes

    const handleSearch = () => {
        setPage(1);
        fetchEntries();
    };

    const toggleExpand = (id: string) => {
        setExpandedIds(prev => 
            prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
        );
    };

    const totalPages = Math.ceil(totalCount / pageSize);

    return (
        <div className={`p-6 max-w-6xl mx-auto`}>
            <div className={`flex items-center justify-between mb-6`}>
                <h1 className={`text-2xl font-bold ${global.text}`}>Journal Entries</h1>
                <div className="flex items-center gap-2">
                    <button
                        onClick={() => navigate('/finance/journal-entry')}
                        className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium text-white transition-colors ${styles.bg} ${styles.hover}`}
                    >
                        <Plus size={18} />
                        Create Entry
                    </button>
                    <button 
                        onClick={fetchEntries}
                        className={`p-2 rounded-full hover:${global.bgSecondary} transition-colors ${global.textSecondary}`}
                        title="Refresh List"
                    >
                        <RefreshCw size={20} />
                    </button>
                </div>
            </div>

            {/* Filters */}
            <div className={`p-4 mb-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
                    <div>
                        <label className={`block text-xs font-medium mb-1 ${global.textSecondary}`}>Document Number</label>
                        <input 
                            type="text" 
                            value={searchDocNum} 
                            onChange={e => setSearchDocNum(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && handleSearch()}
                            className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-sm`}
                            placeholder="Search..."
                        />
                    </div>
                    <div>
                        <label className={`block text-xs font-medium mb-1 ${global.textSecondary}`}>Start Date</label>
                        <input 
                            type="date" 
                            value={startDate} 
                            onChange={e => setStartDate(e.target.value)}
                            className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-sm`}
                        />
                    </div>
                    <div>
                        <label className={`block text-xs font-medium mb-1 ${global.textSecondary}`}>End Date</label>
                        <input 
                            type="date" 
                            value={endDate} 
                            onChange={e => setEndDate(e.target.value)}
                            className={`w-full p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-sm`}
                        />
                    </div>
                    <div>
                        <button 
                            onClick={handleSearch}
                            className={`w-full flex items-center justify-center gap-2 p-2 rounded-lg font-medium text-white transition-colors ${styles.bg} ${styles.hover} text-sm`}
                        >
                            <Search size={16} />
                            Search
                        </button>
                    </div>
                </div>
            </div>

            <div className={`p-6 rounded-xl border ${global.border} ${global.bg} shadow-sm`}>
                {isLoading ? (
                    <div className={`text-center py-8 ${global.textSecondary}`}>Loading entries...</div>
                ) : entries.length === 0 ? (
                    <div className={`text-center py-8 ${global.textSecondary}`}>No journal entries found.</div>
                ) : (
                    <>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left text-sm">
                                <thead>
                                    <tr className={`border-b ${global.border}`}>
                                        <th className="w-8"></th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Document #</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Posting Date</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Description</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Total Amount</th>
                                        <th className={`pb-3 font-medium ${global.textSecondary}`}>Status</th>
                                    </tr>
                                </thead>
                                <tbody className={`divide-y ${global.border}`}>
                                    {entries.map((entry) => {
                                        const isExpanded = expandedIds.includes(entry.id);
                                        const totalDebit = entry.lines.reduce((sum, line) => sum + line.debit, 0);

                                        return (
                                            <React.Fragment key={entry.id}>
                                                <tr 
                                                    className={`group hover:${global.bgSecondary} transition-colors cursor-pointer`}
                                                    onClick={() => toggleExpand(entry.id)}
                                                >
                                                    <td className="py-3 pl-2">
                                                        {isExpanded ? <ChevronDown size={16} className={global.textSecondary} /> : <ChevronRight size={16} className={global.textSecondary} />}
                                                    </td>
                                                    <td className={`py-3 font-medium ${global.text} flex items-center gap-2`}>
                                                        <FileText size={16} className="text-blue-500" />
                                                        {entry.documentNumber}
                                                    </td>
                                                    <td className={`py-3 ${global.text}`}>
                                                        {new Date(entry.postingDate).toLocaleDateString()}
                                                    </td>
                                                    <td className={`py-3 ${global.text}`}>{entry.description}</td>
                                                    <td className={`py-3 font-medium ${global.text}`}>
                                                        {formatCurrency(totalDebit)}
                                                    </td>
                                                    <td className={`py-3`}>
                                                        <span className={`px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300`}>
                                                            Posted
                                                        </span>
                                                    </td>
                                                </tr>
                                                {isExpanded && (
                                                    <tr className={`${global.bgSecondary} bg-opacity-50`}>
                                                        <td colSpan={6} className="p-4">
                                                            <div className={`rounded-lg border ${global.border} overflow-hidden bg-white dark:bg-gray-900`}>
                                                                <table className="w-full text-xs">
                                                                    <thead className={`bg-gray-50 dark:bg-gray-800 border-b ${global.border}`}>
                                                                        <tr>
                                                                            <th className={`py-2 px-4 font-medium ${global.textSecondary}`}>Account</th>
                                                                            <th className={`py-2 px-4 font-medium ${global.textSecondary}`}>Description</th>
                                                                            <th className={`py-2 px-4 font-medium ${global.textSecondary} text-right`}>Debit</th>
                                                                            <th className={`py-2 px-4 font-medium ${global.textSecondary} text-right`}>Credit</th>
                                                                        </tr>
                                                                    </thead>
                                                                    <tbody className={`divide-y ${global.border}`}>
                                                                        {entry.lines.map((line) => (
                                                                            <tr key={line.id}>
                                                                                <td className={`py-2 px-4 ${global.text}`}>
                                                                                    <div className="font-medium">{line.glAccountCode}</div>
                                                                                    <div className="text-gray-500">{line.glAccountName}</div>
                                                                                </td>
                                                                                <td className={`py-2 px-4 ${global.text}`}>{line.description}</td>
                                                                                <td className={`py-2 px-4 ${global.text} text-right`}>
                                                                                    {line.debit > 0 ? formatCurrency(line.debit) : '-'}
                                                                                </td>
                                                                                <td className={`py-2 px-4 ${global.text} text-right`}>
                                                                                    {line.credit > 0 ? formatCurrency(line.credit) : '-'}
                                                                                </td>
                                                                            </tr>
                                                                        ))}
                                                                    </tbody>
                                                                </table>
                                                            </div>
                                                        </td>
                                                    </tr>
                                                )}
                                            </React.Fragment>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>

                        {/* Pagination Controls */}
                        <div className="flex items-center justify-between mt-4 pt-4 border-t border-gray-200 dark:border-gray-800">
                            <div className={`text-sm ${global.textSecondary}`}>
                                Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, totalCount)} of {totalCount} entries
                            </div>
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={() => setPage(p => Math.max(1, p - 1))}
                                    disabled={page === 1}
                                    className={`p-2 rounded-lg border ${global.border} disabled:opacity-50 hover:${global.bgSecondary} transition-colors ${global.text}`}
                                >
                                    <ChevronLeft size={16} />
                                </button>
                                <span className={`text-sm ${global.text} px-2`}>
                                    Page {page} of {totalPages || 1}
                                </span>
                                <button
                                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                                    disabled={page === totalPages || totalPages === 0}
                                    className={`p-2 rounded-lg border ${global.border} disabled:opacity-50 hover:${global.bgSecondary} transition-colors ${global.text}`}
                                >
                                    <ChevronRight size={16} />
                                </button>
                            </div>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
};

export default JournalEntryList;
