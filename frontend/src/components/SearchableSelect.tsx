import React, { useState, useRef, useEffect, KeyboardEvent } from 'react';
import { Search, ChevronDown, Check, X } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';

interface Option {
    value: string;
    label: string;
    subLabel?: string;
}

interface SearchableSelectProps {
    id?: string;
    value: string;
    onChange: (value: string) => void;
    options: Option[];
    placeholder?: string;
    label?: string;
    icon?: React.ReactNode;
    className?: string;
}

const SearchableSelect: React.FC<SearchableSelectProps> = ({ 
    id,
    value, 
    onChange, 
    options, 
    placeholder = "Select...", 
    label,
    icon,
    className
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [highlightedIndex, setHighlightedIndex] = useState(0);
    const { global, isDark } = useTheme();
    const wrapperRef = useRef<HTMLDivElement>(null);
    const searchInputRef = useRef<HTMLInputElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);
    const listRef = useRef<HTMLDivElement>(null);

    // Close when clicking outside
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    // Focus search input when opening
    useEffect(() => {
        if (isOpen && searchInputRef.current) {
            searchInputRef.current.focus();
            setHighlightedIndex(0);
        }
        if (!isOpen) {
            setSearchTerm(""); // Reset search on close
        }
    }, [isOpen]);

    const filteredOptions = options.filter(opt => {
        const term = searchTerm.toLowerCase();
        if (!term) return true;

        const label = String(opt.label || '').toLowerCase();
        const value = String(opt.value || '').toLowerCase();
        const subLabel = String(opt.subLabel || '').toLowerCase();
        const targets = [label, value, subLabel];
        
        if (searchStrategy === 'startsWith') {
            return targets.some(t => t.startsWith(term));
        }

        if (searchStrategy === 'wildcard') {
            // Escape special regex chars except * and ?
            const escapeRegex = (s: string) => s.replace(/[.+^${}()|[\]\\]/g, '\\$&');
            // Convert wildcard to regex: * -> .*, ? -> .
            // We anchor to start (^) to respect the wildcard position
            // "M*" -> "^M.*" (Starts with M)
            // "*M" -> "^.*M" (Ends with M? No, .*M matches anything followed by M. To force end, we need $)
            // But usually wildcard search in UI implies "matches pattern". 
            // If I type "M", it implies "^M.*" (Starts with) or just "^M$" (Exact)? 
            // Standard shell glob: "M" is exact match. "M*" is starts with.
            // Let's implement shell-like glob.
            
            // If no wildcard chars, treat as contains for better UX? 
            // User said: "if a word starts from M*".
            // If I type "M", I probably expect "Material". 
            // So if no wildcard, I will default to 'contains' OR 'startsWith'.
            // But if I strictly follow wildcard rules, "M" matches only "M".
            // Let's detect wildcard usage.
            const hasWildcard = term.includes('*') || term.includes('?');
            if (!hasWildcard) {
                return targets.some(t => t.includes(term));
            }

            const pattern = term.split('*').map(escapeRegex).join('.*').replace(/\?/g, '.');
            const regex = new RegExp(`^${pattern}`, 'i'); 
            return targets.some(t => regex.test(t));
        }

        if (searchStrategy === 'pattern' && searchPattern) {
            try {
               // Escape user input to avoid regex injection unless intent is to allow partial regex
               const escapedTerm = term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
               const regexStr = searchPattern.replace('{term}', escapedTerm);
               const regex = new RegExp(regexStr, 'i');
               return targets.some(t => regex.test(t));
            } catch {
               return targets.some(t => t.includes(term));
            }
        }

        return targets.some(t => t.includes(term));
    });

    const selectedOption = options.find(o => o.value === value);

    const handleSelect = (val: string) => {
        onChange(val);
        setIsOpen(false);
        // Focus back to trigger after selection so we can move to next field
        if (triggerRef.current) {
            triggerRef.current.focus();
        }
    };

    const handleKeyDown = (e: KeyboardEvent) => {
        if (!isOpen) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
                e.preventDefault();
                setIsOpen(true);
            }
            return;
        }

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                setHighlightedIndex(prev => (prev < filteredOptions.length - 1 ? prev + 1 : prev));
                break;
            case 'ArrowUp':
                e.preventDefault();
                setHighlightedIndex(prev => (prev > 0 ? prev - 1 : prev));
                break;
            case 'Enter':
                e.preventDefault();
                if (filteredOptions.length > 0) {
                    handleSelect(filteredOptions[highlightedIndex].value);
                }
                break;
            case 'Escape':
                e.preventDefault();
                setIsOpen(false);
                triggerRef.current?.focus();
                break;
            case 'Tab':
                setIsOpen(false);
                // Let default Tab behavior happen
                break;
        }
    };

    // Scroll highlighted item into view
    useEffect(() => {
        if (isOpen && listRef.current) {
            const highlightedItem = listRef.current.children[highlightedIndex] as HTMLElement;
            if (highlightedItem) {
                highlightedItem.scrollIntoView({ block: 'nearest' });
            }
        }
    }, [highlightedIndex, isOpen]);

    return (
        <div className={`relative ${className}`} ref={wrapperRef}>
            {label && (
                <div className={`flex items-center gap-2 text-xs font-semibold ${global.textSecondary} mb-2 uppercase tracking-wider`}>
                    {icon} {label}
                </div>
            )}
            
            {/* Trigger Button */}
            <button
                id={id}
                ref={triggerRef}
                type="button"
                onClick={() => setIsOpen(!isOpen)}
                onKeyDown={!isOpen ? handleKeyDown : undefined}
                className={`w-full flex items-center justify-between px-3 py-2.5 text-sm rounded-lg border ${global.border} ${global.bg} ${global.text} focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all shadow-sm hover:border-gray-400 dark:hover:border-slate-500 min-h-[42px]`}
            >
                <span className="truncate flex-1 text-left">
                    {selectedOption ? (
                        <div className="flex flex-col leading-tight">
                            <span className="font-medium">{selectedOption.label}</span>
                            {selectedOption.subLabel && <span className={`text-[11px] ${global.textSecondary}`}>{selectedOption.subLabel}</span>}
                        </div>
                    ) : (
                        <span className={`${global.textSecondary} opacity-70`}>{placeholder}</span>
                    )}
                </span>
                <ChevronDown size={16} className={`ml-2 transition-transform duration-200 ${isOpen ? 'rotate-180 text-blue-500' : global.textSecondary}`} />
            </button>

            {/* Dropdown Menu */}
            {isOpen && (
                <div className={`absolute z-50 w-full mt-1.5 max-h-72 rounded-lg border ${global.border} ${global.bg} shadow-xl flex flex-col overflow-hidden ring-1 ring-black/5 animate-in fade-in zoom-in-95 duration-100`}>
                    
                    {/* Search Input */}
                    <div className={`p-2 border-b ${global.border} sticky top-0 ${global.bg} bg-opacity-95 backdrop-blur-sm z-10`}>
                        <div className="relative">
                            <Search size={14} className={`absolute left-3 top-1/2 transform -translate-y-1/2 ${global.textSecondary}`} />
                            <input
                                ref={searchInputRef}
                                type="text"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Search..."
                                className={`w-full pl-9 pr-3 py-2 text-sm rounded-md border ${global.border} ${global.bg} ${global.text} focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all placeholder:text-gray-400`}
                            />
                        </div>
                    </div>

                    {/* Options List */}
                    <div ref={listRef} className="overflow-y-auto flex-1 scrollbar-thin scrollbar-thumb-gray-200 dark:scrollbar-thumb-slate-700">
                        {filteredOptions.length > 0 ? (
                            <div className="p-1">
                                {filteredOptions.map((option, index) => (
                                    <div
                                        key={option.value}
                                        onClick={() => handleSelect(option.value)}
                                        className={`
                                            px-3 py-2.5 rounded-md text-sm cursor-pointer flex items-center justify-between mb-0.5 last:mb-0 transition-colors
                                            ${index === highlightedIndex 
                                                ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300' 
                                                : `${global.text} hover:bg-gray-50 dark:hover:bg-slate-800`
                                            }
                                            ${value === option.value ? 'bg-blue-50/50 dark:bg-blue-900/10 font-medium' : ''}
                                        `}
                                    >
                                        <div className="flex flex-col gap-0.5">
                                            <div className="font-medium">{option.label}</div>
                                            {option.subLabel && <div className={`text-[11px] ${index === highlightedIndex ? 'text-blue-600/80 dark:text-blue-400/80' : global.textSecondary}`}>{option.subLabel}</div>}
                                        </div>
                                        {value === option.value && <Check size={16} className="text-blue-500" />}
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className={`py-8 text-center flex flex-col items-center justify-center ${global.textSecondary}`}>
                                <Search size={24} className="mb-2 opacity-20" />
                                <span className="text-sm">No options found</span>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};

export default SearchableSelect;
