import { fetchApi, unwrapResult } from '../utils/api';

export type TranslationsMap = Record<string, any>;

const globLocales: Record<string, () => Promise<any>> = import.meta.glob('./locales/*.json');

export const loadLocale = async (lang: string): Promise<TranslationsMap | null> => {
  let localData: TranslationsMap = {};
  let serverData: TranslationsMap = {};

  // 1. Load Local
  const key = `./locales/${lang}.json`;
  if (globLocales[key]) {
    try {
      const mod = await globLocales[key]();
      localData = (mod?.default || mod) as TranslationsMap;
    } catch (e) {
      console.warn(`Failed to load local locale: ${lang}`, e);
    }
  }

  // 2. Load Server
  try {
    const res = await fetchApi(`/platform/translations/${lang}`);
    const data = await unwrapResult<TranslationsMap>(res);
    if (data) {
        serverData = data;
    }
  } catch (e) {
    console.warn(`Failed to load server locale: ${lang}`, e);
  }

  // 3. Merge (Server overrides Local)
  // If both are empty, return null to indicate failure/fallback? 
  // But usually we return at least empty object.
  // Original returned null if loader didn't exist.
  // Now we might return empty object if neither exists.
  // Let's check if we have any data.
  if (Object.keys(localData).length === 0 && Object.keys(serverData).length === 0) {
      // If the language is NOT in local files AND not in server, maybe return null?
      // But if we want to support "server-only" languages, we should return serverData.
      // If both empty, return null so ThemeContext falls back to static 'translations.ts' or 'en'.
      return null;
  }

  return deepMerge(localData, serverData);
};

export const deepMerge = (base: TranslationsMap, override: TranslationsMap): TranslationsMap => {
  const result: TranslationsMap = Array.isArray(base) ? [...base] : { ...base };
  Object.keys(override || {}).forEach((k) => {
    const bv = (base || {})[k];
    const ov = override[k];
    if (bv && typeof bv === 'object' && !Array.isArray(bv) && ov && typeof ov === 'object' && !Array.isArray(ov)) {
      result[k] = deepMerge(bv, ov);
    } else {
      result[k] = ov;
    }
  });
  return result;
};
