import { useState, useEffect } from 'react';
import { fetchApi, unwrapResult } from '../utils/api';
import { useTheme } from '../context/ThemeContext';
import { Building, Plus, Search, CheckCircle, AlertCircle, Loader, Copy, Save, X, FileJson, LayoutGrid, Users } from 'lucide-react';
import SearchableSelect from './SearchableSelect';

export default function TenantManager() {
    const { global, styles } = useTheme();

    // Tabs
    const [activeTab, setActiveTab] = useState<'tenants' | 'templates'>('tenants');

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


    // State - Tenants
    const [tenants, setTenants] = useState<any[]>([]);
    const [templates, setTemplates] = useState<string[]>([]); // For dropdown & identification
    const [isLoading, setIsLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');

    // Modal State - Onboard
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [formData, setFormData] = useState({
        tenantId: '',
        name: '',
        environment: 'Prod',
        adminEmail: '',
        sourceTenantId: ''
    });

    // Modal State - Edit Template
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [editingTemplate, setEditingTemplate] = useState<any>(null);
    const [jsonContent, setJsonContent] = useState('');

    const [submitStatus, setSubmitStatus] = useState({ loading: false, error: '', success: '' });

    useEffect(() => {
        loadTenants();
        loadTemplatesList();
    }, []);

    // Data Loading
    const loadTenants = async () => {
        setIsLoading(true);
        try {
            const res = await fetchApi('/api/tenants', { headers: getHeaders() });
            if (res.ok) setTenants(await unwrapResult(res));
        } catch (e) {
            console.error("Failed to load tenants", e);
        } finally {
            setIsLoading(false);
        }
    };

    const loadTemplatesList = async () => {
        try {
            const res = await fetchApi('/api/tenants/templates', { headers: getHeaders() });
            if (res.ok) setTemplates(await unwrapResult(res));
        } catch (e) {
            console.error("Failed to load templates list", e);
        }
    };

    // Actions
    const handleCreate = async (e: React.FormEvent) => {
        e.preventDefault();
        setSubmitStatus({ loading: true, error: '', success: '' });

        try {
            const res = await fetchApi('/api/tenants', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            if (res.ok) {
                setSubmitStatus({ loading: false, error: '', success: 'Tenant onboarded successfully!' });
                loadTenants();
                loadTemplatesList();
                setTimeout(() => {
                    setIsModalOpen(false);
                    setFormData({ tenantId: '', name: '', environment: 'Prod', adminEmail: '', sourceTenantId: '' });
                    setSubmitStatus({ loading: false, error: '', success: '' });
                }, 1500);
            } else {
                try {
                    await unwrapResult(res);
                } catch (e: any) {
                    setSubmitStatus({ loading: false, error: `Failed: ${e.message}`, success: '' });
                }
            }
        } catch (err) {
            setSubmitStatus({ loading: false, error: 'Network error occurred.', success: '' });
        }
    };

    const handleEditTemplate = async (tenantId: string) => {
        setSubmitStatus({ loading: false, error: '', success: '' });
        try {
            const res = await fetchApi(`/api/tenants/configurations/by-tenant/${tenantId}`, { headers: getHeaders() });
            if (res.ok) {
                const data = await res.json();
                setEditingTemplate(data);
                setJsonContent(JSON.stringify(data, null, 2));
                setIsEditModalOpen(true);
            } else {
                 console.error("Failed to load template config");
            }
        } catch (e) {
            console.error("Error fetching config", e);
        }
    };

    const handleUpdateTemplate = async () => {
        setSubmitStatus({ loading: true, error: '', success: '' });
        try {
            const parsed = JSON.parse(jsonContent);
            const id = editingTemplate._id;

            const res = await fetchApi(`/api/tenants/configurations/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(parsed)
            });

            if (res.ok) {
                setSubmitStatus({ loading: false, error: '', success: 'Template updated successfully!' });
                setTimeout(() => {
                    setIsEditModalOpen(false);
                    setSubmitStatus({ loading: false, error: '', success: '' });
                }, 1000);
            } else {
                setSubmitStatus({ loading: false, error: 'Failed to update template', success: '' });
            }
        } catch (e) {
            setSubmitStatus({ loading: false, error: 'Invalid JSON format', success: '' });
        }
    };

    // Filtering
    const filteredTenants = tenants.filter(t =>
        t.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        t.tenantId.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const itemsToRender = activeTab === 'tenants' 
        ? filteredTenants 
        : filteredTenants.filter(t => templates.includes(t.tenantId));

    const templateOptions = templates.map(tId => ({
        value: tId,
        label: tId,
        subLabel: ''
    }));

    if (!templateOptions.find(o => o.value === formData.sourceTenantId)) {
        templateOptions.push({ value: formData.sourceTenantId, label: formData.sourceTenantId, subLabel: '(Manual/Unknown)' });
    }

    return (
        <div className={`p-8 max-w-6xl mx-auto min-h-screen ${global.text}`}>
            {/* Header */}
            <div className="flex items-center justify-between mb-8">
                <div className="flex items-center gap-4">
                    <div className={`p-3 rounded-lg ${styles.light}`}>
                        <Building size={32} className={styles.text} />
                    </div>
                    <div>
                        <h1 className="text-2xl font-bold">Tenant Management</h1>
                        <p className={global.textSecondary}>Onboard and manage platform tenants.</p>
                    </div>
                </div>
                <div className="flex gap-2">
                    <div className={`flex p-1 rounded-lg ${global.bgSecondary} border ${global.border}`}>
                        <button
                            onClick={() => setActiveTab('tenants')}
                            className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2 ${activeTab === 'tenants' ? 'bg-white text-blue-600 shadow-sm dark:bg-gray-800' : `${global.textSecondary} hover:${global.text}`}`}
                        >
                            <Users size={16} /> Tenants
                        </button>
                        <button
                            onClick={() => setActiveTab('templates')}
                            className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2 ${activeTab === 'templates' ? 'bg-white text-blue-600 shadow-sm dark:bg-gray-800' : `${global.textSecondary} hover:${global.text}`}`}
                        >
                            <LayoutGrid size={16} /> Templates
                        </button>
                    </div>
                    {activeTab === 'tenants' && (
                        <button
                            onClick={() => setIsModalOpen(true)}
                            className={`flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 text-white font-medium hover:bg-blue-700 transition-colors shadow-lg shadow-blue-500/20`}
                        >
                            <Plus size={18} /> Onboard Tenant
                        </button>
                    )}
                </div>
            </div>

            {/* Content */}
            <>
                {/* Search */}
                <div className="mb-6">
                    <div className={`flex items-center gap-3 p-3 rounded-xl border ${global.border} ${global.bg} shadow-sm focus-within:ring-2 focus-within:ring-blue-500/20 transition-all`}>
                        <Search size={20} className={global.textSecondary} />
                        <input
                            type="text"
                            placeholder={activeTab === 'tenants' ? "Search tenants by name or ID..." : "Search templates by name or ID..."}
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className={`flex-1 bg-transparent border-none outline-none ${global.text} placeholder:text-gray-400`}
                        />
                    </div>
                </div>

                {/* List */}
                {isLoading ? (
                    <div className="flex justify-center py-12"><Loader className="animate-spin" /></div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {itemsToRender.length === 0 && (
                            <div className={`col-span-full text-center py-12 ${global.textSecondary}`}>
                                {activeTab === 'tenants' ? 'No tenants found.' : 'No templates found.'}
                            </div>
                        )}
                        {itemsToRender.map(tenant => (
                            <div key={tenant.tenantId} className={`p-5 rounded-xl border ${global.border} ${global.bg} hover:border-blue-500/50 transition-colors group relative overflow-hidden`}>
                                <div className="flex justify-between items-start mb-3">
                                    <div>
                                        <h3 className="font-semibold text-lg">{tenant.name}</h3>
                                        <code className={`text-xs px-2 py-1 rounded ${global.bgSecondary} ${global.textSecondary} font-mono`}>{tenant.tenantId}</code>
                                    </div>
                                    <div className={`px-2 py-1 rounded text-xs font-bold uppercase tracking-wider ${tenant.environment === 'prod' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'}`}>
                                        {tenant.environment}
                                    </div>
                                </div>
                                <div className={`text-sm ${global.textSecondary} flex items-center gap-2`}>
                                    <div className="w-2 h-2 rounded-full bg-gray-400"></div>
                                    {tenant.adminEmail || 'No admin email'}
                                </div>
                                <div className={`text-xs ${global.textSecondary} mt-4 pt-3 border-t ${global.border} opacity-70`}>
                                    Onboarded: {new Date(tenant.createdAt).toLocaleDateString()}
                                </div>
                                
                                {activeTab === 'templates' && (
                                    <div className="mt-4 pt-4 border-t border-dashed border-gray-200 dark:border-gray-700">
                                        <button
                                            onClick={() => handleEditTemplate(tenant.tenantId)}
                                            className="w-full py-2 rounded-lg bg-blue-50 dark:bg-blue-900/20 text-blue-600 hover:bg-blue-100 dark:hover:bg-blue-900/40 transition-colors flex items-center justify-center gap-2 font-medium text-sm"
                                        >
                                            <FileJson size={16} /> Edit Configuration
                                        </button>
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </>


            {/* Onboard Modal */}
            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
                    <div className={`w-full max-w-md p-6 rounded-2xl shadow-2xl ${global.bg} ${global.border} border transform transition-all scale-100`}>
                        <div className="flex justify-between items-center mb-6">
                            <h2 className="text-xl font-bold">Onboard New Tenant</h2>
                            <button onClick={() => setIsModalOpen(false)} className={`p-1 rounded hover:${global.bgSecondary}`}><X size={24} /></button>
                        </div>

                        <form onSubmit={handleCreate} className="space-y-4">
                            <div>
                                <label className={`block text-xs font-semibold uppercase tracking-wider ${global.textSecondary} mb-1`}>Tenant Name</label>
                                <input
                                    required
                                    type="text"
                                    className={`w-full p-3 rounded-lg border ${global.border} ${global.bgSecondary} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none`}
                                    placeholder="e.g. Acme Corp"
                                    value={formData.name}
                                    onChange={e => setFormData({ ...formData, name: e.target.value })}
                                />
                            </div>
                            <div>
                                <label className={`block text-xs font-semibold uppercase tracking-wider ${global.textSecondary} mb-1`}>Tenant ID (Code)</label>
                                <input
                                    required
                                    type="text"
                                    className={`w-full p-3 rounded-lg border ${global.border} ${global.bgSecondary} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none font-mono`}
                                    placeholder="e.g. ACME_001"
                                    value={formData.tenantId}
                                    onChange={e => setFormData({ ...formData, tenantId: e.target.value.toUpperCase().replace(/[^A-Z0-9_]/g, '') })}
                                />
                                <p className="text-xs text-gray-500 mt-1">Uppercase, numbers and underscores only.</p>
                            </div>

                            <div className="grid grid-cols-1 gap-4">
                                <div>
                                    <SearchableSelect
                                        label="Source Template (Platform Objects)"
                                        value={formData.sourceTenantId}
                                        onChange={(val) => setFormData({ ...formData, sourceTenantId: val })}
                                        options={templateOptions}
                                        placeholder="Select source template..."
                                        icon={<Copy size={14} />}
                                    />
                                </div>
                                <div>
                                    <label className={`block text-xs font-semibold uppercase tracking-wider ${global.textSecondary} mb-1`}>Environment</label>
                                    <select
                                        className={`w-full p-3 rounded-lg border ${global.border} ${global.bgSecondary} ${global.text} outline-none`}
                                        value={formData.environment}
                                        onChange={e => setFormData({ ...formData, environment: e.target.value })}
                                    >
                                        <option value="Dev">Dev</option>
                                        <option value="Test">Test</option>
                                        <option value="Prod">Prod</option>
                                    </select>
                                </div>
                            </div>

                            <div>
                                <label className={`block text-xs font-semibold uppercase tracking-wider ${global.textSecondary} mb-1`}>Admin Email</label>
                                <input
                                    type="email"
                                    className={`w-full p-3 rounded-lg border ${global.border} ${global.bgSecondary} ${global.text} focus:ring-2 focus:ring-blue-500 outline-none`}
                                    placeholder="admin@acme.com"
                                    value={formData.adminEmail}
                                    onChange={e => setFormData({ ...formData, adminEmail: e.target.value })}
                                />
                            </div>

                            {submitStatus.error && (
                                <div className="p-3 rounded-lg bg-red-500/10 text-red-500 text-sm flex items-center gap-2">
                                    <AlertCircle size={16} /> {submitStatus.error}
                                </div>
                            )}
                            {submitStatus.success && (
                                <div className="p-3 rounded-lg bg-green-500/10 text-green-500 text-sm flex items-center gap-2">
                                    <CheckCircle size={16} /> {submitStatus.success}
                                </div>
                            )}

                            <div className="pt-2">
                                <button
                                    type="submit"
                                    disabled={submitStatus.loading}
                                    className={`w-full py-3 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-semibold transition-all flex items-center justify-center gap-2 ${submitStatus.loading ? 'opacity-70 cursor-not-allowed' : ''}`}
                                >
                                    {submitStatus.loading && <Loader size={18} className="animate-spin" />}
                                    {submitStatus.loading ? 'Provisioning...' : 'Onboard Tenant'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Edit Template Modal */}
            {isEditModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
                    <div className={`w-full max-w-4xl h-[80vh] flex flex-col p-6 rounded-2xl shadow-2xl ${global.bg} ${global.border} border transform transition-all scale-100`}>
                        <div className="flex justify-between items-center mb-4">
                            <div className="flex items-center gap-3">
                                <div className="p-2 bg-blue-100 dark:bg-blue-900/30 text-blue-600 rounded-lg">
                                    <FileJson size={20} />
                                </div>
                                <div>
                                    <h2 className="text-xl font-bold">Edit Configuration</h2>
                                    <p className={`text-sm ${global.textSecondary}`}>
                                        Editing {editingTemplate?.tenantId} ({editingTemplate?._id})
                                    </p>
                                </div>
                            </div>
                            <button onClick={() => setIsEditModalOpen(false)} className={`p-2 rounded-lg hover:${global.bgSecondary}`}><X size={24} /></button>
                        </div>

                        <div className="flex-1 overflow-hidden border rounded-xl bg-gray-50 dark:bg-gray-900 relative">
                            <textarea
                                className="w-full h-full p-4 font-mono text-sm bg-transparent border-none outline-none resize-none"
                                value={jsonContent}
                                onChange={(e) => setJsonContent(e.target.value)}
                                spellCheck={false}
                            />
                        </div>

                        <div className="flex justify-between items-center mt-4">
                            <div className="text-sm">
                                {submitStatus.error && <span className="text-red-500 flex items-center gap-1"><AlertCircle size={14} /> {submitStatus.error}</span>}
                                {submitStatus.success && <span className="text-green-500 flex items-center gap-1"><CheckCircle size={14} /> {submitStatus.success}</span>}
                            </div>
                            <button
                                onClick={handleUpdateTemplate}
                                disabled={submitStatus.loading}
                                className={`px-6 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium flex items-center gap-2 ${submitStatus.loading ? 'opacity-70' : ''}`}
                            >
                                {submitStatus.loading ? <Loader size={16} className="animate-spin" /> : <Save size={16} />}
                                Save Changes
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
