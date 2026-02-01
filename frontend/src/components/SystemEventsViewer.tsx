import React, { useState, useEffect, useRef } from 'react';
import { api } from '../utils/api';

interface KafkaEvent {
  id: string;
  topic: string;
  key: string;
  payload: string;
  timestamp: string;
  processed: boolean;
}

export const SystemEventsViewer: React.FC = () => {
  const [events, setEvents] = useState<KafkaEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [jsonViewId, setJsonViewId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchEvents = async () => {
    try {
      setError(null);
      // Assuming api.get handles the base URL
      // NOTE: Ensure the backend Controller has [Route("api/system-events")]
      const response = await api.get('/api/system-events?limit=50');
      console.log("SystemEventsViewer response:", response);
      
      if (!response) {
          setError("No response from API");
          return;
      }

      if (!response.ok) {
           setError(`API Error: ${response.status} ${response.statusText}`);
           return;
      }

      if (response.data) {
        if (Array.isArray(response.data)) {
            setEvents(response.data);
            setLastUpdated(new Date());
        } else if (response.data.data && Array.isArray(response.data.data)) {
            setEvents(response.data.data);
            setLastUpdated(new Date());
        } else {
            console.error("Unexpected data format", response.data);
            setError(`Unexpected data format: ${JSON.stringify(response.data).substring(0, 100)}...`);
        }
      } else {
        setError(`Empty data received. Status: ${response.status}`);
      }
    } catch (err: any) {
      console.error("Failed to fetch system events", err);
      setError(err.message || "Failed to fetch events");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEvents();
    const interval = setInterval(() => {
      if (autoRefresh) {
        fetchEvents();
      }
    }, 2000); // Poll every 2 seconds

    return () => clearInterval(interval);
  }, [autoRefresh]);

  const formatPayload = (payload: string) => {
    try {
      const parsed = JSON.parse(payload);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return payload;
    }
  };

  return (
    <div className="p-6 bg-gray-50 min-h-screen font-sans">
      <div className="max-w-7xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">Kafka Event Stream (Real-Time)</h1>
            <p className="text-sm text-gray-500">
              Live view of events consumed by the backend. Polling every 2s.
              Last updated: {lastUpdated.toLocaleTimeString()}
            </p>
          </div>
          <div className="flex items-center space-x-4">
            {error && (
              <span className="text-red-600 text-sm font-medium bg-red-50 px-3 py-1 rounded border border-red-200">
                {error}
              </span>
            )}
            <button
              onClick={() => setAutoRefresh(!autoRefresh)}
              className={`px-4 py-2 rounded text-sm font-medium ${
                autoRefresh 
                  ? 'bg-green-100 text-green-800 hover:bg-green-200' 
                  : 'bg-red-100 text-red-800 hover:bg-red-200'
              }`}
            >
              {autoRefresh ? 'Live: ON' : 'Live: OFF'}
            </button>
            <button
              onClick={fetchEvents}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm font-medium"
            >
              Refresh Now
            </button>
          </div>
        </div>

        <div className="bg-white shadow rounded-lg overflow-hidden border border-gray-200">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Time</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Topic</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Key (Tenant)</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Payload Preview</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {events.map((evt) => (
                  <React.Fragment key={evt.id}>
                    <tr className="hover:bg-gray-50 transition-colors">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {new Date(evt.timestamp).toLocaleTimeString()}
                        <span className="text-xs text-gray-400 block">
                          {new Date(evt.timestamp).toLocaleDateString()}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                          {evt.topic}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700 font-mono">
                        {evt.key}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500 max-w-xs truncate font-mono">
                        {evt.payload}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <button
                          onClick={() => setJsonViewId(jsonViewId === evt.id ? null : evt.id)}
                          className="text-blue-600 hover:text-blue-900"
                        >
                          {jsonViewId === evt.id ? 'Hide' : 'Inspect'}
                        </button>
                      </td>
                    </tr>
                    {jsonViewId === evt.id && (
                      <tr className="bg-gray-50">
                        <td colSpan={5} className="px-6 py-4">
                          <pre className="bg-gray-900 text-green-400 p-4 rounded text-xs font-mono overflow-auto max-h-96">
                            {formatPayload(evt.payload)}
                          </pre>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
                {events.length === 0 && !loading && (
                  <tr>
                    <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                      No events found in log.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};
