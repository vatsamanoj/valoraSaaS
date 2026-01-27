import { ModuleSchema } from '../types/schema';

export const normalizeSchema = (data: any): ModuleSchema => {
    // Handle API Response Wrapper (unwrap 'data' if present)
    let source = data;
    if (data && typeof data === 'object') {
        // Check for standard API wrapper { success: true, data: { ... } }
        if ('data' in data && 'success' in data && typeof data.data === 'object') {
            source = data.data;
        }
        // Check for alternative wrapper { result: { ... } }
        else if ('result' in data && typeof data.result === 'object') {
            source = data.result;
        }
    }

    const normalized = { ...source };
    
    // Normalize 'fields'
    if (normalized.Fields && !normalized.fields) {
        normalized.fields = normalized.Fields;
    }
    
    // Normalize 'objectType'
    if (normalized.ObjectType && !normalized.objectType) {
        normalized.objectType = normalized.ObjectType;
    }
    if (!normalized.objectType) {
        normalized.objectType = "Master";
    }

    // Normalize 'uniqueConstraints'
    if (normalized.UniqueConstraints && !normalized.uniqueConstraints) {
        normalized.uniqueConstraints = normalized.UniqueConstraints;
    }

    if (normalized.fields) {
        const newFields: any = {};
        const detectedSections: any[] = [];
        const rootFields: string[] = [];

        const processFields = (source: any, parentPath: string = '', sectionLabel: string = '') => {
            const currentSectionFields: string[] = [];

            Object.keys(source).forEach(key => {
                const value = source[key];
                
                // Heuristic to determine if this is a Field Rule or a Nested Section
                // 1. If it has explicit field properties (type, ui, required, etc), it's a Field.
                // 2. If it's an empty object, default to Field (text).
                // 3. If it has children that look like fields, it's a Section.
                // 4. If it has any primitive properties (string, boolean, number), it's likely a Field (e.g. custom props).
                
                const fieldProps = [
                    'type', 'Type', 
                    'required', 'Required', 
                    'ui', 'Ui', 
                    'unique', 'Unique', 
                    'maxLength', 'MaxLength', 
                    'pattern', 'Pattern', 
                    'autoGenerate', 'AutoGenerate', 
                    'isSensitive', 'IsSensitive',
                    'default', 'Default',
                    'defaultValue', 'DefaultValue',
                    'options', 'Options',
                    'readOnly', 'ReadOnly',
                    'min', 'Min',
                    'max', 'Max',
                    'step', 'Step',
                    'dataModel', 'DataModel',
                    'columns', 'Columns',
                    'label', 'Label',
                    'description', 'Description',
                    'lookup', 'Lookup',
                    'placeholder', 'Placeholder',
                    'validation', 'Validation',
                    'visible', 'Visible',
                    'disabled', 'Disabled'
                ];
                
                // Strict Field Check: Must have explicit field properties to be a field.
                let isField = typeof value === 'object' && value !== null && fieldProps.some(p => p in value);
                
                // Leaf Node Check: If it has NO object children (ignoring null/arrays), it's likely a simple field with custom props.
                // This handles "generic" fields that might only have { "someProp": "value" }
                if (!isField && typeof value === 'object' && value !== null) {
                    const hasObjectChildren = Object.values(value).some(v => typeof v === 'object' && v !== null && !Array.isArray(v));
                    if (!hasObjectChildren) {
                        isField = true;
                    }
                }
                
                // Empty object is ambiguous, but in schema context, usually means "default field config" or "empty section".
                // Defaulting to field if empty is safer for "name": {} -> name: { type: text }.
                const isEmpty = typeof value === 'object' && value !== null && Object.keys(value).length === 0;
                
                if (isEmpty) isField = true;
                
                const fullPath = parentPath ? `${parentPath}.${key}` : key;

                if (!isField && typeof value === 'object' && value !== null && !Array.isArray(value)) {
                    // It's a nested section
                    processFields(value, fullPath, key); 
                } else {
                    // It's a field
                    const field = { ...value };
                    
                    // Normalize Field Rule props
                    if (field.Required !== undefined && field.required === undefined) field.required = field.Required;
                    if (field.Unique !== undefined && field.unique === undefined) field.unique = field.Unique;
                    if (field.MaxLength !== undefined && field.maxLength === undefined) field.maxLength = field.MaxLength;
                    if (field.Ui && !field.ui) field.ui = field.Ui;

                    // Normalize UI props
                    if (field.ui) {
                        const newUi = { ...field.ui };
                        if (newUi.Label && !newUi.label) newUi.label = newUi.Label;
                        if (newUi.Type && !newUi.type) newUi.type = newUi.Type;
                        if (newUi.Placeholder && !newUi.placeholder) newUi.placeholder = newUi.Placeholder;
                        if (newUi.Options && !newUi.options) newUi.options = newUi.Options;
                        
                        // Auto-assign section if missing and we have one from nesting
                        if (sectionLabel && !newUi.section) {
                            newUi.section = sectionLabel;
                        }

                        field.ui = newUi;
                    } else if (sectionLabel) {
                        field.ui = { section: sectionLabel };
                    }
                    
                    newFields[fullPath] = field;

                    if (sectionLabel) {
                        currentSectionFields.push(fullPath);
                    } else {
                        rootFields.push(fullPath);
                    }
                }
            });

            if (sectionLabel && currentSectionFields.length > 0) {
                detectedSections.push({
                    section: sectionLabel,
                    fields: currentSectionFields
                });
            }
        };

        processFields(normalized.fields);
        normalized.fields = newFields;

        // Generate default layout if missing and we found sections
        if ((!normalized.ui || !normalized.ui.layout)) {
             if (!normalized.ui) normalized.ui = {};
             
             if (detectedSections.length > 0) {
                 const layout = [];
                 if (rootFields.length > 0) {
                     // Use empty string for root section to avoid rendering a header by default
                     layout.push({ section: '', fields: rootFields });
                 }
                 layout.push(...detectedSections);
                 normalized.ui.layout = layout;
             } else if (rootFields.length > 0) {
                 // No sections detected, just root fields
                 normalized.ui.layout = [{ section: '', fields: rootFields }];
             }
        } else if (normalized.ui && normalized.ui.layout) {
            // Respect existing layout but ensure field paths are correct
            // If the layout refers to nested fields using simple names, we might need to fix them?
            // Actually, the new schema format already uses simple names within sections in the JSON,
            // BUT our processFields creates flattened keys like "General Info.name".
            
            // Wait, if the user provides explicit layout in JSON, they likely use the keys as they appear in the JSON.
            // If the JSON is nested:
            // "General Info": { "name": ... }
            // The user might put "name" in the layout, or "General Info.name"?
            // The template uses:
            // "layout": [ { "section": "General Info", "fields": ["name"] } ]
            // BUT our flattener produced "General Info.name" as the key in `newFields`.
            
            // This is a mismatch! 
            // If we flatten the fields to "General Info.name", but the layout says "name", 
            // the form renderer won't find "name" in `normalized.fields`.
            
            // We need to map the layout fields to the flattened keys.
            // OR, we need to handle this resolution.
            
            // Let's create a map of ShortName -> FullPath
            // Be careful about collisions (e.g. "name" in multiple sections).
            // But in the nested structure, "name" is unique within "General Info".
            
            // Actually, in the provided template:
            // "fields": { "General Info": { "name": ... } }
            // "layout": [ { "section": "General Info", "fields": ["name"] } ]
            
            // Our flattener makes `fields["General Info.name"]`.
            // The renderer looks for `fields["name"]` and fails.
            
            // FIX:
            // We should iterate the layout and update field references to their full path if found.
            
            const fieldMap = new Map<string, string>(); // Short -> Full
            Object.keys(newFields).forEach(fullPath => {
                const parts = fullPath.split('.');
                const shortName = parts[parts.length - 1];
                // We store the most specific match? 
                // Or maybe we try to match based on section context?
                
                // If the layout is explicit, it might group fields by section.
                // If section="General Info", field="name" -> "General Info.name"
            });
            
            // Better approach:
            // Iterate the layout sections.
            normalized.ui.layout = normalized.ui.layout.map((section: any) => {
                 if (!section.fields) return section;
                 
                 const newSectionFields = section.fields.map((fieldName: string) => {
                     // 1. Check if it already matches a key
                     if (newFields[fieldName]) return fieldName;
                     
                     // 2. Try to find it combined with section name?
                     // The template has section: "Billing Header", fields: ["BillNo"]
                     // But fields are defined as "BillNo" at root level in some cases, or nested.
                     
                     // In the template example provided earlier:
                     // fields: { "General Info": { "name": ... } }
                     // layout: [{ section: "General Info", fields: ["name"] }]
                     // flattened key: "General Info.name"
                     
                     // So if we have a section name, try `${section.section}.${fieldName}`
                     // But section name in layout might not match key in fields exactly?
                     // In the template: "fields": { "General Info": ... } -> key is "General Info"
                     // layout section: "General Info"
                     
                     // Try exact match with known sections
                     const candidate = `${section.section}.${fieldName}`;
                     if (newFields[candidate]) return candidate;
                     
                     // 3. Search for suffix match in all fields?
                     // Risk of ambiguity.
                     const match = Object.keys(newFields).find(k => k.endsWith(`.${fieldName}`) || k === fieldName);
                     if (match) return match;
                     
                     // 4. Fallback: If still not found, check if it exists in the original source
                     // If it's a flat field that didn't get nested, it should be in newFields.
                     // If it's missing, it might be an invalid reference.
                     return fieldName;
                 });
                 
                 return { ...section, fields: newSectionFields };
            });
        }
    }

    if (!normalized.fields) normalized.fields = {};

    return normalized as ModuleSchema;
};
