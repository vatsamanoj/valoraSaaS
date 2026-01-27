import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate, useLocation } from 'react-router-dom';
import DynamicForm from './DynamicForm';
import Modal from './Modal';
import { ModuleSchema } from '../types/schema';
import { ArrowLeft } from 'lucide-react';
import { logger } from '../services/logger';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult, ApiResultError } from '../utils/api';
import { normalizeSchema } from '../utils/schemaUtils';

interface GenericObjectFormProps {
  objectCode?: string;
}

const GenericObjectForm: React.FC<GenericObjectFormProps> = ({ objectCode: propObjectCode }) => {
  const { global, styles, t } = useTheme();
  const { objectCode: paramObjectCode, id } = useParams<{ objectCode: string; id: string }>();
  const objectCode = propObjectCode || paramObjectCode;
  const navigate = useNavigate();
  const location = useLocation();
  
  const isCreateMode = !id;
  const isEditRoute = location.pathname.endsWith('/edit');

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [schema, setSchema] = useState<ModuleSchema | null>(null);
  const [initialData, setInitialData] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [allowedActions, setAllowedActions] = useState<string[]>([]);
  
  // Modal State
  const [modalConfig, setModalConfig] = useState<{
    isOpen: boolean;
    title: string;
    message: React.ReactNode;
    type: 'success' | 'error' | 'info' | 'warning';
    autoCloseDuration?: number;
  }>({
    isOpen: false,
    title: '',
    message: '',
    type: 'info'
  });

  useEffect(() => {
    // Reset state when objectCode changes
    setSchema(null);
    setInitialData(null);
    setIsLoading(true);
    setError(null);
    
    if (!objectCode) return;

    const loadData = async () => {
      try {
        let headers: Record<string, string> = {};
        if (typeof window !== 'undefined') {
            try {
                const raw = window.localStorage.getItem('valora_headers');
                if (raw) headers = JSON.parse(raw);
            } catch {}
        }

        const schemaRes = await fetchApi(`/api/platform/object/${objectCode}/latest`, { headers });
        const body = await unwrapResult<any>(schemaRes); // Schema is now wrapped or direct JSON

        let tenantId = '';
        if (typeof window !== 'undefined') {
          try {
            const ctxRaw = window.localStorage.getItem('valora_context');
            if (ctxRaw) {
              const ctx = JSON.parse(ctxRaw);
              tenantId = ctx.tenantId || '';
            }
          } catch {
          }
        }

        const schemaData = normalizeSchema({
          tenantId: tenantId || 'UNKNOWN',
          module: objectCode,
          version: body.version || 1,
          objectType: body.objectType || 'Master',
          fields: body.fields || {},
          uniqueConstraints: body.uniqueConstraints,
          ui: body.ui
        });

        setSchema(schemaData);

        // Fetch Data if not create mode
        if (id) {
            const queryRes = await fetchApi('/api/query/ExecuteQuery', {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    Module: objectCode,
                    Options: {
                        Filters: { Id: id }
                    }
                })
            });

            const queryData = await unwrapResult<any>(queryRes);
            
            if (queryData && queryData.data && queryData.data.length > 0) {
                const rawData = queryData.data[0];
                // Normalize keys to match schema fields (case-insensitive)
                const normalizedData: any = { ...rawData };
                
                if (schemaData && schemaData.fields) {
                    Object.keys(schemaData.fields).forEach(schemaField => {
                         const foundKey = Object.keys(rawData).find(k => k.toLowerCase() === schemaField.toLowerCase());
                         if (foundKey) {
                             normalizedData[schemaField] = rawData[foundKey];
                         }
                    });
                }
                setInitialData(normalizedData);
            } else {
                throw new Error("Record not found");
            }
        }

      } catch (err: any) {
        console.error("Error loading form:", err);
        setError(err.message || "An error occurred");
      } finally {
        setIsLoading(false);
      }
    };

    loadData();
  }, [objectCode, id]);

  useEffect(() => {
    if (!objectCode) return;

    try {
      if (typeof window === 'undefined') return;
      const raw = window.localStorage.getItem('valora_modules');
      if (!raw) return;
      const modules = JSON.parse(raw) as Array<{ objectCode?: string; actions?: string[] }>;
      const match = modules.find(m => m.objectCode && m.objectCode.toLowerCase() === objectCode.toLowerCase());
      if (match && Array.isArray(match.actions)) {
        setAllowedActions(match.actions);
      }
    } catch {
    }
  }, [objectCode]);

  const canCreate = allowedActions.includes('create');
  const canEdit = allowedActions.includes('edit');
  const canView = allowedActions.includes('view');

  const isEditMode = !!id && isEditRoute;
  const isViewMode = !!id && !isEditRoute;

  const handleSubmit = async (data: any) => {
    setIsSubmitting(true);
    try {
        if (isCreateMode && !canCreate) {
            throw new Error(t('common.noPermission') || 'You do not have permission to create records for this module.');
        }

        if (!isCreateMode && isEditMode && !canEdit) {
            throw new Error(t('common.noPermission') || 'You do not have permission to edit records for this module.');
        }

        const payload = {
            Action: isCreateMode ? 'Create' : 'Update',
            Data: {
                ...data,
                ...(id ? { Id: id } : {})
            }
        };

        const response = await fetchApi(`/api/data/${objectCode}`, {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        const entity = await unwrapResult<any>(response);
        const savedId = entity.Id || id;

        setModalConfig({
            isOpen: true,
            title: t('common.success'),
            message: `${objectCode} ${isCreateMode ? t('common.create') : t('common.update')} Successfully!`,
            type: 'success',
            autoCloseDuration: 500
        });

        // Redirect to List after short delay
        setTimeout(() => {
            navigate(`/app/${objectCode}`, { 
                state: { highlightedId: savedId } 
            });
        }, 500);
    } catch (err: any) {
        console.error(err);
        
        if (err instanceof ApiResultError || (err.errors && Array.isArray(err.errors))) {
            const uniqueErr = err.errors?.find((e: any) => e.code === 'UniqueViolation' || e.code === 'ValidationError');
            if (uniqueErr) {
                setModalConfig({
                    isOpen: true,
                    title: t('common.validationError'),
                    message: (
                        <div className="text-left">
                            <p className="font-medium mb-2 text-amber-800 dark:text-amber-200">
                                {t('common.uniqueViolation')}
                            </p>
                            <div className="bg-amber-50 dark:bg-amber-900/30 p-3 rounded border border-amber-200 dark:border-amber-700/50 mb-3">
                                <p className="text-sm text-amber-900 dark:text-amber-100 font-mono">
                                    {uniqueErr.message}
                                </p>
                            </div>
                            <p className="text-xs text-gray-500 dark:text-gray-400 italic">
                                {t('common.uniqueViolationHint')}
                            </p>
                        </div>
                    ),
                    type: 'warning'
                });
                setIsSubmitting(false);
                return;
            }
        }

        logger.error("Save Record Failure", err, { objectCode, isCreateMode, data });
        setModalConfig({
            isOpen: true,
            title: t('common.operationFailed'),
            message: err.message || t('common.error'),
            type: 'error'
        });
    } finally {
        setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className={`min-h-screen flex items-center justify-center ${global.bgSecondary}`}>
        <div className={`flex flex-col items-center gap-4`}>
           <div className={`w-8 h-8 border-4 border-t-transparent ${styles.border} rounded-full animate-spin`} />
           <span className={`${global.textSecondary} text-sm font-medium`}>{t('common.loading')}</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`p-8 ${global.text} flex flex-col items-center justify-center h-full`}>
         <div className="bg-red-50 dark:bg-red-900/20 p-4 rounded-full mb-4 text-red-500">
            <ArrowLeft size={32} />
         </div>
         <h2 className="text-xl font-bold mb-2">{t('common.error')}</h2>
         <p className={`${global.textSecondary} mb-6 text-center max-w-md`}>{error}</p>
         <Link to={`/app/${objectCode}`} className={`px-4 py-2 ${styles.primary} text-white rounded-lg hover:opacity-90`}>
           {t('common.backToList')}
         </Link>
      </div>
    );
  }

  if (!isCreateMode && isViewMode && !canView) {
    return (
      <div className={`p-8 ${global.text} flex flex-col items-center justify-center h-full`}>
         <h2 className="text-xl font-bold mb-2">{t('common.error')}</h2>
         <p className={`${global.textSecondary} mb-6 text-center max-w-md`}>
           {t('common.noPermission') || 'You do not have permission to view this record.'}
         </p>
         <Link to={`/app/${objectCode}`} className={`px-4 py-2 ${styles.primary} text-white rounded-lg hover:opacity-90`}>
           {t('common.backToList')}
         </Link>
      </div>
    );
  }

  return (
    <div className={`min-h-screen flex flex-col ${global.bgSecondary}`}>
      {/* Header */}
      <div className={`${global.bg} border-b ${global.border} px-4 py-3 sm:px-8 sm:py-4 sticky top-0 z-10 flex items-center justify-between shadow-sm`}>
        <div className="flex items-center gap-4">
          <Link 
            to={`/app/${objectCode}`} 
            className={`p-2 rounded-full hover:${global.bgSecondary} ${global.textSecondary} transition`}
            title={t('common.backToList')}
          >
            <ArrowLeft size={20} />
          </Link>
          <div>
            <h1 className={`text-lg sm:text-xl font-bold ${global.text}`}>
              {isCreateMode ? t('common.new', { module: objectCode || '' }) : `${t('common.edit')} ${objectCode}`}
            </h1>
            {!isCreateMode && (
                <div className={`flex items-center gap-2 text-xs font-mono ${global.textSecondary} mt-0.5`}>
                    <span>ID: {id}</span>
                </div>
            )}
          </div>
        </div>
      </div>

      <div className="flex-1 p-4 sm:p-8 max-w-5xl mx-auto w-full">
        {schema && (
          <DynamicForm 
            schema={schema} 
            initialData={initialData}
            onSubmit={handleSubmit}
            isLoading={isSubmitting}
            readOnly={isViewMode || (!isCreateMode && !canEdit)}
          />
        )}
      </div>

      <Modal
        isOpen={modalConfig.isOpen}
        onClose={() => setModalConfig(prev => ({ ...prev, isOpen: false }))}
        title={modalConfig.title}
        type={modalConfig.type}
        autoCloseDuration={modalConfig.autoCloseDuration}
      >
        {modalConfig.message}
      </Modal>
    </div>
  );
};

export default GenericObjectForm;
