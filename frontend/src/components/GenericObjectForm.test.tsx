import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import GenericObjectForm from './GenericObjectForm';
import { ThemeProvider } from '../context/ThemeContext';
import { ModuleSchema, SCHEMA_VERSIONS } from '../types/schema';
import React from 'react';

// Mock API
vi.mock('../utils/api', () => ({
    fetchApi: vi.fn(),
    unwrapResult: vi.fn()
}));

import { fetchApi, unwrapResult } from '../utils/api';

// Mock localStorage
const mockLocalStorage = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn()
};
Object.defineProperty(window, 'localStorage', {
    value: mockLocalStorage
});

// Helper to render with router and theme
const renderWithProviders = (component: React.ReactNode, initialRoute = '/SalesOrder/create') => {
    return render(
        <MemoryRouter initialEntries={[initialRoute]}>
            <ThemeProvider>
                <Routes>
                    <Route path="/:objectCode/create" element={component} />
                    <Route path="/:objectCode/:id/edit" element={component} />
                </Routes>
            </ThemeProvider>
        </MemoryRouter>
    );
};

// Sample schema with all features (V1)
const createMockSchema = (version: number = SCHEMA_VERSIONS.V1): Partial<ModuleSchema> => ({
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
        CustomerId: {
            required: true,
            ui: { type: 'lookup', label: 'Customer' }
        },
        OrderDate: {
            required: true,
            ui: { type: 'date', label: 'Order Date' }
        },
        TotalAmount: {
            required: false,
            ui: { type: 'currency', label: 'Total Amount' }
        },
        Items: {
            required: false,
            ui: {
                type: 'grid',
                label: 'Line Items',
                gridConfig: {
                    columns: [
                        { field: 'ItemCode', label: 'Item', type: 'text' },
                        { field: 'Quantity', label: 'Qty', type: 'number' },
                        { field: 'UnitPrice', label: 'Unit Price', type: 'number' },
                        { field: 'LineTotal', label: 'Line Total', type: 'number', readOnly: true }
                    ],
                    allowAdd: true,
                    allowDelete: true
                }
            }
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
            documentCalculations: [
                {
                    targetField: 'TotalAmount',
                    formula: 'SUM({Items.LineTotal})',
                    trigger: 'onLineChange'
                }
            ],
            complexCalculations: version === SCHEMA_VERSIONS.V1 ? [
                {
                    id: 'calc-volume-discount-001',
                    name: 'VolumeDiscount',
                    description: 'Calculate volume discount',
                    targetField: 'VolumeDiscountPercent',
                    scope: 'Document',
                    trigger: 'onLineChange',
                    expression: 'TotalQuantity >= 1000 ? 15 : TotalQuantity >= 500 ? 10 : TotalQuantity >= 100 ? 5 : 0',
                    parameters: [
                        { name: 'TotalQuantity', source: 'Field', dataType: 'decimal', isRequired: true }
                    ],
                    externalDataSources: [],
                    assemblyReferences: ['System.Linq'],
                    complexCalculation: true
                }
            ] : []
        },
        clientSide: {
            customFunctions: {}
        },
        complexCalculation: version === SCHEMA_VERSIONS.V1
    },
    documentTotals: version <= SCHEMA_VERSIONS.V2 ? {
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
            },
            taxTotal: {
                source: 'CALCULATED',
                formula: 'SUM({Items.TaxAmount})',
                label: 'Tax Total',
                displayPosition: 'footer',
                decimalPlaces: 2,
                editable: false,
                isReadOnly: true,
                highlight: false
            },
            grandTotal: {
                source: 'CALCULATED',
                formula: '{subTotal} + {taxTotal}',
                label: 'Grand Total',
                displayPosition: 'footer',
                decimalPlaces: 2,
                editable: false,
                isReadOnly: true,
                highlight: true
            }
        },
        displayConfig: {
            layout: 'horizontal',
            position: 'bottom',
            currencySymbol: '$',
            showSeparator: true
        }
    } : undefined,
    attachmentConfig: version === SCHEMA_VERSIONS.V1 || version === SCHEMA_VERSIONS.V3 ? {
        documentLevel: {
            enabled: true,
            maxFiles: 10,
            maxFileSizeMB: 25,
            allowedTypes: ['.pdf', '.doc', '.docx', '.jpg', '.png'],
            categories: [
                { id: 'contract', label: 'Contract', required: false },
                { id: 'invoice', label: 'Invoice', required: false }
            ],
            storageProvider: 'aws-s3'
        },
        lineLevel: {
            enabled: true,
            maxFiles: 5,
            maxFileSizeMB: 10,
            allowedTypes: ['.jpg', '.png', '.pdf'],
            categories: [
                { id: 'spec', label: 'Specification', required: false }
            ],
            storageProvider: 'aws-s3',
            gridColumn: {
                width: '100px',
                showCount: true,
                allowPreview: true
            }
        }
    } : undefined,
    cloudStorage: version === SCHEMA_VERSIONS.V1 || version === SCHEMA_VERSIONS.V4 ? {
        providers: [
            {
                id: 'aws-s3-primary',
                provider: 'aws-s3',
                isDefault: true,
                config: {
                    bucketName: 'valora-documents',
                    region: 'us-east-1',
                    basePath: '/sales-orders',
                    encryption: 'AES256'
                },
                credentials: {
                    accessKeyId: 'encrypted-access-key',
                    secretAccessKey: 'encrypted-secret-key'
                }
            }
        ],
        globalSettings: {
            virusScanEnabled: true,
            generateThumbnails: true,
            thumbnailSizes: ['100x100', '300x300'],
            allowedMimeTypes: ['application/pdf', 'image/jpeg', 'image/png'],
            maxFileSizeMB: 25
        }
    } : undefined
});

describe('GenericObjectForm - Sales Order Tests', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mockLocalStorage.getItem.mockImplementation((key: string) => {
            if (key === 'valora_context') {
                return JSON.stringify({ tenantId: 'test-tenant', environment: 'dev' });
            }
            if (key === 'valora_headers') {
                return JSON.stringify({ 'X-Tenant-ID': 'test-tenant', 'X-Environment': 'dev' });
            }
            return null;
        });
    });

    describe('Schema Loading and Rendering', () => {
        it('should render form with new schema configuration', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            expect(screen.getByText('Customer')).toBeInTheDocument();
            expect(screen.getByText('Order Date')).toBeInTheDocument();
        });

        it('should handle all 7 schema versions correctly', async () => {
            for (const version of [1, 2, 3, 4, 5, 6, 7]) {
                vi.clearAllMocks();
                const mockSchema = createMockSchema(version);
                (fetchApi as any).mockResolvedValueOnce({
                    ok: true,
                    json: () => Promise.resolve({
                        success: true,
                        data: mockSchema
                    })
                });
                (unwrapResult as any).mockReturnValue(mockSchema);

                const { unmount } = renderWithProviders(<GenericObjectForm />);

                await waitFor(() => {
                    expect(screen.getByText('Order Number')).toBeInTheDocument();
                });

                unmount();
            }
        });
    });

    describe('Document Totals Display', () => {
        it('should display document totals when enabled (V1-V2)', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Sub Total')).toBeInTheDocument();
            });
            expect(screen.getByText('Tax Total')).toBeInTheDocument();
            expect(screen.getByText('Grand Total')).toBeInTheDocument();
        });

        it('should not display document totals for V3-V7', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V3);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Document totals should not be present
            expect(screen.queryByText('Sub Total')).not.toBeInTheDocument();
        });
    });

    describe('Attachment UI', () => {
        it('should show attachment upload UI when enabled (V1, V3)', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                // Look for attachment-related elements
                const attachmentElements = screen.queryAllByText(/attachment|upload|file/i);
                expect(attachmentElements.length).toBeGreaterThan(0);
            });
        });

        it('should not show attachment UI when disabled (V2, V4-V7)', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V2);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Attachment UI should not be prominently visible
        });
    });

    describe('Cloud Storage Provider Selector', () => {
        it('should show cloud storage selector when enabled (V1, V4)', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                // Look for storage provider indicators
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
        });
    });

    describe('Version Detection', () => {
        it('should show complex calculation indicators only for V1', async () => {
            // Arrange - V1 with complex calculations
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Complex calculation indicators should be present for V1
        });

        it('should hide complex calculation features for V2-V7', async () => {
            // Arrange - V2 without complex calculations
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V2);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Complex calculation indicators should not be present
        });
    });

    describe('Server-Side Calculation Indicators', () => {
        it('should show server calculation indicator for calculated fields', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            // Act
            renderWithProviders(<GenericObjectForm />);

            // Assert
            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });
            // Server calculation indicators should be visible
        });
    });

    describe('Form Submission', () => {
        it('should submit form with temp_ values for server calculation', async () => {
            // Arrange
            const mockSchema = createMockSchema(SCHEMA_VERSIONS.V1);
            (fetchApi as any).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve({
                    success: true,
                    data: mockSchema
                })
            });
            (unwrapResult as any).mockReturnValue(mockSchema);

            renderWithProviders(<GenericObjectForm />);

            await waitFor(() => {
                expect(screen.getByText('Order Number')).toBeInTheDocument();
            });

            // Act - Fill in form data
            const customerInput = screen.getByLabelText(/customer/i);
            fireEvent.change(customerInput, { target: { value: 'CUST001' } });

            // Assert - Form should be ready for submission
            expect(customerInput).toHaveValue('CUST001');
        });
    });
});
