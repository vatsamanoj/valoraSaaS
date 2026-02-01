import React, { useEffect, useState, useRef } from 'react';
import { useForm, FieldValues, Controller } from 'react-hook-form';
import { ModuleSchema, FieldRule, LayoutSection } from '../types/schema';
import { Calendar, User, Phone, Mail, Activity } from 'lucide-react';
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

  // Pre-process initialData for Date fields to ensure YYYY-MM-DD format
  const formattedInitialData = React.useMemo(() => {
    if (!initialData || !schema?.fields) return initialData;
    const copy = { ...initialData };
    Object.keys(schema.fields).forEach(key => {
        const rule = schema.fields[key];
        if (rule.ui?.type?.toLowerCase() === 'date' && copy[key]) {
            try {
                const d = new Date(copy[key]);
                if (!isNaN(d.getTime())) {
                    copy[key] = d.toISOString().split('T')[0];
                }
            } catch {}
        }
    });
    return copy;
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
    defaultValues: formattedInitialData || {},
  });

  const [gridSchemas, setGridSchemas] = useState<Record<string, ModuleSchema>>({});
  const [lookupData, setLookupData] = useState<Record<string, any[]>>({});
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

  // Auto-calculation for Totals
  const values = watch();
  useEffect(() => {
      if (readOnly) return;
      if (!schema.ui?.layout) return;

      const grids = schema.ui.layout.filter(s => s.type === 'grid' && s.gridConfig?.dataSource);
      
      grids.forEach(grid => {
          const ds = grid.gridConfig!.dataSource!;
          const items = getNestedValue(values, ds);
          
          if (Array.isArray(items)) {
              const totalAmount = items.reduce((sum, item) => sum + (parseFloat(item.Price || 0) * parseFloat(item.Qty || 0)), 0);
              const discountAmount = items.reduce((sum, item) => sum + (parseFloat(item.Discount || 0)), 0);
              const taxAmount = items.reduce((sum, item) => sum + (parseFloat(item.TaxAmount || 0)), 0);
              const netAmount = items.reduce((sum, item) => sum + (parseFloat(item.NetAmount || 0)), 0);

              const updateIfChanged = (field: string, newVal: number) => {
                  if (schema.fields[field]) {
                      const fieldRule = schema.fields[field];
                      const decimalsRaw = (fieldRule.ui as any)?.decimalPlaces;
                      const decimalsNum = Number(decimalsRaw);
                      const decimals = Number.isFinite(decimalsNum) && decimalsNum >= 0 ? Math.floor(decimalsNum) : 2;
                      const currentVal = parseFloat(getNestedValue(values, field) || 0);
                      const rounded = parseFloat(newVal.toFixed(decimals));
                      const epsilon = Math.pow(10, -decimals);
                      if (Math.abs(currentVal - rounded) > epsilon) {
                          setValue(field, rounded);
                      }
                  }
              };

              const uiTotals = (schema.ui as any)?.totals || (schema.ui as any)?.Totals;
              const totalAmountField = uiTotals?.totalAmountField || uiTotals?.TotalAmountField || 'TotalAmount';
              const discountAmountField = uiTotals?.discountAmountField || uiTotals?.DiscountAmountField || 'DiscountAmount';
              const taxAmountField = uiTotals?.taxAmountField || uiTotals?.TaxAmountField || 'TaxAmount';
              const netAmountField = uiTotals?.netAmountField || uiTotals?.NetAmountField || 'NetAmount';

              updateIfChanged(totalAmountField, totalAmount);
              updateIfChanged(discountAmountField, discountAmount);
              updateIfChanged(taxAmountField, taxAmount);
              updateIfChanged(netAmountField, netAmount);
          }
      });
  }, [values, schema, readOnly, setValue]);

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

  const renderField = (fieldName: string, rule: FieldRule) => {
    const { ui, required, maxLength } = rule;
    const label = getFieldLabel(fieldName, rule);
    const placeholder = ui?.placeholder || t('common.enter', { field: label });
    const rawType = (ui?.type || 'text').toLowerCase();
    const inputType = rawType === 'money' ? 'currency' : rawType;

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
                        searchStrategy={(ui as any).searchStrategy}
                        searchPattern={(ui as any).searchPattern}
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
    <form ref={formRef} onSubmit={handleSubmit(onSubmit)} onKeyDown={handleKeyDown} className="space-y-6">
      <div className={`${global.bg} p-6 sm:p-8 rounded-xl shadow-sm border ${global.border}`}>
        <div className={`flex items-center gap-3 mb-6 border-b ${global.border} pb-4`}>
          <div className={`p-2 ${theme.light} rounded-lg`}>
            {schema.ui?.icon ? (
                 <span className="material-icons">{schema.ui.icon}</span> // Assuming Material Icons if provided as string
            ) : (
                 <Activity className={theme.text} size={24} />
            )}
          </div>
          <div>
            <h2 className={`text-xl font-semibold ${global.text}`}>
                {schema.ui?.title || `${schema.module} ${t('common.registration')}`}
            </h2>
            <p className={`text-sm ${global.textSecondary}`}>{t('common.version')} : v{schema.version}</p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
          {schema.ui?.layout ? (
              schema.ui.layout.map((section, idx) => renderSection(section, idx))
          ) : (
              Object.entries(schema.fields).map(([fieldName, rule]) => renderField(fieldName, rule))
          )}
        </div>

        {!readOnly && (
        <div className="mt-8 flex justify-end">
          <button
            type="submit"
            disabled={isLoading}
            className={`w-full sm:w-auto px-6 py-2.5 ${theme.primary} text-white font-medium rounded-lg ${theme.hover} focus:ring-4 ${theme.ring} focus:ring-opacity-50 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2`}
          >
            {isLoading ? (
              <>
                <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {t('common.loading')}
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
