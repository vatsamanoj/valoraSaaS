export interface UiHint {
  type: 'text' | 'number' | 'date' | 'select' | 'textarea' | 'email' | 'tel' | 'currency' | 'money' | 'grid' | 'lookup';
  label?: string;
  mask?: string;
  options?: string[];
  placeholder?: string;
  skipFocus?: boolean;
  searchStrategy?: 'contains' | 'startsWith' | 'wildcard' | 'pattern';
  searchPattern?: string;
  gridConfig?: GridConfig;
  section?: string;
}

export interface FieldRule {
  required: boolean;
  unique?: boolean;
  maxLength?: number;
  isSensitive?: boolean;
  autoGenerate?: boolean;
  pattern?: string;
  ui?: UiHint;
}

export interface GridColumnConfig {
  field: string;
  label: string;
  type: 'text' | 'number' | 'date' | 'select' | 'checkbox' | 'lookup';
  width?: string;
  readOnly?: boolean;
  skipFocus?: boolean;
  options?: string[];
  lookupModule?: string;
  lookupField?: string;
  displayField?: string;
  mapping?: Record<string, string>;
}

export interface GridConfig {
  dataSource?: string;
  allowAdd?: boolean;
  allowDelete?: boolean;
  allowEdit?: boolean;
  columns: (string | GridColumnConfig)[];
  footer?: {
    show: boolean;
    sum: string[];
  };
}

export interface LayoutSection {
  section: string;
  type?: 'default' | 'grid';
  fields?: string[];
  gridConfig?: GridConfig;
}

export interface ModuleUi {
    title?: string;
    icon?: string;
    layout?: LayoutSection[];
    enterKeyNavigation?: boolean;
    listFields?: string[];
    filterFields?: string[];
    rowsPerPage?: number;
    totals?: {
      totalAmountField?: string;
      discountAmountField?: string;
      taxAmountField?: string;
      netAmountField?: string;
      rowsPerPage?: number;
    };
}

export interface ModuleSchema {
  tenantId: string;
  module: string;
  version: number;
  ObjectType?: string;
  objectType?: string;
  fields: Record<string, FieldRule>;
  uniqueConstraints?: string[][];
  ui?: ModuleUi;
}
