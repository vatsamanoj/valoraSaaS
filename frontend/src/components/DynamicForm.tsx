import React, { useEffect, useState, useRef, useCallback } from 'react';
import { useForm, FieldValues, Controller } from 'react-hook-form';
import {
  ModuleSchema,
  FieldRule,
  LayoutSection,
  getSchemaFeatureSupport,
  isTempField,
  getFieldNameFromTemp,
  createTempFieldName,
  commitTempValues,
  SCHEMA_VERSIONS
} from '../types/schema';

export { DynamicForm };
import { Calendar, Phone, Mail, Activity, Paperclip, Upload, FileText, X, Calculator, Server } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';
import FormGrid from './FormGrid';
import SearchableSelect from './SearchableSelect';

interface DynamicFormProps {
  schema: ModuleSchema;
  onSubmit: (data: any) => void;
  isLoading?: boolean;
  initialData?: any;
  readOnly?: boolean;
}

const DynamicForm: React.FC<DynamicFormProps> = ({ schema, onSubmit, isLoading = false, initialData, readOnly = false }) => {
  const { global, styles: theme, isDark, t, currency } = useTheme();

  // Get feature support based on schema version
  const featureSupport = getSchemaFeatureSupport(schema.version);

  // Pre-process initialData for Date fields to ensure YYYY-MM-DD format
  // Also separate temp_ prefixed values from committed values
  const { formattedInitialData, tempValues } = React.useMemo(() => {
    if (!initialData || !schema?.fields) {
      return { formattedInitialData: initialData, tempValues: {} };
    }
    
    const copy: Record<string, any> = {};
    const temps: Record<string, any> = {};
    
    Object.entries(initialData).forEach(([key, value]) => {
      // Handle temp_ prefixed values
      if (isTempField(key)) {
        temps[key] = value;
        // Also set the actual field value if not present
        const actualField = getFieldNameFromTemp(key);
        if (initialData[actualField] === undefined) {
          copy[actualField] = value;
        }
      } else {
        // Check if this is a date field
        const rule = schema.fields[key];
        if (rule?.ui?.type?.toLowerCase() === 'date' && value && (typeof value === 'string' || typeof value === 'number')) {
          try {
            const d = new Date(value);
            if (!isNaN(d.getTime())) {
              copy[key] = d.toISOString().split('T')[0];
            } else {
              copy[key] = value;
            }
          } catch {
            copy[key] = value;
          }
        } else {
          copy[key] = value;
        }
      }
    });
    
    return { formattedInitialData: copy, tempValues: temps };
  }, [initialData, schema]);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    getValues,
    formState: { errors },
  } = useForm<FieldValues>({
    defaultValues: { ...formattedInitialData, ...tempValues },
  });

  const [gridSchemas, setGridSchemas] = useState<Record<string, ModuleSchema>>({});
  const [lookupData, setLookupData] = useState<Record<string, any[]>>({});
  const [attachments, setAttachments] = useState<File[]>([]);
  const [serverCalculatedTotals, setServerCalculatedTotals] = useState<Record<string, number>>({});
  const [tempValueState, setTempValueState] = useState<Record<string, any>>(tempValues);
  const [isCalculating, setIsCalculating] = useState(false);
  const formRef = useRef<HTMLFormElement | null>(null);

  const getNestedValue = (obj: any, path: string) => {
    return path.split('.').reduce((acc, part) => acc && acc[part], obj);
  };

  const getNestedError = (errors: any, path: string) => {
    return path.split('.').reduce((acc, part) => acc && acc[part], errors);
  };

  const handleNumericFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    e.target.select();
  };

  // ===== REMOVED: Client-Side Calculation Execution =====
  // All calculations are now server-side only for security
  // The executeClientSideScript function has been removed

  // ===== Server-Side Calculation Trigger =====
  const triggerServerSideCalculation = useCallback(async (changedField?: string) => {
    // Only trigger for V1 schemas with complex calculations enabled
    if (schema.version !== SCHEMA_VERSIONS.V1) return;
    if (!schema.calculationRules?.complexCalculation) return;
    
    setIsCalculating(true);
    try {
      const formData = getValues();
      
      // Prepare calculation request
      const calculationRequest = {
        module: schema.module,
        changedField,
        formData: commitTempValues(formData),
        tempValues: Object.entries(formData).reduce((acc, [key, value]) => {
          if (isTempField(key)) acc[key] = value;
          return acc;
        }, {} as Record<string, any>),
      };

      // Call server-side calculation endpoint
      const response = await fetchApi('/api/calculation/execute', {
        method: 'POST',
        body: JSON.stringify(calculationRequest),
      });

      const result = await unwrapResult<any>(response);
      
      // Update calculated values from server response
      if (result.calculatedValues) {
        Object.entries(result.calculatedValues).forEach(([field, value]) => {
          setValue(field, value, { shouldValidate: false });
        });
      }
      
      // Update document totals from server
      if (result.documentTotals) {
        setServerCalculatedTotals(result.documentTotals);
      }
      
      // Update temp values from server
      if (result.tempValues) {
        Object.entries(result.tempValues).forEach(([field, value]) => {
          setValue(field, value, { shouldValidate: false });
        });
        setTempValueState(prev => ({ ...prev, ...result.tempValues }));
      }
    } catch (err) {
      console.error('Server-side calculation error:', err);
    } finally {
      setIsCalculating(false);
    }
  }, [schema, getValues, setValue]);

  // ===== Temp Value Management =====
  const handleTempValueChange = useCallback((fieldName: string, value: any) => {
    const tempFieldName = createTempFieldName(fieldName);
    setValue(tempFieldName, value, { shouldValidate: false });
    setTempValueState(prev => ({ ...prev, [tempFieldName]: value }));
    
    // Trigger server-side calculation if V1 and complex calculations enabled
    if (schema.version === SCHEMA_VERSIONS.V1 && schema.calculationRules?.complexCalculation) {
      triggerServerSideCalculation(fieldName);
    }
  }, [setValue, schema, triggerServerSideCalculation]);

  const commitTempValuesOnSave = useCallback(() => {
    const formData = getValues();
    const committed = commitTempValues(formData);
    
    // Update form values without temp_ prefixes
    Object.entries(committed).forEach(([key, value]) => {
      if (formData[key] !== value) {
        setValue(key, value, { shouldValidate: true });
      }
    });
    
    return committed;
  }, [getValues, setValue]);

  // ===== Form Submission Handler =====
  const handleFormSubmit = useCallback((data: any) => {
    // Commit all temp values before submission
    const committedData = commitTempValuesOnSave();
    
    // No client-side onBeforeSave script execution - server handles all validations
    onSubmit(committedData);
  }, [commitTempValuesOnSave, onSubmit]);

  // ===== Document Totals Display (Server-Calculated Only) =====
  const values = watch();
  
  // Only fetch document totals from server for V1 and V2 schemas
  useEffect(() => {
    if (readOnly) return;
    if (!featureSupport.documentTotals) return;
    if (!schema.documentTotals?.fields) return;

    const fetchDocumentTotals = async () => {
      // For V1 with complex calculations, totals are fetched via triggerServerSideCalculation
      if (schema.version === SCHEMA_VERSIONS.V1 && schema.calculationRules?.complexCalculation) {
        return;
      }
      
      // For other versions, calculate simple aggregations client-side (no JS execution)
      // These are simple sums/averages, not script-based calculations
      const newTotals: Record<string, number> = {};
      
      Object.entries(schema.documentTotals!.fields).forEach(([fieldName, config]) => {
        let calculatedValue = config.defaultValue || 0;
        
        if (config.source) {
          // Simple source-based aggregation (no formula execution)
          const sourceValue = getNestedValue(values, config.source);
          if (Array.isArray(sourceValue)) {
            calculatedValue = sourceValue.reduce((sum, item) => {
              if (typeof item === 'number') return sum + item;
              if (typeof item === 'object' && item !== null) {
                // Sum a specific field from array items
                const fieldValue = parseFloat(item[config.source.split('.').pop() || ''] || 0);
                return sum + (isNaN(fieldValue) ? 0 : fieldValue);
              }
              return sum;
            }, 0);
          } else {
            calculatedValue = parseFloat(sourceValue) || config.defaultValue || 0;
          }
        }
        
        // Apply decimal places
        const decimals = config.decimalPlaces ?? 2;
        calculatedValue = parseFloat(calculatedValue.toFixed(decimals));
        
        newTotals[fieldName] = calculatedValue;
      });
      
      setServerCalculatedTotals(newTotals);
    };

    // Debounce the calculation
    const timeoutId = setTimeout(fetchDocumentTotals, 300);
    return () => clearTimeout(timeoutId);
  }, [values, schema, readOnly, featureSupport.documentTotals]);

  useEffect(() => {
    const loadLookups = async () => {
      let headers: Record<string, string> = {};
      if (typeof window !== 'undefined') {
          try {
              const raw = window.localStorage.getItem('valora_headers');
              if (raw) headers = JSON.parse(raw);
          } catch {}
      }

      if (!schema?.fields) return;

      for (const [fieldName, rule] of Object.entries(schema.fields)) {
        if (rule.ui?.type?.toLowerCase() === 'lookup' && (rule.ui as any).lookupModule) {
           try {
             const res = await fetchApi('/api/query/ExecuteQuery', {
               method: 'POST',
               headers: { ...headers, 'Content-Type': 'application/json' },
               body: JSON.stringify({ 
                 Module: (rule.ui as any).lookupModule,
                 Options: { 
                    PageSize: 1000,
                    Filters: (rule.ui as any).filter 
                 } 
               })
             });
             const result = await unwrapResult<any>(res);
             setLookupData(prev => ({ ...prev, [fieldName]: result.data || [] }));
           } catch (err) {
             console.error(`Failed to load lookup for ${fieldName}`, err);
           }
        }
      }
    };
    loadLookups();
  }, [schema.module]);

  useEffect(() => {
    const loadSequences = async () => {
      let headers: Record<string, string> = {};
      if (typeof window !== 'undefined') {
          try {
              const raw = window.localStorage.getItem('valora_headers');
              if (raw) headers = JSON.parse(raw);
          } catch {}
      }

      // Only fetch sequence if value is missing (Create mode)
      if (!schema?.fields) return;

      for (const [fieldName, rule] of Object.entries(schema.fields)) {
        if ((rule as any).autoGenerate && (rule as any).pattern) {
             // If field already has value (Edit mode or manually set), skip
             const currentVal = watch(fieldName);
             if (currentVal) continue;

             try {
                 const res = await fetchApi(`/api/sequence/preview/${schema.module}/${fieldName}`, { headers });
                 const data = await unwrapResult<any>(res);
                 setValue(fieldName, data.value);
             } catch (err) {
                 console.error(`Failed to load sequence for ${fieldName}`, err);
             }
        }
      }
    };
    loadSequences();
  }, [schema.module]); // Run once per schema load

  useEffect(() => {
    const loadGridSchemas = async () => {
      if (!schema.ui?.layout) return;
      
      const newGridSchemas: Record<string, ModuleSchema> = {};
      for (const section of schema.ui.layout) {
        if (section.type === 'grid' && section.gridConfig?.dataSource) {
          const ds = section.gridConfig.dataSource;
          if (gridSchemas[ds]) continue;
          
          // CHECK IF LOCAL FIELD FIRST
          // If the dataSource refers to a field within the current schema (e.g. "Items" which has inline columns),
          // use that instead of fetching from API.
          const localField = schema.fields[ds];
          if (localField && (localField as any).columns) {
              newGridSchemas[ds] = {
                  tenantId: schema.tenantId,
                  module: ds,
                  version: 1,
                  fields: (localField as any).columns, // Use the inline columns as the schema fields
                  ui: { layout: [] } 
              };
              continue;
          }

          try {
            let headers: Record<string, string> = {};
            if (typeof window !== 'undefined') {
                try {
                    const raw = window.localStorage.getItem('valora_headers');
                    if (raw) headers = JSON.parse(raw);
                } catch {}
            }

            const res = await fetchApi(`/api/platform/object/${ds}/latest`, { headers });
            const body = await unwrapResult<any>(res);

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

              newGridSchemas[ds] = {
                tenantId: tenantId || 'UNKNOWN',
                module: ds,
                version: body.version || 1,
                objectType: body.objectType,
                fields: body.fields || {},
                uniqueConstraints: body.uniqueConstraints,
                ui: body.ui
              };
          } catch (e) {
            console.error(`Failed to load schema for ${ds}`, e);
          }
        }
      }
      
      if (Object.keys(newGridSchemas).length > 0) {
        setGridSchemas(prev => ({ ...prev, ...newGridSchemas }));
      }
    };
    
    loadGridSchemas();
  }, [schema]); // Depend on entire schema to catch inline column changes

  const getFieldLabel = (field: string, rule: any) => {
    const moduleKey = `${schema.module}.fields.${field}`;
    const commonKey = `common.fields.${field}`;
    
    const moduleLabel = t(moduleKey);
    if (moduleLabel !== moduleKey) return moduleLabel;
    
    const commonLabel = t(commonKey);
    if (commonLabel !== commonKey) return commonLabel;
    
    return rule.ui?.label || field;
  };

  // ===== Render Calculation Rule Input =====
  const renderCalculationInput = (fieldName: string, rule: FieldRule, calculationConfig?: any) => {
    if (!calculationConfig) return null;
    
    const isComplex = calculationConfig.complexCalculation;
    const tempFieldName = createTempFieldName(fieldName);
    const currentValue = watch(tempFieldName) ?? watch(fieldName);
    
    return (
      <div className="relative">
        <input
          type="number"
          value={currentValue || ''}
          onChange={(e) => handleTempValueChange(fieldName, parseFloat(e.target.value) || 0)}
          className={`w-full px-4 py-2.5 border ${global.inputBorder} rounded-lg focus:ring-2 ${theme.ring} focus:border-transparent outline-none transition text-base sm:text-sm ${global.inputBg} ${global.text} pl-10`}
          placeholder={t('common.calculated')}
          disabled={readOnly || isCalculating}
        />
        <div className={`absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none ${global.textSecondary}`}>
          {isComplex ? <Server size={16} /> : <Calculator size={16} />}
        </div>
        {isCalculating && (
          <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
            <div className="animate-spin h-4 w-4 border-2 border-current border-t-transparent rounded-full" />
          </div>
        )}
      </div>
    );
  };

  const renderField = (fieldName: string, rule: FieldRule) => {
    const { ui, required, maxLength } = rule;
    const label = getFieldLabel(fieldName, rule);
    const placeholder = ui?.placeholder || t('common.enter', { field: label });
    const rawType = (ui?.type || 'text').toLowerCase();
    const inputType = rawType === 'money' ? 'currency' : rawType;

    // Check if this field has a calculation rule (V1 only)
    const calculationConfig = schema.version === SCHEMA_VERSIONS.V1 
      ? schema.calculationRules?.serverSide?.lineItemCalculations?.find(c => c.targetField === fieldName)
        || schema.calculationRules?.serverSide?.documentCalculations?.find(c => c.targetField === fieldName)
      : null;

    // Handle Grid/Array type fields (Inline rendering)
    if (inputType === 'grid' || (rule as any).columns) {
        let columns = (ui as any)?.columns || (ui?.gridConfig as any)?.columns;
        
        if (!columns && (rule as any).columns) {
             if (Array.isArray((rule as any).columns)) {
                 columns = (rule as any).columns;
             } else {
                 columns = Object.keys((rule as any).columns);
             }
        }

        const gridConfig = {
            ...(ui?.gridConfig || {}),
            dataSource: fieldName,
            // If columns are not explicitly listed in ui, use keys from columns definition
            columns: columns || [],
            allowAdd: (ui as any)?.allowAdd ?? true,
            allowDelete: (ui as any)?.allowDelete ?? true,
            allowEdit: (ui as any)?.allowEdit ?? true,
            footer: (ui as any)?.footer
        };

        // Ensure we have an array of columns
        if (!Array.isArray(gridConfig.columns) && (rule as any).columns) {
            gridConfig.columns = Object.keys((rule as any).columns);
        }

        return (
            <div key={fieldName} className="col-span-1 sm:col-span-2 mt-2 mb-4">
                 <div className="flex items-baseline justify-between mb-2">
                    <label className={`block text-sm font-medium ${global.text}`}>
                        {label}
                    </label>
                 </div>
                 <FormGrid
                    control={control}
                    register={register}
                    watch={watch}
                    setValue={setValue}
                    getValues={getValues}
                    name={fieldName}
                    config={gridConfig as any}
                    itemSchema={gridSchemas[fieldName]}
                    readOnly={readOnly}
                    enterKeyNavigation={schema.ui?.enterKeyNavigation}
                 />
            </div>
        );
    }

    const commonClasses = `w-full px-4 py-2.5 border ${global.inputBorder} rounded-lg focus:ring-2 ${theme.ring} focus:border-transparent outline-none transition text-base sm:text-sm ${readOnly ? `${global.bgSecondary} ${global.textSecondary} cursor-not-allowed` : `${global.inputBg} ${global.text}`}`;
    
    // Validation rules for react-hook-form
    const validationRules = {
      required: !readOnly && required ? t('common.required', { field: label }) : false,
      maxLength: maxLength ? { value: maxLength, message: t('common.maxLength', { length: maxLength.toString() }) } : undefined,
      valueAsNumber: inputType === 'number' || inputType === 'currency',
    };

    const renderInput = () => {
      // If field has calculation config and is complex, render calculation input
      if (calculationConfig && schema.version === SCHEMA_VERSIONS.V1) {
        return renderCalculationInput(fieldName, rule, calculationConfig);
      }

      switch (inputType) {
        case 'select':
          return (
            <div className="relative">
              <select
                {...register(fieldName, validationRules)}
                className={`${commonClasses} appearance-none`}
                disabled={readOnly}
                data-skip-focus={ui?.skipFocus || undefined}
              >
                <option value="">{t('common.select')} {label}</option>
                {ui?.options?.map((opt) => {
                  const optLabelKey = `${schema.module}.fields.${fieldName}.options.${opt}`;
                  const translated = t(optLabelKey);
                  const display = translated !== optLabelKey ? translated : opt;
                  return (
                    <option key={opt} value={opt}>
                      {display}
                    </option>
                  );
                })}
              </select>
              <div className="absolute inset-y-0 right-0 flex items-center px-2 pointer-events-none">
                <svg className={`w-4 h-4 ${global.textSecondary}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>
          );

        case 'textarea':
          return (
            <textarea
              {...register(fieldName, validationRules)}
              className={commonClasses}
              placeholder={placeholder}
              rows={3}
              disabled={readOnly}
              data-skip-focus={ui?.skipFocus || undefined}
            />
          );
        
        case 'lookup':
           const data = lookupData[fieldName] || [];
           const options = data.map(opt => ({
               value: (ui as any).lookupField ? opt[(ui as any).lookupField] : (opt.Name || opt.Id),
               label: opt.Name || opt.Code || opt.Id,
               subLabel: opt.Code || opt.Description
           }));

           return (
            <div className="relative">
              <Controller
                name={fieldName}
                control={control}
                rules={validationRules}
                render={({ field }) => (
                    <SearchableSelect
                        id={fieldName}
                        value={field.value}
                        onChange={field.onChange}
                        options={options}
                        placeholder={placeholder}
                        className="w-full"
                    />
                )}
              />
            </div>
           );

        default:
          return (
            <div className="relative">
              <input
                type={inputType === 'currency' ? 'number' : inputType}
                step={
                  inputType === 'currency' || inputType === 'number'
                    ? (() => {
                        const dpRaw = (ui as any)?.decimalPlaces;
                        if (dpRaw === undefined || dpRaw === null) {
                          return inputType === 'currency' ? '0.01' : 'any';
                        }
                        const dpNum = Number(dpRaw);
                        if (!Number.isFinite(dpNum) || dpNum < 0) {
                          return inputType === 'currency' ? '0.01' : 'any';
                        }
                        const dp = Math.floor(dpNum);
                        if (dp === 0) return '1';
                        return `0.${'0'.repeat(dp - 1)}1`;
                      })()
                    : undefined
                }
                {...register(fieldName, validationRules)}
                className={`${commonClasses} ${getIconForType(inputType) ? 'pl-10' : ''}`}
                placeholder={placeholder}
                disabled={readOnly}
                onFocus={inputType === 'currency' || inputType === 'number' ? handleNumericFocus : undefined}
                data-skip-focus={ui?.skipFocus || undefined}
              />
              {getIconForType(inputType) && (
                <div className={`absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none ${global.textSecondary}`}>
                  {getIconForType(inputType)}
                </div>
              )}
            </div>
          );
      }
    };

    return (
      <div key={fieldName} className={`${inputType === 'textarea' ? 'col-span-1 sm:col-span-2' : ''}`}>
        <div className="flex items-baseline justify-between mb-1">
            <label className={`block text-sm font-medium ${global.text}`}>
            {label} {required && <span className="text-red-500">*</span>}
          </label>
          {rule.unique && (
            <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium border ${
                isDark 
                ? 'bg-blue-900/30 text-blue-300 border-blue-800' 
                : 'bg-blue-50 text-blue-600 border-blue-100'
            }`}>
                {t('common.unique')}
            </span>
          )}
          {calculationConfig && schema.version === SCHEMA_VERSIONS.V1 && (
            <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium border ${
                isDark 
                ? 'bg-purple-900/30 text-purple-300 border-purple-800' 
                : 'bg-purple-50 text-purple-600 border-purple-100'
            }`} title={calculationConfig.complexCalculation ? 'Server-side C# calculation' : 'Server-side calculation'}>
                {calculationConfig.complexCalculation ? 'C#' : 'Σ'}
            </span>
          )}
        </div>
        {renderInput()}
        {getNestedError(errors, fieldName) && (
          <p className="mt-1 text-sm text-red-500">{getNestedError(errors, fieldName)?.message as string}</p>
        )}
      </div>
    );
  };

  const getIconForType = (type: string) => {
    switch (type) {
      case 'date': return <Calendar size={18} />;
      case 'email': return <Mail size={18} />;
      case 'tel': return <Phone size={18} />;
      case 'currency': return <span className="text-sm font-bold">{currency}</span>;
      // case 'text': return <User size={18} />; // Remove default icon for cleaner look
      default: return null;
    }
  };

  const renderSection = (section: LayoutSection, index: number) => {
     if (section.type === 'grid' && section.gridConfig) {
         return (
             <div key={index} className="col-span-1 sm:col-span-2 mt-4 mb-4">
                 <h3 className={`text-lg font-medium ${global.text} mb-3 border-l-4 ${theme.border} pl-3`}>
                     {section.section}
                 </h3>
                 <FormGrid
                    control={control}
                    register={register}
                    watch={watch}
                    setValue={setValue}
                    getValues={getValues}
                    name={section.gridConfig.dataSource || ''}
                    config={section.gridConfig}
                    itemSchema={section.gridConfig.dataSource ? gridSchemas[section.gridConfig.dataSource] : undefined}
                    readOnly={readOnly}
                    enterKeyNavigation={schema.ui?.enterKeyNavigation}
                 />
             </div>
         );
     }

     return (
        <div key={index} className="col-span-1 sm:col-span-2 mt-2">
            {section.section && (
                <h3 className={`text-sm font-bold uppercase tracking-wider ${global.textSecondary} mb-4 mt-2 border-b ${global.border} pb-2`}>
                    {section.section}
                </h3>
            )}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
                {section.fields?.map(fieldName => {
                    let rule = schema.fields[fieldName];
                    
                    // Fallback: Try to find field by suffix if exact match fails
                    // This handles cases where layout has short names but fields are nested/normalized
                    if (!rule) {
                        const match = Object.keys(schema.fields).find(k => k.endsWith(`.${fieldName}`) || k === fieldName);
                        if (match) {
                            rule = schema.fields[match];
                            // Note: We use the original fieldName for the key, but we should probably pass the MATCHED name to renderField
                            // However, renderField uses the name for register().
                            // If we use the short name for register, it might not map to the right data path if the schema expects nested.
                            // But React Hook Form creates nested data based on dots.
                            // If the field rule says "General Info.name", we want data to be { "General Info": { "name": ... } }.
                            // So we MUST use the full path (match) for renderField.
                            return renderField(match, rule);
                        }
                    }

                    if (!rule) return null;
                    return renderField(fieldName, rule);
                })}
            </div>
        </div>
     );
  };

  // ===== Attachment Handlers =====
  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;
    const files = Array.from(e.target.files);
    const attachmentConfig = schema.attachmentConfig?.documentLevel;
    
    if (attachmentConfig) {
      // Validate file count
      const totalFiles = attachments.length + files.length;
      if (totalFiles > attachmentConfig.maxFiles) {
        alert(`Maximum ${attachmentConfig.maxFiles} files allowed`);
        return;
      }
      
      // Validate file size
      const maxSizeBytes = attachmentConfig.maxFileSizeMB * 1024 * 1024;
      const invalidFiles = files.filter(f => f.size > maxSizeBytes);
      if (invalidFiles.length > 0) {
        alert(`Files must be smaller than ${attachmentConfig.maxFileSizeMB}MB`);
        return;
      }
      
      // Validate file types
      if (attachmentConfig.allowedTypes.length > 0) {
        const invalidTypes = files.filter(f => {
          const ext = f.name.split('.').pop()?.toLowerCase();
          return !attachmentConfig.allowedTypes.some(type => 
            type.toLowerCase() === ext || type.toLowerCase() === `.${ext}`
          );
        });
        if (invalidTypes.length > 0) {
          alert(`Invalid file type. Allowed: ${attachmentConfig.allowedTypes.join(', ')}`);
          return;
        }
      }
    }
    
    setAttachments(prev => [...prev, ...files]);
  }, [attachments, schema.attachmentConfig]);

  const handleRemoveAttachment = useCallback((index: number) => {
    setAttachments(prev => prev.filter((_, i) => i !== index));
  }, []);

  // ===== Render Helpers =====
  const renderDocumentTotals = () => {
    // Only show document totals for V1 and V2 schemas
    if (!featureSupport.documentTotals) return null;
    
    const documentTotals = schema.documentTotals;
    if (!documentTotals?.fields || Object.keys(documentTotals.fields).length === 0) {
      return null;
    }

    const displayConfig = documentTotals.displayConfig;
    const currencySymbol = displayConfig?.currencySymbol || currency;
    const layout = displayConfig?.layout || 'stacked';

    return (
      <div className={`mt-6 pt-4 border-t ${global.border}`}>
        <div className="flex items-center gap-2 mb-3">
          <Calculator size={16} className={global.textSecondary} />
          <span className={`text-sm font-semibold ${global.text}`}>
            {t('common.documentTotals') || 'Document Totals'}
          </span>
          {schema.version === SCHEMA_VERSIONS.V1 && schema.calculationRules?.complexCalculation && (
            <span className={`text-xs px-2 py-0.5 rounded ${theme.light} ${theme.text}`}>
              Server Calculated
            </span>
          )}
        </div>
        <div className={`${layout === 'horizontal' ? 'flex flex-wrap gap-4' : 'space-y-2'}`}>
          {Object.entries(documentTotals.fields).map(([fieldName, config]) => {
            const value = serverCalculatedTotals[fieldName] ?? config.defaultValue ?? 0;
            const isHighlighted = config.highlight;
            
            return (
              <div 
                key={fieldName} 
                className={`flex justify-between items-center ${layout === 'stacked' ? 'py-1' : ''} ${isHighlighted ? `${theme.light} px-3 py-2 rounded` : ''}`}
              >
                <span className={`text-sm ${isHighlighted ? 'font-semibold' : global.textSecondary}`}>
                  {config.label || fieldName}
                </span>
                <span className={`text-sm font-mono ${isHighlighted ? `font-bold ${theme.text}` : global.text}`}>
                  {currencySymbol} {Number(value).toFixed(config.decimalPlaces)}
                </span>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  const renderAttachments = () => {
    // Only show attachments for V1 and V3 schemas
    if (!featureSupport.attachments) return null;
    
    const attachmentConfig = schema.attachmentConfig?.documentLevel;
    if (!attachmentConfig?.enabled) return null;

    return (
      <div className={`mt-6 pt-4 border-t ${global.border}`}>
        <h3 className={`text-sm font-semibold ${global.text} mb-3 flex items-center gap-2`}>
          <Paperclip size={16} />
          {t('common.attachments') || 'Attachments'}
          {attachmentConfig.maxFiles > 0 && (
            <span className={`text-xs ${global.textSecondary}`}>
              ({attachments.length}/{attachmentConfig.maxFiles})
            </span>
          )}
        </h3>
        
        {/* File Upload Area */}
        {!readOnly && attachments.length < attachmentConfig.maxFiles && (
          <div className="mb-4">
            <label className={`flex flex-col items-center justify-center w-full h-24 border-2 border-dashed ${global.border} rounded-lg cursor-pointer hover:${global.bgSecondary} transition`}>
              <div className="flex flex-col items-center justify-center pt-5 pb-6">
                <Upload className={`w-6 h-6 ${global.textSecondary} mb-2`} />
                <p className={`text-xs ${global.textSecondary}`}>
                  Click to upload or drag and drop
                </p>
                <p className={`text-xs ${global.textSecondary} mt-1`}>
                  Max {attachmentConfig.maxFileSizeMB}MB • {attachmentConfig.allowedTypes.length > 0 ? attachmentConfig.allowedTypes.join(', ') : 'Any file type'}
                </p>
              </div>
              <input 
                type="file" 
                className="hidden" 
                multiple 
                onChange={handleFileSelect}
                accept={attachmentConfig.allowedTypes.join(',')}
              />
            </label>
          </div>
        )}

        {/* Attachment List */}
        {attachments.length > 0 && (
          <div className="space-y-2">
            {attachments.map((file, index) => (
              <div 
                key={index} 
                className={`flex items-center justify-between p-2 ${global.bgSecondary} rounded border ${global.border}`}
              >
                <div className="flex items-center gap-2 overflow-hidden">
                  <FileText size={16} className={global.textSecondary} />
                  <span className={`text-sm truncate ${global.text}`}>{file.name}</span>
                  <span className={`text-xs ${global.textSecondary}`}>
                    ({(file.size / 1024).toFixed(1)} KB)
                  </span>
                </div>
                {!readOnly && (
                  <button
                    type="button"
                    onClick={() => handleRemoveAttachment(index)}
                    className={`p-1 hover:${global.bg} rounded transition`}
                  >
                    <X size={14} className="text-red-500" />
                  </button>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Categories */}
        {attachmentConfig.categories.length > 0 && (
          <div className="mt-3">
            <p className={`text-xs ${global.textSecondary} mb-2`}>Categories:</p>
            <div className="flex flex-wrap gap-2">
              {attachmentConfig.categories.map(cat => (
                <span 
                  key={cat.id}
                  className={`text-xs px-2 py-1 rounded ${cat.required ? `${theme.light} ${theme.text}` : `${global.bgSecondary} ${global.textSecondary}`}`}
                >
                  {cat.label} {cat.required && '*'}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  };

  // ===== Cloud Storage Provider Selection =====
  const renderCloudStorageSelector = () => {
    // Only show for V1 and V4 schemas
    if (!featureSupport.cloudStorage) return null;
    
    const providers = schema.cloudStorage?.providers;
    if (!providers || providers.length === 0) return null;

    const defaultProvider = providers.find(p => p.isDefault) || providers[0];

    return (
      <div className={`mt-4 p-3 ${global.bgSecondary} rounded-lg border ${global.border}`}>
        <div className="flex items-center justify-between">
          <span className={`text-xs font-medium ${global.textSecondary}`}>
            Storage Provider
          </span>
          <select 
            className={`text-xs px-2 py-1 rounded border ${global.inputBorder} ${global.inputBg} ${global.text}`}
            disabled={readOnly}
          >
            {providers.map(provider => (
              <option key={provider.id} value={provider.id}>
                {provider.provider} {provider.isDefault ? '(Default)' : ''}
              </option>
            ))}
          </select>
        </div>
      </div>
    );
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && schema.ui?.enterKeyNavigation) {
      if (e.defaultPrevented) return;
      
      const target = e.target as HTMLElement;
      // if (target.tagName === 'TEXTAREA') return;

      e.preventDefault();
      const inputs = Array.from(
        document.querySelectorAll(
          'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]):not([type="submit"])'
        )
      ) as HTMLElement[];
      const index = inputs.indexOf(target);
      if (index > -1) {
        for (let i = index + 1; i < inputs.length; i++) {
          const el = inputs[i];
          if (!el.hasAttribute('data-skip-focus')) {
            el.focus();
            break;
          }
        }
      }
    }
  };

  useEffect(() => {
    if (readOnly) return;
    if (!formRef.current) return;
    const focusables = Array.from(
      formRef.current.querySelectorAll(
        'input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled])'
      )
    ) as HTMLElement[];
    const first = focusables.find(el => !el.hasAttribute('data-skip-focus')) || null;
    if (first) {
      first.focus();
    }
  }, [schema.module, readOnly]);

  if (!schema || !schema.fields) {
      return <div className="p-4 text-red-500">Invalid Schema: Missing fields definition.</div>;
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit(handleFormSubmit)} onKeyDown={handleKeyDown} className="space-y-6">
      <div className={`${global.bg} p-6 sm:p-8 rounded-xl shadow-sm border ${global.border}`}>
        <div className={`flex items-center gap-3 mb-6 border-b ${global.border} pb-4`}>
          <div className={`p-2 ${theme.light} rounded-lg`}>
            {schema.ui?.icon ? (
                 <span className="material-icons">{schema.ui.icon}</span> // Assuming Material Icons if provided as string
            ) : (
                 <Activity className={theme.text} size={24} />
            )}
          </div>
          <div className="flex-1">
            <h2 className={`text-xl font-semibold ${global.text}`}>
                {schema.ui?.title || `${schema.module} ${t('common.registration')}`}
            </h2>
            <div className="flex items-center gap-2">
              <p className={`text-sm ${global.textSecondary}`}>{t('common.version')} : v{schema.version}</p>
              {/* Version Badge */}
              {schema.version === SCHEMA_VERSIONS.V1 && (
                <span className={`text-xs px-2 py-0.5 rounded ${theme.light} ${theme.text}`}>
                  Full Features
                </span>
              )}
              {schema.version > SCHEMA_VERSIONS.V1 && (
                <span className={`text-xs px-2 py-0.5 rounded bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400`}>
                  Legacy v{schema.version}
                </span>
              )}
            </div>
          </div>
          {/* Complex Calculation Indicator */}
          {schema.version === SCHEMA_VERSIONS.V1 && schema.calculationRules?.complexCalculation && (
            <div className={`flex items-center gap-1 text-xs px-2 py-1 rounded ${theme.light} ${theme.text}`} title="Complex C# calculations enabled">
              <Server size={14} />
              <span>C# Calc</span>
            </div>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
          {schema.ui?.layout ? (
              schema.ui.layout.map((section, idx) => renderSection(section, idx))
          ) : (
              Object.entries(schema.fields).map(([fieldName, rule]) => renderField(fieldName, rule))
          )}
        </div>

        {/* Cloud Storage Provider Selection */}
        {renderCloudStorageSelector()}

        {/* Document Totals Section */}
        {renderDocumentTotals()}

        {/* Attachments Section */}
        {renderAttachments()}

        {!readOnly && (
        <div className="mt-8 flex justify-end">
          <button
            type="submit"
            disabled={isLoading || isCalculating}
            className={`w-full sm:w-auto px-6 py-2.5 ${theme.primary} text-white font-medium rounded-lg ${theme.hover} focus:ring-4 ${theme.ring} focus:ring-opacity-50 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2`}
          >
            {isLoading || isCalculating ? (
              <>
                <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {isCalculating ? 'Calculating...' : t('common.loading')}
              </>
            ) : (
              t('common.save')
            )}
          </button>
        </div>
        )}
      </div>
    </form>
  );
};

export default DynamicForm;
