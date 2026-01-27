import { useState, useEffect } from 'react';
import { fetchApi, unwrapResult } from '../utils/api';
import { useTheme } from '../context/ThemeContext';
import { Shield, Key, Trash2, CheckCircle, AlertCircle, Building, Users } from 'lucide-react';

export default function PlatformPermissionsManager() {
    const { global, styles, t } = useTheme();
    
    const [tenants, setTenants] = useState<any[]>([]);
    const [selectedTenant, setSelectedTenant] = useState<string>('');
    
    const [activeTab, setActiveTab] = useState<'roles' | 'features'>('roles');
    
    // Roles & Permissions State
    const [roles, setRoles] = useState<any[]>([]);
    const [selectedRole, setSelectedRole] = useState<string>('');
    const [permissions, setPermissions] = useState<any[]>([]);
    const [newPermission, setNewPermission] = useState('');
    
    // Features State
    const [features, setFeatures] = useState<any[]>([]);
    const [newFeature, setNewFeature] = useState('');

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    useEffect(() => {
        loadTenants();
    }, []);

    useEffect(() => {
        if (selectedTenant) {
            if (activeTab === 'roles') loadRoles(selectedTenant);
            if (activeTab === 'features') loadFeatures(selectedTenant);
        }
    }, [selectedTenant, activeTab]);

    useEffect(() => {
        if (selectedTenant && selectedRole) {
            loadPermissions(selectedTenant, selectedRole);
        }
    }, [selectedRole]);

    const loadTenants = async () => {
        try {
            const res = await fetchApi('/api/tenants');
            const data = await unwrapResult<any[]>(res);
            setTenants(data || []);
            if (data && data.length > 0) setSelectedTenant(data[0].tenantId);
        } catch (e) {
            console.error(e);
        }
    };

    const loadRoles = async (tenantId: string) => {
        try {
            const res = await fetchApi(`/tenant-roles/${tenantId}`);
            const data = await unwrapResult<any>(res);
            setRoles(data.roles || []);
            if (data.roles?.length > 0) setSelectedRole(data.roles[0].roleCode);
        } catch (e) {
            console.error(e);
        }
    };

    const loadPermissions = async (tenantId: string, roleCode: string) => {
        try {
            const res = await fetchApi(`/tenant-roles/${tenantId}`);
            const data = await unwrapResult<any>(res);
            // Find role ID first
            const role = data.roles.find((r:any) => r.roleCode === roleCode);
            if (role) {
                const rolePerms = data.permissions.filter((p: any) => p.roleId === role.id);
                setPermissions(rolePerms);
            } else {
                setPermissions([]);
            }
        } catch (e) {
            console.error(e);
        }
    };

    const loadFeatures = async (tenantId: string) => {
        try {
            // Explicitly pass tenant context headers if needed, though URL param usually sufficient for admin
            let headers: Record<string, string> = {};
            if (typeof window !== 'undefined') {
                try {
                    const raw = window.localStorage.getItem('valora_headers');
                    if (raw) headers = JSON.parse(raw);
                } catch {}
            }
            // Override with selected tenant context for admin actions? 
            // Actually, for admin managing *other* tenants, we usually keep our own admin context 
            // and target the tenant via URL. But let's ensure we at least send current user context.
            
            const res = await fetchApi(`/admin/tenant/${tenantId}/features`, { headers });
            const data = await unwrapResult<any[]>(res);
            setFeatures(data || []);
        } catch (e) {
            console.error(e);
        }
    };

    const handleGrantPermission = async () => {
        if (!newPermission) return;
        setLoading(true);
        setError('');
        try {
            // Use camelCase for property names to ensure backend binding works
            const res = await fetchApi(`/admin/tenant/${selectedTenant}/roles/${selectedRole}/permissions`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ permissionCode: newPermission })
            });
            
            await unwrapResult(res);
            setSuccess(t('permissions.toasts.grantPermissionSuccess', { code: newPermission }));
            setNewPermission('');
            loadPermissions(selectedTenant, selectedRole);
            setTimeout(() => setSuccess(''), 500);
        } catch (e: any) {
            setError(e.message || t('permissions.errors.grantPermissionError'));
        } finally {
            setLoading(false);
        }
    };

    const handleRevokePermission = async (permCode: string) => {
        if (!confirm(t('permissions.confirmations.revokePermission', { code: permCode }))) return;
        setLoading(true);
        setError('');
        try {
            const res = await fetchApi(`/admin/tenant/${selectedTenant}/roles/${selectedRole}/permissions/${permCode}`, {
                method: 'DELETE'
            });
            
            await unwrapResult(res);
            setSuccess(t('permissions.toasts.revokePermissionSuccess', { code: permCode }));
            loadPermissions(selectedTenant, selectedRole);
            setTimeout(() => setSuccess(''), 500);
        } catch (e: any) {
             setError(e.message || t('permissions.errors.revokePermissionError'));
        } finally {
            setLoading(false);
        }
    };

    const handleGrantFeature = async () => {
        if (!newFeature) return;
        setLoading(true);
        setError('');
        try {
            // Use camelCase
            const res = await fetchApi(`/admin/tenant/${selectedTenant}/features`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ featureCode: newFeature })
            });
            
            await unwrapResult(res);
            setSuccess(t('permissions.toasts.grantFeatureSuccess', { code: newFeature }));
            setNewFeature('');
            loadFeatures(selectedTenant);
            setTimeout(() => setSuccess(''), 500);
        } catch (e: any) {
             setError(e.message || t('permissions.errors.grantFeatureError'));
        } finally {
            setLoading(false);
        }
    };

    const handleRevokeFeature = async (featCode: string) => {
         if (!confirm(t('permissions.confirmations.revokeFeature'))) return;
        setLoading(true);
        setError('');
        try {
            const res = await fetchApi(`/admin/tenant/${selectedTenant}/features/${featCode}`, {
                method: 'DELETE'
            });
            
            await unwrapResult(res);
            setSuccess(t('permissions.toasts.revokeFeatureSuccess', { code: featCode }));
            loadFeatures(selectedTenant);
            setTimeout(() => setSuccess(''), 500);
        } catch (e: any) {
             setError(e.message || t('permissions.errors.revokeFeatureError'));
        } finally {
            setLoading(false);
        }
    };


    return (
        <div className={`p-8 max-w-6xl mx-auto min-h-screen ${global.text}`}>
            <div className="flex items-center gap-4 mb-8">
                <div className={`p-3 rounded-lg ${styles.light}`}>
                    <Shield size={32} className={styles.text} />
                </div>
                <div>
                    <h1 className="text-2xl font-bold">{t('permissions.title')}</h1>
                    <p className={global.textSecondary}>{t('permissions.subtitle')}</p>
                </div>
            </div>

            {/* Tenant Selector */}
            <div className={`p-6 rounded-xl border ${global.border} ${global.bg} mb-8`}>
                <label className={`block text-sm font-medium ${global.textSecondary} mb-2`}>{t('permissions.selectTenantLabel')}</label>
                <select 
                    value={selectedTenant}
                    onChange={(e) => setSelectedTenant(e.target.value)}
                    className={`w-full p-3 rounded-lg border ${global.border} ${global.bg} ${global.text}`}
                >
                    {tenants.map(t => (
                        <option key={t.tenantId} value={t.tenantId}>{t.tenantName} ({t.tenantId})</option>
                    ))}
                </select>
            </div>

            {/* Tabs */}
            <div className="flex gap-4 mb-6 border-b border-gray-200 dark:border-gray-800">
                <button
                    onClick={() => setActiveTab('roles')}
                    className={`pb-3 px-4 text-sm font-medium transition-colors border-b-2 ${
                        activeTab === 'roles' 
                        ? `border-blue-500 text-blue-500` 
                        : `border-transparent ${global.textSecondary} hover:${global.text}`
                    }`}
                >
                    {t('permissions.tabs.roles')}
                </button>
                <button
                    onClick={() => setActiveTab('features')}
                    className={`pb-3 px-4 text-sm font-medium transition-colors border-b-2 ${
                        activeTab === 'features' 
                        ? `border-blue-500 text-blue-500` 
                        : `border-transparent ${global.textSecondary} hover:${global.text}`
                    }`}
                >
                    {t('permissions.tabs.features')}
                </button>
            </div>

            {/* Content */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                {activeTab === 'roles' && (
                    <>
                        {/* Role List */}
                        <div className={`col-span-1 p-4 rounded-xl border ${global.border} ${global.bg}`}>
                            <h3 className="font-semibold mb-4 flex items-center gap-2">
                                <Users size={18} /> {t('permissions.roles.header')}
                            </h3>
                            <div className="space-y-2">
                                {roles.map(role => (
                                    <button
                                        key={role.roleCode}
                                        onClick={() => setSelectedRole(role.roleCode)}
                                        className={`w-full text-left px-4 py-3 rounded-lg text-sm transition-colors ${
                                            selectedRole === role.roleCode
                                            ? `${styles.light} ${styles.text} font-medium`
                                            : `${global.textSecondary} hover:${global.bgSecondary}`
                                        }`}
                                    >
                                        {role.roleName}
                                        <div className="text-xs opacity-60">{role.roleCode}</div>
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Permissions List */}
                        <div className={`col-span-2 p-6 rounded-xl border ${global.border} ${global.bg}`}>
                            <h3 className="font-semibold mb-4 flex items-center gap-2">
                                <Key size={18} /> {t('permissions.roles.permissionsFor', { role: roles.find(r => r.roleCode === selectedRole)?.roleName || '' })}
                            </h3>

                            {/* Add Permission */}
                            <div className="flex gap-2 mb-6">
                                <input 
                                    type="text" 
                                    value={newPermission}
                                    onChange={(e) => setNewPermission(e.target.value)}
                                    placeholder={t('permissions.roles.inputPlaceholder')}
                                    className={`flex-1 p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-sm`}
                                />
                                <button 
                                    onClick={handleGrantPermission}
                                    disabled={loading || !newPermission}
                                    className={`px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50`}
                                >
                                    {t('permissions.roles.grantButton')}
                                </button>
                            </div>

                            <div className="space-y-2">
                                {permissions.length === 0 && <p className={`text-sm ${global.textSecondary}`}>{t('permissions.roles.none')}</p>}
                                {permissions.map(perm => (
                                    <div key={perm.id || perm.permissionCode} className={`flex items-center justify-between p-3 rounded-lg border ${global.border} ${global.bgSecondary}`}>
                                        <span className="text-sm font-mono">{perm.permissionCode}</span>
                                        <button 
                                            onClick={() => handleRevokePermission(perm.permissionCode)}
                                            className="text-red-500 hover:text-red-600 p-1"
                                        >
                                            <Trash2 size={16} />
                                        </button>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </>
                )}

                {activeTab === 'features' && (
                    <div className={`col-span-3 p-6 rounded-xl border ${global.border} ${global.bg}`}>
                         <h3 className="font-semibold mb-4 flex items-center gap-2">
                            <Building size={18} /> {t('permissions.features.header')}
                        </h3>

                         {/* Add Feature */}
                         <div className="flex gap-2 mb-6 max-w-md">
                            <input 
                                type="text" 
                                value={newFeature}
                                onChange={(e) => setNewFeature(e.target.value)}
                                placeholder={t('permissions.features.inputPlaceholder')}
                                className={`flex-1 p-2 rounded-lg border ${global.border} ${global.bg} ${global.text} text-sm`}
                            />
                            <button 
                                onClick={handleGrantFeature}
                                disabled={loading || !newFeature}
                                className={`px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50`}
                            >
                                {t('permissions.features.grantButton')}
                            </button>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                             {features.length === 0 && <p className={`text-sm ${global.textSecondary}`}>{t('permissions.features.none')}</p>}
                             {features.map(feat => (
                                <div key={feat.id} className={`flex items-center justify-between p-4 rounded-xl border ${global.border} ${global.bgSecondary}`}>
                                    <div>
                                        <p className="font-medium text-sm">{feat.featureCode}</p>
                                        <p className="text-xs text-green-500 flex items-center gap-1 mt-1">
                                            <CheckCircle size={10} /> {t('permissions.features.enabled')}
                                        </p>
                                    </div>
                                    <button 
                                        onClick={() => handleRevokeFeature(feat.featureCode)}
                                        className="text-red-500 hover:text-red-600 p-2 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                                    >
                                        <Trash2 size={18} />
                                    </button>
                                </div>
                             ))}
                        </div>
                    </div>
                )}
            </div>
            
            {/* Status Messages */}
            {success && (
                <div className="fixed bottom-8 right-8 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg flex items-center gap-2 animate-in fade-in slide-in-from-bottom-4">
                    <CheckCircle size={20} /> {success}
                </div>
            )}
            {error && (
                <div className="fixed bottom-8 right-8 bg-red-500 text-white px-6 py-3 rounded-lg shadow-lg flex items-center gap-2 animate-in fade-in slide-in-from-bottom-4">
                    <AlertCircle size={20} /> {error}
                </div>
            )}
        </div>
    );
}
