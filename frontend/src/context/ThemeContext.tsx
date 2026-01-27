import React, { createContext, useContext, useState, useEffect } from 'react';
import { getApiUrl } from '../utils/api';
import { translations, Language } from '../i18n/translations';
import { loadLocale, deepMerge, TranslationsMap } from '../i18n/dynamicLoader';

export type ThemeMode = 'light' | 'dark' | 'midnight' | 'sepia';
export type ThemeColor = 'blue' | 'indigo' | 'emerald' | 'violet' | 'rose' | 'amber' | 'cyan' | 'teal' | 'lime' | 'fuchsia' | 'orange' | 'slate';

export interface ColorStyles {
  primary: string;
  hover: string;
  text: string;
  textDark: string;
  light: string;
  border: string;
  ring: string;
  focusBorder: string;
  icon: string;
  gradientFrom: string;
  gradientTo: string;
}

export interface GlobalStyles {
  bg: string;
  bgSecondary: string; // Sidebar, headers
  text: string;
  textSecondary: string;
  border: string;
  inputBg: string;
  inputBorder: string;
  overlay: string;
}

const THEME_COLORS: Record<ThemeColor, ColorStyles> = {
  blue: { 
    primary: 'bg-blue-600', 
    hover: 'hover:bg-blue-700', 
    text: 'text-blue-600', 
    textDark: 'text-blue-700', 
    light: 'bg-blue-50', 
    border: 'border-blue-600', 
    ring: 'focus:ring-blue-500', 
    focusBorder: 'focus:border-blue-500',
    icon: 'text-blue-600',
    gradientFrom: 'from-blue-600',
    gradientTo: 'to-indigo-600'
  },
  indigo: { 
    primary: 'bg-indigo-600', 
    hover: 'hover:bg-indigo-700', 
    text: 'text-indigo-600', 
    textDark: 'text-indigo-700', 
    light: 'bg-indigo-50', 
    border: 'border-indigo-600', 
    ring: 'focus:ring-indigo-500', 
    focusBorder: 'focus:border-indigo-500',
    icon: 'text-indigo-600',
    gradientFrom: 'from-indigo-600',
    gradientTo: 'to-purple-600'
  },
  emerald: { 
    primary: 'bg-emerald-600', 
    hover: 'hover:bg-emerald-700', 
    text: 'text-emerald-600', 
    textDark: 'text-emerald-700', 
    light: 'bg-emerald-50', 
    border: 'border-emerald-600', 
    ring: 'focus:ring-emerald-500', 
    focusBorder: 'focus:border-emerald-500',
    icon: 'text-emerald-600',
    gradientFrom: 'from-emerald-600',
    gradientTo: 'to-teal-600'
  },
  violet: { 
    primary: 'bg-violet-600', 
    hover: 'hover:bg-violet-700', 
    text: 'text-violet-600', 
    textDark: 'text-violet-700', 
    light: 'bg-violet-50', 
    border: 'border-violet-600', 
    ring: 'focus:ring-violet-500', 
    focusBorder: 'focus:border-violet-500',
    icon: 'text-violet-600',
    gradientFrom: 'from-violet-600',
    gradientTo: 'to-fuchsia-600'
  },
  rose: { 
    primary: 'bg-rose-600', 
    hover: 'hover:bg-rose-700', 
    text: 'text-rose-600', 
    textDark: 'text-rose-700', 
    light: 'bg-rose-50', 
    border: 'border-rose-600', 
    ring: 'focus:ring-rose-500', 
    focusBorder: 'focus:border-rose-500',
    icon: 'text-rose-600',
    gradientFrom: 'from-rose-600',
    gradientTo: 'to-pink-600'
  },
  amber: { 
    primary: 'bg-amber-600', 
    hover: 'hover:bg-amber-700', 
    text: 'text-amber-600', 
    textDark: 'text-amber-700', 
    light: 'bg-amber-50', 
    border: 'border-amber-600', 
    ring: 'focus:ring-amber-500', 
    focusBorder: 'focus:border-amber-500',
    icon: 'text-amber-600',
    gradientFrom: 'from-amber-600',
    gradientTo: 'to-orange-600'
  },
  cyan: { 
    primary: 'bg-cyan-600', 
    hover: 'hover:bg-cyan-700', 
    text: 'text-cyan-600', 
    textDark: 'text-cyan-700', 
    light: 'bg-cyan-50', 
    border: 'border-cyan-600', 
    ring: 'focus:ring-cyan-500', 
    focusBorder: 'focus:border-cyan-500',
    icon: 'text-cyan-600',
    gradientFrom: 'from-cyan-600',
    gradientTo: 'to-blue-600'
  },
  teal: { 
    primary: 'bg-teal-600', 
    hover: 'hover:bg-teal-700', 
    text: 'text-teal-600', 
    textDark: 'text-teal-700', 
    light: 'bg-teal-50', 
    border: 'border-teal-600', 
    ring: 'focus:ring-teal-500', 
    focusBorder: 'focus:border-teal-500',
    icon: 'text-teal-600',
    gradientFrom: 'from-teal-600',
    gradientTo: 'to-emerald-600'
  },
  lime: { 
    primary: 'bg-lime-600', 
    hover: 'hover:bg-lime-700', 
    text: 'text-lime-600', 
    textDark: 'text-lime-700', 
    light: 'bg-lime-50', 
    border: 'border-lime-600', 
    ring: 'focus:ring-lime-500', 
    focusBorder: 'focus:border-lime-500',
    icon: 'text-lime-600',
    gradientFrom: 'from-lime-600',
    gradientTo: 'to-green-600'
  },
  fuchsia: { 
    primary: 'bg-fuchsia-600', 
    hover: 'hover:bg-fuchsia-700', 
    text: 'text-fuchsia-600', 
    textDark: 'text-fuchsia-700', 
    light: 'bg-fuchsia-50', 
    border: 'border-fuchsia-600', 
    ring: 'focus:ring-fuchsia-500', 
    focusBorder: 'focus:border-fuchsia-500',
    icon: 'text-fuchsia-600',
    gradientFrom: 'from-fuchsia-600',
    gradientTo: 'to-pink-600'
  },
  orange: { 
    primary: 'bg-orange-600', 
    hover: 'hover:bg-orange-700', 
    text: 'text-orange-600', 
    textDark: 'text-orange-700', 
    light: 'bg-orange-50', 
    border: 'border-orange-600', 
    ring: 'focus:ring-orange-500', 
    focusBorder: 'focus:border-orange-500',
    icon: 'text-orange-600',
    gradientFrom: 'from-orange-600',
    gradientTo: 'to-red-600'
  },
  slate: { 
    primary: 'bg-slate-600', 
    hover: 'hover:bg-slate-700', 
    text: 'text-slate-600', 
    textDark: 'text-slate-700', 
    light: 'bg-slate-50', 
    border: 'border-slate-600', 
    ring: 'focus:ring-slate-500', 
    focusBorder: 'focus:border-slate-500',
    icon: 'text-slate-600',
    gradientFrom: 'from-slate-600',
    gradientTo: 'to-gray-600'
  }
};

const GLOBAL_STYLES: Record<ThemeMode, GlobalStyles> = {
    light: {
        bg: 'bg-white',
        bgSecondary: 'bg-gray-50',
        text: 'text-gray-900',
        textSecondary: 'text-gray-500',
        border: 'border-gray-200',
        inputBg: 'bg-white',
        inputBorder: 'border-gray-300',
        overlay: 'bg-gray-500/75'
    },
    dark: {
        bg: 'bg-gray-900',
        bgSecondary: 'bg-gray-800',
        text: 'text-gray-100',
        textSecondary: 'text-gray-400',
        border: 'border-gray-700',
        inputBg: 'bg-gray-800',
        inputBorder: 'border-gray-600',
        overlay: 'bg-gray-900/75'
    },
    midnight: {
        bg: 'bg-[#020617]', // slate-950
        bgSecondary: 'bg-[#0f172a]', // slate-900
        text: 'text-slate-100',
        textSecondary: 'text-slate-400',
        border: 'border-slate-800',
        inputBg: 'bg-[#0f172a]',
        inputBorder: 'border-slate-700',
        overlay: 'bg-black/80'
    },
    sepia: {
        bg: 'bg-[#f8f4e5]',
        bgSecondary: 'bg-[#efeadd]',
        text: 'text-[#433422]',
        textSecondary: 'text-[#8a7e72]',
        border: 'border-[#dcd5c5]',
        inputBg: 'bg-[#fffdf5]',
        inputBorder: 'border-[#dcd5c5]',
        overlay: 'bg-[#433422]/20'
    }
};

interface ThemeContextType {
  mode: ThemeMode;
  color: ThemeColor;
  language: Language;
  currency: string;
  setMode: (mode: ThemeMode) => void;
  setColor: (color: ThemeColor) => void;
  setLanguage: (lang: Language) => void;
  setCurrency: (curr: string) => void;
  saveTheme: () => Promise<void>;
  isSaving: boolean;
  styles: ColorStyles;
  global: GlobalStyles;
  isDark: boolean;
  themeColors: typeof THEME_COLORS;
  t: (key: string, params?: Record<string, string>) => string;
  formatCurrency: (value: number) => string;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [mode, setMode] = useState<ThemeMode>(() => {
        if (typeof window !== 'undefined') {
            const saved = localStorage.getItem('valora_theme_mode');
            if (saved && ['light', 'dark', 'midnight', 'sepia'].includes(saved)) {
                return saved as ThemeMode;
            }
        }
        return 'light';
    });

    const [color, setColor] = useState<ThemeColor>(() => {
        if (typeof window !== 'undefined') {
            const saved = localStorage.getItem('valora_theme_color');
            if (saved && THEME_COLORS[saved as ThemeColor]) {
                return saved as ThemeColor;
            }
        }
        return 'blue';
    });

    const [language, setLanguage] = useState<Language>(() => {
        if (typeof window !== 'undefined') {
            const context = localStorage.getItem('valora_context');
            if (!context) {
                return 'en';
            }

            const saved = localStorage.getItem('valora_language');
            if (saved) {
                return saved as Language;
            }
        }
        return 'en';
    });

    const [currency, setCurrency] = useState<string>(() => {
        if (typeof window !== 'undefined') {
            const saved = localStorage.getItem('valora_currency');
            if (saved) return saved;
        }
        return 'USD';
    });

    const [isSaving, setIsSaving] = useState(false);
    const [dynamicTranslations, setDynamicTranslations] = useState<TranslationsMap | null>(null);

    // Load theme from API on mount (only when logged in)
    useEffect(() => {
        if (typeof window !== 'undefined') {
            const context = localStorage.getItem('valora_context');
            if (!context) {
                return;
            }
        }

        const loadTheme = async () => {
            try {
                const res = await fetch(`${getApiUrl()}/api/user/preferences`);
                if (res.ok) {
                    const data = await res.json();
                    if (data.mode && ['light', 'dark', 'midnight', 'sepia'].includes(data.mode)) {
                        setMode(data.mode);
                    }
                    if (data.color && THEME_COLORS[data.color as ThemeColor]) {
                        setColor(data.color as ThemeColor);
                    }
                    if (data.language) {
                        setLanguage(data.language as Language);
                    }
                    if (data.currency) {
                        setCurrency(data.currency);
                    }
                }
            } catch (e) {
                console.error("Failed to load theme preferences", e);
            }
        };
        loadTheme();
    }, []);

    const saveTheme = async () => {
        setIsSaving(true);
        try {
            const res = await fetch(`${getApiUrl()}/api/user/preferences`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ mode, color, language, currency })
            });
            if (!res.ok) throw new Error('Failed to save');
        } catch (e) {
            console.error("Failed to save theme", e);
        } finally {
            setIsSaving(false);
        }
    };

    const isDark = mode === 'dark' || mode === 'midnight';

    useEffect(() => {
        localStorage.setItem('valora_theme_mode', mode);
        // Apply dark class to body for Tailwind dark mode if configured
        if (isDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }, [mode, isDark]);

    useEffect(() => {
        localStorage.setItem('valora_theme_color', color);
    }, [color]);

    useEffect(() => {
        localStorage.setItem('valora_language', language);
        // Load translations by merging server JSON (DB) over local JSON templates
        (async () => {
            try {
                const [localData, serverRes] = await Promise.all([
                    loadLocale(language),
                    fetch(`${getApiUrl()}/platform/translations/${language}`).catch(() => null)
                ]);

                let serverData: any = {};
                if (serverRes && serverRes.ok) {
                    try {
                        serverData = await serverRes.json();
                    } catch {
                        serverData = {};
                    }
                }

                // Merge priority: server -> local
                const merged = deepMerge(localData || {}, serverData || {});
                setDynamicTranslations(merged);
                localStorage.setItem(`valora_i18n_${language}`, JSON.stringify(merged));
            } catch {
                // Fallback to cached if network/import fails
                const cacheKey = `valora_i18n_${language}`;
                const cached = localStorage.getItem(cacheKey);
                if (cached) {
                    try {
                        const parsed = JSON.parse(cached);
                        setDynamicTranslations(parsed);
                        return;
                    } catch { /* ignore */ }
                }
                // Last resort: try local only
                const localeOnly = await loadLocale(language);
                setDynamicTranslations(localeOnly || null);
                if (localeOnly) {
                    localStorage.setItem(`valora_i18n_${language}`, JSON.stringify(localeOnly));
                }
            }
        })();
    }, [language]);

    useEffect(() => {
        localStorage.setItem('valora_currency', currency);
    }, [currency]);

    // Translation Helper
    const t = (key: string, params?: Record<string, string>) => {
        const keys = key.split('.');
        
        const getValFrom = (source: any) => {
            let v = source;
            for (const k of keys) {
                v = v?.[k];
            }
            return v;
        };

        // Priority: dynamic translations -> static translations
        let value = getValFrom(dynamicTranslations || {});
        if (typeof value !== 'string') {
            value = getValFrom(translations[language]);
        }
        
        // Fallback to English
        if (typeof value !== 'string') {
            value = getValFrom(translations['en']);
        }

        if (typeof value !== 'string') return key;

        if (params) {
            Object.entries(params).forEach(([k, v]) => {
                value = value.replace(`{{${k}}}`, v);
                value = value.replace(`{${k}}`, v);
            });
        }
        return value;
    };

    // Currency Formatter
    const formatCurrency = (value: number) => {
        try {
            return new Intl.NumberFormat(language || 'en-US', {
                style: 'currency',
                currency: currency
            }).format(value);
        } catch (e) {
            try {
                return new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: currency
                }).format(value);
            } catch (e2) {
                return `${currency} ${value}`;
            }
        }
    };

    const styles = THEME_COLORS[color];
    const global = GLOBAL_STYLES[mode];

    // Adjust color styles for dark mode if needed
    // For now, we reuse the same colors, but we might want to lighten/darken them
    // E.g. styles.light might need to be darker in dark mode
    const activeStyles = {
        ...styles,
        light: isDark ? styles.primary.replace('bg-', 'bg-opacity-20 bg-') : styles.light
    };

    return (
        <ThemeContext.Provider value={{
            mode,
            color,
            setMode,
            setColor,
            saveTheme,
            isSaving,
            styles: activeStyles,
            global,
            isDark,
            themeColors: THEME_COLORS,
            language,
            currency,
            setLanguage,
            setCurrency,
            t,
            formatCurrency
        }}>
            {children}
        </ThemeContext.Provider>
    );
};

export const useTheme = () => {
    const context = useContext(ThemeContext);
    if (context === undefined) {
        throw new Error('useTheme must be used within a ThemeProvider');
    }
    return context;
};
