import { useState, useEffect } from 'react';
import { useTheme } from '../context/ThemeContext';
import { Users, Building2, Globe, ArrowRight, Loader } from 'lucide-react';
import { fetchApi, unwrapResult } from '../utils/api';

interface UserOption {
    label: string;
    email: string;
    type: 'PlatformAdmin' | 'ChannelPartner' | 'TenantAdmin' | 'TenantUser';
    tenantId?: string;
}

const USERS: UserOption[] = [
    { label: 'Platform Admin', email: 'admin@platform.com', type: 'PlatformAdmin' },
    { label: 'Tenant Admin', email: 'admin@tenant.com', type: 'TenantAdmin' }
];

const ENVIRONMENTS_PLATFORM = ['DEV', 'TEST', 'PROD'];
const ENVIRONMENTS_TENANT = ['PROD'];

export default function Login({ onLogin }: { onLogin: () => void }) {
    const { global, styles } = useTheme();
    const [selectedUser, setSelectedUser] = useState<UserOption>(USERS[0]);
    const [selectedTenant, setSelectedTenant] = useState<string>('');
    const [selectedEnv, setSelectedEnv] = useState<string>('PROD');
    
    // Tenants State
    const [tenants, setTenants] = useState<{ id: string; name: string }[]>([]);
    const [isLoadingTenants, setIsLoadingTenants] = useState(false);

    useEffect(() => {
        const fetchTenants = async () => {
            setIsLoadingTenants(true);
            try {
                // We might need to handle the case where the user is not logged in yet, 
                // but usually getting a list of tenants for login selection implies a public or semi-public endpoint
                // or we use a hardcoded token for "bootstrapping" if auth is strict.
                // Assuming /api/tenants is accessible or we rely on the fact that we are in dev mode.
                // If it fails (401), we might need to fallback to hardcoded or seeded tenants.
                
                const response = await fetchApi('/api/tenants');
                if (response.ok) {
                    const data = await unwrapResult<any[]>(response);
                    const mapped = data.map(t => ({
                        id: t.tenantId, // backend sends camelCase usually
                        name: t.name || t.tenantId
                    }));
                    setTenants(mapped);
                    if (mapped.length > 0) {
                        setSelectedTenant(mapped[0].id);
                    }
                } else {
                    setTenants([]);
                }
            } catch (error) {
                console.error("Failed to fetch tenants", error);
                setTenants([]);
            } finally {
                setIsLoadingTenants(false);
            }
        };

        fetchTenants();
    }, []);

    // Update tenant selection when user changes
    useEffect(() => {
        if (selectedUser.tenantId) {
            setSelectedTenant(selectedUser.tenantId);
        }
    }, [selectedUser]);

    const handleLogin = () => {
        // Store context in localStorage
        const context = {
            userId: '1', // Dummy ID, backend middleware will look up by email if needed or we can pass email
            email: selectedUser.email,
            userType: selectedUser.type,
            tenantId: selectedUser.tenantId || selectedTenant, // Use selected tenant for Platform/Channel
            environment: selectedEnv
        };
        
        localStorage.setItem('valora_context', JSON.stringify(context));
        
        localStorage.setItem('valora_headers', JSON.stringify({
            'X-User-Email': context.email,
            'X-User-Type': context.userType,
            'X-Tenant-Id': context.tenantId,
            'X-Environment': context.environment.toLowerCase(),
            'X-Role': context.userType
        }));

        onLogin();
    };

    const needsTenantSelection = selectedUser.type === 'PlatformAdmin';
    const isPlatformAdmin = selectedUser.type === 'PlatformAdmin';
    const availableEnvs = isPlatformAdmin ? ENVIRONMENTS_PLATFORM : ENVIRONMENTS_TENANT;

    return (
        <div className={`min-h-screen flex items-center justify-center ${global.bgSecondary}`}>
            <div className={`w-full max-w-md p-8 rounded-2xl shadow-xl ${global.bg} ${global.border} border`}>
                <div className="mb-8 text-center">
                    <h1 className={`text-3xl font-bold mb-2 ${global.text}`}>Welcome Back</h1>
                    <p className={`${global.textSecondary}`}>Sign in to access your dashboard</p>
                </div>

                <div className="space-y-6">
                    {/* User Selection */}
                    <div className="space-y-2">
                        <label className={`flex items-center gap-2 text-sm font-medium ${global.textSecondary}`}>
                            <Users size={16} /> User Profile
                        </label>
                        <select
                            value={selectedUser.email}
                            onChange={(e) => setSelectedUser(USERS.find(u => u.email === e.target.value) || USERS[0])}
                            className={`w-full p-3 rounded-lg border ${global.border} ${global.bg} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none`}
                        >
                            {USERS.map(u => (
                                <option key={u.email} value={u.email}>{u.label} ({u.type})</option>
                            ))}
                        </select>
                    </div>

                    {/* Tenant Selection (Conditional) */}
                    {needsTenantSelection && (
                        <div className="space-y-2 animate-in fade-in slide-in-from-top-2">
                            <label className={`flex items-center gap-2 text-sm font-medium ${global.textSecondary}`}>
                                <Building2 size={16} /> Target Tenant
                            </label>
                            {isLoadingTenants ? (
                                <div className={`w-full p-3 rounded-lg border ${global.border} ${global.bg} flex items-center gap-2 text-sm ${global.textSecondary}`}>
                                    <Loader size={16} className="animate-spin" /> Loading tenants...
                                </div>
                            ) : (
                                <select
                                    value={selectedTenant}
                                    onChange={(e) => setSelectedTenant(e.target.value)}
                                    className={`w-full p-3 rounded-lg border ${global.border} ${global.bg} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none`}
                                >
                                    {tenants.map(t => (
                                        <option key={t.id} value={t.id}>{t.name} ({t.id})</option>
                                    ))}
                                </select>
                            )}
                        </div>
                    )}

                    {/* Environment Selection */}
                    <div className="space-y-2">
                        <label className={`flex items-center gap-2 text-sm font-medium ${global.textSecondary}`}>
                            <Globe size={16} /> Environment
                        </label>
                        <select
                            value={selectedEnv}
                            onChange={(e) => setSelectedEnv(e.target.value)}
                            className={`w-full p-3 rounded-lg border ${global.border} ${global.bg} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none`}
                        >
                            {availableEnvs.map(env => (
                                <option key={env} value={env}>{env}</option>
                            ))}
                        </select>
                    </div>

                    {/* Login Button */}
                    <button
                        onClick={handleLogin}
                        className={`w-full flex items-center justify-center gap-2 py-3 mt-4 rounded-lg font-semibold text-white transition-all transform active:scale-95 ${styles.primary} hover:opacity-90`}
                    >
                        <span>Enter System</span>
                        <ArrowRight size={18} />
                    </button>
                </div>

                <div className={`mt-6 pt-6 text-center border-t ${global.border}`}>
                    <p className={`text-xs ${global.textSecondary}`}>
                        Simulated Login (Dev Mode) - No password required
                    </p>
                </div>
            </div>
        </div>
    );
}
