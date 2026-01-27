import React from 'react';
import { X, CheckCircle, AlertCircle, Info } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  message?: React.ReactNode;
  children?: React.ReactNode;
  footer?: React.ReactNode;
  type?: 'success' | 'error' | 'info' | 'warning';
  autoCloseDuration?: number;
  onConfirm?: () => void;
  confirmText?: string;
  cancelText?: string;
  confirmDisabled?: boolean;
}

const Modal: React.FC<ModalProps> = ({ 
  isOpen, 
  onClose, 
  title, 
  message, 
  children,
  footer,
  type = 'info', 
  autoCloseDuration,
  onConfirm,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  confirmDisabled = false
}) => {
  const { global, styles } = useTheme();

  React.useEffect(() => {
    if (isOpen && autoCloseDuration && !onConfirm) {
      const timer = setTimeout(() => {
        onClose();
      }, autoCloseDuration);
      return () => clearTimeout(timer);
    }
  }, [isOpen, autoCloseDuration, onClose]);

  if (!isOpen) return null;

  const getIcon = () => {
    switch (type) {
      case 'success':
        return <CheckCircle className="h-6 w-6 text-green-600" />;
      case 'error':
        return <AlertCircle className="h-6 w-6 text-red-600" />;
      case 'warning':
        return <AlertCircle className="h-6 w-6 text-orange-600" />;
      default:
        return <Info className={`h-6 w-6 ${styles.text}`} />;
    }
  };

  const getButtonColor = () => {
      switch (type) {
          case 'success': return 'bg-green-600 hover:bg-green-700 focus:ring-green-500';
          case 'error': return 'bg-red-600 hover:bg-red-700 focus:ring-red-500';
          case 'warning': return 'bg-orange-600 hover:bg-orange-700 focus:ring-orange-500';
          default: return `${styles.primary} ${styles.hover} ${styles.ring}`;
      }
  };

  return (
    <div className="fixed inset-0 z-[100] overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
      <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        
        {/* Background overlay */}
        <div 
          className={`fixed inset-0 ${global.overlay} transition-opacity backdrop-blur-sm`}
          aria-hidden="true"
          onClick={onClose}
        ></div>

        {/* This element is to trick the browser into centering the modal contents. */}
        <span className="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

        <div className={`inline-block align-bottom ${global.bg} rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full ${global.border} border`}>
          <div className={`${global.bg} px-4 pt-5 pb-4 sm:p-6 sm:pb-4`}>
            <div className="sm:flex sm:items-start">
              <div className={`mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full sm:mx-0 sm:h-10 sm:w-10 ${type === 'success' ? 'bg-green-100 dark:bg-green-900/30' : type === 'error' ? 'bg-red-100 dark:bg-red-900/30' : type === 'warning' ? 'bg-orange-100 dark:bg-orange-900/30' : styles.light}`}>
                {getIcon()}
              </div>
              <div className="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
                <h3 className={`text-lg leading-6 font-medium ${global.text}`} id="modal-title">
                  {title}
                </h3>
                <div className="mt-2">
                  <div className={`text-sm ${global.textSecondary}`}>
                    {message}
                    {children}
                  </div>
                </div>
              </div>
            </div>
          </div>
          {(onConfirm || footer) && (
            <div className={`${global.bgSecondary} px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse border-t ${global.border}`}>
                {footer ? footer : (
                    <>
                        {onConfirm && (
                            <button
                                type="button"
                                className={`w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 text-base font-medium text-white focus:outline-none focus:ring-2 focus:ring-offset-2 sm:ml-3 sm:w-auto sm:text-sm ${getButtonColor()} ${confirmDisabled ? 'opacity-50 cursor-not-allowed' : ''}`}
                                onClick={onConfirm}
                                disabled={confirmDisabled}
                            >
                                {confirmText}
                            </button>
                        )}
                        <button
                            type="button"
                            className={`mt-3 w-full inline-flex justify-center rounded-md border ${global.border} shadow-sm px-4 py-2 ${global.inputBg} text-base font-medium ${global.text} hover:${global.bgSecondary} focus:outline-none focus:ring-2 focus:ring-offset-2 ${styles.ring} sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm`}
                            onClick={onClose}
                        >
                            {cancelText}
                        </button>
                    </>
                )}
            </div>
          )}
          <button 
             onClick={onClose}
             className="absolute top-0 right-0 pt-4 pr-4"
          >
              <X className={`h-5 w-5 ${global.textSecondary} hover:${global.text}`} />
          </button>
        </div>
      </div>
    </div>
  );
};

export default Modal;
