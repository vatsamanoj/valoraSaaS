import React from 'react';

export interface SimulationLog {
    id: number;
    timestamp: string;
    message: string;
    type: 'info' | 'success' | 'error' | 'data';
    data?: any;
}

interface ConsoleSimulatorProps {
    logs: SimulationLog[];
    isVisible: boolean;
    isLoading?: boolean;
    title?: string;
    version?: string;
}

export const ConsoleSimulator: React.FC<ConsoleSimulatorProps> = ({ 
    logs, 
    isVisible, 
    isLoading = false,
    title = "TRANSACTION SIMULATOR CONSOLE",
    version = "v1.0.2"
}) => {
    if (!isVisible) return null;

    return (
        <div className="mb-6 bg-slate-900 border border-slate-700 rounded-lg shadow-xl overflow-hidden">
            <div className="bg-slate-800 px-4 py-2 border-b border-slate-700 flex items-center justify-between">
                <h3 className="text-xs font-bold text-slate-300 flex items-center gap-2">
                    <span className={`w-2 h-2 rounded-full ${isLoading ? 'bg-green-400 animate-pulse' : 'bg-gray-400'}`}></span>
                    {title}
                </h3>
                <span className="text-xs text-slate-500 font-mono">{version}</span>
            </div>
            <div className="p-4 h-96 overflow-y-auto font-mono text-xs space-y-2">
                {logs.length === 0 && !isLoading && (
                    <div className="text-slate-500 italic">Waiting for activity...</div>
                )}
                {logs.map((log) => (
                    <div key={log.id} className="border-l-2 border-slate-700 pl-3 py-1">
                        <div className="flex items-center gap-2 mb-1">
                            <span className="text-slate-500">[{log.timestamp}]</span>
                            <span className={`font-bold ${
                                log.type === 'success' ? 'text-green-400' :
                                log.type === 'error' ? 'text-red-400' :
                                log.type === 'data' ? 'text-blue-300' :
                                'text-slate-300'
                            }`}>
                                {log.type === 'success' && '✓ '}
                                {log.type === 'error' && '✗ '}
                                {log.message}
                            </span>
                        </div>
                        {log.data && (
                            <pre className="mt-2 p-3 bg-slate-950 text-slate-300 rounded border border-slate-800 overflow-x-auto">
                                {JSON.stringify(log.data, null, 2)}
                            </pre>
                        )}
                    </div>
                ))}
                {isLoading && (
                    <div className="text-slate-500 animate-pulse pl-3 pt-2">_ Processing...</div>
                )}
            </div>
        </div>
    );
};
