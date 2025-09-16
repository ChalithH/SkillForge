import { ReactNode } from 'react';

interface ModalLayoutProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: ReactNode;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl';
}

export default function ModalLayout({ 
  isOpen, 
  onClose, 
  title, 
  subtitle, 
  children, 
  maxWidth = 'md' 
}: ModalLayoutProps) {
  if (!isOpen) return null;

  const maxWidthClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl'
  };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50 p-4 sm:p-0">
      <div className={`relative top-10 sm:top-20 mx-auto p-4 sm:p-5 border w-full ${maxWidthClasses[maxWidth]} shadow-lg rounded-md bg-white`}>
        <div className="mt-1 sm:mt-3">
          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex-1 mr-2">
              <h3 className="text-base sm:text-lg font-medium text-gray-900">{title}</h3>
              {subtitle && (
                <p className="text-xs sm:text-sm text-gray-600 mt-1">{subtitle}</p>
              )}
            </div>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors flex-shrink-0"
              aria-label="Close modal"
            >
              <svg className="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Content */}
          {children}
        </div>
      </div>
    </div>
  );
}