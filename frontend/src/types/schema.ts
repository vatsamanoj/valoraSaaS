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

// ===== Schema Version Support =====

/**
 * Schema Version Constants
 * - V1: Full new schema features (primary injection point)
 * - V2-V7: Backward compatibility with partial or no support
 */
export const SCHEMA_VERSIONS = {
  V1: 1, // Full features: complex calculations, temp values, document totals, attachments, cloud storage
  V2: 2, // Partial: document totals, basic calculations
  V3: 3, // Partial: attachments support
  V4: 4, // Partial: cloud storage
  V5: 5, // Legacy compatibility
  V6: 6, // Legacy compatibility
  V7: 7, // Legacy compatibility
} as const;

export type SchemaVersion = typeof SCHEMA_VERSIONS[keyof typeof SCHEMA_VERSIONS];

/**
 * Feature support flags based on schema version
 */
export interface SchemaFeatureSupport {
  complexCalculations: boolean;
  documentTotals: boolean;
  attachments: boolean;
  cloudStorage: boolean;
  tempValues: boolean;
  clientSideScripts: boolean; // Always false - no client-side JS execution
}

/**
 * Get feature support based on schema version
 */
export function getSchemaFeatureSupport(version: number): SchemaFeatureSupport {
  return {
    // V1: Full support
    // V2-V7: Partial or no support
    complexCalculations: version === SCHEMA_VERSIONS.V1,
    documentTotals: version <= SCHEMA_VERSIONS.V2,
    attachments: version === SCHEMA_VERSIONS.V1 || version === SCHEMA_VERSIONS.V3,
    cloudStorage: version === SCHEMA_VERSIONS.V1 || version === SCHEMA_VERSIONS.V4,
    tempValues: version === SCHEMA_VERSIONS.V1,
    clientSideScripts: false, // Never supported - server-side only
  };
}

// ===== Template Configuration Types =====

// Calculation Rules
export interface CalculationParameter {
  name: string;
  source: string;
  dataType?: string;
  isRequired: boolean;
}

export interface ExternalDataSource {
  name: string;
  sourceType: string;
  queryOrEndpoint?: string;
  parameters: Record<string, string>;
}

export interface LineItemCalculation {
  targetField: string;
  formula: string;
  trigger: string;
  dependentFields: string[];
  condition?: string;
  // NEW: Complex calculation flag
  complexCalculation?: boolean;
}

export interface DocumentCalculation {
  targetField: string;
  formula: string;
  trigger: string;
  // NEW: Complex calculation flag
  complexCalculation?: boolean;
}

export interface ComplexCalculation {
  id: string;
  name: string;
  description: string;
  targetField: string;
  scope: 'LineItem' | 'Document';
  trigger: string;
  expression: string;
  codeBlock?: string;
  parameters: CalculationParameter[];
  externalDataSources: ExternalDataSource[];
  assemblyReferences: string[];
  // NEW: Complex calculation flag - always true for ComplexCalculation
  complexCalculation: true;
}

export interface ServerSideCalculations {
  lineItemCalculations: LineItemCalculation[];
  documentCalculations: DocumentCalculation[];
  complexCalculations: ComplexCalculation[];
}

/**
 * DEPRECATED: Client-side calculations are no longer supported.
 * All calculations must be server-side for security.
 * This interface is kept for backward compatibility with existing schemas.
 */
export interface ClientSideCalculations {
  onLoad?: string; // DEPRECATED - not executed
  onBeforeSave?: string; // DEPRECATED - not executed
  onLineItemAdd?: string; // DEPRECATED - not executed
  onLineItemRemove?: string; // DEPRECATED - not executed
  customFunctions: Record<string, string>; // DEPRECATED - not executed
}

export interface CalculationRulesConfig {
  serverSide: ServerSideCalculations;
  /**
   * DEPRECATED: Client-side calculations are no longer supported.
   * Kept for backward compatibility with existing schemas.
   */
  clientSide: ClientSideCalculations;
  /**
   * NEW: Global flag to enable complex calculations (C# template-driven)
   * When true, uses C# template-driven calculations
   * When false, uses normal line/document calculations
   */
  complexCalculation?: boolean;
}

// Document Totals
export interface TotalFieldConfig {
  source: string;
  formula?: string;
  label: string;
  displayPosition: string;
  decimalPlaces: number;
  editable: boolean;
  isReadOnly: boolean;
  highlight: boolean;
  defaultValue?: number;
}

export interface TotalsDisplayConfig {
  layout: string;
  position: string;
  currencySymbol?: string;
  showSeparator: boolean;
}

export interface DocumentTotalsConfig {
  fields: Record<string, TotalFieldConfig>;
  displayConfig: TotalsDisplayConfig;
}

// Attachments
export interface AttachmentCategory {
  id: string;
  label: string;
  required: boolean;
  maxFilesPerCategory?: number;
}

export interface AttachmentGridColumnConfig {
  width: string;
  showCount: boolean;
  allowPreview: boolean;
}

export interface DocumentLevelAttachmentConfig {
  enabled: boolean;
  maxFiles: number;
  maxFileSizeMB: number;
  allowedTypes: string[];
  categories: AttachmentCategory[];
  storageProvider: string;
}

export interface LineLevelAttachmentConfig {
  enabled: boolean;
  maxFiles: number;
  maxFileSizeMB: number;
  allowedTypes: string[];
  categories: AttachmentCategory[];
  storageProvider: string;
  gridColumn: AttachmentGridColumnConfig;
}

export interface AttachmentConfig {
  documentLevel: DocumentLevelAttachmentConfig;
  lineLevel: LineLevelAttachmentConfig;
}

// Cloud Storage
export interface ProviderConfig {
  bucketName?: string;
  region?: string;
  accountName?: string;
  containerName?: string;
  projectId?: string;
  basePath: string;
  encryption?: string;
}

export interface EncryptedCredentials {
  accessKeyId?: string;
  secretAccessKey?: string;
  sessionToken?: string;
  connectionString?: string;
  serviceAccountKey?: string;
}

export interface LifecycleRules {
  autoDeleteAfterDays?: number;
  moveToColdStorageAfterDays?: number;
}

export interface StorageProviderConfig {
  id: string;
  provider: string;
  isDefault: boolean;
  config: ProviderConfig;
  credentials: EncryptedCredentials;
  lifecycleRules?: LifecycleRules;
}

export interface GlobalStorageSettings {
  virusScanEnabled: boolean;
  generateThumbnails: boolean;
  thumbnailSizes: string[];
  allowedMimeTypes: string[];
  maxFileSizeMB: number;
}

export interface CloudStorageConfig {
  providers: StorageProviderConfig[];
  globalSettings: GlobalStorageSettings;
}

// ===== Temporary Values Management =====

/**
 * Temporary value entry - stored with temp_ prefix
 * Only committed to actual values on save
 */
export interface TempValueEntry {
  fieldName: string;
  tempValue: any;
  originalValue: any;
  timestamp: number;
}

/**
 * Form state with temporary values
 */
export interface FormStateWithTempValues {
  committed: Record<string, any>;
  temp: Record<string, any>; // Values with temp_ prefix
}

/**
 * Check if a field name is a temporary value
 */
export function isTempField(fieldName: string): boolean {
  return fieldName.startsWith('temp_');
}

/**
 * Get the actual field name from a temp field name
 */
export function getFieldNameFromTemp(tempFieldName: string): string {
  return tempFieldName.replace(/^temp_/, '');
}

/**
 * Create a temp field name from an actual field name
 */
export function createTempFieldName(fieldName: string): string {
  return `temp_${fieldName}`;
}

/**
 * Extract all temp values from form data
 */
export function extractTempValues(formData: Record<string, any>): Record<string, any> {
  const tempValues: Record<string, any> = {};
  for (const [key, value] of Object.entries(formData)) {
    if (isTempField(key)) {
      tempValues[key] = value;
    }
  }
  return tempValues;
}

/**
 * Commit temp values to actual values
 * Returns the committed data without temp_ prefixes
 */
export function commitTempValues(formData: Record<string, any>): Record<string, any> {
  const committed = { ...formData };
  
  for (const [key, value] of Object.entries(formData)) {
    if (isTempField(key)) {
      const actualFieldName = getFieldNameFromTemp(key);
      committed[actualFieldName] = value;
      delete committed[key];
    }
  }
  
  return committed;
}

/**
 * Prepare form data for submission
 * - Commits temp values
 * - Removes any client-side only fields
 */
export function prepareFormDataForSubmission(
  formData: Record<string, any>,
  schema: ModuleSchema
): Record<string, any> {
  // Commit temp values
  let prepared = commitTempValues(formData);
  
  // Remove any calculated fields that should be server-side only
  if (schema.calculationRules?.serverSide) {
    const calculatedFields = new Set<string>();
    
    schema.calculationRules.serverSide.lineItemCalculations.forEach(calc => {
      calculatedFields.add(calc.targetField);
    });
    schema.calculationRules.serverSide.documentCalculations.forEach(calc => {
      calculatedFields.add(calc.targetField);
    });
    schema.calculationRules.serverSide.complexCalculations.forEach(calc => {
      calculatedFields.add(calc.targetField);
    });
    
    // Don't remove from submission - server will validate
    // Just mark them as server-calculated in metadata
  }
  
  return prepared;
}

// ===== Main Module Schema =====

export interface ModuleSchema {
  tenantId: string;
  module: string;
  version: number;
  ObjectType?: string;
  objectType?: string;
  fields: Record<string, FieldRule>;
  uniqueConstraints?: string[][];
  ui?: ModuleUi;
  // Template Configuration Extensions
  calculationRules?: CalculationRulesConfig;
  documentTotals?: DocumentTotalsConfig;
  attachmentConfig?: AttachmentConfig;
  cloudStorage?: CloudStorageConfig;
  // Smart Projection Configuration
  smartProjection?: SmartProjectionConfig;
}

// ===== Smart Projection Configuration Types =====

export type IndexType = 'Standard' | 'Text' | 'Hashed' | 'Wildcard' | 'Compound';
export type DenormalizationUpdateStrategy = 'OnWrite' | 'OnRead' | 'Scheduled' | 'EventDriven';
export type CacheProvider = 'Memory' | 'Redis' | 'Distributed';
export type EvictionPolicy = 'LRU' | 'LFU' | 'Random';
export type ValidationLevel = 'Off' | 'Strict' | 'Moderate';
export type ValidationAction = 'Error' | 'Warn';
export type ArchiveDestination = 'ColdStorage' | 'S3' | 'SeparateCollection';
export type ArchiveCompression = 'None' | 'Gzip' | 'Bzip2' | 'Lz4';
export type CompressionAlgorithm = 'Gzip' | 'Zstd' | 'Lz4' | 'Snappy';

export interface IndexConfig {
  name: string;
  fields: Record<string, number>; // field name -> 1 (asc) or -1 (desc)
  type: IndexType;
  isUnique?: boolean;
  isSparse?: boolean;
  partialFilterExpression?: string;
  collation?: CollationConfig;
  expireAfterSeconds?: number;
  isAutoGenerated?: boolean;
  usageStats?: IndexUsageStats;
}

export interface CollationConfig {
  locale: string;
  caseLevel?: boolean;
  caseFirst?: string;
  strength?: number;
  numericOrdering?: boolean;
  alternate?: string;
  maxVariable?: string;
  normalization?: boolean;
  backwards?: boolean;
}

export interface IndexUsageStats {
  queryCount: number;
  lastUsedAt?: string;
  averageQueryTimeMs: number;
  scanToReturnRatio: number;
}

export interface DenormalizationConfig {
  name: string;
  sourceEntity: string;
  targetFieldPath: string;
  sourceFields: string[];
  foreignKeyField: string;
  updateStrategy: DenormalizationUpdateStrategy;
  scheduleCron?: string;
  embedAsArray?: boolean;
  maxDepth?: number;
}

export interface ShardingConfig {
  shardKey: Record<string, number>;
  isUnique?: boolean;
  initialShards?: number;
  zones?: ShardZoneConfig[];
}

export interface ShardZoneConfig {
  name: string;
  min: string;
  max: string;
}

export interface CacheConfig {
  enabled: boolean;
  provider: CacheProvider;
  defaultTtlMinutes?: number;
  patterns?: CachePatternConfig[];
  cacheKeyFields?: string[];
  maxSizeMb?: number;
  evictionPolicy?: EvictionPolicy;
}

export interface CachePatternConfig {
  name: string;
  queryPattern: string;
  ttlMinutes: number;
  invalidateOnWrite?: boolean;
}

export interface AggregationPipelineConfig {
  name: string;
  description: string;
  stages: Record<string, unknown>[];
  isAutoGenerated?: boolean;
  usageStats?: PipelineUsageStats;
}

export interface PipelineUsageStats {
  executionCount: number;
  averageExecutionTimeMs: number;
  lastExecutedAt?: string;
}

export interface DocumentValidationConfig {
  jsonSchema?: Record<string, unknown>;
  level: ValidationLevel;
  action: ValidationAction;
}

export interface ArchivalConfig {
  enabled: boolean;
  ageField: string;
  archiveAfterDays: number;
  destination: ArchiveDestination;
  targetName: string;
  compression: ArchiveCompression;
  deleteAfterArchive?: boolean;
  schedule?: string;
}

export interface CompressionConfig {
  enabled: boolean;
  fields: string[];
  minSizeBytes?: number;
  algorithm: CompressionAlgorithm;
  level?: number;
}

export interface QueryPatternConfig {
  enabled: boolean;
  sampleRate?: number;
  maxPatterns?: number;
  minQueryCountForAutoIndex?: number;
  analysisWindowHours?: number;
  autoCreateIndexes?: boolean;
  autoSuggestDenormalizations?: boolean;
}

export interface SmartProjectionConfig {
  autoOptimize?: boolean;
  indexes?: IndexConfig[];
  denormalizations?: DenormalizationConfig[];
  ttlDays?: number;
  sharding?: ShardingConfig;
  caching?: CacheConfig;
  aggregationPipelines?: AggregationPipelineConfig[];
  validation?: DocumentValidationConfig;
  archival?: ArchivalConfig;
  compression?: CompressionConfig;
  queryPatternTracking?: QueryPatternConfig;
}
