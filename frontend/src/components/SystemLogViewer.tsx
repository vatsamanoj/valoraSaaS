import { useEffect, useState } from 'react';
import Modal from './Modal';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';

interface FixStep {
  id: number;
  stepDescription: string;
  stepType: string;
  status: string;
  createdAt: string;
}

interface SystemLog {
  id: string;
  type: string;
  isFixed: boolean;
  data: string; // JSON
  createdAt: string;
  fixedAt?: string;
  fixedBy?: string;
  fixSteps?: FixStep[];
}

export default function SystemLogViewer() {
  const { global, styles: theme, t } = useTheme();
  const [logs, setLogs] = useState<SystemLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedLog, setSelectedLog] = useState<SystemLog | null>(null);
  
  const [confirmationModal, setConfirmationModal] = useState<{
      isOpen: boolean;
      title: string;
      message: string;
      onConfirm: () => void;
      type?: 'success' | 'error' | 'info' | 'warning';
      confirmText?: string;
  }>({
      isOpen: false,
      title: '',
      message: '',
      onConfirm: () => {},
      type: 'warning'
  });

  const fetchLogs = async () => {
    try {
      let headers: Record<string, string> = {};
      if (typeof window !== 'undefined') {
          try {
              const raw = window.localStorage.getItem('valora_headers');
              if (raw) headers = JSON.parse(raw);
          } catch {}
      }

      const res = await fetchApi('/api/debug/logs', { headers });
      const data = await unwrapResult<SystemLog[]>(res);
      setLogs(data || []);
    } catch (err: any) {
      if (logs.length === 0) setError(err.message || t('systemLogs.errors.fetchFailed'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
    const interval = setInterval(fetchLogs, 5000); // Poll every 5s
    return () => clearInterval(interval);
  }, []);

  const handleMarkFixed = async (id: string, isFixed: boolean) => {
    try {
      const res = await fetchApi(`/api/debug/logs/${id}/fix?isFixed=${isFixed}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' }
      });
      await unwrapResult(res);
      fetchLogs(); // Refresh
    } catch (err) {
      setError(t('systemLogs.errors.updateStatusFailed'));
    }
  };

  const handleClearAll = () => {
      setConfirmationModal({
          isOpen: true,
          title: t('systemLogs.confirmClearTitle'),
          message: t('systemLogs.confirmClearMessage'),
          type: 'warning',
          confirmText: t('systemLogs.confirmClearConfirm'),
          onConfirm: () => executeClearAll()
      });
  };

  const executeClearAll = async () => {
      setConfirmationModal(prev => ({ ...prev, isOpen: false }));
      try {
          // Use the bulk fix endpoint
          const res = await fetchApi('/api/debug/fix-logs', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' }
          });
          await unwrapResult(res);
          fetchLogs();
      } catch (err) {
          setError(t('systemLogs.errors.clearFailed'));
      }
  };

  if (loading && logs.length === 0) return <div>{t('systemLogs.loading')}</div>;

  return (
    <div className="p-4">
      <Modal
        isOpen={!!error}
        onClose={() => setError('')}
        title={t('systemLogs.errorTitle')}
        message={error}
        type="error"
      />
      <Modal
          isOpen={confirmationModal.isOpen}
          onClose={() => setConfirmationModal(prev => ({ ...prev, isOpen: false }))}
          title={confirmationModal.title}
          message={confirmationModal.message}
          type={confirmationModal.type}
          onConfirm={confirmationModal.onConfirm}
          confirmText={confirmationModal.confirmText}
      />
      
      {/* Detail Modal */}
      {selectedLog && (
        <div className="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
            <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
                <div className={`fixed inset-0 ${global.overlay} transition-opacity`} aria-hidden="true" onClick={() => setSelectedLog(null)}></div>
                <span className="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
                <div className={`inline-block align-bottom ${global.bg} rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full border ${global.border}`}>
                    <div className={`${global.bg} px-4 pt-5 pb-4 sm:p-6 sm:pb-4`}>
                        <div className="sm:flex sm:items-start">
                            <div className="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
                                <h3 className={`text-lg leading-6 font-medium ${global.text} mb-4`} id="modal-title">
                                    {t('systemLogs.detail.title')}
                                </h3>
                                
                                <div className="grid grid-cols-2 gap-4 mb-6">
                                    <div>
                                        <label className={`block text-sm font-medium ${global.textSecondary}`}>{t('systemLogs.detail.type')}</label>
                                        <div className={`mt-1 text-sm ${global.text}`}>{selectedLog.type}</div>
                                    </div>
                                    <div>
                                        <label className={`block text-sm font-medium ${global.textSecondary}`}>{t('systemLogs.detail.createdAt')}</label>
                                        <div className={`mt-1 text-sm ${global.text}`}>{new Date(selectedLog.createdAt).toLocaleString()}</div>
                                    </div>
                                    <div>
                                        <label className={`block text-sm font-medium ${global.textSecondary}`}>{t('systemLogs.detail.status')}</label>
                                        <div className="mt-1">
                                            {selectedLog.isFixed ? (
                                                <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400">{t('systemLogs.status.fixed')}</span>
                                            ) : (
                                                <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400">{t('systemLogs.status.open')}</span>
                                            )}
                                        </div>
                                    </div>
                                    {selectedLog.fixedBy && (
                                        <div>
                                            <label className={`block text-sm font-medium ${global.textSecondary}`}>{t('systemLogs.detail.fixedBy')}</label>
                                            <div className={`mt-1 text-sm ${global.text}`}>{selectedLog.fixedBy} ({new Date(selectedLog.fixedAt!).toLocaleString()})</div>
                                        </div>
                                    )}
                                </div>

                                <div className="mb-6">
                                    <label className={`block text-sm font-medium ${global.textSecondary} mb-2`}>{t('systemLogs.detail.errorData')}</label>
                                    <div className={`${global.bgSecondary} rounded p-3 overflow-auto max-h-40 text-xs font-mono border ${global.border} ${global.text}`}>
                                        <pre>{(() => {
                                            try { return JSON.stringify(JSON.parse(selectedLog.data), null, 2); }
                                            catch { return selectedLog.data; }
                                        })()}</pre>
                                    </div>
                                </div>

                                <div>
                                    <label className={`block text-sm font-medium ${global.textSecondary} mb-2`}>{t('systemLogs.detail.fixSteps')}</label>
                                    {selectedLog.fixSteps && selectedLog.fixSteps.length > 0 ? (
                                        <div className={`border ${global.border} rounded-md overflow-hidden`}>
                                            <table className={`min-w-full divide-y ${global.border.replace('border', 'divide')}`}>
                                                <thead className={global.bgSecondary}>
                                                    <tr>
                                                        <th className={`px-4 py-2 text-left text-xs font-medium ${global.textSecondary} uppercase tracking-wider`}>{t('systemLogs.detail.step')}</th>
                                                        <th className={`px-4 py-2 text-left text-xs font-medium ${global.textSecondary} uppercase tracking-wider`}>{t('systemLogs.detail.stepType')}</th>
                                                        <th className={`px-4 py-2 text-left text-xs font-medium ${global.textSecondary} uppercase tracking-wider`}>{t('systemLogs.detail.stepStatus')}</th>
                                                        <th className={`px-4 py-2 text-left text-xs font-medium ${global.textSecondary} uppercase tracking-wider`}>{t('systemLogs.detail.stepTime')}</th>
                                                    </tr>
                                                </thead>
                                                <tbody className={`${global.bg} divide-y ${global.border.replace('border', 'divide')}`}>
                                                    {selectedLog.fixSteps.map((step) => (
                                                        <tr key={step.id}>
                                                            <td className={`px-4 py-2 text-sm ${global.text}`}>{step.stepDescription}</td>
                                                            <td className={`px-4 py-2 text-sm ${global.textSecondary}`}>{step.stepType}</td>
                                                            <td className="px-4 py-2 text-sm">
                                                                <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                                                                    step.status === 'Success' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' : 
                                                                    step.status === 'Failed' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400' : `${global.bgSecondary} ${global.text}`
                                                                }`}>
                                                                    {step.status}
                                                                </span>
                                                            </td>
                                                            <td className={`px-4 py-2 text-sm ${global.textSecondary}`}>{new Date(step.createdAt).toLocaleTimeString()}</td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    ) : (
                                        <div className={`text-sm ${global.textSecondary} italic ${global.bgSecondary} p-3 rounded text-center`}>{t('systemLogs.detail.noFixSteps')}</div>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className={`${global.bgSecondary} px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse border-t ${global.border}`}>
                        <button
                            type="button"
                            className={`w-full inline-flex justify-center rounded-md border ${global.border} shadow-sm px-4 py-2 ${global.bg} text-base font-medium ${global.text} hover:${global.bgSecondary} focus:outline-none sm:ml-3 sm:w-auto sm:text-sm`}
                            onClick={() => setSelectedLog(null)}
                        >
                            {t('systemLogs.actions.close')}
                        </button>
                    </div>
                </div>
            </div>
        </div>
      )}

      <div className="flex justify-between items-center mb-4">
        <h2 className={`text-2xl font-bold ${global.text}`}>{t('systemLogs.title')}</h2>
        <button
            onClick={handleClearAll}
            className="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700 transition-colors shadow-sm text-sm font-medium dark:bg-red-700 dark:hover:bg-red-800"
        >
            {t('systemLogs.actions.markAllFixed')}
        </button>
      </div>
      <div className="overflow-x-auto">
        <table className={`min-w-full ${global.bg} border ${global.border}`}>
          <thead>
            <tr className={global.bgSecondary}>
              <th className={`py-2 px-4 border-b ${global.border} text-left ${global.text}`}>{t('systemLogs.table.type')}</th>
              <th className={`py-2 px-4 border-b ${global.border} text-left ${global.text}`}>{t('systemLogs.table.message')}</th>
              <th className={`py-2 px-4 border-b ${global.border} text-left ${global.text}`}>{t('systemLogs.table.createdAt')}</th>
              <th className={`py-2 px-4 border-b ${global.border} text-left ${global.text}`}>{t('systemLogs.table.status')}</th>
              <th className={`py-2 px-4 border-b ${global.border} text-left ${global.text}`}>{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {logs.map(log => {
              let details: any = {};
              let isJson = false;
              try { 
                details = JSON.parse(log.data); 
                isJson = true;
              } catch {
                details = { Message: log.data };
              }
              
              return (
                <tr 
                    key={log.id} 
                    className={`${log.isFixed ? "bg-green-50 dark:bg-green-900/10" : "bg-red-50 dark:bg-red-900/10"} cursor-pointer hover:bg-opacity-80 transition-colors`}
                    onClick={() => setSelectedLog(log)}
                >
                  <td className={`py-2 px-4 border-b ${global.border} font-semibold ${global.text}`}>{log.type}</td>
                  <td className={`py-2 px-4 border-b ${global.border} max-w-md`}>
                    <div className={`font-medium truncate ${global.text}`} title={details.Message || t('systemLogs.table.unknownError')}>{details.Message || t('systemLogs.table.unknownError')}</div>
                    {isJson && details.Stack && (
                      <div className={`text-xs ${global.textSecondary} truncate`} title={details.Stack}>{details.Stack}</div>
                    )}
                  </td>
                  <td className={`py-2 px-4 border-b ${global.border} text-sm ${global.text}`}>
                    {new Date(log.createdAt).toLocaleString()}
                  </td>
                  <td className={`py-2 px-4 border-b ${global.border}`}>
                    {log.isFixed ? (
                      <span className="text-green-600 dark:text-green-400 font-bold">{t('systemLogs.status.fixed')}</span>
                    ) : (
                      <span className="text-red-600 dark:text-red-400 font-bold">{t('systemLogs.status.open')}</span>
                    )}
                  </td>
                  <td className={`py-2 px-4 border-b ${global.border}`}>
                    {!log.isFixed && (
                      <button
                        onClick={(e) => { e.stopPropagation(); handleMarkFixed(log.id, true); }}
                        className={`${theme.primary} text-white px-3 py-1 rounded text-sm ${theme.hover}`}
                      >
                        {t('systemLogs.actions.markFixed')}
                      </button>
                    )}
                    {log.isFixed && (
                      <button
                       onClick={(e) => { e.stopPropagation(); handleMarkFixed(log.id, false); }}
                       className={`${global.bgSecondary} border ${global.border} ${global.text} px-3 py-1 rounded text-sm hover:${global.bg} transition-colors`}
                     >
                       {t('systemLogs.actions.reopen')}
                     </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
