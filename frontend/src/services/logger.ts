
// Basic Logger Service
const API_URL = 'http://localhost:5177/api/logs';
// Hardcoded tenant for now, or could be dynamic
const TENANT_ID = 'LAB003';

interface LogEntry {
    level: 'Error' | 'Warning' | 'Info';
    message: string;
    stackTrace?: string;
    source: 'Frontend' | 'Backend';
    additionalData?: any;
}

export const logger = {
    error: async (message: string, error?: any, additionalData?: any) => {
        // Always log to console first
        console.error(message, error);

        try {
            const payload: LogEntry = {
                level: 'Error',
                message: message,
                stackTrace: error?.stack || error?.toString(),
                source: 'Frontend',
                additionalData: {
                    ...additionalData,
                    url: window.location.href,
                    userAgent: navigator.userAgent,
                    originalError: error ? JSON.stringify(error, Object.getOwnPropertyNames(error)) : null
                }
            };

            await fetch(API_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Tenant-ID': TENANT_ID
                },
                body: JSON.stringify(payload)
            });
        } catch (loggingError) {
            // If logging fails, we don't want to crash the app again
            console.warn("Failed to send log to backend:", loggingError);
        }
    }
};
