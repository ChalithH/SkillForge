import React, { useEffect, useState } from 'react';
import { X, CheckCircle, AlertCircle, Info, Clock, Gift } from 'lucide-react';
import { SignalRNotification } from '../hooks/useSignalR';

interface NotificationToastProps {
  notification: SignalRNotification;
  onClose: () => void;
  autoClose?: boolean;
  autoCloseDelay?: number;
}

export const NotificationToast: React.FC<NotificationToastProps> = ({
  notification,
  onClose,
  autoClose = true,
  autoCloseDelay = 5000,
}) => {
  const [isVisible, setIsVisible] = useState(false);
  const [isClosing, setIsClosing] = useState(false);

  useEffect(() => {
    // Animate in
    const timer = setTimeout(() => setIsVisible(true), 100);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (autoClose) {
      const timer = setTimeout(() => {
        handleClose();
      }, autoCloseDelay);
      return () => clearTimeout(timer);
    }
  }, [autoClose, autoCloseDelay]);

  const handleClose = () => {
    setIsClosing(true);
    setTimeout(() => {
      onClose();
    }, 300); // Animation duration
  };

  const getIcon = () => {
    switch (notification.type) {
      case 'exchange_request':
        return <Clock className="w-5 h-5 text-blue-600" />;
      case 'exchange_status_update':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'credit_transfer':
        return <Gift className="w-5 h-5 text-purple-600" />;
      case 'error':
        return <AlertCircle className="w-5 h-5 text-red-600" />;
      case 'success':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'warning':
        return <AlertCircle className="w-5 h-5 text-yellow-600" />;
      default:
        return <Info className="w-5 h-5 text-blue-600" />;
    }
  };

  const getBorderColor = () => {
    switch (notification.type) {
      case 'exchange_request':
        return 'border-l-blue-500';
      case 'exchange_status_update':
        return 'border-l-green-500';
      case 'credit_transfer':
        return 'border-l-purple-500';
      case 'error':
        return 'border-l-red-500';
      case 'success':
        return 'border-l-green-500';
      case 'warning':
        return 'border-l-yellow-500';
      default:
        return 'border-l-blue-500';
    }
  };

  const getBackgroundColor = () => {
    switch (notification.type) {
      case 'exchange_request':
        return 'bg-blue-50';
      case 'exchange_status_update':
        return 'bg-green-50';
      case 'credit_transfer':
        return 'bg-purple-50';
      case 'error':
        return 'bg-red-50';
      case 'success':
        return 'bg-green-50';
      case 'warning':
        return 'bg-yellow-50';
      default:
        return 'bg-blue-50';
    }
  };

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes < 1) return 'Just now';
    if (diffMinutes < 60) return `${diffMinutes}m ago`;
    if (diffMinutes < 1440) return `${Math.floor(diffMinutes / 60)}h ago`;
    return date.toLocaleDateString();
  };

  const getTitle = () => {
    switch (notification.type) {
      case 'exchange_request':
        return 'New Exchange Request';
      case 'exchange_status_update':
        return 'Exchange Update';
      case 'credit_transfer':
        return 'Credit Update';
      default:
        return 'Notification';
    }
  };

  const getDetails = () => {
    if (notification.type === 'exchange_request') {
      return (
        <div className="text-xs text-gray-600 mt-1">
          {notification.skillName && <span>Skill: {notification.skillName}</span>}
          {notification.duration && (
            <span className="ml-2">Duration: {notification.duration}h</span>
          )}
        </div>
      );
    }

    if (notification.type === 'credit_transfer' && notification.amount) {
      return (
        <div className="text-xs text-gray-600 mt-1">
          {notification.amount > 0 ? '+' : ''}{notification.amount} credits
        </div>
      );
    }

    return null;
  };

  return (
    <div
      className={`
        max-w-sm w-full
        transition-all duration-300 ease-in-out
        ${isVisible && !isClosing ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-4'}
      `}
    >
      <div className={`
        ${getBackgroundColor()} ${getBorderColor()}
        border-l-4 rounded-lg shadow-lg p-4
        hover:shadow-xl transition-shadow duration-200
      `}>
        <div className="flex items-start">
          <div className="flex-shrink-0">
            {getIcon()}
          </div>
          <div className="ml-3 flex-1">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-medium text-gray-900">
                {getTitle()}
              </h4>
              <div className="flex items-center space-x-2">
                <span className="text-xs text-gray-500">
                  {formatTime(notification.timestamp)}
                </span>
                <button
                  onClick={handleClose}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            </div>
            <p className="text-sm text-gray-700 mt-1">
              {notification.message}
            </p>
            {getDetails()}
          </div>
        </div>
      </div>
    </div>
  );
};