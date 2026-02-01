import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom';
import PlatformStudio from './components/PlatformStudio'
import GenericObjectForm from './components/GenericObjectForm'
import SystemLogViewer from './components/SystemLogViewer'
import { SystemEventsViewer } from './components/SystemEventsViewer'
import GenericObjectList from './components/GenericObjectList'
import AiUsageViewer from './components/AiUsageViewer'
import TranslationManager from './components/TranslationManager'
import PlatformPermissionsManager from './components/PlatformPermissionsManager'
import SearchableSelect from './components/SearchableSelect'
import { Users, Activity, Layout as LayoutIcon, Stethoscope, Box, Brain, Moon, Sun, Palette, ChevronUp, CloudMoon, Coffee, Save, Loader, LogOut, Shield, Globe, Coins, Star, Building, Briefcase, Plus } from 'lucide-react';
import TenantManager from './components/TenantManager';
import { useState, useEffect } from 'react';
import { useTheme, ThemeColor } from './context/ThemeContext';
import { SUPPORTED_LANGUAGES, SUPPORTED_CURRENCIES } from './i18n/translations';
import { fetchApi, unwrapResult } from './utils/api';
import Login from './components/Login';
import ContextSelector from './components/ContextSelector';
import GLAccountList from './components/Finance/GLAccountList';
import CreateJournalEntry from './components/Finance/CreateJournalEntry';
import JournalEntryList from './components/Finance/JournalEntryList';
import EmployeePayrollForm from './components/HumanCapital/EmployeePayrollForm';

// Sidebar Link Component
const SidebarLink = ({ to, icon: Icon, label }: { to: string, icon: any, label: string }) => {
    const location = useLocation();
    const isActive = location.pathname === to || (to !== '/' && location.pathname.startsWith(to + '/'));
    const { styles, global } = useTheme();

    return (
        <Link to={to} className={`flex items-center gap-3 px-4 py-3 text-sm font-medium rounded-lg transition-colors ${isActive
            ? `${styles.light} ${styles.text}`
            : `${global.textSecondary} hover:${global.bgSecondary} hover:${global.text}`
            }`}>
            <Icon size={20} className={isActive ? styles.text : global.textSecondary} />
            {label}
        </Link>
    );
};

// Scrollable Wrapper for standard pages
const Scrollable = ({ children }: { children: React.ReactNode }) => {
    const { global } = useTheme();
    return (
        <div className={`h-full overflow-y-auto ${global.bgSecondary} scroll-smooth`}>
            {children}
        </div>
    );
};

// Theme Selector Component
const ThemeSelector = () => {
    const { mode, setMode, color, setColor, themeColors, global, isDark, saveTheme, isSaving, language, setLanguage, currency, setCurrency, t } = useTheme();
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className={`mx-4 mb-4 p-3 rounded-xl border ${global.border} ${global.bg}`}>
            <div
                className={`flex items-center justify-between cursor-pointer ${global.text}`}
                onClick={() => setIsOpen(!isOpen)}
            >
                <div className="flex items-center gap-2 text-sm font-medium">
                    <Palette size={16} />
                    <span>{t('nav.appearance')}</span>
                </div>
                <ChevronUp size={14} className={`transition-transform ${isOpen ? 'rotate-180' : ''}`} />
            </div>

            {isOpen && (
                <div className={`mt-3 space-y-3 pt-3 border-t ${global.border} animate-in fade-in slide-in-from-bottom-2`}>
                    {/* Mode Toggle */}
                    <div className="grid grid-cols-2 gap-2">
                        <button
                            onClick={() => setMode('light')}
                            className={`flex items-center justify-center gap-2 py-2 text-xs font-medium rounded-md transition-all border ${mode === 'light'
                                ? `${global.bg} ${global.text} border-gray-200 dark:border-gray-700 shadow-sm`
                                : `border-transparent ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary}`
                                }`}
                        >
                            <Sun size={14} /> {t('app.theme.light')}
                        </button>
                        <button
                            onClick={() => setMode('dark')}
                            className={`flex items-center justify-center gap-2 py-2 text-xs font-medium rounded-md transition-all border ${mode === 'dark'
                                ? 'bg-gray-800 text-white border-gray-700 shadow-sm'
                                : `border-transparent ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary}`
                                }`}
                        >
                            <Moon size={14} /> {t('app.theme.dark')}
                        </button>
                        <button
                            onClick={() => setMode('midnight')}
                            className={`flex items-center justify-center gap-2 py-2 text-xs font-medium rounded-md transition-all border ${mode === 'midnight'
                                ? 'bg-slate-950 text-white border-slate-800 shadow-sm'
                                : `border-transparent ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary}`
                                }`}
                        >
                            <CloudMoon size={14} /> {t('app.theme.midnight')}
                        </button>
                        <button
                            onClick={() => setMode('sepia')}
                            className={`flex items-center justify-center gap-2 py-2 text-xs font-medium rounded-md transition-all border ${mode === 'sepia'
                                ? 'bg-[#f8f4e5] text-[#433422] border-[#dcd5c5] shadow-sm'
                                : `border-transparent ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary}`
                                }`}
                        >
                            <Coffee size={14} /> {t('app.theme.sepia')}
                        </button>
                    </div>

                    {/* Color Swatches */}
                    <div>
                        <div className={`text-xs font-semibold ${global.textSecondary} mb-2 uppercase tracking-wider`}>{t('app.accentColor')}</div>
                        <div className="grid grid-cols-6 gap-2">
                            {(Object.keys(themeColors) as ThemeColor[]).map(c => (
                                <button
                                    key={c}
                                    onClick={() => setColor(c)}
                                    className={`w-6 h-6 rounded-full ${themeColors[c].primary} hover:opacity-80 transition-opacity ring-2 ring-offset-2 ${isDark ? 'ring-offset-gray-900' : 'ring-offset-white'} ${color === c ? 'ring-gray-400' : 'ring-transparent'
                                        }`}
                                    title={c}
                                />
                            ))}
                        </div>
                    </div>

                    {/* Language Selector */}
                    <div>
                        <SearchableSelect
                            label={t('nav.language')}
                            value={language}
                            onChange={(val) => setLanguage(val as any)}
                            options={SUPPORTED_LANGUAGES.map(l => ({ value: l.code, label: l.label }))}
                            icon={<Globe size={12} />}
                            placeholder="Select Language..."
                        />
                    </div>

                    {/* Currency Selector */}
                    <div>
                        <SearchableSelect
                            label={t('nav.currency')}
                            value={currency}
                            onChange={(val) => setCurrency(val)}
                            options={SUPPORTED_CURRENCIES.map(c => ({
                                value: c.code,
                                label: `${c.label} (${c.symbol})`,
                                subLabel: c.code
                            }))}
                            icon={<Coins size={12} />}
                            placeholder="Select Currency..."
                        />
                    </div>

                    {/* Save Button */}
                    <button
                        onClick={saveTheme}
                        disabled={isSaving}
                        className={`w-full flex items-center justify-center gap-2 py-2 text-xs font-medium rounded-md transition-all border ${global.border} ${global.textSecondary} hover:${global.text} hover:${global.bgSecondary}`}
                    >
                        {isSaving ? <Loader size={14} className="animate-spin" /> : <Save size={14} />}
                        {t('common.save')}
                    </button>
                </div>
            )}
        </div>
    );
};

type ScreenModule = {
    objectCode: string;
    version: number;
    actions: string[];
};

const AppLayout = ({ onLogout }: { onLogout: () => void }) => {
    const [modules, setModules] = useState<ScreenModule[]>([]);
    const [permissions, setPermissions] = useState<string[]>([]);
    const { global, styles, t } = useTheme();
    const [user, setUser] = useState<any>(null);
    const [favoriteModules, setFavoriteModules] = useState<string[]>([]);
    const [moduleFilter, setModuleFilter] = useState<'all' | 'favorites'>('all');

    useEffect(() => {
        const contextStr = localStorage.getItem('valora_context');
        if (contextStr) {
            setUser(JSON.parse(contextStr));
        }
        fetchModules();
        fetchPermissions();
    }, []);

    useEffect(() => {
        if (!user) return;
        const key = `appFavorites_${user.email || user.userType || 'default'}`;
        try {
            const raw = localStorage.getItem(key);
            if (raw) {
                const parsed = JSON.parse(raw);
                if (Array.isArray(parsed)) {
                    setFavoriteModules(parsed);
                }
            }
        } catch {
        }
    }, [user]);

    useEffect(() => {
        if (!user) return;
        const key = `appFavorites_${user.email || user.userType || 'default'}`;
        try {
            localStorage.setItem(key, JSON.stringify(favoriteModules));
        } catch {
        }
    }, [favoriteModules, user]);

    const fetchModules = async () => {
        try {
            const response = await fetchApi('/api/screens');
            const payload = await unwrapResult<any>(response);
            const data: ScreenModule[] = Array.isArray(payload)
                ? payload.map((x: any) => ({
                    objectCode: x.objectCode,
                    version: x.version,
                    actions: Array.isArray(x.actions)
                        ? x.actions.map((a: any) => String(a).toLowerCase())
                        : []
                }))
                : [];

            setModules(data);

            try {
                if (typeof window !== 'undefined') {
                    window.localStorage.setItem('valora_modules', JSON.stringify(data));
                }
            } catch {
            }

            setFavoriteModules(prev => prev.filter(m => data.some(d => d.objectCode === m)));
        } catch {
        }
    };

    const fetchPermissions = async () => {
        try {
            const response = await fetchApi('/api/user/permissions');
            const data = await unwrapResult<any>(response);
            console.log("Permissions fetched:", data);
            setPermissions(data);
        } catch (err) {
            console.error("Failed to fetch permissions", err);
        }
    };

    const canAccessStudio = permissions.includes('STUDIO_ACCESS') || permissions.includes('*');
    const canAccessPlatformAdmin = permissions.includes('PLATFORM_ADMIN_ACCESS') || permissions.includes('*');

    return (
        <div className={`flex h-screen ${global.bgSecondary} overflow-hidden font-sans transition-colors duration-200`}>
            {/* Sidebar */}
            <aside className={`w-64 ${global.bg} ${global.border} border-r flex flex-col z-20 shadow-sm flex-shrink-0 transition-colors duration-200`}>
                <div className={`p-6 ${global.border} border-b`}>
                    <div className={`font-bold text-xl ${styles.text} flex items-center gap-2`}>
                        <span>Valora</span>
                        <span className={`text-xs font-normal ${global.textSecondary} ${global.border} border px-2 py-0.5 rounded`}>v0.1</span>
                    </div>
                </div>

                <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
                    <div className={`pb-4 mb-4 ${global.border} border-b`}>
                        <p className={`px-4 text-xs font-semibold ${global.textSecondary} uppercase tracking-wider mb-2`}>Core Modules</p>
                        <SidebarLink to="/finance/gl-accounts" icon={Coins} label="GL Accounts" />
                        <SidebarLink to="/finance/journal-entries" icon={Activity} label="Journal Entries" />
                        <SidebarLink to="/finance/journal-entry" icon={Plus} label="New Journal" />
                        <SidebarLink to="/hcm/payroll" icon={Briefcase} label="Secure Payroll" />
                        <SidebarLink to="/app/SalesOrder" icon={Box} label="Sales Orders" />
                        <SidebarLink to="/app/Material" icon={Box} label="Materials" />
                        <SidebarLink to="/app/CostCenter" icon={Building} label="Cost Centers" />
                    </div>

                    <div className={`pb-4 mb-4 ${global.border} border-b`}>
                        <p className={`px-4 text-xs font-semibold ${global.textSecondary} uppercase tracking-wider mb-2`}>{t('nav.main')}</p>
                        {modules.length > 0 && (
                            <div className="px-0 pt-1">
                                <div className="px-4 pb-2 flex items-center gap-2 text-[11px] uppercase tracking-wide">
                                    <span className={global.textSecondary}>{t('nav.view') || 'View'}:</span>
                                    <button
                                        onClick={() => setModuleFilter('all')}
                                        className={`px-2 py-0.5 rounded-full text-[11px] ${moduleFilter === 'all'
                                            ? `${styles.light} ${styles.text}`
                                            : `${global.bgSecondary} ${global.textSecondary}`
                                            }`}
                                    >
                                        {t('nav.all') || 'All'}
                                    </button>
                                    <button
                                        onClick={() => setModuleFilter('favorites')}
                                        className={`px-2 py-0.5 rounded-full text-[11px] flex items-center gap-1 ${moduleFilter === 'favorites'
                                            ? `${styles.light} ${styles.text}`
                                            : `${global.bgSecondary} ${global.textSecondary}`
                                            }`}
                                    >
                                        <Star size={10} /> {t('nav.favourites') || 'Favourites'}
                                    </button>
                                </div>
                                <div className="space-y-1">
                                    {(() => {
                                        const sorted = [...modules].sort((a, b) => {
                                            const af = favoriteModules.includes(a.objectCode);
                                            const bf = favoriteModules.includes(b.objectCode);
                                            if (af === bf) return a.objectCode.localeCompare(b.objectCode);
                                            return af ? -1 : 1;
                                        });
                                        const filtered =
                                            moduleFilter === 'favorites'
                                                ? sorted.filter(m => favoriteModules.includes(m.objectCode))
                                                : sorted;
                                        if (filtered.length === 0 && moduleFilter === 'favorites') {
                                            return (
                                                <div className={`px-4 py-1 text-[11px] ${global.textSecondary}`}>
                                                    {t('nav.noFavourites') || 'No favourite modules selected.'}
                                                </div>
                                            );
                                        }
                                        return filtered.map(module => {
                                            const moduleCode = module.objectCode;
                                            const to = `/app/${moduleCode}`;
                                            const Icon =
                                                moduleCode === 'Doctor'
                                                    ? Stethoscope
                                                    : moduleCode === 'Patient'
                                                        ? Users
                                                        : Box;
                                            return (
                                                <SidebarLink
                                                    key={moduleCode}
                                                    to={to}
                                                    icon={(props: any) => (
                                                        <div className="flex items-center gap-2">
                                                            <button
                                                                onClick={e => {
                                                                    e.preventDefault();
                                                                    e.stopPropagation();
                                                                    setFavoriteModules(prev =>
                                                                        prev.includes(moduleCode)
                                                                            ? prev.filter(m => m !== moduleCode)
                                                                            : [...prev, moduleCode]
                                                                    );
                                                                }}
                                                                className="p-0.5 rounded text-yellow-500 hover:bg-yellow-100"
                                                                title={
                                                                    favoriteModules.includes(moduleCode)
                                                                        ? t('nav.unpinFavourite') || 'Unpin from favourites'
                                                                        : t('nav.pinFavourite') || 'Pin to favourites'
                                                                }
                                                            >
                                                                <Star
                                                                    size={14}
                                                                    fill={favoriteModules.includes(moduleCode) ? 'currentColor' : 'none'}
                                                                    className="shrink-0"
                                                                />
                                                            </button>
                                                            <Icon {...props} />
                                                        </div>
                                                    )}
                                                    label={
                                                        moduleCode === 'Doctor'
                                                            ? t('nav.doctors')
                                                            : moduleCode === 'Patient'
                                                                ? t('nav.patients')
                                                                : moduleCode
                                                    }
                                                />
                                            );
                                        });
                                    })()}
                                </div>
                            </div>
                        )}
                    </div>

                    {canAccessStudio && (
                        <div className="animate-in fade-in slide-in-from-left-2 duration-300">
                            <p className={`px-4 text-xs font-semibold ${global.textSecondary} uppercase tracking-wider mb-2`}>{t('nav.platform')}</p>
                            <SidebarLink to="/studio" icon={LayoutIcon} label={t('nav.studio')} />
                            <SidebarLink to="/studio/translations" icon={Globe} label={t('nav.translations')} />
                            <SidebarLink to="/studio/logs" icon={Activity} label={t('nav.systemLogs')} />
                            <SidebarLink to="/studio/events" icon={Activity} label="Kafka Events (Live)" />
                            <SidebarLink to="/studio/ai-usage" icon={Brain} label={t('nav.aiDashboard')} />
                            {canAccessPlatformAdmin && (
                                <>
                                    <SidebarLink to="/admin/permissions" icon={Shield} label={t('nav.permissions')} />
                                    <SidebarLink to="/admin/tenants" icon={Building} label="Tenants" />
                                </>
                            )}        </div>
                    )}
                </nav>

                <ThemeSelector />

                <div className={`p-4 ${global.border} border-t`}>
                    <div className="flex items-center gap-3 px-4 py-2">
                        <div className={`w-8 h-8 rounded-full ${styles.light} flex items-center justify-center ${styles.text} font-bold text-xs`}>
                            {user?.userType?.substring(0, 2).toUpperCase() || 'GU'}
                        </div>
                        <div className="flex-1 min-w-0">
                            <p className={`font-medium ${global.text} truncate`}>{user?.userType || 'Guest'}</p>
                            <p className={`text-xs ${global.textSecondary} truncate`}>{user?.email}</p>
                        </div>
                        <button onClick={onLogout} className={`p-1 hover:${global.bgSecondary} rounded-full transition-colors`} title="Logout">
                            <LogOut size={16} className={global.textSecondary} />
                        </button>
                    </div>
                </div>
            </aside>

            {/* Main Content */}
            <main className="flex-1 overflow-hidden relative flex flex-col">
                <Routes>
                    {/* Standard Pages wrapped in Scrollable */}
                    <Route path="/" element={<div className={`p-10 text-center ${global.textSecondary}`}>Select a module to begin.</div>} />
                    <Route path="/studio/logs" element={<Scrollable><SystemLogViewer /></Scrollable>} />
                    <Route path="/studio/events" element={<Scrollable><SystemEventsViewer /></Scrollable>} />
                    <Route path="/studio/ai-usage" element={<Scrollable><AiUsageViewer /></Scrollable>} />
                    <Route path="/app/:objectCode" element={<GenericObjectList />} />
                    <Route path="/app/:objectCode/new" element={<Scrollable><GenericObjectForm /></Scrollable>} />
                    <Route path="/app/:objectCode/:id" element={<Scrollable><GenericObjectForm /></Scrollable>} />
                    <Route path="/app/:objectCode/:id/edit" element={<Scrollable><GenericObjectForm /></Scrollable>} />

                    {/* Platform Studio handles its own scrolling/layout */}
                    <Route path="/studio" element={<PlatformStudio />} />
                    <Route path="/studio/translations" element={<TranslationManager />} />
                    <Route path="/admin/permissions" element={<Scrollable><PlatformPermissionsManager /></Scrollable>} />
                    <Route path="/admin/tenants" element={<Scrollable><TenantManager /></Scrollable>} />
                    
                    {/* Finance Routes */}
                    <Route path="/finance/gl-accounts" element={<Scrollable><GLAccountList /></Scrollable>} />
                    <Route path="/finance/journal-entries" element={<Scrollable><JournalEntryList /></Scrollable>} />
                    <Route path="/finance/journal-entry" element={<Scrollable><CreateJournalEntry /></Scrollable>} />
                    <Route path="/finance/journal-entry/:id" element={<Scrollable><CreateJournalEntry /></Scrollable>} />

                    {/* HCM Routes */}
                    <Route path="/hcm/payroll" element={<Scrollable><EmployeePayrollForm /></Scrollable>} />
                </Routes>
            </main>
        </div>
    );
};

function App() {
    const { setLanguage } = useTheme();
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const context = localStorage.getItem('valora_context');
        if (context) setIsAuthenticated(true);
        setIsLoading(false);
    }, []);

    useEffect(() => {
        if (!isAuthenticated) {
            setLanguage('en');
        }
    }, [isAuthenticated, setLanguage]);

    if (isLoading) return null;

    if (!isAuthenticated) {
        return <Login onLogin={() => setIsAuthenticated(true)} />;
    }

    return (
        <BrowserRouter>
            <AppLayout onLogout={() => {
                localStorage.removeItem('valora_context');
                localStorage.removeItem('valora_headers');
                setIsAuthenticated(false);
            }} />
        </BrowserRouter>
    )
}

export default App
