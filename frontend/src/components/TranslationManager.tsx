import { useState, useEffect } from 'react';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';
import { Save, RefreshCw, Globe, Sparkles } from 'lucide-react';

const TranslationManager = () => {
    const { global, styles, t } = useTheme();
    const [selectedLang, setSelectedLang] = useState('en');
    const [jsonContent, setJsonContent] = useState('');
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [translating, setTranslating] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [fromLang, setFromLang] = useState('en');
    const [useEditorAsSource, setUseEditorAsSource] = useState(false);

    const getHeaders = () => {
        let headers: Record<string, string> = {};
        if (typeof window !== 'undefined') {
            try {
                const raw = window.localStorage.getItem('valora_headers');
                if (raw) headers = JSON.parse(raw);
            } catch {}
        }
        return headers;
    };

    // Load available languages (mock for now, or fetch from DB if we had a list endpoint)
    // Since we store them as entities, we could fetch keys. 
    // For now, we'll just allow typing a code or selecting common ones.
    const knownLanguages = ['en', 'es', 'fr', 'de', 'zh', 'jp'];

    useEffect(() => {
        loadTranslation(selectedLang);
    }, [selectedLang]);

    const loadTranslation = async (lang: string) => {
        setLoading(true);
        setError('');
        try {
            const res = await fetchApi(`/api/platform/translations/${lang}`, { headers: getHeaders() });
            const data = await unwrapResult<any>(res);
            setJsonContent(JSON.stringify(data, null, 2));
        } catch (err) {
            console.error(err);
            setJsonContent('{}'); // Reset on error, or maybe show error?
            // setError(t('translationsManager.errors.loadFailed')); 
            // Original code swallowed error in catch but set jsonContent to empty on !res.ok
            // unwrapResult throws on error. 
        } finally {
            setLoading(false);
        }
    };

    const handleSave = async () => {
        setSaving(true);
        setSuccess('');
        setError('');
        try {
            let parsed;
            try {
                parsed = JSON.parse(jsonContent);
            } catch (e) {
                throw new Error(t('translationsManager.errors.invalidJsonFormat'));
            }

            const res = await fetchApi(`/api/platform/translations/${selectedLang}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(parsed)
            });

            if (res.ok) {
                setSuccess(t('translationsManager.messages.saveSuccess'));
                setTimeout(() => setSuccess(''), 3000);
            } else {
                throw new Error(t('translationsManager.errors.saveFailed'));
            }
        } catch (err: any) {
            setError(err.message || t('translationsManager.errors.saveFailed'));
        } finally {
            setSaving(false);
        }
    };

    const handleAutoTranslate = async () => {
        setTranslating(true);
        setSuccess('');
        setError('');
        try {
            if (fromLang === selectedLang) {
                throw new Error(t('translationsManager.errors.sourceTargetSame'));
            }
            let body: any = null;
            if (useEditorAsSource) {
                try {
                    body = JSON.parse(jsonContent || '{}');
                } catch {
                    throw new Error(t('translationsManager.errors.invalidEditorJsonSource'));
                }
            }
            const res = await fetchApi(`/api/platform/translations/auto/${selectedLang}?from=${encodeURIComponent(fromLang)}`, {
                method: 'POST',
                headers: body ? { 'Content-Type': 'application/json' } : undefined,
                body: body ? JSON.stringify(body) : undefined
            });
            if (!res.ok) {
                const text = await res.text();
                throw new Error(text || t('translationsManager.errors.autoTranslateFailed'));
            }
            const data = await res.json();
            setJsonContent(JSON.stringify(data, null, 2));
            setSuccess(t('translationsManager.messages.autoTranslateSuccess'));
            setTimeout(() => setSuccess(''), 4000);
        } catch (e: any) {
            setError(e.message || t('translationsManager.errors.autoTranslateFailed'));
        } finally {
            setTranslating(false);
        }
    };

    return (
        <div className={`h-full flex flex-col ${global.bg}`}>
            {/* Header */}
            <div className={`p-4 border-b ${global.border} flex justify-between items-center`}>
                <div className="flex items-center gap-2">
                    <Globe size={20} className={styles.text} />
                    <h2 className={`font-bold text-lg ${global.text}`}>{t('translationsManager.title')}</h2>
                </div>
                <div className="flex items-center gap-3">
                    <div className="flex items-center gap-2">
                        <label className={`text-xs ${global.textSecondary}`}>{t('translationsManager.sourceLabel')}</label>
                        <select
                            value={fromLang}
                            onChange={(e) => setFromLang(e.target.value)}
                            className={`text-xs px-2 py-1 border ${global.inputBorder} rounded ${global.inputBg} ${global.text}`}
                        >
                            {knownLanguages.map(l => <option key={l} value={l}>{l.toUpperCase()}</option>)}
                        </select>
                        <span className={`text-xs ${global.textSecondary}`}>→</span>
                        <label className={`text-xs ${global.textSecondary}`}>{t('translationsManager.targetLabel')}</label>
                        <select
                            value={selectedLang}
                            onChange={(e) => setSelectedLang(e.target.value)}
                            className={`text-xs px-2 py-1 border ${global.inputBorder} rounded ${global.inputBg} ${global.text}`}
                        >
                            {knownLanguages.map(l => <option key={l} value={l}>{l.toUpperCase()}</option>)}
                        </select>
                        <label className={`text-xs ml-3 inline-flex items-center gap-1 ${global.textSecondary}`}>
                            <input
                                type="checkbox"
                                className="rounded"
                                checked={useEditorAsSource}
                                onChange={(e) => setUseEditorAsSource(e.target.checked)}
                            />
                            {t('translationsManager.useEditorAsSource')}
                        </label>
                        <button
                            onClick={async () => {
                                setTranslating(true);
                                setError('');
                                setSuccess('');
                                try {
                                    if (!useEditorAsSource) {
                                        throw new Error(t('translationsManager.errors.schemaTranslateEnableSource'));
                                    }
                                    let schemaBody: any;
                                    try {
                                        schemaBody = JSON.parse(jsonContent || '{}');
                                    } catch {
                                        throw new Error(t('translationsManager.errors.schemaTranslateInvalidJson'));
                                    }
                                    const res = await fetchApi(`/api/platform/translations/schema/${selectedLang}?from=${encodeURIComponent(fromLang)}`, {
                                        method: 'POST',
                                        headers: { ...getHeaders(), 'Content-Type': 'application/json' },
                                        body: JSON.stringify(schemaBody)
                                    });
                                    await unwrapResult(res);
                                    // Reload full translation to show merged content
                                    await loadTranslation(selectedLang);
                                    setSuccess(t('translationsManager.messages.schemaTranslateSuccess'));
                                } catch (e: any) {
                                    setError(e.message || t('translationsManager.errors.schemaTranslateFailed'));
                                } finally {
                                    setTranslating(false);
                                }
                            }}
                            disabled={translating}
                            className={`flex items-center gap-2 px-3 py-2 rounded-lg border ${global.border} ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary} disabled:opacity-50`}
                            title={t('translationsManager.translateSchemaTooltip')}
                        >
                            <Sparkles size={16} />
                            {translating ? t('translationsManager.translating') : t('translationsManager.translateSchemaButton')}
                        </button>
                        <button
                            onClick={handleAutoTranslate}
                            disabled={translating}
                            className={`flex items-center gap-2 px-3 py-2 rounded-lg text-white ${styles.primary} ${styles.hover} disabled:opacity-50 transition-colors`}
                            title={t('translationsManager.autoTranslateTooltip')}
                        >
                            <Sparkles size={16} />
                            {translating ? t('translationsManager.translating') : t('translationsManager.autoTranslateButton')}
                        </button>
                    </div>
                    <button 
                        onClick={() => loadTranslation(selectedLang)}
                        className={`p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-800 ${global.textSecondary}`}
                        title={t('translationsManager.refreshTooltip')}
                    >
                        <RefreshCw size={18} />
                    </button>
                    <button 
                        onClick={handleSave}
                        disabled={saving}
                        className={`flex items-center gap-2 px-4 py-2 rounded-lg text-white ${styles.primary} ${styles.hover} disabled:opacity-50 transition-colors`}
                    >
                        <Save size={18} />
                        {saving ? t('translationsManager.saving') : t('translationsManager.saveChangesButton')}
                    </button>
                </div>
            </div>

            {/* Content */}
            <div className="flex-1 flex overflow-hidden">
                {/* Sidebar */}
                <div className={`w-48 border-r ${global.border} flex flex-col ${global.bgSecondary}`}>
                    <div className="p-2 font-medium text-xs text-gray-500 uppercase tracking-wider">{t('translationsManager.sidebarLanguages')}</div>
                    <div className="flex-1 overflow-y-auto">
                        {knownLanguages.map(lang => (
                            <button
                                key={lang}
                                onClick={() => setSelectedLang(lang)}
                                className={`w-full text-left px-4 py-2 text-sm flex items-center justify-between ${
                                    selectedLang === lang 
                                        ? `${styles.light} ${styles.text} font-medium border-r-2 ${styles.border}` 
                                        : `${global.textSecondary} hover:${global.text}`
                                }`}
                            >
                                <span className="uppercase">{lang}</span>
                                {selectedLang === lang && <div className={`w-1.5 h-1.5 rounded-full ${styles.primary}`} />}
                            </button>
                        ))}
                    </div>
                </div>

                {/* Editor */}
                <div className="flex-1 flex flex-col p-4 relative">
                    {loading && (
                        <div className="absolute inset-0 bg-white/50 dark:bg-black/50 flex items-center justify-center z-10">
                            <div className={`animate-spin rounded-full h-8 w-8 border-b-2 ${styles.border}`}></div>
                        </div>
                    )}
                    
                    {error && (
                        <div className="mb-4 p-3 bg-red-50 text-red-600 border border-red-200 rounded-lg text-sm">
                            {error}
                        </div>
                    )}
                    
                    {success && (
                        <div className="mb-4 p-3 bg-green-50 text-green-600 border border-green-200 rounded-lg text-sm">
                            {success}
                        </div>
                    )}

                    <div className="flex-1 flex flex-col">
                        <label className={`block text-sm font-medium ${global.text} mb-2`}>
                            {t('translationsManager.editorLabel', { lang: selectedLang.toUpperCase() })}
                        </label>
                        <textarea
                            value={jsonContent}
                            onChange={(e) => setJsonContent(e.target.value)}
                            className={`flex-1 w-full p-4 font-mono text-sm border ${global.inputBorder} rounded-lg outline-none focus:ring-2 ${styles.ring} ${global.inputBg} ${global.text}`}
                            spellCheck={false}
                        />
                        <p className={`mt-2 text-xs ${global.textSecondary}`}>
                            {t('translationsManager.editorHint')}
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default TranslationManager;
