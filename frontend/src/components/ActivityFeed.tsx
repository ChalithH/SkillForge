import React, { useEffect, useState } from 'react';
import { Clock, Users, CheckCircle, X, AlertCircle, Gift, Calendar, User } from 'lucide-react';
import { SignalRNotification, useSignalR } from '../hooks/useSignalR';
import { useAppSelector } from '../store/hooks';

interface ActivityItem extends SignalRNotification {
  id: string;
}

export const ActivityFeed: React.FC = () => {
  const { user } = useAppSelector((state) => state.auth);
  const { notifications, addNotificationListener, onlineUsers } = useSignalR();
  const [activities, setActivities] = useState<ActivityItem[]>([]);

  useEffect(() => {
    // Convert SignalR notifications to activity items
    const activityItems: ActivityItem[] = notifications.map((notif, index) => ({
      ...notif,
      id: `${notif.timestamp}-${index}`,
    }));

    // Sort by timestamp (newest first) and keep only recent items
    const allActivities = activityItems
      .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
      .slice(0, 20); // Keep only 20 most recent

    setActivities(allActivities);
  }, [notifications, onlineUsers, user?.id]);

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'exchange_request':
        return <Clock className="w-4 h-4 text-blue-600" />;
      case 'exchange_status_update':
        return <CheckCircle className="w-4 h-4 text-green-600" />;
      case 'credit_transfer':
        return <Gift className="w-4 h-4 text-purple-600" />;
      case 'user_online':
        return <Users className="w-4 h-4 text-green-600" />;
      case 'user_offline':
        return <User className="w-4 h-4 text-gray-600" />;
      case 'error':
        return <AlertCircle className="w-4 h-4 text-red-600" />;
      default:
        return <Calendar className="w-4 h-4 text-blue-600" />;
    }
  };

  const getActivityColor = (type: string) => {
    switch (type) {
      case 'exchange_request':
        return 'border-l-blue-500 bg-blue-50';
      case 'exchange_status_update':
        return 'border-l-green-500 bg-green-50';
      case 'credit_transfer':
        return 'border-l-purple-500 bg-purple-50';
      case 'user_online':
        return 'border-l-green-500 bg-green-50';
      case 'user_offline':
        return 'border-l-gray-500 bg-gray-50';
      case 'error':
        return 'border-l-red-500 bg-red-50';
      default:
        return 'border-l-blue-500 bg-blue-50';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes < 1) return 'Just now';
    if (diffMinutes < 60) return `${diffMinutes}m ago`;
    if (diffMinutes < 1440) return `${Math.floor(diffMinutes / 60)}h ago`;
    return date.toLocaleDateString();
  };

  const getActivityTitle = (activity: ActivityItem) => {
    switch (activity.type) {
      case 'exchange_request':
        return 'New Exchange Request';
      case 'exchange_status_update':
        return 'Exchange Update';
      case 'credit_transfer':
        return 'Credit Update';
      case 'user_online':
        return 'User Online';
      case 'user_offline':
        return 'User Offline';
      default:
        return 'Activity';
    }
  };

  const getActivityDetails = (activity: ActivityItem) => {
    if (activity.type === 'exchange_request' && activity.skillName) {
      return (
        <div className="text-xs text-gray-600 mt-1">
          <span className="font-medium">Skill:</span> {activity.skillName}
          {activity.duration && (
            <span className="ml-2">
              <span className="font-medium">Duration:</span> {activity.duration}h
            </span>
          )}
        </div>
      );
    }

    if (activity.type === 'credit_transfer' && activity.amount) {
      return (
        <div className="text-xs text-gray-600 mt-1">
          <span className="font-medium">Amount:</span> {activity.amount > 0 ? '+' : ''}{activity.amount} credits
          {activity.reason && (
            <div className="mt-1">
              <span className="font-medium">Reason:</span> {activity.reason}
            </div>
          )}
        </div>
      );
    }

    return null;
  };

  if (activities.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Activity</h3>
        <div className="text-center py-8">
          <Calendar className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-600">No recent activity</p>
          <p className="text-sm text-gray-500 mt-1">
            Activity will appear here when you start learning and teaching
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Recent Activity</h3>
        <div className="flex items-center space-x-2">
          <div className="w-2 h-2 bg-green-400 rounded-full animate-pulse"></div>
          <span className="text-sm text-gray-600">Live</span>
        </div>
      </div>

      <div className="space-y-3 max-h-96 overflow-y-auto">
        {activities.map((activity) => (
          <div
            key={activity.id}
            className={`border-l-4 rounded-r-lg p-3 transition-all duration-200 hover:shadow-sm ${getActivityColor(activity.type)}`}
          >
            <div className="flex items-start space-x-3">
              <div className="flex-shrink-0 mt-1">
                {getActivityIcon(activity.type)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between">
                  <h4 className="text-sm font-medium text-gray-900">
                    {getActivityTitle(activity)}
                  </h4>
                  <span className="text-xs text-gray-500 flex-shrink-0">
                    {formatTimestamp(activity.timestamp)}
                  </span>
                </div>
                <p className="text-sm text-gray-700 mt-1 break-words">
                  {activity.message}
                </p>
                {getActivityDetails(activity)}
              </div>
            </div>
          </div>
        ))}
      </div>

      {activities.length > 10 && (
        <div className="mt-4 text-center">
          <button className="text-sm text-blue-600 hover:text-blue-800 font-medium">
            View All Activity
          </button>
        </div>
      )}
    </div>
  );
};