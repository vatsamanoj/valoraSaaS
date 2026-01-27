import React, { useEffect, useState } from 'react';
import { useFieldArray, Control, UseFormRegister, FieldValues, UseFormWatch, UseFormSetValue, UseFormGetValues } from 'react-hook-form';
import { ModuleSchema, GridConfig, GridColumnConfig } from '../types/schema';
import { useTheme } from '../context/ThemeContext';
import { Trash2, Plus } from 'lucide-react';
import { fetchApi, unwrapResult } from '../utils/api';

interface FormGridProps {
  control: Control<FieldValues>;
  register: UseFormRegister<FieldValues>;
  watch: UseFormWatch<FieldValues>;
  setValue: UseFormSetValue<FieldValues>;
  getValues: UseFormGetValues<FieldValues>;
  name: string;
  config: GridConfig;
  itemSchema?: ModuleSchema;
  readOnly?: boolean;
  enterKeyNavigation?: boolean;
}

const FormGrid: React.FC<FormGridProps> = ({ 
  control, 
  register, 
  watch,
  setValue,
  getValues,
  name, 
  config, 
  itemSchema,
  readOnly
}) => {
  const { global, styles: theme, formatCurrency } = useTheme();
  
  const { fields, append, remove } = useFieldArray({
    control,
    name
  });

  const [lookupData, setLookupData] = useState<Record<string, any[]>>({});

  const handleNumericFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    e.target.select();
  };

  const getColumnConfig = (col: string | GridColumnConfig): GridColumnConfig => {
    if (typeof col === 'string') {
        // Fallback to itemSchema or defaults
        const fieldRule = itemSchema?.fields[col];
        const ui = fieldRule?.ui as any;
        return {
            field: col,
            label: ui?.label || col,
            type: (ui?.type as any) || 'text',
            width: 'auto',
            skipFocus: ui?.skipFocus,
            lookupModule: ui?.lookup,
            lookupField: ui?.lookupField,
            displayField: ui?.displayField,
            mapping: ui?.mapping
        };
    }
    return col;
  };

  useEffect(() => {
    const loadLookups = async () => {
      let headers: Record<string, string> = {};
      if (typeof window !== 'undefined') {
          try {
              const raw = window.localStorage.getItem('valora_headers');
              if (raw) headers = JSON.parse(raw);
          } catch {}
      }

      for (const rawCol of config.columns) {
        const col = getColumnConfig(rawCol);
        if (col.type === 'lookup' && col.lookupModule) {
          try {
            const res = await fetchApi('/api/query/ExecuteQuery', {
              method: 'POST',
              headers: { ...headers, 'Content-Type': 'application/json' },
              body: JSON.stringify({ 
                Module: col.lookupModule,
                Options: { PageSize: 1000 } // Load all for now
              })
            });
            const result = await unwrapResult<any>(res);
            setLookupData(prev => ({ ...prev, [col.field]: result.data || [] }));
          } catch (err) {
            console.error(`Failed to load lookup for ${col.field}`, err);
          }
        }
      }
    };
    loadLookups();
  }, [config.columns, itemSchema]);

  const itemsRaw = watch(name);
  let items: any[] = [];
  
  // Explicit Row Calculation Helper
  const calculateRow = (index: number, field: string, value: any) => {
      if (readOnly) return;
      
      const hasTax = config.columns.some(c => (typeof c === 'string' ? c : c.field) === 'TaxAmount');
      const hasNet = config.columns.some(c => (typeof c === 'string' ? c : c.field) === 'NetAmount');
      
      if (!hasTax && !hasNet) return;

      // Get current row values
      const currentItems = getValues(name) || [];
      const row = currentItems[index] || {};

      // Overlay the changed value
      const updatedRow = { ...row, [field]: value };

      const price = parseFloat(updatedRow.Price) || 0;
      const qty = updatedRow.Qty !== undefined ? (parseFloat(updatedRow.Qty) || 0) : 0;
      const discount = parseFloat(updatedRow.Discount) || 0;
      const taxPercent = parseFloat(updatedRow.TaxPercent) || 0;
      
      const getDecimals = (fieldName: string) => {
          const col = config.columns.find(c => (typeof c === 'string' ? c : c.field) === fieldName);
          const colConfig = col ? getColumnConfig(col) : null;
          return (colConfig as any)?.decimalPlaces ?? 2;
      };

      const taxDecimals = getDecimals('TaxAmount');
      const netDecimals = getDecimals('NetAmount');

      const gross = price * qty;
      const taxable = gross - discount;
      const taxAmount = parseFloat((taxable * (taxPercent / 100)).toFixed(taxDecimals));
      const netAmount = parseFloat((taxable + taxAmount).toFixed(netDecimals));
      
      // Update fields if changed
      if (row.TaxAmount !== taxAmount) {
           setValue(`${name}.${index}.TaxAmount`, taxAmount, { shouldValidate: true });
      }
      if (row.NetAmount !== netAmount) {
           setValue(`${name}.${index}.NetAmount`, netAmount, { shouldValidate: true });
      }
  };

  useEffect(() => {
      if (typeof itemsRaw === 'string') {
           try {
               const parsed = JSON.parse(itemsRaw);
               if (Array.isArray(parsed)) {
                   setValue(name, parsed);
               }
           } catch {
               // ignore
           }
      }
  }, [itemsRaw, setValue, name]);

  if (Array.isArray(itemsRaw)) {
      items = itemsRaw;
  } else if (typeof itemsRaw === 'string') {
      try {
          items = JSON.parse(itemsRaw);
          if (!Array.isArray(items)) items = [];
      } catch {
          items = [];
      }
  }

  const getSum = (field: string) => {
    return items.reduce((sum: number, item: any) => {
      const val = parseFloat(item[field] || 0);
      return sum + (isNaN(val) ? 0 : val);
    }, 0);
  };

  const handleLookupChange = (index: number, col: GridColumnConfig, value: string) => {
      // Find selected item
      const data = lookupData[col.field];
      if (!data) return;
      
      const selected = data.find(d => {
          // Try to match by lookupField (usually Name/TestName) or ID
          const displayVal = col.lookupField ? d[col.lookupField] : (d.Name || d.TestName || d.Id);
          return displayVal === value;
      });

      if (selected) {
          // Auto-map fields
          if (col.mapping) {
              Object.entries(col.mapping).forEach(([source, target]) => {
                  if (selected[source] !== undefined) {
                      setValue(`${name}.${index}.${target}`, selected[source]);
                  }
              });
          } else {
              // Legacy/Default heuristics
              // Map MRP -> Price
              if (selected.MRP !== undefined) setValue(`${name}.${index}.Price`, selected.MRP);
              
              // Map GSTPercent -> TaxPercent
              if (selected.GSTPercent !== undefined) setValue(`${name}.${index}.TaxPercent`, selected.GSTPercent);
              
              // Map CostPrice -> Cost
              if (selected.CostPrice !== undefined) setValue(`${name}.${index}.Cost`, selected.CostPrice);
          }
          
          // Trigger calculation after setting values
          // We need to wait for values to be set, or manually trigger with updated values.
          // Since setValue is async-ish, let's use a small timeout or just invoke calculateRow with undefined to force re-read
          setTimeout(() => calculateRow(index, 'Lookup', null), 0);
      }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      const inputs = Array.from(
        document.querySelectorAll(
          'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]):not([type="submit"])'
        )
      ) as HTMLElement[];
      const index = inputs.indexOf(e.target as HTMLElement);
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

  return (
    <div className="overflow-x-auto border rounded-lg border-gray-200 dark:border-gray-700">
      <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead className={global.bgSecondary}>
          <tr>
            {config.columns.map((rawCol, idx) => {
              const col = getColumnConfig(rawCol);
              return (
                <th key={idx} className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider" style={{ width: col.width }}>
                  {col.label}
                </th>
              );
            })}
            {!readOnly && config.allowDelete && (
              <th className="px-4 py-3 text-right">Actions</th>
            )}
          </tr>
        </thead>
        <tbody className={`${global.bg} divide-y divide-gray-200 dark:divide-gray-700`}>
          {fields.map((item, index) => (
            <tr key={item.id}>
              {config.columns.map((rawCol, colIdx) => {
                  const col = getColumnConfig(rawCol);
                  const isReadOnly = readOnly || col.readOnly;
                  
                  if (col.type === 'lookup') {
                      const listId = `list-${name}-${index}-${col.field}`;
                      const data = lookupData[col.field] || [];
                      return (
                        <td key={`${item.id}-${colIdx}`} className="px-2 py-2">
                             <input
                                list={listId}
                                {...register(`${name}.${index}.${col.field}`, {
                                    onChange: (e) => handleLookupChange(index, col, e.target.value)
                                })}
                                onKeyDown={handleKeyDown}
                                data-skip-focus={col.skipFocus || undefined}
                                disabled={isReadOnly}
                                className={`w-full px-2 py-1 text-sm border-0 bg-transparent focus:ring-1 focus:ring-blue-500 rounded ${global.text}`}
                                placeholder="Search..."
                                autoComplete="off"
                             />
                             <datalist id={listId}>
                                 {data.map((opt, i) => {
                                     const val = col.lookupField ? opt[col.lookupField] : (opt.Name || opt.TestName || opt.Id);
                                     const display = col.displayField ? opt[col.displayField] : (opt.Name || opt.TestName || opt.Id);
                                     return <option key={i} value={val}>{display}</option>;
                                 })}
                             </datalist>
                        </td>
                      );
                  }

                  if (col.type === 'select') {
                      return (
                        <td key={`${item.id}-${colIdx}`} className="px-2 py-2">
                             <select
                                {...register(`${name}.${index}.${col.field}`)}
                                onKeyDown={handleKeyDown}
                                data-skip-focus={col.skipFocus || undefined}
                                disabled={isReadOnly}
                                className={`w-full px-2 py-1 text-sm border-0 bg-transparent focus:ring-1 focus:ring-blue-500 rounded ${global.text}`}
                             >
                                <option value="">-</option>
                                {col.options?.map(opt => (
                                    <option key={opt} value={opt}>{opt}</option>
                                ))}
                             </select>
                        </td>
                      );
                  }

                  // Default input
                  return (
                    <td key={`${item.id}-${colIdx}`} className="px-2 py-2">
                        <input
                            type={col.type === 'number' ? 'number' : 'text'}
                            step={
                              col.type === 'number'
                                ? (() => {
                                    const dpRaw = (col as any).decimalPlaces;
                                    if (dpRaw === undefined || dpRaw === null) {
                                      return "any";
                                    }
                                    const dpNum = Number(dpRaw);
                                    if (!Number.isFinite(dpNum) || dpNum < 0) {
                                      return "any";
                                    }
                                    const dp = Math.floor(dpNum);
                                    if (dp === 0) return "1";
                                    return `0.${"0".repeat(dp - 1)}1`;
                                  })()
                                : undefined
                            }
                            {...register(`${name}.${index}.${col.field}`, { 
                                valueAsNumber: col.type === 'number',
                                onChange: (e) => {
                                    if (['Price', 'Qty', 'Discount', 'TaxPercent'].includes(col.field)) {
                                        calculateRow(index, col.field, e.target.value);
                                    }
                                }
                            })}
                            onKeyDown={handleKeyDown}
                            onFocus={col.type === 'number' ? handleNumericFocus : undefined}
                            data-skip-focus={col.skipFocus || undefined}
                            disabled={isReadOnly}
                            className={`w-full px-2 py-1 text-sm border-0 bg-transparent focus:ring-1 focus:ring-blue-500 rounded ${global.text}`}
                            placeholder={isReadOnly ? '-' : ''}
                        />
                    </td>
                  );
              })}
              {!readOnly && config.allowDelete && (
                <td className="px-2 py-2 text-right">
                  <button
                    type="button"
                    onClick={() => remove(index)}
                    className="text-red-500 hover:text-red-700 p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20"
                  >
                    <Trash2 size={16} />
                  </button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
        {config.footer?.show && (
            <tfoot className={global.bgSecondary}>
                <tr>
                    {config.columns.map((rawCol, idx) => {
                        const col = getColumnConfig(rawCol);
                        return (
                        <td key={`footer-${idx}`} className="px-4 py-3 text-sm font-semibold text-gray-700 dark:text-gray-300">
                            {config.footer?.sum.includes(col.field) ? (
                                formatCurrency(getSum(col.field))
                            ) : ''}
                        </td>
                        );
                    })}
                    {!readOnly && config.allowDelete && <td></td>}
                </tr>
            </tfoot>
        )}
      </table>
      
      {!readOnly && config.allowAdd && (
        <div className="p-2 border-t border-gray-200 dark:border-gray-700">
          <button
            type="button"
            onClick={() => append({ Qty: 1, Discount: 0 })}
            className={`flex items-center gap-2 px-4 py-2 text-sm font-medium ${theme.primary} text-white rounded hover:opacity-90 transition`}
          >
            <Plus size={16} />
            Add Item
          </button>
        </div>
      )}
    </div>
  );
};

export default FormGrid;
