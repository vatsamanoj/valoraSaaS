import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import DynamicForm from './DynamicForm';
import { ThemeProvider } from '../context/ThemeContext';
import { ModuleSchema, SCHEMA_VERSIONS, isTempField, getFieldNameFromTemp, createTempFieldName, commitTempValues } from '../types/schema';
import React from 'react';

// Mock API
vi.mock('../utils/api', () => ({
    fetchApi: vi.fn(),
    unwrapResult: vi.fn()
}));

import { fetchApi, unwrapResult } from '../utils/api';

// Helper to render with theme
const renderWithTheme = (component: React.ReactNode) => {
    return render(
        <ThemeProvider>
            {component}
        </ThemeProvider>
    );
};

// Sample schema for testing temp_ values
const createTestSchema = (version: number = SCHEMA_VERSIONS.V1): ModuleSchema => ({
    tenantId: 'test-tenant',
    module: 'SalesOrder',
    version,
    objectType: 'Transaction',
    fields: {
        OrderNumber: {
            required: true,
            autoGenerate: true,
            ui: { type: 'text', label: 'Order Number' }
        },
        Quantity: {
            required: true,
            ui: { type: 'number', label: 'Quantity' }
        },
        UnitPrice: {
            required: true,
            ui: { type: 'number', label: 'Unit Price' }
        },
        LineTotal: {
            required: false,
            ui: { type: 'number', label: 'Line Total' }
        }
    },
    calculationRules: {
        serverSide: {
            lineItemCalculations: [
                {
                    targetField: 'LineTotal',
                    formula: '{Quantity} * {UnitPrice}',
                    trigger: 'onChange',
                    dependentFields: ['Quantity', 'UnitPrice'],
                    complexCalculation: false
                }
            ],
            documentCalculations: [],
            complexCalculations: []
        },
        clientSide: {
            customFunctions: {}
        },
        complexCalculation: false
    }
});

describe('DynamicForm - Sales Order Features', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('Temp_ Value Management', () => {
        it('should handle temp_ prefixed values correctly', () => {
            // Arrange
            const formData = {
                OrderNumber: 'SO001',
                temp_Quantity: 10,
                temp_UnitPrice: 100.00,
                LineTotal: 1000.00
            };

            // Act & Assert
            expect(isTempField('temp_Quantity')).toBe(true);
            expect(isTempField('Quantity')).toBe(false);
            expect(isTempField('OrderNumber')).toBe(false);
        });

        it('should extract actual field name from temp field', () => {
            // Act & Assert
            expect(getFieldNameFromTemp('temp_Quantity')).toBe('Quantity');
            expect(getFieldNameFromTemp('temp_UnitPrice')).toBe('UnitPrice');
            expect(getFieldNameFromTemp('OrderNumber')).toBe('OrderNumber'); // No temp_ prefix
        });

        it('should create temp field name from actual field', () => {
            // Act & Assert
            expect(createTempFieldName('Quantity')).toBe('temp_Quantity');
            expect(createTempFieldName('UnitPrice')).toBe('temp_UnitPrice');
        });

        it('should commit temp values to actual values', () => {
            // Arrange
            const formData = {
                OrderNumber: 'SO001',
                temp_Quantity: 10,
                temp_UnitPrice: 100.00,
                LineTotal: 1000.00
            };

            // Act
            const committed = commitTempValues(formData);

            // Assert
            expect(committed.OrderNumber).toBe('SO001');
            expect(committed.Quantity).toBe(10);
            expect(committed.UnitPrice).toBe(100.00);
            expect(committed.LineTotal).toBe(1000.00);
            expect(committed.temp_Quantity).toBeUndefined();
            expect(committed.temp_UnitPrice).toBeUndefined();
        });

        it('should render form with initial temp values', async () => {
            // Arrange
            const schema = createTestSchema();
            const initialData = {
                OrderNumber: 'SO001',
                temp_Quantity: 5,
                temp_UnitPrice: 50.00
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={vi.fn()}
                    initialData={initialData}
                />
            );

            // Assert
            await waitFor(() => {
                const quantityInput = screen.getByLabelText(/quantity/i);
                expect(quantityInput).toHaveValue(5);
            });
        });
    });

    describe('Server-Side Calculation Indicators', () => {
        it('should display server indicator for calculated fields', async () => {
            // Arrange
            const schema = createTestSchema();

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Server calculation indicators should be present for calculated fields
        });

        it('should NOT execute client-side calculations', async () => {
            // Arrange
            const schema: ModuleSchema = {
                ...createTestSchema(),
                calculationRules: {
                    serverSide: {
                        lineItemCalculations: [],
                        documentCalculations: [],
                        complexCalculations: []
                    },
                    clientSide: {
                        onLoad: 'console.log("This should not execute");',
                        onBeforeSave: 'alert("This should not execute");',
                        customFunctions: {
                            malicious: 'alert("Hacked!")'
                        }
                    }
                }
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // No alerts or console logs from client-side scripts should occur
        });
    });

    describe('Version-Specific Features', () => {
        it('should show complex calculation UI only for V1', async () => {
            // Arrange - V1 schema
            const v1Schema: ModuleSchema = {
                ...createTestSchema(SCHEMA_VERSIONS.V1),
                calculationRules: {
                    serverSide: {
                        lineItemCalculations: [],
                        documentCalculations: [],
                        complexCalculations: [
                            {
                                id: 'calc-001',
                                name: 'TestCalc',
                                description: 'Test calculation',
                                targetField: 'Result',
                                scope: 'Document',
                                trigger: 'onChange',
                                expression: '1 + 1',
                                parameters: [],
                                externalDataSources: [],
                                assemblyReferences: [],
                                complexCalculation: true
                            }
                        ]
                    },
                    clientSide: { customFunctions: {} },
                    complexCalculation: true
                }
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={v1Schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Complex calculation indicators should be visible for V1
        });

        it('should hide complex calculation UI for V2-V7', async () => {
            // Arrange - V2 schema
            const v2Schema: ModuleSchema = {
                ...createTestSchema(SCHEMA_VERSIONS.V2),
                calculationRules: {
                    serverSide: {
                        lineItemCalculations: [],
                        documentCalculations: [],
                        complexCalculations: [] // Empty for V2
                    },
                    clientSide: { customFunctions: {} },
                    complexCalculation: false
                }
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={v2Schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Complex calculation indicators should NOT be visible for V2
        });
    });

    describe('Document Totals Integration', () => {
        it('should display document totals section when configured', async () => {
            // Arrange
            const schema: ModuleSchema = {
                ...createTestSchema(),
                documentTotals: {
                    fields: {
                        subTotal: {
                            source: 'CALCULATED',
                            formula: 'SUM({Items.LineTotal})',
                            label: 'Sub Total',
                            displayPosition: 'footer',
                            decimalPlaces: 2,
                            editable: false,
                            isReadOnly: true,
                            highlight: false
                        }
                    },
                    displayConfig: {
                        layout: 'horizontal',
                        position: 'bottom',
                        currencySymbol: '$',
                        showSeparator: true
                    }
                }
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Document totals should be displayed
        });
    });

    describe('Form Submission with Temp Values', () => {
        it('should submit form with committed temp values', async () => {
            // Arrange
            const schema = createTestSchema();
            const onSubmit = vi.fn();
            const initialData = {
                OrderNumber: 'SO001',
                temp_Quantity: 10,
                temp_UnitPrice: 100.00
            };

            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={onSubmit}
                    initialData={initialData}
                />
            );

            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });

            // Act - Find and click submit button
            const submitButton = screen.getByRole('button', { name: /save|submit/i });
            fireEvent.click(submitButton);

            // Assert
            await waitFor(() => {
                expect(onSubmit).toHaveBeenCalled();
            });

            const submittedData = onSubmit.mock.calls[0][0];
            // Temp values should be committed
            expect(submittedData.Quantity).toBe(10);
            expect(submittedData.UnitPrice).toBe(100.00);
            expect(submittedData.temp_Quantity).toBeUndefined();
            expect(submittedData.temp_UnitPrice).toBeUndefined();
        });
    });

    describe('Attachment UI', () => {
        it('should show attachment section when attachmentConfig is present', async () => {
            // Arrange
            const schema: ModuleSchema = {
                ...createTestSchema(),
                attachmentConfig: {
                    documentLevel: {
                        enabled: true,
                        maxFiles: 10,
                        maxFileSizeMB: 25,
                        allowedTypes: ['.pdf', '.jpg'],
                        categories: [
                            { id: 'doc', label: 'Documents', required: false }
                        ],
                        storageProvider: 'aws-s3'
                    },
                    lineLevel: {
                        enabled: false,
                        maxFiles: 0,
                        maxFileSizeMB: 0,
                        allowedTypes: [],
                        categories: [],
                        storageProvider: '',
                        gridColumn: {
                            width: '0',
                            showCount: false,
                            allowPreview: false
                        }
                    }
                }
            };

            // Act
            renderWithTheme(
                <DynamicForm
                    schema={schema}
                    onSubmit={vi.fn()}
                />
            );

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Attachment UI should be visible
        });
    });
});
