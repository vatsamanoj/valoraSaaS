import { useEffect, useState } from 'react';
import { Brain, Database, Activity, Clock, RotateCcw } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';

interface AiUsageLog {
  id: number;
  timestamp: string;
  source: string;
  requestType: string;
  promptSummary: string;
  tokensUsed: number;
  tenantId: string;
}

interface AiStats {
  source: string;
  count: number;
}

export default function AiUsageViewer() {
  const { global, styles, isDark, t } = useTheme();
  const [logs, setLogs] = useState<AiUsageLog[]>([]);
  const [stats, setStats] = useState<AiStats[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchData = async () => {
    try {
      let headers: Record<string, string> = {};
      if (typeof window !== 'undefined') {
          try {
              const raw = window.localStorage.getItem('valora_headers');
              if (raw) headers = JSON.parse(raw);
          } catch {}
      }

      const [logsRes, statsRes] = await Promise.all([
          fetchApi(`/platform/ai-usage`, { headers }),
          fetchApi(`/platform/ai-usage/stats`, { headers })
      ]);

      const logsData = await unwrapResult<AiUsageLog[]>(logsRes);
      const statsData = await unwrapResult<AiStats[]>(statsRes);
      
      setLogs(logsData);
      setStats(statsData);
    } catch (err) {
      console.error("Failed to fetch AI usage data", err);
    } finally {
      setLoading(false);
    }
  };

  const handleReset = async () => {
    if (!confirm(t('aiDashboard.confirmReset'))) return;
    
    try {
        const res = await fetchApi('/platform/ai-usage', { method: 'DELETE' });
        await unwrapResult(res);
        fetchData();
    } catch (e) {
        console.error(e);
        alert(t('aiDashboard.errors.resetError'));
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5000); // Poll every 5s
    return () => clearInterval(interval);
  }, []);

  const getStat = (source: string) => stats.find(s => s.source === source)?.count || 0;
  const aiHits = getStat("AI Server");
  const kbHits = getStat("KnowledgeBase");
  const total = aiHits + kbHits;
  const efficiency = total > 0 ? Math.round((kbHits / total) * 100) : 0;

  if (loading && logs.length === 0) return <div>{t('aiDashboard.loading')}</div>;

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="mb-8">
        <div className="flex justify-between items-center mb-4">
            <h2 className={`text-2xl font-bold ${global.text} flex items-center gap-2`}>
                <Brain className={styles.text} /> {t('aiDashboard.title')}
            </h2>
            <button 
                onClick={handleReset}
                className="flex items-center gap-2 px-4 py-2 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 transition-colors text-sm font-medium dark:bg-red-900/30 dark:text-red-400 dark:hover:bg-red-900/50"
            >
                <RotateCcw size={16} />
                {t('aiDashboard.restart')}
            </button>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className={`${global.bg} p-6 rounded-xl shadow-sm border ${global.border} flex items-center gap-4`}>
                <div className="p-3 bg-purple-100 text-purple-600 rounded-lg dark:bg-purple-900/30 dark:text-purple-400">
                    <Activity size={24} />
                </div>
                <div>
                    <p className={`text-sm ${global.textSecondary} font-medium`}>{t('aiDashboard.cards.aiServerHits')}</p>
                    <p className={`text-2xl font-bold ${global.text}`}>{aiHits}</p>
                </div>
            </div>

            <div className={`${global.bg} p-6 rounded-xl shadow-sm border ${global.border} flex items-center gap-4`}>
                <div className="p-3 bg-green-100 text-green-600 rounded-lg dark:bg-green-900/30 dark:text-green-400">
                    <Database size={24} />
                </div>
                <div>
                    <p className={`text-sm ${global.textSecondary} font-medium`}>{t('aiDashboard.cards.kbHits')}</p>
                    <p className={`text-2xl font-bold ${global.text}`}>{kbHits}</p>
                </div>
            </div>

            <div className={`${global.bg} p-6 rounded-xl shadow-sm border ${global.border} flex items-center gap-4`}>
                <div className={`p-3 rounded-lg ${isDark ? `${styles.primary} bg-opacity-20` : styles.light} ${styles.text}`}>
                    <Brain size={24} />
                </div>
                <div>
                    <p className={`text-sm ${global.textSecondary} font-medium`}>{t('aiDashboard.cards.learningEfficiency')}</p>
                    <p className={`text-2xl font-bold ${global.text}`}>{efficiency}%</p>
                </div>
            </div>
        </div>
      </div>

      <div className={`${global.bg} rounded-xl shadow-sm border ${global.border} overflow-hidden`}>
        <div className={`p-4 border-b ${global.border} ${global.bgSecondary}`}>
            <h3 className={`font-bold ${global.text}`}>{t('aiDashboard.recentActivity')}</h3>
        </div>
        <div className="overflow-x-auto">
            <table className="min-w-full">
            <thead>
                <tr className={`${global.bgSecondary} text-xs font-semibold ${global.textSecondary} uppercase tracking-wider text-left`}>
                <th className={`py-3 px-6 border-b ${global.border}`}>{t('aiDashboard.columns.time')}</th>
                <th className={`py-3 px-6 border-b ${global.border}`}>{t('aiDashboard.columns.source')}</th>
                <th className={`py-3 px-6 border-b ${global.border}`}>{t('aiDashboard.columns.type')}</th>
                <th className={`py-3 px-6 border-b ${global.border}`}>{t('aiDashboard.columns.promptSummary')}</th>
                </tr>
            </thead>
            <tbody className={`divide-y ${global.border.replace('border', 'divide')}`}>
                {logs.map(log => (
                <tr key={log.id} className={`hover:${global.bgSecondary} transition-colors`}>
                    <td className={`py-3 px-6 text-sm ${global.textSecondary} whitespace-nowrap`}>
                        <div className="flex items-center gap-2">
                            <Clock size={14} className={global.textSecondary}/>
                            {new Date(log.timestamp).toLocaleString()}
                        </div>
                    </td>
                    <td className="py-3 px-6">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            log.source === 'KnowledgeBase' 
                            ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' 
                            : 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400'
                        }`}>
                            {log.source === 'KnowledgeBase' ? t('aiDashboard.badges.selfLearned') : t('aiDashboard.badges.aiServer')}
                        </span>
                    </td>
                    <td className={`py-3 px-6 text-sm ${global.text}`}>{log.requestType}</td>
                    <td className={`py-3 px-6 text-sm ${global.textSecondary} max-w-md truncate`} title={log.promptSummary}>
                        {log.promptSummary}
                    </td>
                </tr>
                ))}
            </tbody>
            </table>
        </div>
      </div>
    </div>
  );
}
