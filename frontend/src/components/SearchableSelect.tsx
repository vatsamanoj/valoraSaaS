import React, { useState, useRef, useEffect } from 'react';
import { Search, ChevronDown, Check, X } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';

interface Option {
    value: string;
    label: string;
    subLabel?: string;
}

interface SearchableSelectProps {
    value: string;
    onChange: (value: string) => void;
    options: Option[];
    placeholder?: string;
    label?: string;
    icon?: React.ReactNode;
}

const SearchableSelect: React.FC<SearchableSelectProps> = ({ 
    value, 
    onChange, 
    options, 
    placeholder = "Select...", 
    label,
    icon
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const { global, isDark } = useTheme();
    const wrapperRef = useRef<HTMLDivElement>(null);
    const searchInputRef = useRef<HTMLInputElement>(null);

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
        }
        if (!isOpen) {
            setSearchTerm(""); // Reset search on close
        }
    }, [isOpen]);

    const filteredOptions = options.filter(opt => 
        opt.label.toLowerCase().includes(searchTerm.toLowerCase()) || 
        opt.value.toLowerCase().includes(searchTerm.toLowerCase()) ||
        opt.subLabel?.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const selectedOption = options.find(o => o.value === value);

    return (
        <div className="relative" ref={wrapperRef}>
            {label && (
                <div className={`flex items-center gap-2 text-xs font-semibold ${global.textSecondary} mb-2 uppercase tracking-wider`}>
                    {icon} {label}
                </div>
            )}
            
            {/* Trigger Button */}
            <button
                type="button"
                onClick={() => setIsOpen(!isOpen)}
                className={`w-full flex items-center justify-between p-2 text-xs rounded-md border ${global.border} ${global.bg} ${global.text} focus:outline-none focus:ring-1 focus:ring-blue-500 transition-colors`}
            >
                <span className="truncate flex-1 text-left">
                    {selectedOption ? (
                        <span>
                            {selectedOption.label} 
                            {selectedOption.subLabel && <span className={`ml-1 ${global.textSecondary} text-[10px]`}>{selectedOption.subLabel}</span>}
                        </span>
                    ) : (
                        <span className={global.textSecondary}>{placeholder}</span>
                    )}
                </span>
                <ChevronDown size={14} className={`ml-2 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
            </button>

            {/* Dropdown Menu */}
            {isOpen && (
                <div className={`absolute z-50 w-full mt-1 max-h-60 rounded-md border ${global.border} ${global.bg} shadow-lg flex flex-col`}>
                    
                    {/* Search Input */}
                    <div className={`p-2 border-b ${global.border} sticky top-0 ${global.bg}`}>
                        <div className="relative">
                            <Search size={12} className={`absolute left-2 top-1/2 transform -translate-y-1/2 ${global.textSecondary}`} />
                            <input
                                ref={searchInputRef}
                                type="text"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                placeholder="Search..."
                                className={`w-full pl-7 pr-2 py-1.5 text-xs rounded-md border ${global.border} ${global.inputBg} ${global.text} focus:outline-none focus:border-blue-500 placeholder-gray-400`}
                            />
                            {searchTerm && (
                                <button 
                                    onClick={() => setSearchTerm("")}
                                    className={`absolute right-2 top-1/2 transform -translate-y-1/2 ${global.textSecondary} hover:${global.text}`}
                                >
                                    <X size={12} />
                                </button>
                            )}
                        </div>
                    </div>

                    {/* Options List */}
                    <div className="overflow-y-auto flex-1 scrollbar-thin">
                        {filteredOptions.length > 0 ? (
                            filteredOptions.map(option => (
                                <button
                                    key={option.value}
                                    onClick={() => {
                                        onChange(option.value);
                                        setIsOpen(false);
                                    }}
                                    className={`w-full flex items-center justify-between px-3 py-2 text-xs text-left transition-colors ${
                                        option.value === value 
                                        ? `${isDark ? 'bg-blue-900/30 text-blue-200' : 'bg-blue-50 text-blue-700'}` 
                                        : `hover:${global.bgSecondary} ${global.text}`
                                    }`}
                                >
                                    <div className="flex flex-col truncate">
                                        <span className="truncate">{option.label}</span>
                                        {option.subLabel && <span className={`text-[10px] ${global.textSecondary}`}>{option.subLabel}</span>}
                                    </div>
                                    {option.value === value && <Check size={12} className="shrink-0 ml-2" />}
                                </button>
                            ))
                        ) : (
                            <div className={`p-3 text-center text-xs ${global.textSecondary}`}>
                                No results found
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};

export default SearchableSelect;
