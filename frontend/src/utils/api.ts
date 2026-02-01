import { supabase } from '../lib/supabase';

export const getApiUrl = () => {
    if (typeof window === 'undefined') return '';
    // Use window.location to determine if we are in dev mode
    // If accessing via localhost, assume backend is at :5028 (standard .NET Kestrel)
    // If accessing via 127.0.0.1, likewise.
    const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
    
    // Hardcode port 5028 for now as that's what the backend is running on
    return isLocalhost ? 'http://localhost:5028' : 'http://your-prod-api';
};

export const getHeaders = async () => {
    const { data } = await supabase.auth.getSession();
    const token = data.session?.access_token;

    let extraHeaders: Record<string, string> = {};

    if (typeof window !== 'undefined') {
        try {
            const raw = window.localStorage.getItem('valora_headers');
            if (raw) {
                const parsed = JSON.parse(raw) as Record<string, string>;
                extraHeaders = parsed;
            }
        } catch {
        }
    }

    const baseHeaders: Record<string, string> = {
        'Content-Type': 'application/json'
    };

    if (token) {
        baseHeaders['Authorization'] = `Bearer ${token}`;
    }

    // Try to get Tenant from valora_context if missing in headers
    if (!extraHeaders['X-Tenant-Id'] && typeof window !== 'undefined') {
        try {
            const contextRaw = window.localStorage.getItem('valora_context');
            if (contextRaw) {
                const context = JSON.parse(contextRaw);
                if (context.tenantId) {
                    extraHeaders['X-Tenant-Id'] = context.tenantId;
                }
            }
        } catch {}
    }

    // Default Tenant ID for Development/Lab Mode if still not set
    if (!extraHeaders['X-Tenant-Id']) {
        extraHeaders['X-Tenant-Id'] = 'LAB_001';
    }

    return {
        ...baseHeaders,
        ...extraHeaders
    };
};

export interface ApiError {
    code: string;
    message: string;
}

export class ApiResultError extends Error {
    public errors: ApiError[];
    constructor(message: string, errors: ApiError[]) {
        super(message);
        this.errors = errors;
        this.name = 'ApiResultError';
    }
}

export interface ApiResult<T> {
    success: boolean;
    tenantId: string;
    module: string;
    action: string;
    data: T;
    errors: ApiError[];
}

export const unwrapResult = async <T>(response: Response): Promise<T> => {
    const text = await response.text();
    try {
        const json = JSON.parse(text);
        if (json && typeof json === 'object' && 'success' in json) {
            const result = json as ApiResult<T>;
            if (!result.success) {
                const msg = result.errors?.[0]?.message || 'API request failed';
                throw new ApiResultError(msg, result.errors || []);
            }
            return result.data;
        }
        
        // Handle non-ApiResult JSON error responses (legacy)
        if (!response.ok) {
             throw new Error(json.message || json.detail || response.statusText);
        }

        return json as T;
    } catch (e) {
        if (e instanceof ApiResultError) throw e;
        
        // If parsing failed or it's not JSON
        if (!response.ok) {
            throw new Error(text || response.statusText);
        }
        // If ok but not JSON (or simple primitive)
        return text as unknown as T;
    }
};

export const api = {
    write: async (endpoint: string, data: any, method = 'POST') => {
        const headers = await getHeaders();
        const response = await fetch(`${getApiUrl()}/api/data${endpoint}`, {
            method,
            headers: headers as any,
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            // Try to extract error from ApiResult if possible
            try {
                return await unwrapResult(response);
            } catch (e: any) {
                throw new Error(e.message || 'Write failed');
            }
        }
        return unwrapResult(response);
    },

    read: async (endpoint: string, params: any = {}) => {
        const headers = await getHeaders();
        const queryString = new URLSearchParams(params).toString();
        const url = `${getApiUrl()}/api/query${endpoint}${queryString ? '?' + queryString : ''}`;
        
        const response = await fetch(url, {
            method: 'GET',
            headers: headers as any
        });
        if (!response.ok) {
             try {
                return await unwrapResult(response);
            } catch (e: any) {
                throw new Error(e.message || 'Read failed');
            }
        }
        return unwrapResult(response);
    },
    
    get: async (endpoint: string, options: RequestInit = {}) => {
        const headers = await getHeaders();
        const url = endpoint.startsWith('http') ? endpoint : `${getApiUrl()}${endpoint}`;
        
        const res = await fetch(url, {
            ...options,
            method: 'GET',
            headers: {
                ...headers,
                ...(options.headers || {})
            }
        });

        // Wrap in object with data property to match Axios-like usage in some components
        let json;
        try {
            const text = await res.text();
            try {
                json = JSON.parse(text);
            } catch {
                console.warn("API response was not JSON:", text.substring(0, 100));
                json = null;
            }
        } catch (e) {
            console.error("Failed to read response body", e);
            json = null;
        }
        return { data: json, status: res.status, statusText: res.statusText, ok: res.ok };
    },
    
    fetch: async (endpoint: string, options: RequestInit = {}) => {
        const headers = await getHeaders();
        const url = endpoint.startsWith('http') ? endpoint : `${getApiUrl()}${endpoint}`;
        
        return fetch(url, {
            ...options,
            headers: {
                ...headers,
                ...(options.headers || {})
            }
        });
    }
};

export const fetchApi = api.fetch;
