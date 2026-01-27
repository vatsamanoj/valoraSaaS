import React, { useState, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import { ModuleSchema } from '../types/schema';
import DynamicForm from './DynamicForm';
import Modal from './Modal';
import { Plus, Box, Layout, ChevronRight, X, Sparkles, Loader, Settings, Save, Trash2, Check, Server, Copy, Star, Database, ChevronDown } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';
import SearchableSelect from './SearchableSelect';
import { normalizeSchema } from '../utils/schemaUtils';

interface AiConfig {
    id: string;
    name: string;
    provider: string;
    model: string;
    apiKey: string;
    baseUrl: string;
    isActive: boolean;
}

const PlatformStudio = () => {
  const [objectCode, setObjectCode] = useState('Patient');
  const [objectList, setObjectList] = useState<string[]>([]);
  const [jsonSchema, setJsonSchema] = useState('');
  const [parsedSchema, setParsedSchema] = useState<ModuleSchema | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Theme State
  const { styles: currentTheme, global, isDark, t } = useTheme();

  // Context State (Internal to Studio)
  const [studioTenantId, setStudioTenantId] = useState('');
  const [studioEnv, setStudioEnv] = useState('prod');
  const [availableTenants, setAvailableTenants] = useState<{value: string, label: string}[]>([]);

  // Effect to initialize tenant selection if empty but options available
  useEffect(() => {
      if (!studioTenantId && availableTenants.length > 0) {
          setStudioTenantId(availableTenants[0].value);
      }
  }, [availableTenants, studioTenantId]);

  // Track unsaved (local-only) modules to prevent 404 fetches
  const [unsavedModules, setUnsavedModules] = useState<string[]>([]);

  // Use a ref to control whether we should fetch when objectCode changes
  const shouldFetchRef = React.useRef(true);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [newModuleName, setNewModuleName] = useState('');
  const [newModuleDesc, setNewModuleDesc] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  
  // Clone Modal State
  const [isCloneModalOpen, setIsCloneModalOpen] = useState(false);
  const [cloneModuleName, setCloneModuleName] = useState('');
  const [cloneSourceEnv] = useState('PROD');
  const [cloneTargetEnv] = useState('TEST');

  // Bulk Clone Modal State
  const [isBulkCloneModalOpen, setIsBulkCloneModalOpen] = useState(false);
  const [bulkCloneSourceEnv, setBulkCloneSourceEnv] = useState('PROD');
  const [bulkCloneTargetEnv, setBulkCloneTargetEnv] = useState('TEST');
  const [selectedModules, setSelectedModules] = useState<string[]>([]);
  const [bulkCloneStatus, setBulkCloneStatus] = useState('');
  const [bulkCloneModuleList, setBulkCloneModuleList] = useState<string[]>([]);
  const [isFetchingModules, setIsFetchingModules] = useState(false);

  
  // Confirmation Modal State
  const [confirmationModal, setConfirmationModal] = useState<{
      isOpen: boolean;
      title: string;
      message: React.ReactNode;
      onConfirm?: () => void;
      type?: 'success' | 'error' | 'info' | 'warning';
      confirmText?: string;
  }>({
      isOpen: false,
      title: '',
      message: '',
      onConfirm: undefined,
      type: 'warning'
  });

  // Publication Status
  const [isPublished, setIsPublished] = useState(false);

  const [versions, setVersions] = useState<{ version: number; isPublished: boolean }[]>([]);
  const [selectedVersion, setSelectedVersion] = useState<number | null>(null);

  // Auto-Save State
  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false);

  // Settings Modal State
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [aiConfigs, setAiConfigs] = useState<AiConfig[]>([]);
  const [selectedConfig, setSelectedConfig] = useState<AiConfig | null>(null);
  const [savingSettings, setSavingSettings] = useState(false);

  // Unique Constraints Modal State
  const [isUniqueConstraintsModalOpen, setIsUniqueConstraintsModalOpen] = useState(false);
  const [tempConstraints, setTempConstraints] = useState<string[][]>([]);
  const [selectedConstraintFields, setSelectedConstraintFields] = useState<string[]>([]);

  // Environment Info
  const [envInfo, setEnvInfo] = useState<{ environment: string, tenantId: string } | null>(null);
  const [publishTargetEnv, setPublishTargetEnv] = useState<string>('');

  const [favoriteModules, setFavoriteModules] = useState<string[]>([]);
  const [moduleFilter, setModuleFilter] = useState<'all' | 'favorites'>('all');

  // Debug Mode
  const [showDebug, setShowDebug] = useState(false);

  // Fetch list on mount
  useEffect(() => {
    fetchTenants();
    
    // Initialize context from local storage if available, else default
    const savedContext = localStorage.getItem('studio_context');
    if (savedContext) {
        try {
            const ctx = JSON.parse(savedContext);
            if (ctx.tenantId) setStudioTenantId(ctx.tenantId);
            if (ctx.environment) setStudioEnv(ctx.environment);
        } catch {}
    }
  }, []);

  // Update EnvInfo whenever studio context changes
  useEffect(() => {
      setEnvInfo({
          environment: studioEnv,
          tenantId: studioTenantId
      });
      
      // Update Headers for subsequent API calls in this session (hacky but works with api.ts)
      const headers = {
        'X-Tenant-Id': studioTenantId,
        'X-Environment': studioEnv
      };
      localStorage.setItem('valora_headers', JSON.stringify(headers));
      localStorage.setItem('studio_context', JSON.stringify({ tenantId: studioTenantId, environment: studioEnv }));

      fetchObjectList();
  }, [studioTenantId, studioEnv]);

  const getHeaders = () => {
    let headers: Record<string, string> = {
        'X-Tenant-Id': studioTenantId,
        'X-Environment': studioEnv
    };
    return headers;
  };

  const fetchTenants = async () => {
      try {
          const response = await fetchApi('/api/tenants', { headers: getHeaders() });
          if (response.ok) {
              const data = await unwrapResult<any[]>(response);
              const options = data.map(t => ({
                  value: t.tenantId,
                  label: t.name || t.tenantId
              }));
              setAvailableTenants(options);

              // Auto-select if empty
              setStudioTenantId(prev => {
                  if (!prev && options.length > 0) return options[0].value;
                  return prev;
              });
          }
      } catch (err) {
          console.error("Failed to fetch tenants", err);
      }
  };

  useEffect(() => {
    if (objectCode) {
        setVersions([]);
        setSelectedVersion(null);
        if (shouldFetchRef.current) {
            fetchSchema();
        } else {
            shouldFetchRef.current = true;
        }
        fetchVersions();
    }
  }, [objectCode, studioTenantId, studioEnv]); // Re-fetch when context changes

  const fetchObjectList = async () => {
      try {
          const response = await fetchApi('/api/platform/object/list', {
              headers: {
                  'X-Tenant-Id': studioTenantId,
                  'X-Environment': studioEnv
              }
          });
          const data = await unwrapResult<any>(response);
          setObjectList(data);
      } catch (err) {
          console.error("Failed to fetch object list", err);
      }
  };

  const fetchEnvInfo = async () => {
      try {
          if (typeof window === 'undefined') return;
          const raw = window.localStorage.getItem('valora_context');
          if (!raw) return;
          const ctx = JSON.parse(raw);
          setEnvInfo({
              environment: ctx.environment || 'Unknown',
              tenantId: ctx.tenantId || 'Unknown'
          });
      } catch (err) {
          console.error("Failed to fetch env info", err);
      }
  };

  const fetchVersions = async () => {
      try {
          const response = await fetchApi(`/api/platform/object/${objectCode}/versions`, { headers: getHeaders() });
          if (!response.ok) {
              setVersions([]);
              return;
          }
          const data = await response.json();
          if (!Array.isArray(data)) {
              setVersions([]);
              return;
          }

          const mapped = data
              .map((x: any) => ({
                  version: typeof x.version === 'number' ? x.version : typeof x.Version === 'number' ? x.Version : 0,
                  isPublished: typeof x.isPublished === 'boolean' ? x.isPublished : typeof x.IsPublished === 'boolean' ? x.IsPublished : false
              }))
              .filter(x => x.version > 0)
              .sort((a, b) => b.version - a.version);

          setVersions(mapped);
      } catch {
          setVersions([]);
      }
  };

  useEffect(() => {
      if (!envInfo) return;
      const key = `studioFavorites_${envInfo.tenantId}_${envInfo.environment}`;
      try {
          const raw = window.localStorage.getItem(key);
          if (raw) {
              const parsed = JSON.parse(raw);
              if (Array.isArray(parsed)) {
                  setFavoriteModules(parsed);
              }
          }
      } catch {
      }
  }, [envInfo]);

  useEffect(() => {
      if (!envInfo) return;
      const key = `studioFavorites_${envInfo.tenantId}_${envInfo.environment}`;
      try {
          window.localStorage.setItem(key, JSON.stringify(favoriteModules));
      } catch {
      }
  }, [favoriteModules, envInfo]);

  useEffect(() => {
      if (!envInfo?.environment) return;
      if (publishTargetEnv) return;

      const order = ['DEV', 'TEST', 'PREVIEW', 'PROD'];
      const fromEnv = envInfo.environment.toUpperCase();
      const fromIndex = order.indexOf(fromEnv);
      if (fromIndex === -1) return;

      const firstHigher = order.find(target => {
          const idx = order.indexOf(target);
          if (idx <= fromIndex) return false;
          if (target === 'PROD' && fromEnv !== 'PREVIEW') return false;
          return true;
      });

      if (firstHigher) {
          setPublishTargetEnv(firstHigher);
      }
  }, [envInfo, publishTargetEnv]);

  const toggleFavoriteModule = (name: string) => {
      setFavoriteModules(prev =>
          prev.includes(name) ? prev.filter(m => m !== name) : [...prev, name]
      );
  };

  const fetchAiConfigs = async () => {
      try {
          const response = await fetchApi('/platform/ai-config', { headers: getHeaders() });
          if (response.ok) {
              const data = await unwrapResult<AiConfig[]>(response);
              setAiConfigs(data);
              // Select active one by default if nothing selected
              if (!selectedConfig) {
                  const active = data.find((c: AiConfig) => c.isActive);
                  if (active) setSelectedConfig(active);
                  else if (data.length > 0) setSelectedConfig(data[0]);
                  else createNewConfig();
              }
          }
      } catch (err) {
          console.error("Failed to fetch AI configs", err);
      }
  };

  const createNewConfig = () => {
      setSelectedConfig({
          id: '', // Empty ID for new
          name: 'New Configuration',
          provider: 'OpenAI',
          model: 'gpt-4o',
          apiKey: '',
          baseUrl: 'https://api.openai.com/v1',
          isActive: false
      });
  };

  const openSettings = () => {
      fetchAiConfigs();
      setIsSettingsOpen(true);
  };

  const handleProviderChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
      if (!selectedConfig) return;
      const provider = e.target.value;
      let baseUrl = selectedConfig.baseUrl;
      let model = selectedConfig.model;

      // Presets
      if (provider === 'OpenAI') {
          baseUrl = 'https://api.openai.com/v1';
          model = 'gpt-4o';
      } else if (provider === 'Groq') {
          baseUrl = 'https://api.groq.com/openai/v1';
          model = 'llama3-70b-8192';
      } else if (provider === 'DeepSeek') {
          baseUrl = 'https://api.deepseek.com/v1';
          model = 'deepseek-chat';
      } else if (provider === 'Local (Ollama)') {
          baseUrl = 'http://localhost:11434/v1';
          model = 'llama3';
      } else if (provider === 'Local (LM Studio)') {
          baseUrl = 'http://localhost:1234/v1';
          model = 'local-model';
      }

      setSelectedConfig({
          ...selectedConfig,
          provider,
          baseUrl,
          model
      });
  };

  const saveCurrentConfig = async () => {
      if (!selectedConfig) return;
      setSavingSettings(true);
      try {
          const payload = {
            ...selectedConfig,
            id: selectedConfig.id || '00000000-0000-0000-0000-000000000000'
          };

          const response = await fetchApi('/platform/ai-config', {
              method: 'POST',
              headers: { ...getHeaders(), 'Content-Type': 'application/json' },
              body: JSON.stringify(payload)
          });

          if (!response.ok) throw new Error(t('studio.ai.saveError'));
          
          setSuccess(t('studio.ai.saveSuccess'));
          setTimeout(() => setSuccess(''), 3000);
          
          const updatedList = await response.json();
          setAiConfigs(updatedList);
          
          const saved = updatedList.find((c: AiConfig) => c.name === selectedConfig.name) || updatedList[updatedList.length - 1];
          if (saved) setSelectedConfig(saved);

      } catch (err: any) {
          setError(err.message);
      } finally {
          setSavingSettings(false);
      }
  };

  const activateConfig = async (id: string) => {
      try {
          const response = await fetchApi(`/platform/ai-config/${id}/activate`, {
              method: 'POST',
              headers: getHeaders()
          });
          if (response.ok) {
              const updatedList = await response.json();
              setAiConfigs(updatedList);
              const active = updatedList.find((c: AiConfig) => c.id === id);
              if (active) setSelectedConfig(active);
              setSuccess(t('studio.ai.activateSuccess'));
              setTimeout(() => setSuccess(''), 3000);
          }
      } catch (err) {
          console.error("Failed to activate", err);
      }
  };

  const deleteConfig = (id: string) => {
      setConfirmationModal({
          isOpen: true,
          title: t('studio.ai.deleteTitle'),
          message: t('studio.ai.deleteMessage'),
          type: 'warning',
          confirmText: t('studio.ai.deleteConfirm'),
          onConfirm: () => executeDeleteConfig(id)
      });
  };

  const executeDeleteConfig = async (id: string) => {
      setConfirmationModal(prev => ({ ...prev, isOpen: false }));
      try {
          const response = await fetchApi(`/platform/ai-config/${id}`, {
              method: 'DELETE',
              headers: getHeaders()
          });
          if (response.ok) {
              const updatedList = await response.json();
              setAiConfigs(updatedList);
              if (updatedList.length > 0) setSelectedConfig(updatedList[0]);
              else createNewConfig();
          }
      } catch (err) {
          console.error("Failed to delete", err);
      }
  };

  const fetchSchema = async (version?: number) => {
    setLoading(true);
    setError('');
    try {
      const url = typeof version === 'number'
          ? `/api/platform/object/${objectCode}/version/${version}`
          : `/api/platform/object/${objectCode}/latest`;
      const response = await fetchApi(url, { headers: getHeaders() });
      if (response.status === 404) {
          // Default template for new objects
          const defaultSchema = {
              module: objectCode,
              objectType: "Master",
              fields: {
                  "General Info": {
                      name: {
                          type: "String",
                          required: true,
                          ui: { label: "Name", type: "text" }
                      }
                  }
              }
          };
          setJsonSchema(JSON.stringify(defaultSchema, null, 2));
          setParsedSchema(normalizeSchema(defaultSchema));
          return;
      }
      if (!response.ok) throw new Error('Failed to fetch schema');
      
      const publishedHeader = response.headers.get("X-Is-Published");
      setIsPublished(publishedHeader === "true");
      
      // Use unwrapResult to handle API envelope if present
      let data;
      try {
        data = await unwrapResult(response);
      } catch (e) {
        // Fallback if unwrap fails or response is already cloned/read (though fetchApi returns new response)
        // If unwrapResult consumes body, we need to be careful. 
        // But here we haven't read it yet in this block.
        // Actually, unwrapResult reads the body.
        // If unwrapResult fails (e.g. 500 error wrapped), we catch it.
        // But if it succeeds, 'data' is the inner object.
        // If the response was NOT wrapped, unwrapResult (in many implementations) returns the raw json or throws.
        // Let's assume unwrapResult handles { success: true, data: ... } -> returns data.
        throw e;
      }

      const normalized = normalizeSchema(data);
      setJsonSchema(JSON.stringify(data, null, 2)); // Keep clean JSON in editor
      setParsedSchema(normalized); // Use normalized for Preview
      const schemaVersion = typeof (data as any).version === 'number'
          ? (data as any).version
          : typeof (data as any).Version === 'number'
              ? (data as any).Version
              : null;
      if (schemaVersion && schemaVersion > 0) {
          setSelectedVersion(schemaVersion);
      }
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateNew = () => {
      setNewModuleName('');
      setNewModuleDesc('');
      setIsModalOpen(true);
  };

  const createModule = async (useAi: boolean) => {
      if (!newModuleName) return;
      
      if (useAi) {
          setIsGenerating(true);
          try {
              const response = await fetchApi('/api/platform/object/suggest', {
                  method: 'POST',
                  headers: { ...getHeaders(), 'Content-Type': 'application/json' },
                  body: JSON.stringify({ module: newModuleName, description: newModuleDesc })
              });
              
              if (!response.ok) {
                  const errorData = await response.json().catch(() => ({}));
                  throw new Error(errorData.Error || errorData.Message || t('messages.network.unknownError'));
              }
              const schema = await response.json();
              
              finishCreation(newModuleName, schema);
          } catch (err: any) {
              setError(t('messages.network.unknownError'));
          } finally {
              setIsGenerating(false);
          }
      } else {
          // Create Blank
          const defaultSchema = {
              module: newModuleName,
              objectType: "Master",
              fields: {
                  "General Info": {
                      name: {
                          type: "String",
                          required: true,
                          ui: { label: "Name", type: "text" }
                      }
                  }
              }
          };
          finishCreation(newModuleName, defaultSchema);
      }
  };

  const finishCreation = (name: string, schema: any) => {
      // Prevent fetch on next objectCode change
      shouldFetchRef.current = false;
      
      setObjectCode(name);
      setJsonSchema(JSON.stringify(schema, null, 2));
      setParsedSchema(normalizeSchema(schema));
      if (!objectList.includes(name)) {
          setObjectList([...objectList, name].sort());
      }
      
      // Add to unsaved modules
      if (!unsavedModules.includes(name)) {
          setUnsavedModules(prev => [...prev, name]);
      }
      
      setIsModalOpen(false);
  };

  const handleCloneClick = () => {
      let baseName = `${objectCode}_Copy`;
      let newName = baseName;
      let counter = 1;

      // Find unique name
      while (objectList.includes(newName)) {
          newName = `${baseName}_${counter}`;
          counter++;
      }

      setCloneModuleName(newName);
      setIsCloneModalOpen(true);
  };

  const executeClone = async () => {
      if (!cloneModuleName) return;
      try {
          let currentSchema: any;
          try {
             currentSchema = JSON.parse(jsonSchema);
          } catch {
             currentSchema = {};
          }
          
          // Fetch from Source Env if needed
          const getSourceHeaders = (env: string) => ({
              'X-Tenant-Id': studioTenantId,
              'X-Environment': env
          });

          if (cloneSourceEnv === 'PROD') {
              const response = await fetchApi(`/api/platform/object/${objectCode}/published`, { headers: getSourceHeaders('PROD') });
              if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
              currentSchema = await unwrapResult(response);
          } else if (cloneSourceEnv === 'TEST') {
               const response = await fetchApi(`/api/platform/object/${objectCode}/latest`, { headers: getSourceHeaders('TEST') });
               if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
               currentSchema = await unwrapResult(response);
          } else if (cloneSourceEnv === 'PREVIEW') {
               // Treat PREVIEW as Published for now
               const response = await fetchApi(`/api/platform/object/${objectCode}/published`, { headers: getSourceHeaders('PREVIEW') });
               if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
               currentSchema = await unwrapResult(response);
          }

          // const currentSchema = JSON.parse(sourceSchemaJson); // Removed as we have object now
          
          // Deep copy
          const newSchema = JSON.parse(JSON.stringify(currentSchema));
          
          // Update module name, preserving case if 'Module' is used instead of 'module'
          let updated = false;
          if (newSchema.hasOwnProperty('Module')) {
              newSchema.Module = cloneModuleName;
              updated = true;
          }
          if (newSchema.hasOwnProperty('module')) {
              newSchema.module = cloneModuleName;
              updated = true;
          }
          
          // If neither existed, default to 'module'
          if (!updated) {
              newSchema.module = cloneModuleName;
          }
          
          finishCreation(cloneModuleName, newSchema);
          setIsCloneModalOpen(false);
          setSuccess(t('studio.clone.success', { name: cloneModuleName, source: cloneSourceEnv, target: cloneTargetEnv }));
          setTimeout(() => setSuccess(''), 3000);
      } catch (err: any) {
          setError(t('messages.network.unknownError'));
      }
  };

  const fetchBulkCloneModuleList = async (env: string) => {
      setIsFetchingModules(true);
      try {
          const response = await fetchApi(`/api/platform/object/list/${env}`, {
              headers: {
                  'X-Tenant-Id': studioTenantId,
                  'X-Environment': env
              }
          });
          if (response.ok) {
              const data = await response.json();
              setBulkCloneModuleList(data);
              setSelectedModules([]); // Clear selection when list changes
          }
      } catch (err) {
          console.error("Failed to fetch bulk clone module list", err);
      } finally {
          setIsFetchingModules(false);
      }
  };

  const handleBulkCloneClick = () => {
      setSelectedModules([]);
      setBulkCloneStatus('');
      setBulkCloneSourceEnv('PROD');
      fetchBulkCloneModuleList('PROD');
      setIsBulkCloneModalOpen(true);
  };

  const executeBulkClone = async () => {
      if (selectedModules.length === 0) return;
      setBulkCloneStatus(t('studio.bulkClone.statusStarting'));
      
      let successCount = 0;
      let failCount = 0;

      for (const moduleName of selectedModules) {
          try {
              setBulkCloneStatus(t('studio.bulkClone.statusCloning', { module: moduleName }));
              
              let sourceSchemaJson = '';
              // Fetch from Source Env
              if (bulkCloneSourceEnv === 'PROD') {
                  const response = await fetchApi(`/api/platform/object/${moduleName}/published`, { headers: getHeaders() });
                  if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
                  sourceSchemaJson = await response.text();
              } else if (bulkCloneSourceEnv === 'TEST') {
                   const response = await fetchApi(`/api/platform/object/${moduleName}/latest`, { headers: getHeaders() });
                   if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
                   sourceSchemaJson = await response.text();
              } else if (bulkCloneSourceEnv === 'PREVIEW') {
                   const response = await fetchApi(`/api/platform/object/${moduleName}/published`, { headers: getHeaders() });
                   if (!response.ok) throw new Error(t('messages.crud.fetchFailed', { entity: 'schema' }));
                   sourceSchemaJson = await response.text();
              }

              // Save to Current Env (as Draft)
              // NOTE: This assumes the user is currently connected to the "Target" environment's backend.
              // If Target != Current, we are effectively just cloning to current.
              // Ideally, we would POST to the Target Env URL, but we don't have it.
              
              // Validate JSON
              const schema = JSON.parse(sourceSchemaJson);
              
              const saveResponse = await fetchApi(`/api/platform/object/${moduleName}/draft`, {
                  method: 'POST',
                  headers: { ...getHeaders(), 'Content-Type': 'application/json' },
                  body: JSON.stringify(schema)
              });
              
              if (!saveResponse.ok) throw new Error(t('messages.crud.saveFailed', { entity: 'draft' }));
              successCount++;

          } catch (err: any) {
              console.error(`Failed to clone ${moduleName}`, err);
              failCount++;
          }
      }
      
      setBulkCloneStatus(t('studio.bulkClone.statusCompleted', { success: String(successCount), fail: String(failCount) }));
      setTimeout(() => {
          setIsBulkCloneModalOpen(false);
          setSuccess(t('studio.bulkClone.success', { count: String(successCount) }));
          fetchObjectList();
          if (selectedModules.includes(objectCode)) {
              fetchSchema(); // Refresh current if it was updated
          }
      }, 2000);
  };

  const handleJsonChange = (value: string) => {
    setJsonSchema(value);
    try {
      const parsed = JSON.parse(value);
      const normalized = normalizeSchema(parsed);
      setParsedSchema(normalized);
      setError('');
    } catch (err) {
      // Invalid JSON
    }
  };

  const handleUnpublish = () => {
      if (envInfo?.environment === 'PROD') {
          setConfirmationModal({
              isOpen: true,
              title: t('studio.unpublish.blockedTitle'),
              message: t('studio.unpublish.blockedMessage'),
              type: 'error',
              onConfirm: undefined
          });
          return;
      }

      setConfirmationModal({
          isOpen: true,
          title: t('studio.unpublish.confirmTitle'),
          message: t('studio.unpublish.confirmMessage'),
          type: 'warning',
          confirmText: t('studio.unpublish.confirmAction'),
          onConfirm: () => executeUnpublish()
      });
  };

  const executeUnpublish = async () => {
      setConfirmationModal(prev => ({ ...prev, isOpen: false }));
      try {
          const response = await fetchApi(`/api/platform/object/${objectCode}/unpublish`, { 
              method: 'POST',
              headers: getHeaders()
          });
          if (!response.ok) {
              const errText = await response.text();
              throw new Error(errText || t('messages.crud.deleteFailed', { entity: objectCode || 'screen' }));
          }
          setSuccess(t('studio.unpublish.success'));
          setIsPublished(false);
          setTimeout(() => setSuccess(''), 3000);
      } catch (err: any) {
          setError(err.message);
      }
  };

  const handleDelete = (moduleName?: string) => {
      const target = moduleName || objectCode;
      if (!target) return;
      setConfirmationModal({
          isOpen: true,
          title: t('studio.moduleDelete.title'),
          message: t('studio.moduleDelete.message'),
          type: 'error',
          confirmText: t('studio.moduleDelete.confirm'),
          onConfirm: () => executeDelete(target)
      });
  };

  const executeDelete = async (moduleName: string) => {
      setConfirmationModal(prev => ({ ...prev, isOpen: false }));
      try {
          const response = await fetchApi(`/api/platform/object/${moduleName}`, { 
              method: 'DELETE',
              headers: getHeaders()
          });
          if (!response.ok) {
               const errText = await response.text();
               throw new Error(errText || t('messages.crud.deleteFailed', { entity: 'module' }));
          }
          setSuccess(t('studio.moduleDelete.success'));
          const newList = objectList.filter(o => o !== moduleName);
          setObjectList(newList);
          setUnsavedModules(prev => prev.filter(m => m !== moduleName));
          setFavoriteModules(prev => prev.filter(m => m !== moduleName));
          if (newList.length > 0) {
              if (moduleName === objectCode) {
                  setObjectCode(newList[0]);
              }
          } else {
              setObjectCode(''); 
              setJsonSchema('');
              setParsedSchema(null);
          }
          setTimeout(() => setSuccess(''), 3000);
      } catch (err: any) {
          setError(err.message);
      }
  };

  const handleSaveDraft = async (silent: boolean = false) => {
    try {
        JSON.parse(jsonSchema); // Validate JSON
        const response = await fetchApi(`/api/platform/object/${objectCode}/draft`, {
            method: 'POST',
            headers: { ...getHeaders(), 'Content-Type': 'application/json' },
            body: jsonSchema
        });
        if (!response.ok) throw new Error('Failed to save draft');
        
        if (!silent) {
            setSuccess(t('messages.workflow.draftSaved'));
            setTimeout(() => setSuccess(''), 3000);
        }

        // Remove from unsaved list as it is now saved
        setUnsavedModules(prev => prev.filter(m => m !== objectCode));
        
        // Refresh list if it was new (not in list yet) or explicit save
        if (!silent || !objectList.includes(objectCode)) {
            fetchObjectList(); 
        }
    } catch (err: any) {
        if (!silent) {
            setError(t('messages.crud.saveFailed', { entity: objectCode || 'screen' }));
        }
    }
  };

  // Auto-Save Effect
  useEffect(() => {
    if (!autoSaveEnabled || !jsonSchema) return;

    const timer = setTimeout(() => {
        handleSaveDraft(true);
    }, 2000);

    return () => clearTimeout(timer);
  }, [jsonSchema, autoSaveEnabled]);

  const executePublish = async () => {
      try {
        setConfirmationModal(prev => ({ ...prev, isOpen: false }));

        if (!objectCode) {
            throw new Error(t('studio.publish.noModuleSelected'));
        }

        let tenantId = envInfo?.tenantId || '';
        let environment = envInfo?.environment || '';

        if ((!tenantId || !environment) && typeof window !== 'undefined') {
            try {
                const raw = window.localStorage.getItem('valora_context');
                if (raw) {
                    const ctx = JSON.parse(raw);
                    tenantId = tenantId || ctx.tenantId || '';
                    environment = environment || ctx.environment || '';
                }
            } catch {
            }
        }

        const fromEnv = environment;
        const toEnv = publishTargetEnv;

        if (!tenantId || !fromEnv || !toEnv) {
            throw new Error(t('studio.publish.envInfoMissing'));
        }

        setError('');
        setSuccess('');

        const response = await fetchApi('/studio/screens/publish', {
            method: 'POST',
            headers: { ...getHeaders(), 'Content-Type': 'application/json' },
            body: JSON.stringify({
                TenantId: tenantId,
                ObjectCode: objectCode,
                FromEnv: fromEnv,
                ToEnv: toEnv
            })
        });

        if (!response.ok) {
            try {
                await unwrapResult(response);
            } catch (e: any) {
                throw new Error(e.message || t('studio.publish.genericError'));
            }
        }
        
        setSuccess(t('messages.workflow.publishSuccess'));
        setIsPublished(true);
        setUnsavedModules(prev => prev.filter(m => m !== objectCode));
        setTimeout(() => setSuccess(''), 3000);
      } catch (err: any) {
          setError(err.message || t('messages.workflow.publishFailed'));
      }
  };

  const handlePublish = () => {
      if (!objectCode) {
          setError(t('studio.publish.noModuleSelected'));
          return;
      }

      const fromEnv = envInfo?.environment;
      const toEnv = publishTargetEnv;

      if (!fromEnv) {
          setError(t('studio.publish.envInfoMissing'));
          return;
      }

      if (!toEnv) {
          setError(t('studio.publish.selectTargetEnv'));
          return;
      }

      setConfirmationModal({
          isOpen: true,
          title: t('studio.publish.confirmTitle'),
          message: t('studio.publish.confirmMessage', { module: objectCode, fromEnv, toEnv }),
          type: 'info',
          confirmText: t('studio.publish.confirmAction'),
          onConfirm: () => executePublish()
      });
  };

  const openUniqueConstraintsModal = () => {
      if (parsedSchema) {
          setTempConstraints(parsedSchema.uniqueConstraints || []);
          setSelectedConstraintFields([]);
          setIsUniqueConstraintsModalOpen(true);
      }
  };

  const saveUniqueConstraints = () => {
      try {
          const current = JSON.parse(jsonSchema);
          current.uniqueConstraints = tempConstraints;
          const newJson = JSON.stringify(current, null, 2);
          setJsonSchema(newJson);
          setParsedSchema(normalizeSchema(current));
          setIsUniqueConstraintsModalOpen(false);
          setSuccess(t('messages.success.processed'));
          setTimeout(() => setSuccess(''), 3000);
      } catch (err) {
          setError(t('messages.network.unknownError'));
      }
  };

  const addConstraint = () => {
      if (selectedConstraintFields.length > 0) {
          setTempConstraints([...tempConstraints, selectedConstraintFields]);
          setSelectedConstraintFields([]);
      }
  };

  const removeConstraint = (index: number) => {
      const newConstraints = [...tempConstraints];
      newConstraints.splice(index, 1);
      setTempConstraints(newConstraints);
  };

  const toggleFieldSelection = (field: string) => {
      if (selectedConstraintFields.includes(field)) {
          setSelectedConstraintFields(selectedConstraintFields.filter(f => f !== field));
      } else {
          setSelectedConstraintFields([...selectedConstraintFields, field]);
      }
  };

  return (
    <div className={`flex h-full ${global.bgSecondary}`}>
      
      {/* BULK CLONE MODAL */}
      {isBulkCloneModalOpen && (
          <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm">
              <div className={`${global.bg} rounded-xl shadow-2xl w-[600px] h-[600px] flex flex-col overflow-hidden ${global.border} border`}>
                  <div className={`p-4 border-b ${global.border} flex justify-between items-center ${currentTheme.light}`}>
                      <h3 className={`font-bold ${global.text} text-lg flex items-center gap-2`}>
                          <Copy size={20} className={currentTheme.icon}/> {t('studio.bulkClone.title')}
                      </h3>
                      <button onClick={() => setIsBulkCloneModalOpen(false)} className={`${global.textSecondary} hover:${global.text}`}>
                          <X size={20} />
                      </button>
                  </div>
                  
                  <div className="p-6 flex-1 overflow-y-auto space-y-6">
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className={`block text-sm font-medium ${global.text} mb-1`}>{t('studio.bulkClone.sourceEnv')}</label>
                            <select 
                                className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} ${currentTheme.focusBorder} outline-none transition-all ${global.inputBg} ${global.text}`}
                                value={bulkCloneSourceEnv}
                                onChange={e => {
                                    const newEnv = e.target.value;
                                    setBulkCloneSourceEnv(newEnv);
                                    fetchBulkCloneModuleList(newEnv);
                                }}
                            >
                                <option value="PROD">PROD (Published Only)</option>
                                <option value="PREVIEW">PREVIEW</option>
                                <option value="TEST">TEST</option>
                            </select>
                        </div>
                        <div>
                            <label className={`block text-sm font-medium ${global.text} mb-1`}>{t('studio.bulkClone.targetEnv')}</label>
                            <select 
                                className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} ${currentTheme.focusBorder} outline-none transition-all ${global.inputBg} ${global.text}`}
                                value={bulkCloneTargetEnv}
                                onChange={e => setBulkCloneTargetEnv(e.target.value)}
                                disabled={!bulkCloneSourceEnv}
                            >
                                <option value="TEST">TEST</option>
                                <option value="DEV">DEV</option>
                            </select>
                            {envInfo?.environment && bulkCloneTargetEnv !== envInfo.environment && (
                                <p className="text-xs text-orange-600 mt-1">
                                    {t('studio.bulkClone.envWarning', { env: envInfo.environment })}
                                </p>
                            )}
                        </div>
                      </div>

                      <div>
                          <div className="flex justify-between items-center mb-2">
                              <label className={`block text-sm font-medium ${global.text}`}>{t('studio.bulkClone.selectModules')}</label>
                              <div className="space-x-2">
                                  <button 
                                      onClick={() => setSelectedModules([...bulkCloneModuleList])}
                                      className={`text-xs ${currentTheme.text} hover:underline`}
                                  >
                                      {t('studio.bulkClone.selectAll')}
                                  </button>
                                  <button 
                                      onClick={() => setSelectedModules([])}
                                      className={`text-xs ${global.textSecondary} hover:underline`}
                                  >
                                      {t('studio.bulkClone.clear')}
                                  </button>
                              </div>
                          </div>
                          <div className={`border ${global.border} rounded-lg max-h-64 overflow-y-auto ${global.bgSecondary} p-2`}>
                              {isFetchingModules ? (
                                  <div className="flex justify-center p-4">
                                      <Loader className={`animate-spin ${global.textSecondary}`} size={24} />
                                  </div>
                              ) : (
                                  <>
                                      {bulkCloneModuleList.length === 0 && (
                                          <p className={`text-sm ${global.textSecondary} p-2`}>{t('common.noRecords')}</p>
                                      )}
                                      {bulkCloneModuleList.map(mod => (
                                          <label key={mod} className={`flex items-center gap-2 p-2 hover:${global.bg} rounded cursor-pointer`}>
                                              <input 
                                                  type="checkbox" 
                                                  className={`rounded ${global.border} ${currentTheme.text} focus:ring-2 ${currentTheme.ring}`}
                                                  checked={selectedModules.includes(mod)}
                                                  onChange={e => {
                                                      if (e.target.checked) {
                                                          setSelectedModules([...selectedModules, mod]);
                                                      } else {
                                                          setSelectedModules(selectedModules.filter(m => m !== mod));
                                                      }
                                                  }}
                                              />
                                              <span className={`text-sm ${global.text}`}>{mod}</span>
                                          </label>
                                      ))}
                                  </>
                              )}
                          </div>
                          <p className={`text-xs ${global.textSecondary} mt-2`}>
                              {t('studio.bulkClone.selectedCount', { count: String(selectedModules.length) })}
                          </p>
                      </div>

                      {bulkCloneStatus && (
                          <div className={`p-3 ${currentTheme.light} ${currentTheme.textDark} text-sm rounded border ${currentTheme.border}`}>
                              {bulkCloneStatus}
                          </div>
                      )}
                  </div>

                  <div className={`p-4 ${global.bgSecondary} border-t ${global.border} flex justify-end gap-2`}>
                      <button 
                          onClick={() => setIsBulkCloneModalOpen(false)}
                          className={`px-4 py-2 text-sm font-medium ${global.textSecondary} hover:${global.text}`}
                      >
                          {t('common.cancel')}
                      </button>
                      <button 
                          onClick={executeBulkClone}
                          disabled={selectedModules.length === 0 || !!bulkCloneStatus}
                          className={`px-4 py-2 text-sm font-bold text-white ${currentTheme.primary} rounded-lg ${currentTheme.hover} shadow-md transition-all disabled:opacity-50`}
                      >
                          {bulkCloneStatus ? (
                              <Loader size={16} className="animate-spin" />
                          ) : (
                              t('studio.bulkClone.startAction')
                          )}
                      </button>
                  </div>
              </div>
          </div>
      )}

      {/* CLONE MODAL - Removed duplicate */}


      {/* CONFIRMATION MODAL */}
      <Modal
          isOpen={confirmationModal.isOpen}
          onClose={() => setConfirmationModal(prev => ({ ...prev, isOpen: false }))}
          title={confirmationModal.title}
          message={confirmationModal.message}
          type={confirmationModal.type}
          onConfirm={confirmationModal.onConfirm}
          confirmText={confirmationModal.confirmText}
      />

      {/* NEW MODULE MODAL */}
      {isModalOpen && (
          <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm">
              <div className={`${global.bg} rounded-xl shadow-2xl w-[500px] overflow-hidden`}>
                  <div className={`p-4 border-b ${global.border} flex justify-between items-center ${currentTheme.light}`}>
                      <h3 className={`font-bold ${global.text} text-lg flex items-center gap-2`}>
                          <Box size={20} className={currentTheme.icon}/> {t('studio.newModule.title')}
                      </h3>
                      <button onClick={() => setIsModalOpen(false)} className={`${global.textSecondary} hover:${global.text}`}>
                          <X size={20} />
                      </button>
                  </div>
                  
                  <div className="p-6 space-y-4">
                      <div>
                          <label className={`block text-sm font-medium ${global.text} mb-1`}>{t('studio.newModule.nameLabel')}</label>
                          <input 
                              autoFocus
                              type="text" 
                              className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} ${currentTheme.focusBorder} outline-none transition-all ${global.inputBg} ${global.text}`}
                              placeholder={t('studio.newModule.namePlaceholder')}
                              value={newModuleName}
                              onChange={e => setNewModuleName(e.target.value)}
                          />
                      </div>
                      
                      <div>
                          <label className={`block text-sm font-medium ${global.text} mb-1`}>
                              {t('studio.newModule.descriptionLabel')}
                          </label>
                          <textarea 
                              className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} ${currentTheme.focusBorder} outline-none transition-all h-24 resize-none ${global.inputBg} ${global.text}`}
                              placeholder={t('studio.newModule.descriptionPlaceholder')}
                              value={newModuleDesc}
                              onChange={e => setNewModuleDesc(e.target.value)}
                          />
                      </div>
                  </div>

                  <div className={`p-4 ${global.bgSecondary} border-t ${global.border} flex justify-between items-center`}>
                      <button 
                          onClick={() => createModule(false)}
                          disabled={!newModuleName}
                          className={`px-4 py-2 text-sm font-medium ${global.textSecondary} hover:${global.text} disabled:opacity-50`}
                      >
                          {t('studio.newModule.createBlank')}
                      </button>
                      
                      <button 
                          onClick={() => createModule(true)}
                          disabled={!newModuleName || isGenerating}
                          className={`flex items-center gap-2 px-5 py-2 text-sm font-bold text-white bg-gradient-to-r ${currentTheme.gradientFrom} ${currentTheme.gradientTo} rounded-lg hover:opacity-90 shadow-md transition-all disabled:opacity-70 disabled:cursor-not-allowed`}
                      >
                          {isGenerating ? (
                              <>
                                  <Loader size={16} className="animate-spin" /> {t('studio.newModule.generating')}
                              </>
                          ) : (
                              <>
                                  <Sparkles size={16} /> {t('studio.newModule.suggestWithAi')}
                              </>
                          )}
                      </button>
                  </div>
              </div>
          </div>
      )}

      {/* CLONE MODULE MODAL */}
      <Modal
          isOpen={isCloneModalOpen}
          onClose={() => setIsCloneModalOpen(false)}
          title={t('studio.clone.title')}
          confirmText={t('studio.clone.confirm')}
          onConfirm={executeClone}
          confirmDisabled={!cloneModuleName}
          type="info"
      >
          <div className="py-4">
              <label className={`block text-sm font-medium ${global.text} mb-1`}>{t('studio.clone.newNameLabel')}</label>
              <input 
                  autoFocus
                  type="text" 
                  className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} ${currentTheme.focusBorder} outline-none transition-all ${global.inputBg} ${global.text}`}
                  placeholder={t('studio.clone.namePlaceholder')}
                  value={cloneModuleName}
                  onChange={e => setCloneModuleName(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && executeClone()}
              />
          </div>
      </Modal>

      {/* UNIQUE CONSTRAINTS MODAL */}
      <Modal
          isOpen={isUniqueConstraintsModalOpen}
          onClose={() => setIsUniqueConstraintsModalOpen(false)}
          title="Manage Unique Constraints"
          confirmText="Save Constraints"
          onConfirm={saveUniqueConstraints}
          type="info"
      >
          <div className="py-4 space-y-6">
              {/* Existing Constraints */}
              <div>
                  <h4 className={`text-sm font-bold ${global.text} mb-2`}>Existing Constraints</h4>
                  {tempConstraints.length === 0 ? (
                      <p className={`text-sm ${global.textSecondary} italic`}>No composite unique constraints defined.</p>
                  ) : (
                      <div className="space-y-2">
                          {tempConstraints.map((constraint, idx) => (
                              <div key={idx} className={`flex items-center justify-between p-2 rounded border ${global.border} ${global.bgSecondary}`}>
                                  <div className="flex gap-1 flex-wrap">
                                      {constraint.map(field => (
                                          <span key={field} className={`px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200`}>
                                              {field}
                                          </span>
                                      ))}
                                  </div>
                                  <button 
                                      onClick={() => removeConstraint(idx)}
                                      className="p-1 text-red-500 hover:bg-red-50 rounded"
                                  >
                                      <Trash2 size={16} />
                                  </button>
                              </div>
                          ))}
                      </div>
                  )}
              </div>

              {/* Add New Constraint */}
              <div className={`p-4 rounded border ${global.border} ${global.bgSecondary}`}>
                  <h4 className={`text-sm font-bold ${global.text} mb-2`}>Add New Composite Constraint</h4>
                  <p className={`text-xs ${global.textSecondary} mb-3`}>Select fields that must be unique when combined together.</p>
                  
                  <div className="grid grid-cols-2 gap-2 mb-4 max-h-40 overflow-y-auto">
                      {parsedSchema && parsedSchema.fields && Object.keys(parsedSchema.fields).map(fieldKey => (
                          <label key={fieldKey} className={`flex items-center gap-2 p-1 hover:${global.bg} rounded cursor-pointer`}>
                              <input 
                                  type="checkbox" 
                                  className={`rounded ${global.border}`}
                                  checked={selectedConstraintFields.includes(fieldKey)}
                                  onChange={() => toggleFieldSelection(fieldKey)}
                              />
                              <span className={`text-sm ${global.text}`}>{parsedSchema.fields[fieldKey].ui?.label || fieldKey} <span className="text-xs text-gray-400 font-mono">({fieldKey})</span></span>
                          </label>
                      ))}
                  </div>

                  <div className="flex justify-end">
                      <button 
                          onClick={addConstraint}
                          disabled={selectedConstraintFields.length === 0}
                          className={`px-3 py-1.5 text-xs font-bold text-white bg-blue-600 rounded hover:bg-blue-700 shadow-sm disabled:opacity-50`}
                      >
                          Add Constraint
                      </button>
                  </div>
              </div>
          </div>
      </Modal>

      {/* LEFT SIDEBAR LIST */}
      <div className={`w-64 ${global.bg} border-r ${global.border} flex flex-col shadow-sm z-10 transition-colors`}>
          <div className={`p-4 border-b ${global.border} flex flex-col gap-3 ${currentTheme.light}`}>
              <div className="flex justify-between items-center">
                <h2 className={`font-bold ${global.text} flex items-center gap-2`}>
                    <Box size={20} className={currentTheme.text}/> {t('nav.studio')}
                </h2>
                <div className="flex gap-1 relative">
                      <button 
                          onClick={openSettings}
                          className={`p-1.5 hover:${global.bgSecondary} rounded-md ${global.textSecondary} transition-colors`}
                          title={t('studio.ai.tooltip')}
                      >
                        <Settings size={18} />
                    </button>
                    <button 
                      onClick={handleCreateNew}
                      className={`p-1.5 hover:${global.bgSecondary} rounded-md ${currentTheme.text} transition-colors`}
                      title={t('common.create')}
                    >
                        <Plus size={20} />
                    </button>
                </div>
              </div>
              <button 
                  onClick={handleBulkCloneClick}
                  className={`w-full flex items-center justify-center gap-2 ${global.bg} border ${global.border} ${currentTheme.textDark} hover:${global.bgSecondary} px-3 py-1.5 rounded text-xs font-medium transition-colors shadow-sm`}
              >
                  <Copy size={14} /> {t('studio.bulkClone.sidebarButton')}
              </button>
          </div>
          <div className="flex-1 overflow-y-auto">
              {objectList.length === 0 && (
                  <div className={`p-4 ${global.textSecondary} text-sm text-center`}>{t('common.noRecords')}</div>
              )}
              {objectList.length > 0 && (
                  <div className="px-3 py-2 flex items-center justify-between text-[11px] uppercase tracking-wide">
                      <div className="flex items-center gap-1">
                          <span className={global.textSecondary}>{t('common.view')}</span>
                          <button
                              onClick={() => setModuleFilter('all')}
                              className={`px-2 py-0.5 rounded-full ${
                                  moduleFilter === 'all'
                                      ? `${currentTheme.light} ${currentTheme.textDark}`
                                      : `${global.bgSecondary} ${global.textSecondary}`
                              }`}
                              >
                              {t('common.all')}
                          </button>
                          <button
                              onClick={() => setModuleFilter('favorites')}
                              className={`px-2 py-0.5 rounded-full flex items-center gap-1 ${
                                  moduleFilter === 'favorites'
                                      ? `${currentTheme.light} ${currentTheme.textDark}`
                                      : `${global.bgSecondary} ${global.textSecondary}`
                              }`}
                              >
                              <Star size={10} /> {t('common.favorite') ?? 'Favourites'}
                          </button>
                      </div>
                  </div>
              )}
              {objectList.length > 0 && (() => {
                  const sorted = [...objectList].sort((a, b) => {
                      const af = favoriteModules.includes(a);
                      const bf = favoriteModules.includes(b);
                      if (af === bf) return a.localeCompare(b);
                      return af ? -1 : 1;
                  });
                  const filtered =
                      moduleFilter === 'favorites'
                          ? sorted.filter(m => favoriteModules.includes(m))
                          : sorted;
                  if (filtered.length === 0) {
                      return (
                          <div className={`px-4 py-2 text-xs ${global.textSecondary}`}>
                              {t('common.noRecords')}
                          </div>
                      );
                  }
                  return filtered.map(obj => (
                      <div key={obj} className="relative group">
                          <button 
                            onClick={() => setObjectCode(obj)}
                            className={`w-full px-4 py-3 cursor-pointer flex items-center justify-between text-sm transition-colors border-l-4 ${
                                objectCode === obj 
                                ? `${currentTheme.light} ${currentTheme.textDark} ${currentTheme.border} font-medium` 
                                : `${global.textSecondary} hover:${global.bgSecondary} border-transparent`
                            }`}
                          >
                              <div className="flex items-center gap-2">
                                  <button
                                      onClick={(e) => {
                                          e.stopPropagation();
                                          toggleFavoriteModule(obj);
                                      }}
                                      className="p-0.5 rounded text-yellow-500 hover:bg-yellow-100"
                                      title={favoriteModules.includes(obj) ? t('common.unpin') ?? 'Unpin' : t('common.pin') ?? 'Pin'}
                                  >
                                      <Star size={14} fill={favoriteModules.includes(obj) ? "currentColor" : "none"} />
                                  </button>
                                  <div className="flex items-center gap-2">
                                      <Layout size={16} />
                                      {obj}
                                  </div>
                              </div>
                              {objectCode === obj && <ChevronRight size={14} />}
                          </button>
                          <button
                            onClick={(e) => {
                                e.stopPropagation();
                                handleDelete(obj);
                            }}
                            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 rounded text-red-500 hover:bg-red-50 opacity-0 group-hover:opacity-100 transition-opacity"
                            title={t('common.delete')}
                          >
                              <Trash2 size={14} />
                          </button>
                      </div>
                  ));
              })()}
          </div>
          
          {/* Tenant/Env Footer - REMOVED since we have top toolbar */}
      </div>

      {/* SETTINGS MODAL */}
      {isSettingsOpen && (
          <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center backdrop-blur-sm">
              <div className={`${global.bg} rounded-xl shadow-2xl w-[900px] h-[600px] flex flex-col overflow-hidden`}>
                  <div className={`p-4 border-b ${global.border} flex justify-between items-center ${currentTheme.light}`}>
                      <h3 className={`font-bold ${global.text} text-lg flex items-center gap-2`}>
                          <Settings size={20} className={currentTheme.text}/> {t('studio.ai.settingsTitle')}
                      </h3>
                      <button onClick={() => setIsSettingsOpen(false)} className={`${global.textSecondary} hover:${global.text}`}>
                          <X size={20} />
                      </button>
                  </div>
                  
                  <div className="flex flex-1 overflow-hidden">
                      {/* Sidebar List */}
                      <div className={`w-64 ${global.bgSecondary} border-r ${global.border} flex flex-col`}>
                          <div className="flex-1 overflow-y-auto p-2 space-y-1">
                              {aiConfigs.map(config => (
                                  <button
                                      key={config.id}
                                      onClick={() => setSelectedConfig(config)}
                                      className={`w-full text-left px-3 py-2 rounded-lg text-sm flex items-center justify-between group ${
                                          selectedConfig?.id === config.id 
                                          ? `${global.bg} shadow-sm ring-1 ${global.border}` 
                                          : `hover:${global.bg}`
                                      }`}
                                  >
                                      <div className={`truncate font-medium ${global.text}`}>
                                          {config.name}
                                          <div className={`text-xs ${global.textSecondary} font-normal truncate`}>{config.provider}</div>
                                      </div>
                                      {config.isActive && <div className="h-2 w-2 rounded-full bg-green-500 shadow-sm shadow-green-200" title={t('common.active')}></div>}
                                  </button>
                              ))}
                          </div>
                          <div className={`p-3 border-t ${global.border}`}>
                              <button 
                                  onClick={createNewConfig}
                                  className={`w-full flex items-center justify-center gap-2 px-3 py-2 ${global.bg} border ${global.border} rounded-lg text-sm font-medium ${global.text} hover:${global.bgSecondary} transition-colors`}
                              >
                                  <Plus size={16} /> Add Configuration
                              </button>
                          </div>
                      </div>

                      {/* Detail View */}
                      <div className={`flex-1 ${global.bg} p-6 overflow-y-auto`}>
                          {selectedConfig ? (
                              <div className="space-y-5">
                                  <div className="flex justify-between items-start">
                                      <div>
                                          <label className={`block text-xs font-bold ${global.textSecondary} uppercase tracking-wider mb-1`}>Configuration Name</label>
                                          <input 
                                              type="text" 
                                              className={`text-xl font-bold ${global.text} border-b border-transparent hover:${global.border} ${currentTheme.focusBorder} outline-none bg-transparent w-full`}
                                              value={selectedConfig.name}
                                              onChange={e => setSelectedConfig({...selectedConfig, name: e.target.value})}
                                              placeholder="My Configuration"
                                          />
                                      </div>
                                      {selectedConfig.id && (
                                          <div className="flex gap-2">
                                              {!selectedConfig.isActive && (
                                                  <button 
                                                      onClick={() => activateConfig(selectedConfig.id)}
                                                      className={`flex items-center gap-1 px-3 py-1.5 text-xs font-medium ${global.text} ${global.bgSecondary} hover:${global.bg} border ${global.border} rounded-md transition-colors`}
                                                  >
                                                      <Check size={14} /> Set Active
                                                  </button>
                                              )}
                                              {selectedConfig.isActive && (
                                      <span className="flex items-center gap-1 px-3 py-1.5 text-xs font-bold text-green-700 bg-green-50 rounded-md border border-green-100">
                                                      <Check size={14} /> Active
                                                  </span>
                                              )}
                                              <button 
                                                  onClick={() => deleteConfig(selectedConfig.id)}
                                                  className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded-md transition-colors"
                                                  title={t('common.delete')}
                                              >
                                                  <Trash2 size={16} />
                                              </button>
                                          </div>
                                      )}
                                  </div>

                                  <div className="grid grid-cols-2 gap-4">
                                      <div className="col-span-2">
                                          <label className={`block text-sm font-medium ${global.text} mb-1`}>AI Provider</label>
                                          <select 
                                              className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg outline-none ${global.inputBg} ${global.text} focus:ring-2 ${currentTheme.ring}`}
                                              value={selectedConfig.provider}
                                              onChange={handleProviderChange}
                                          >
                                              <option value="OpenAI">OpenAI</option>
                                              <option value="Groq">Groq</option>
                                              <option value="DeepSeek">DeepSeek</option>
                                              <option value="Local (Ollama)">Local (Ollama)</option>
                                              <option value="Local (LM Studio)">Local (LM Studio)</option>
                                              <option value="Custom">Custom (Other OpenAI Compatible)</option>
                                          </select>
                                      </div>

                                      <div className="col-span-2">
                                          <label className={`block text-sm font-medium ${global.text} mb-1`}>Base URL</label>
                                          <div className="relative">
                                              <Server size={16} className={`absolute left-3 top-3 ${global.textSecondary}`} />
                                              <input 
                                                  type="text" 
                                                  className={`w-full pl-9 pr-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} outline-none font-mono text-sm ${global.inputBg} ${global.text}`}
                                                  placeholder="https://api.openai.com/v1"
                                                  value={selectedConfig.baseUrl}
                                                  onChange={e => setSelectedConfig({...selectedConfig, baseUrl: e.target.value})}
                                              />
                                          </div>
                                          <p className={`text-xs ${global.textSecondary} mt-1`}>The API endpoint base URL.</p>
                                      </div>

                                      <div>
                                          <label className={`block text-sm font-medium ${global.text} mb-1`}>Model ID</label>
                                          <input 
                                              type="text" 
                                              className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} outline-none ${global.inputBg} ${global.text}`}
                                              placeholder="e.g. gpt-4o"
                                              value={selectedConfig.model}
                                              onChange={e => setSelectedConfig({...selectedConfig, model: e.target.value})}
                                          />
                                      </div>

                                      <div>
                                          <label className={`block text-sm font-medium ${global.text} mb-1`}>API Key</label>
                                          <input 
                                              type="password" 
                                              className={`w-full px-3 py-2 border ${global.inputBorder} rounded-lg focus:ring-2 ${currentTheme.ring} outline-none font-mono ${global.inputBg} ${global.text}`}
                                              placeholder="sk-..."
                                              value={selectedConfig.apiKey}
                                              onChange={e => setSelectedConfig({...selectedConfig, apiKey: e.target.value})}
                                          />
                                      </div>
                                  </div>

                                  <div className="pt-4 flex justify-end">
                                      <button 
                                          onClick={saveCurrentConfig}
                                          disabled={savingSettings || !selectedConfig.name}
                                          className={`flex items-center gap-2 px-6 py-2 text-sm font-bold text-white ${currentTheme.primary} rounded-lg ${currentTheme.hover} shadow-md transition-all disabled:opacity-70 disabled:cursor-not-allowed`}
                                      >
                                          {savingSettings ? <Loader size={16} className="animate-spin" /> : <Save size={16} />}
                                          Save Configuration
                                      </button>
                                  </div>
                              </div>
                          ) : (
                                <div className={`h-full flex flex-col items-center justify-center ${global.textSecondary}`}>
                                    <Settings size={48} className={`mb-4 ${global.textSecondary} opacity-50`} />
                                    <p>Select a configuration or create a new one.</p>
                                </div>
                            )}
                      </div>
                  </div>
              </div>
          </div>
      )}

      {/* MAIN CONTENT (Split View) */}
      <div className="flex-1 flex overflow-hidden">
          {/* Editor Area */}
        <div className={`w-1/2 flex flex-col border-r ${global.border} ${global.bg}`}>
            <div className={`p-4 border-b ${global.border} flex flex-col gap-3 shrink-0 ${currentTheme.light}`}>
                
                {/* Context Selection Toolbar */}
                <div className={`flex items-center gap-2 pb-3 mb-3 border-b ${currentTheme.border}`}>
                    <div className="flex items-center gap-2 flex-1">
                         <Database size={14} className={currentTheme.text} />
                         <span className={`text-xs font-bold uppercase tracking-wider ${global.textSecondary}`}>Context:</span>
                         
                         {/* Tenant Selector */}
                         <div className="w-48">
                             <SearchableSelect 
                                 value={studioTenantId}
                                 onChange={setStudioTenantId}
                                 options={availableTenants}
                                 placeholder="Select Tenant"
                             />
                         </div>

                         <span className={global.textSecondary}>/</span>

                         {/* Env Selector */}
                         <div className="relative">
                             <select 
                                 value={studioEnv}
                                 onChange={(e) => setStudioEnv(e.target.value)}
                                 className={`appearance-none pl-2 pr-8 py-1.5 text-xs font-bold rounded border ${global.inputBorder} ${global.inputBg} ${global.text} focus:ring-1 ${currentTheme.ring} outline-none uppercase cursor-pointer`}
                             >
                                 <option value="dev">DEV</option>
                                 <option value="test">TEST</option>
                                 <option value="preview">PREVIEW</option>
                                 <option value="prod">PROD</option>
                             </select>
                             <ChevronDown size={12} className={`absolute right-2 top-1/2 -translate-y-1/2 ${global.textSecondary} pointer-events-none`} />
                         </div>
                    </div>
                    
                    <div className={`text-[10px] ${global.textSecondary}`}>
                        Editing: <span className="font-mono">{studioTenantId}</span> / <span className="uppercase">{studioEnv}</span>
                    </div>
                </div>

                <div className="flex items-center gap-4 min-w-0">
                     <div className="flex items-center gap-2 min-w-0">
                        <span className={`text-sm ${global.textSecondary} font-mono shrink-0`}>Module:</span>
                        <h2 className={`text-lg font-bold ${global.text} truncate`} title={objectCode}>{objectCode}</h2>
                     </div>
                     
                     <div className={`h-6 w-px ${global.border} mx-2 shrink-0`}></div>
                     
                     <div className="flex items-center gap-2 shrink-0">
                        <span className={`text-xs ${global.textSecondary} font-medium uppercase tracking-wider`}>Storage:</span>
                        <select 
                            value={parsedSchema?.objectType || parsedSchema?.ObjectType || 'Master'}
                            onChange={(e) => {
                                const newType = e.target.value;
                                try {
                                    const current = JSON.parse(jsonSchema);
                                    current.objectType = newType;
                                    // Sync legacy property if it exists
                                    if (current.hasOwnProperty('ObjectType')) {
                                        current.ObjectType = newType;
                                    }
                                    
                                    const newJson = JSON.stringify(current, null, 2);
                                    setJsonSchema(newJson);
                                    setParsedSchema(normalizeSchema(current));
                                } catch (err) {
                                    console.error("Failed to update object type", err);
                                }
                            }}
                            className={`text-xs font-medium ${global.text} ${global.inputBg} border ${global.inputBorder} rounded px-2 py-1 outline-none ${currentTheme.focusBorder} focus:ring-1 ${currentTheme.ring}`}
                        >
                            <option value="Master">Master Data</option>
                            <option value="Transaction">Transaction</option>
                        </select>
                     </div>

                     <div className={`h-6 w-px ${global.border} mx-2 shrink-0`}></div>

                     {versions.length > 0 && (
                         <div className="flex items-center gap-2 shrink-0">
                             <span className={`text-xs ${global.textSecondary} font-medium uppercase tracking-wider`}>Version:</span>
                             <select
                                 value={selectedVersion ?? ''}
                                 onChange={e => {
                                     const value = e.target.value;
                                     if (!value) {
                                         return;
                                     }
                                     const v = parseInt(value, 10);
                                     if (Number.isNaN(v)) {
                                         return;
                                     }
                                     setSelectedVersion(v);
                                     fetchSchema(v);
                                 }}
                                 className={`text-xs font-medium ${global.text} ${global.inputBg} border ${global.inputBorder} rounded px-2 py-1 outline-none ${currentTheme.focusBorder} focus:ring-1 ${currentTheme.ring}`}
                             >
                                 <option value="">Select</option>
                                 {versions.map(v => (
                                     <option key={v.version} value={v.version}>
                                         v{v.version}{v.isPublished ? ' (Published)' : ''}
                                     </option>
                                 ))}
                             </select>
                         </div>
                     )}
                </div>
                
                <div className="flex flex-wrap gap-2 items-center justify-start w-full shrink-0">
                    <button
                        onClick={() => setAutoSaveEnabled(!autoSaveEnabled)}
                        className={`px-3 py-1.5 text-xs font-medium border rounded shadow-sm transition-colors ${
                            autoSaveEnabled 
                            ? 'bg-green-100 text-green-700 border-green-200 hover:bg-green-200' 
                            : `${global.bg} ${global.text} ${global.border} hover:${global.bgSecondary}`
                        }`}
                        title={autoSaveEnabled ? "Auto Save is ON" : "Auto Save is OFF"}
                    >
                        Auto Save: {autoSaveEnabled ? 'ON' : 'OFF'}
                    </button>

                    {envInfo?.environment !== 'PROD' && (
                        <button onClick={handleCloneClick} className={`px-3 py-1.5 text-xs font-medium ${global.bg} border ${currentTheme.border} ${currentTheme.text} rounded hover:${global.bgSecondary} shadow-sm flex items-center gap-1`} title="Clone Module">
                            <Copy size={14} /> Clone
                        </button>
                    )}
                    <button 
                        onClick={openUniqueConstraintsModal}
                        className={`px-3 py-1.5 text-xs font-medium ${global.bg} border ${currentTheme.border} ${currentTheme.text} rounded hover:${global.bgSecondary} shadow-sm flex items-center gap-1`}
                        title="Manage Unique Constraints"
                    >
                        Unique Constraints
                    </button>
                    <button onClick={() => fetchSchema()} className="px-3 py-1.5 text-xs font-medium text-white bg-teal-600 rounded hover:bg-teal-700 shadow-sm">Reload</button>
                    
                    {isPublished ? (
                        envInfo?.environment !== 'PROD' && (
                            <button onClick={handleUnpublish} className="px-3 py-1.5 text-xs font-medium text-white bg-orange-500 rounded hover:bg-orange-600 shadow-sm" title="Unpublish Module">Unpublish</button>
                        )
                    ) : (
                        <button onClick={() => handleDelete()} className="px-3 py-1.5 text-xs font-medium text-white bg-red-600 rounded hover:bg-red-700 shadow-sm" title="Delete Module">Delete</button>
                    )}
                    
                    <button onClick={() => handleSaveDraft(false)} className={`px-3 py-1.5 text-xs font-medium text-white ${currentTheme.primary} rounded ${currentTheme.hover} shadow-sm`}>Save Draft</button>
                    
                    {(() => {
                        const order = ['DEV', 'TEST', 'PREVIEW', 'PROD'];
                        const fromEnv = envInfo?.environment ? envInfo.environment.toUpperCase() : '';
                        const fromIndex = order.indexOf(fromEnv);
                        const allowedTargets = fromIndex === -1
                            ? []
                            : order.filter(target => {
                                  const idx = order.indexOf(target);
                                  if (idx <= fromIndex) return false;
                                  if (target === 'PROD' && fromEnv !== 'PREVIEW') return false;
                                  return true;
                              });

                        const hasTargets = allowedTargets.length > 0;

                        return (
                            <select
                                value={publishTargetEnv || ''}
                                onChange={e => setPublishTargetEnv(e.target.value)}
                                disabled={!envInfo?.environment || !hasTargets}
                                className={`text-xs font-medium ${global.text} ${global.inputBg} border ${global.inputBorder} rounded px-2 py-1 outline-none ${currentTheme.focusBorder} focus:ring-1 ${currentTheme.ring}`}
                            >
                                {!hasTargets && <option value="">{t('studio.publish.noTargetEnv')}</option>}
                                {hasTargets && <option value="">{t('studio.publish.selectTargetEnvPlaceholder')}</option>}
                                {hasTargets &&
                                    allowedTargets.map(target => (
                                        <option key={target} value={target}>
                                            {target}
                                        </option>
                                    ))}
                            </select>
                        );
                    })()}

                    <button
                        onClick={handlePublish}
                        disabled={!envInfo?.environment || !publishTargetEnv}
                        className="px-3 py-1.5 text-xs font-medium text-white bg-green-600 rounded hover:bg-green-700 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        {envInfo?.environment
                            ? publishTargetEnv
                                ? `Publish to ${publishTargetEnv}`
                                : `Publish from ${envInfo.environment}`
                            : 'Publish'}
                    </button>
                </div>
            </div>
            
            {envInfo?.environment === 'PROD' && (
                <div className="m-4 mb-0 p-3 text-sm bg-amber-50 text-amber-800 border border-amber-200 rounded flex items-center gap-2">
                    <span className="font-bold">Warning:</span> You are currently connected to PROD. This action will save to PROD.
                </div>
            )}
            {error && <div className="m-4 mb-0 p-3 text-sm bg-red-50 text-red-700 border border-red-200 rounded flex items-center gap-2">⚠️ {error}</div>}
            {success && <div className="m-4 mb-0 p-3 text-sm bg-green-50 text-green-700 border border-green-200 rounded flex items-center gap-2">✅ {success}</div>}

            <div className="flex-1 p-0 relative">
                <Editor
                  height="100%"
                  defaultLanguage="json"
                  value={jsonSchema}
                  onChange={(val) => handleJsonChange(val || '')}
                  theme={isDark ? 'vs-dark' : 'vs-light'}
                  options={{
                    minimap: { enabled: false },
                    fontSize: 13,
                    scrollBeyondLastLine: false,
                    wordWrap: 'on',
                    automaticLayout: true,
                    tabSize: 2
                  }}
                />
            </div>
          </div>

          {/* Preview Area */}
          <div className={`w-1/2 ${global.bgSecondary} flex flex-col`}>
            <div className={`p-4 border-b ${global.border} flex justify-between items-center shadow-sm z-10 ${currentTheme.light}`}>
                <h2 className={`font-semibold ${global.text} flex items-center gap-2`}>
                    Live Preview
                </h2>
                {isPublished ? (
                    <span className="px-2 py-0.5 bg-green-100 text-green-800 text-xs rounded-full font-medium border border-green-200 dark:bg-green-900/30 dark:text-green-300 dark:border-green-800">Published</span>
                ) : (
                    <span className="px-2 py-0.5 bg-yellow-100 text-yellow-800 text-xs rounded-full font-medium border border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300 dark:border-yellow-800">Draft Mode</span>
                )}
            </div>
            
            <div className="flex-1 overflow-y-auto p-8">
                <div className={`max-w-2xl mx-auto ${global.bg} p-8 rounded-xl shadow-sm border ${global.border}`}>
                  {parsedSchema ? (
                    <DynamicForm 
                        schema={parsedSchema}
                        onSubmit={(data) => setConfirmationModal({
                            isOpen: true,
                            title: "Form Data Preview",
                            message: (
                                <pre className={`${global.bgSecondary} p-4 rounded text-xs font-mono overflow-auto max-h-96 ${global.text}`}>
                                    {JSON.stringify(data, null, 2)}
                                </pre>
                            ),
                            type: 'info',
                            onConfirm: undefined
                        })} 
                    />
                  ) : (
                    <div className={`${global.textSecondary} text-center py-20 flex flex-col items-center gap-4`}>
                        <div className={`w-12 h-12 rounded-full ${global.bgSecondary} flex items-center justify-center`}>
                            <Layout size={24} className="opacity-50"/>
                        </div>
                        <p>{loading ? 'Loading schema...' : 'Invalid JSON Schema'}</p>
                    </div>
                  )}
                </div>
            </div>
          </div>
      </div>
    </div>
  );
};

export default PlatformStudio;
