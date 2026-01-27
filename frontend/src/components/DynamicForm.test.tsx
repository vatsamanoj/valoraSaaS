import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import DynamicForm from './DynamicForm';
import { normalizeSchema } from '../utils/schemaUtils';
import { ThemeProvider } from '../context/ThemeContext';
import React from 'react';

// Mock API to avoid network calls
vi.mock('../utils/api', () => ({
    fetchApi: vi.fn(),
    unwrapResult: vi.fn()
}));

// Wrap component with ThemeProvider
const renderWithTheme = (component: React.ReactNode) => {
    return render(
        <ThemeProvider>
            {component}
        </ThemeProvider>
    );
};

describe('DynamicForm & Schema Normalization', () => {

    it('should normalize generic fields with "columns" property as valid fields', () => {
        const rawSchema = {
            module: 'TestModule',
            fields: {
                "SimpleField": { "type": "text" },
                "GenericGrid": {
                    "columns": {
                        "Col1": { "type": "text" },
                        "Col2": { "type": "number" }
                    }
                }
            }
        };

        const normalized = normalizeSchema(rawSchema);

        // Check if SimpleField is preserved
        expect(normalized.fields['SimpleField']).toBeDefined();

        // Check if GenericGrid is preserved (it should be detected as a field due to 'columns' prop)
        expect(normalized.fields['GenericGrid']).toBeDefined();
        
        // Ensure it didn't get flattened incorrectly or ignored
        expect((normalized.fields['GenericGrid'] as any).columns).toBeDefined();
    });

    it('should render a Grid for a field with "columns" property even without explicit type="grid"', async () => {
        const rawSchema = {
            module: 'PathologyBilling',
            fields: {
                "BillNo": { "ui": { "label": "Bill Number", "type": "text" } },
                "Items": {
                    "columns": {
                        "TestName": { "ui": { "type": "text" } },
                        "Price": { "ui": { "type": "number" } }
                    }
                }
            }
        };

        const normalized = normalizeSchema(rawSchema);
        
        renderWithTheme(
            <DynamicForm 
                schema={normalized} 
                onSubmit={vi.fn()} 
            />
        );

        // 1. Verify standard field rendering
        expect(screen.getByText('Bill Number')).toBeInTheDocument();

        // 2. Verify Grid rendering
        // The Grid renderer (FormGrid) usually renders table headers.
        // Based on FormGrid.tsx, it renders headers for columns.
        // We expect to see "TestName" and "Price" (or their labels) in the document.
        
        // Wait for potential async effects (though grid rendering should be immediate now)
        await waitFor(() => {
            expect(screen.getByText('TestName')).toBeInTheDocument();
            expect(screen.getByText('Price')).toBeInTheDocument();
        });

        // Verify "Add Item" button exists (default allowAdd is true)
        expect(screen.getByText('Add Item')).toBeInTheDocument();
    });

    it('should render inline grid within a generic layout', () => {
        // This tests the "Live Preview" scenario where no layout is defined initially
        const rawSchema = {
            module: 'InlineTest',
            fields: {
                "LineItems": {
                    "columns": {
                        "Item": {},
                        "Qty": {}
                    }
                }
            }
        };
        const normalized = normalizeSchema(rawSchema);

        renderWithTheme(
            <DynamicForm 
                schema={normalized} 
                onSubmit={vi.fn()} 
            />
        );

        // Should see headers for Item and Qty
        expect(screen.getByText('Item')).toBeInTheDocument();
        expect(screen.getByText('Qty')).toBeInTheDocument();
    });
});
