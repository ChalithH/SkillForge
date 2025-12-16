import { useState, useRef, useEffect } from 'react';
import { Bell, Clock, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import { useSignalR, SignalRNotification } from '../hooks/useSignalR';
import { NotificationBadge } from './NotificationBadge';
import { useNavigate } from 'react-router-dom';

interface NotificationDropdownProps {
  pendingCount: number;
}

export const NotificationDropdown: React.FC<NotificationDropdownProps> = ({ pendingCount }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { notifications, clearNotifications } = useSignalR();
  const navigate = useNavigate();

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  const getIcon = (type: string) => {
    switch (type) {
      case 'exchange_request':
        return <Clock className="w-4 h-4 text-blue-500" />;
      case 'exchange_status_update':
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'error':
        return <XCircle className="w-4 h-4 text-red-500" />;
      default:
        return <AlertCircle className="w-4 h-4 text-gray-500" />;
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

  const handleNotificationClick = (notification: SignalRNotification) => {
    setIsOpen(false);
    if (notification.exchangeId) {
      navigate('/exchanges');
    }
  };

  const handleViewAll = () => {
    setIsOpen(false);
    navigate('/exchanges');
  };

  // Combine SignalR notifications count with pending requests count
  const totalCount = Math.max(pendingCount, notifications.length);

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="text-gray-500 hover:text-gray-700 transition-colors p-2 relative flex items-center"
        title={`${totalCount} notification${totalCount !== 1 ? 's' : ''}`}
      >
        <Bell className="h-5 w-5" />
        {totalCount > 0 && (
          <NotificationBadge
            count={totalCount}
            className="absolute -top-1 -right-1"
          />
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 z-50">
          <div className="p-3 border-b border-gray-100 flex items-center justify-between">
            <h3 className="font-medium text-gray-900">Notifications</h3>
            {notifications.length > 0 && (
              <button
                onClick={clearNotifications}
                className="text-xs text-gray-500 hover:text-gray-700"
              >
                Clear all
              </button>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto">
            {notifications.length === 0 && pendingCount === 0 ? (
              <div className="p-4 text-center text-gray-500 text-sm">
                No new notifications
              </div>
            ) : notifications.length === 0 && pendingCount > 0 ? (
              <div className="p-4 text-center text-sm text-blue-600">
                You have {pendingCount} pending request{pendingCount !== 1 ? 's' : ''}
              </div>
            ) : (
              notifications.slice(0, 10).map((notification, index) => (
                <button
                  key={index}
                  onClick={() => handleNotificationClick(notification)}
                  className="w-full p-3 hover:bg-gray-50 border-b border-gray-50 last:border-0 text-left transition-colors"
                >
                  <div className="flex items-start gap-3">
                    <div className="flex-shrink-0 mt-0.5">
                      {getIcon(notification.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-gray-900 line-clamp-2">
                        {notification.message}
                      </p>
                      <p className="text-xs text-gray-500 mt-1">
                        {formatTime(notification.timestamp)}
                      </p>
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>

          <div className="p-2 border-t border-gray-100">
            <button
              onClick={handleViewAll}
              className="w-full text-center text-sm text-blue-600 hover:text-blue-800 py-2 rounded hover:bg-blue-50 transition-colors"
            >
              View all in My Sessions
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

// Optional future improvements:
// - Add read/unread status with visual distinction
// - Persist notifications to localStorage
// - Add "Mark all as read" functionality
// - Group notifications by date
// - Add notification sounds
// - Deep link to specific exchange details
