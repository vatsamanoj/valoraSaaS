import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { ModuleSchema } from '../types/schema';
import { Plus, Filter, Eye, Edit2, Download, FileText, Table } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import { fetchApi, unwrapResult } from '../utils/api';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

interface GenericObjectListProps {
  objectCode?: string;
}

const GenericObjectList: React.FC<GenericObjectListProps> = ({ objectCode: propObjectCode }) => {
  const { global, styles: theme, isDark, t, formatCurrency } = useTheme();
  const { objectCode: paramObjectCode } = useParams<{ objectCode: string }>();
  const objectCode = propObjectCode || paramObjectCode;
  const location = useLocation();
  const highlightedId = location.state?.highlightedId;

  const getFieldLabel = (field: string, rule: any) => {
    const moduleKey = `${objectCode}.fields.${field}`;
    const commonKey = `common.fields.${field}`;
    
    const moduleLabel = t(moduleKey);
    if (moduleLabel !== moduleKey) return moduleLabel;
    
    const commonLabel = t(commonKey);
    if (commonLabel !== commonKey) return commonLabel;
    
    return rule.ui?.label || field;
  };

  const [schema, setSchema] = useState<ModuleSchema | null>(null);
  const [data, setData] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [showFilters, setShowFilters] = useState(false);
  const [showExportMenu, setShowExportMenu] = useState(false);
  const [allowedActions, setAllowedActions] = useState<string[]>([]);
  
  // Pagination & Sorting State
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(15); // Default, will be calculated
  const [sortBy, setSortBy] = useState<string | null>(null);
  const [sortDesc, setSortDesc] = useState(false);

  const tableContainerRef = useRef<HTMLDivElement>(null);

  // Calculate dynamic page size to fit screen
  useEffect(() => {
    const calculatePageSize = () => {
      if (tableContainerRef.current) {
        const containerHeight = tableContainerRef.current.clientHeight;
        const headerHeight = 40; // Approx header height
        const rowHeight = 53; // Approx row height (py-4 + text)
        const availableHeight = containerHeight - headerHeight;
        const calculatedSize = Math.max(5, Math.floor(availableHeight / rowHeight));
        // Only update if changed to avoid loop
        setPageSize(prev => prev !== calculatedSize ? calculatedSize : prev);
      }
    };

    calculatePageSize();
    
    // Debounce resize
    let resizeTimer: ReturnType<typeof setTimeout>;
    const handleResize = () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(calculatePageSize, 100);
    };

    window.addEventListener('resize', handleResize);
    return () => {
      window.removeEventListener('resize', handleResize);
      clearTimeout(resizeTimer);
    };
  }, []);

  // Fetch Schema
  useEffect(() => {
    if (!objectCode) return;
    
    const fetchSchema = async () => {
      try {
        let headers: Record<string, string> = {};
        if (typeof window !== 'undefined') {
          try {
            const raw = window.localStorage.getItem('valora_headers');
            if (raw) headers = JSON.parse(raw);
          } catch {}
        }

        const response = await fetchApi(`/api/platform/object/${objectCode}/latest`, { headers });

        // If 404, we might not have a template, but we can still show a generic table if we knew the fields.
        // But for now, we rely on the template.
        if (!response.ok) return;

        const data = await response.json();
        setSchema(data);
      } catch (err) {
        console.error("Failed to fetch schema", err);
      }
    };
    fetchSchema();
  }, [objectCode]);

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

  // Fetch Data (Query)
  const prevFiltersRef = useRef(filters);

  useEffect(() => {
    if (!objectCode) return;

    const fetchData = async () => {
      setIsLoading(true);
      try {
        let headers: Record<string, string> = {};
        if (typeof window !== 'undefined') {
            try {
                const raw = window.localStorage.getItem('valora_headers');
                if (raw) headers = JSON.parse(raw);
            } catch {}
        }

        // Build filters object, removing empty values
        const activeFilters = Object.entries(filters).reduce((acc, [key, value]) => {
          if (value !== '' && value !== null && value !== undefined) {
            acc[key] = value;
          }
          return acc;
        }, {} as Record<string, any>);

        const response = await fetchApi('/api/query/ExecuteQuery', {
          method: 'POST',
          headers: { ...headers, 'Content-Type': 'application/json' },
          body: JSON.stringify({
            Module: objectCode,
            Options: {
              Filters: activeFilters,
              Page: page,
              PageSize: pageSize,
              SortBy: sortBy,
              SortDesc: sortDesc
            }
          })
        });

        const result = await unwrapResult<any>(response);
        setData(result.data || []);
      } catch (err) {
        console.error("Failed to fetch data", err);
      } finally {
        setIsLoading(false);
      }
    };

    const filtersChanged = JSON.stringify(filters) !== JSON.stringify(prevFiltersRef.current);
    prevFiltersRef.current = filters;

    let timeoutId: ReturnType<typeof setTimeout>;

    if (filtersChanged) {
      // Debounce only for filter changes
      timeoutId = setTimeout(() => {
        setPage(1); // Reset to page 1 on filter change
        fetchData();
      }, 500);
    } else {
      // Immediate fetch for Page/Sort/Size changes
      fetchData();
    }

    return () => {
      if (timeoutId) clearTimeout(timeoutId);
    };
  }, [objectCode, filters, page, pageSize, sortBy, sortDesc]);

  // Scroll to highlighted item
  useEffect(() => {
    if (highlightedId && data.length > 0) {
      const element = document.getElementById(`row-${highlightedId}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }
  }, [highlightedId, data]);

  const handleFilterChange = (field: string, value: string) => {
    setFilters(prev => ({ ...prev, [field]: value }));
  };

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortDesc(!sortDesc);
    } else {
      setSortBy(field);
      setSortDesc(false);
    }
  };

  const getValueCaseInsensitive = (row: any, field: string) => {
    if (row[field] !== undefined) return row[field];
    const key = Object.keys(row).find(k => k.toLowerCase() === field.toLowerCase());
    return key ? row[key] : undefined;
  };

  const handleExportCSV = () => {
    if (!data.length || !schema) return;

    // Headers
    const headers = [t('common.id'), ...Object.keys(schema.fields).map(field => getFieldLabel(field, schema.fields[field]))];
    
    // Rows
    const rows = data.map(row => [
      row.Id, 
      ...Object.keys(schema.fields).map(field => getValueCaseInsensitive(row, field) || '')
    ]);

    // CSV Content
    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
    ].join('\n');

    // Download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `${objectCode}_export_${new Date().toISOString().slice(0, 10)}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setShowExportMenu(false);
  };

  const handleExportPDF = () => {
    if (!data.length || !schema) return;

    const doc = new jsPDF();

    // Title
    doc.setFontSize(18);
    doc.text(`${objectCode} List`, 14, 22);
    doc.setFontSize(10);
    doc.text(`Generated on ${new Date().toLocaleDateString()}`, 14, 30);

    // Columns
    const columns = [
      { header: t('common.id'), dataKey: 'Id' },
      ...Object.keys(schema.fields).map(field => ({
        header: getFieldLabel(field, schema.fields[field]),
        dataKey: field
      }))
    ];

    // Data
    const tableData = data.map(row => {
      const rowData: any = { Id: (row.Id || '').substring(0, 8) + '...' };
      Object.keys(schema.fields).forEach(field => {
        rowData[field] = getValueCaseInsensitive(row, field) || '';
      });
      return rowData;
    });

    // Table
    autoTable(doc, {
      startY: 35,
      head: [columns.map(c => c.header)],
      body: tableData.map(row => columns.map(c => row[c.dataKey])),
      styles: { fontSize: 8 },
      headStyles: { fillColor: [41, 128, 185] }
    });

    doc.save(`${objectCode}_export_${new Date().toISOString().slice(0, 10)}.pdf`);
    setShowExportMenu(false);
  };

  if (!objectCode) return <div>{t('common.error')}</div>;

  const canCreate = allowedActions.includes('create');
  const canEdit = allowedActions.includes('edit');
  const canView = allowedActions.includes('view');

  const getFieldRule = (field: string) => schema?.fields[field];
  
  // Handle potentially different casing from backend
  const ui = schema?.ui as any;
  const listFields = ui?.listFields || ui?.ListFields;
  const filterFields = ui?.filterFields || ui?.FilterFields;

  const visibleFilters = filterFields || Object.keys(schema?.fields || {});
  const visibleColumns = listFields || Object.keys(schema?.fields || {});

  // Debugging
  useEffect(() => {
    if (schema) {
       console.log("Schema UI:", schema.ui);
       console.log("Visible Columns:", visibleColumns);
       console.log("Visible Filters:", visibleFilters);
    }
  }, [schema]);

  return (
    <div className={`h-full flex flex-col ${global.bgSecondary}`}>
      {/* Header & Filters Section (Fixed) */}
      <div className="flex-none p-6 pb-4">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className={`text-2xl font-bold ${global.text}`}>{objectCode} List</h1>
            <p className={`mt-1 text-sm ${global.textSecondary}`}>{t('common.manage', { module: objectCode || '' })}</p>
          </div>
          <div className="flex items-center">
          {canCreate && (
            <Link 
              to={`/app/${objectCode}/new`}
              className={`flex items-center gap-2 px-4 py-2 ${theme.primary} text-white rounded-lg ${theme.hover} transition shadow-sm`}
            >
              <Plus size={20} />
              <span>{t('common.new', { module: objectCode || '' })}</span>
            </Link>
          )}
          
          {/* Export Menu */}
          <div className="relative ml-2">
            <button
              onClick={() => setShowExportMenu(!showExportMenu)}
              className={`flex items-center gap-2 px-4 py-2 border ${global.border} ${global.bg} ${global.text} rounded-lg hover:${global.bgSecondary} transition shadow-sm`}
              title={t('common.export')}
            >
              <Download size={20} />
            </button>
            
            {showExportMenu && (
              <div className={`absolute right-0 mt-2 w-48 rounded-md shadow-lg ${global.bg} border ${global.border} z-50`}>
                <div className="py-1">
                  <button
                    onClick={handleExportCSV}
                    className={`flex items-center gap-2 w-full px-4 py-2 text-sm ${global.text} hover:${global.bgSecondary} text-left`}
                  >
                    <Table size={16} />
                    {t('common.exportCSV')}
                  </button>
                  <button
                    onClick={handleExportPDF}
                    className={`flex items-center gap-2 w-full px-4 py-2 text-sm ${global.text} hover:${global.bgSecondary} text-left`}
                  >
                    <FileText size={16} />
                    {t('common.exportPDF')}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Filters & Search */}
        <div className={`${global.bg} rounded-xl shadow-sm border ${global.border} mb-2 overflow-hidden`}>
          <div 
            className={`px-4 py-3 ${global.bgSecondary} border-b ${global.border} flex justify-between items-center cursor-pointer`}
            onClick={() => setShowFilters(!showFilters)}
          >
            <div className={`flex items-center gap-2 ${global.text} font-medium`}>
              <Filter size={18} />
              <span>{t('common.filters')}</span>
            </div>
            <span className={`text-xs ${theme.light} ${theme.text} px-2 py-1 rounded-full`}>
              {Object.keys(filters).filter(k => filters[k]).length} {t('common.active')}
            </span>
          </div>
          
          {/* Collapsible Filter Area */}
          {(showFilters || true) && (
            <div className="p-4 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
               {/* Always add ID search */}
               <div>
                  <label className={`block text-xs font-semibold ${global.textSecondary} uppercase tracking-wider mb-1`}>{t('common.id')}</label>
                  <input
                    type="text"
                    placeholder={t('common.searchId')}
                    className={`w-full px-3 py-2 border ${global.inputBorder} ${global.inputBg} ${global.text} rounded-md text-sm focus:ring-1 ${theme.ring} outline-none`}
                    value={filters['Id'] || ''}
                    onChange={(e) => handleFilterChange('Id', e.target.value)}
                  />
               </div>

              {schema && visibleFilters.map((field: string) => {
                const rule = getFieldRule(field);
                if (!rule) return null;
                return (
                <div key={field}>
                  <label className={`block text-xs font-semibold ${global.textSecondary} uppercase tracking-wider mb-1`}>
                    {getFieldLabel(field, rule)}
                  </label>
                  {rule.ui?.type === 'select' ? (
                    <select
                      className={`w-full px-3 py-2 border ${global.inputBorder} rounded-md text-sm focus:ring-1 ${theme.ring} outline-none ${global.bg} ${global.text}`}
                      value={filters[field] || ''}
                      onChange={(e) => handleFilterChange(field, e.target.value)}
                    >
                      <option value="">{t('common.all')}</option>
                      {rule.ui.options?.map(opt => {
                        const key = `${objectCode}.fields.${field}.options.${opt}`;
                        const translated = t(key);
                        const display = translated !== key ? translated : opt;
                        return (
                          <option key={opt} value={opt}>{display}</option>
                        );
                      })}
                    </select>
                  ) : (
                    <input
                      type={rule.ui?.type === 'number' ? 'number' : 'text'}
                      placeholder={`${t('common.filterBy')} ${getFieldLabel(field, rule)}...`}
                      className={`w-full px-3 py-2 border ${global.inputBorder} ${global.inputBg} ${global.text} rounded-md text-sm focus:ring-1 ${theme.ring} outline-none`}
                      value={filters[field] || ''}
                      onChange={(e) => handleFilterChange(field, e.target.value)}
                    />
                  )}
                </div>
              )})}
            </div>
          )}
        </div>
      </div>

      {/* Data Table Section (Scrollable) */}
      <div className="flex-1 overflow-hidden p-6 pt-0 relative">
        <div ref={tableContainerRef} className={`h-full flex flex-col ${global.bg} rounded-xl shadow-sm border ${global.border} overflow-hidden relative`}>
            
            {/* Loading Overlay */}
            {isLoading && (
               <div className={`absolute inset-0 z-20 ${global.bg} bg-opacity-70 flex flex-col justify-center items-center ${global.textSecondary}`}>
                  <div className={`animate-spin rounded-full h-8 w-8 border-b-2 ${theme.text} mb-3`}></div>
                  {t('common.loading')}
               </div>
            )}

            {/* Empty State */}
            {!isLoading && data.length === 0 ? (
              <div className={`flex-1 flex flex-col justify-center items-center ${global.textSecondary}`}>
                {t('common.noRecords')}
              </div>
            ) : (
              <>
              <div className="flex-1 overflow-y-auto">
                <table className={`w-full text-left text-sm ${global.textSecondary}`}>
                  <thead className={`sticky top-0 z-10 ${global.bgSecondary} text-xs uppercase font-semibold ${global.textSecondary} border-b ${global.border}`}>
                  <tr>
                    <th 
                      className="px-6 py-4 bg-inherit cursor-pointer select-none hover:text-opacity-80 transition"
                      onClick={() => handleSort('Id')}
                    >
                      <div className="flex items-center gap-1">
                        ID
                        {sortBy === 'Id' && (
                           <span>{sortDesc ? '↓' : '↑'}</span>
                        )}
                      </div>
                    </th>
                    {schema && visibleColumns.map((field: string) => {
                      const rule = getFieldRule(field);
                      if (!rule) return null;
                      return (
                      <th 
                        key={field} 
                        className="px-6 py-4 whitespace-nowrap bg-inherit cursor-pointer select-none hover:text-opacity-80 transition"
                        onClick={() => handleSort(field)}
                      >
                        <div className="flex items-center gap-1">
                          {rule.ui?.label || field}
                          {sortBy === field && (
                             <span>{sortDesc ? '↓' : '↑'}</span>
                          )}
                        </div>
                      </th>
                    )})}
                    <th className="px-6 py-4 text-right bg-inherit">{t('common.actions')}</th>
                  </tr>
                </thead>
                <tbody className={`divide-y ${isDark ? 'divide-slate-700' : 'divide-gray-100'}`}>
                  {data.map((row, idx) => {
                    const isHighlighted = row.Id === highlightedId;
                    return (
                    <tr 
                        key={row.Id || idx} 
                        id={`row-${row.Id}`}
                        className={`
                            transition-colors duration-1000
                            ${isHighlighted 
                                ? `${theme.primary} bg-opacity-10 border-l-4 ${theme.border}` // Highlight style
                                : `hover:${global.bgSecondary}`
                            }
                        `}
                    >
                      <td className={`px-6 py-4 font-mono text-xs ${global.textSecondary}`}>
                        {(row.Id || '').substring(0, 8)}...
                      </td>
                      {schema && visibleColumns.map((field: string) => {
                        const rule = getFieldRule(field);
                        if (!rule) return null;
                        return (
                      <td key={field} className={`px-6 py-4 ${global.text}`}>
                          {rule.ui?.type === 'currency' 
                            ? formatCurrency(Number(getValueCaseInsensitive(row, field)) || 0)
                            : (getValueCaseInsensitive(row, field) || '-')
                          }
                        </td>
                      )})}
                      <td className="px-6 py-4 text-right flex justify-end gap-2">
                        {canView && (
                          <Link 
                            to={`/app/${objectCode}/${row.Id}`}
                            className={`${global.textSecondary} hover:${theme.text} transition`}
                            title={t('common.view')}
                          >
                            <Eye size={18} />
                          </Link>
                        )}
                        {canEdit && (
                          <Link 
                            to={`/app/${objectCode}/${row.Id}/edit`}
                            className={`${theme.text} transition`}
                            title={t('common.edit')}
                          >
                            <Edit2 size={18} />
                          </Link>
                        )}
                      </td>
                    </tr>
                  )})}
                </tbody>
              </table>
            </div>
            {/* Pagination Controls */}
            <div className={`px-6 py-3 border-t ${global.border} ${global.bgSecondary} flex justify-between items-center text-xs ${global.textSecondary}`}>
               <div>
                  Page {page}
               </div>
               <div className="flex gap-2">
                 <button 
                   onClick={() => setPage(p => Math.max(1, p - 1))}
                   disabled={page === 1}
                   className={`px-3 py-1 rounded border ${global.border} hover:${global.bg} disabled:opacity-50 disabled:cursor-not-allowed`}
                 >
                   Previous
                 </button>
                 <button 
                   onClick={() => setPage(p => p + 1)}
                   disabled={data.length < pageSize}
                   className={`px-3 py-1 rounded border ${global.border} hover:${global.bg} disabled:opacity-50 disabled:cursor-not-allowed`}
                 >
                   Next
                 </button>
               </div>
            </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default GenericObjectList;
