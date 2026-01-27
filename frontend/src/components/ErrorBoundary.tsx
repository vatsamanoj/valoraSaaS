import { Component, ErrorInfo, ReactNode } from 'react';
import { logger } from '../services/logger';
import { useTheme } from '../context/ThemeContext';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

const ErrorFallback = ({ error, errorInfo }: { error: Error | null, errorInfo: ErrorInfo | null }) => {
    const { global, styles, t } = useTheme();
    
    return (
        <div className={`min-h-screen ${global.bg} flex flex-col items-center justify-center p-4`}>
            <div className={`${global.bg} rounded-lg shadow-xl p-8 max-w-2xl w-full border-l-4 ${styles.border} border-t border-r border-b ${global.border}`}>
            <h1 className={`text-2xl font-bold ${styles.text} mb-4 flex items-center gap-2`}>
              <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              {t('errorBoundary.title')}
            </h1>
            <p className={`${global.textSecondary} mb-6`}>
              {t('errorBoundary.message')}
            </p>
            
            {error && (
              <div className={`${global.bgSecondary} rounded p-4 mb-6 overflow-auto max-h-64 text-sm font-mono ${global.text}`}>
                <p className="font-bold mb-2">{error.toString()}</p>
                <pre className={`whitespace-pre-wrap text-xs ${global.textSecondary}`}>
                  {errorInfo?.componentStack}
                </pre>
              </div>
            )}

            <div className="flex justify-end gap-3">
              <button 
                onClick={() => window.location.reload()}
                className={`${styles.primary} ${styles.hover} text-white px-4 py-2 rounded transition-colors`}
              >
                {t('errorBoundary.reload')}
              </button>
            </div>
          </div>
        </div>
    );
};

class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null
  };

  public static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI.
    return { hasError: true, error, errorInfo: null };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log using our service
    logger.error("React Component Error", error, { componentStack: errorInfo.componentStack });
    this.setState({ errorInfo });
  }

  public render() {
    if (this.state.hasError) {
      return <ErrorFallback error={this.state.error} errorInfo={this.state.errorInfo} />;
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
