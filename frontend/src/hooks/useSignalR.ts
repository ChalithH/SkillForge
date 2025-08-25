import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAppSelector } from '../store/hooks';

export interface SignalRNotification {
  type: string;
  message: string;
  timestamp: string;
  exchangeId?: number;
  senderId?: number;
  senderName?: string;
  skillName?: string;
  scheduledAt?: string;
  duration?: number;
  status?: string;
  previousStatus?: string;
  amount?: number;
  reason?: string;
  actorId?: number;
  actorName?: string;
}

export const useSignalR = () => {
  const { token, user } = useAppSelector((state) => state.auth);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [notifications, setNotifications] = useState<SignalRNotification[]>([]);
  const [onlineUsers, setOnlineUsers] = useState<number[]>([]);
  const notificationListeners = useRef<Array<(notification: SignalRNotification) => void>>([]);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // Initialize connection
  useEffect(() => {
    if (!token || !user) {
      // Clean up connection if user logs out
      if (connection) {
        connection.stop().catch(() => {
          // Ignore errors during cleanup
        });
        setConnection(null);
        setIsConnected(false);
        setNotifications([]);
        setOnlineUsers([]);
      }
      return;
    }

    // Prevent creating multiple connections if one already exists
    if (connection && (
      connection.state === signalR.HubConnectionState.Connected ||
      connection.state === signalR.HubConnectionState.Connecting
    )) {
      return;
    }

    // Create connection setup as async function to handle cleanup properly
    const setupConnection = async () => {
      // Clean up any existing connection before creating new one
      if (connection) {
        try {
          await connection.stop();
        } catch (error) {
          console.warn('Error stopping previous connection:', error);
        }
        setConnection(null);
        setIsConnected(false);
      }

      // Construct SignalR URL - use separate VITE_SIGNALR_URL for Docker networking
      // or fallback to localhost for development
      const signalRBaseUrl = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000';
      const signalRUrl = `${signalRBaseUrl}/hubs/notification`;
      
      console.log('SignalR connecting to:', signalRUrl);
      
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(signalRUrl, {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets,
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext: signalR.RetryContext) => {
            // Exponential backoff: 1s, 2s, 4s, 8s, then 30s max
            console.log(`SignalR reconnection attempt ${retryContext.previousRetryCount + 1}`);
            if (retryContext.previousRetryCount < 4) {
              return Math.pow(2, retryContext.previousRetryCount) * 1000;
            }
            return 30000;
          }
        })
        .configureLogging(import.meta.env.MODE === 'production' ? signalR.LogLevel.Error : signalR.LogLevel.Warning)
        .build();

      // Set up event handlers
      newConnection.on('ReceiveNotification', (notification: SignalRNotification) => {
        console.log('Received notification:', notification);
        
        // Add to notifications list
        setNotifications(prev => [notification, ...prev.slice(0, 49)]); // Keep last 50 notifications
        
        // Call all registered listeners
        notificationListeners.current.forEach(listener => listener(notification));
        
        // Show browser notification if permission is granted
        if (Notification.permission === 'granted') {
          const browserNotification = new Notification(notification.message, {
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            tag: notification.type, // Prevents duplicate notifications of same type
          });

          // Auto-close after 5 seconds
          setTimeout(() => {
            browserNotification.close();
          }, 5000);
        }
      });

      newConnection.on('UserOnline', (userId: number) => {
        console.log(`User ${userId} came online`);
        setOnlineUsers(prev => {
          if (prev.includes(userId)) {
            return prev; // Already online, no update needed
          }
          return [...prev, userId];
        });
      });

      newConnection.on('UserOffline', (userId: number) => {
        console.log(`User ${userId} went offline`);
        setOnlineUsers(prev => {
          if (!prev.includes(userId)) {
            return prev; // Already offline, no update needed
          }
          return prev.filter(id => id !== userId);
        });
      });

      // Connection state handlers
      newConnection.onclose((error?: Error) => {
        console.log('SignalR connection closed');
        if (error) {
          console.error('Connection closed due to error:', error);
        }
        setIsConnected(false);
      });

      newConnection.onreconnecting((error?: Error) => {
        console.log('SignalR reconnecting...');
        if (error) {
          console.error('Reconnecting due to error:', error);
        }
        setIsConnected(false);
      });

      newConnection.onreconnected((connectionId?: string) => {
        console.log('SignalR reconnected successfully', connectionId ? `with ID: ${connectionId}` : '');
        setIsConnected(true);
        
        // Refresh online users list after reconnection
        newConnection.invoke('GetOnlineUsers')
          .then((onlineUserIds: number[]) => {
            console.log('Refreshed online users after reconnection:', onlineUserIds);
            setOnlineUsers(onlineUserIds);
          })
          .catch((err: Error) => {
            console.warn('Failed to refresh online users after reconnection:', err);
          });
      });

      // Start connection
      try {
        await newConnection.start();
        console.log('SignalR connected successfully to:', signalRBaseUrl);
        setIsConnected(true);
        setConnection(newConnection);
        connectionRef.current = newConnection;
        
        // Get initial list of online users after connection
        try {
          const onlineUserIds: number[] = await newConnection.invoke('GetOnlineUsers');
          console.log('Initial online users:', onlineUserIds);
          setOnlineUsers(onlineUserIds);
        } catch (err) {
          console.warn('Failed to get initial online users:', err);
        }
      } catch (err) {
        const error = err as Error;
        console.error('SignalR connection failed:', error);
        console.error('Failed URL:', signalRBaseUrl);
        console.error('Error details:', {
          name: error.name,
          message: error.message,
          stack: error.stack
        });
        setIsConnected(false);
        
        // Try alternative connection if Docker connection fails
        if (signalRBaseUrl.includes('backend:5000')) {
          console.log('Attempting fallback connection to localhost...');
          // This will be handled by the reconnection logic
        }
      }
    };

    // Call the async setup function
    setupConnection();

    return () => {
      console.log('Cleaning up SignalR connection...');
      if (connectionRef.current) {
        connectionRef.current.stop().catch((err: Error) => {
          console.warn('Error stopping SignalR connection during cleanup:', err);
        });
        connectionRef.current = null;
      }
    };
  }, [token, user?.id]); // Only depend on user ID, not full user object

  // Helper functions
  const addNotificationListener = (listener: (notification: SignalRNotification) => void) => {
    notificationListeners.current.push(listener);
    return () => {
      notificationListeners.current = notificationListeners.current.filter(l => l !== listener);
    };
  };

  const clearNotifications = () => {
    setNotifications([]);
  };

  const markNotificationAsRead = (index: number) => {
    setNotifications(prev => prev.filter((_, i) => i !== index));
  };

  const sendNotificationToUser = async (targetUserId: number, message: string, type = 'info') => {
    if (connection && isConnected) {
      try {
        await connection.invoke('SendNotificationToUser', targetUserId, message, type);
      } catch (err) {
        console.error('Failed to send notification:', err);
      }
    }
  };

  const joinGroup = async (groupName: string) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('JoinGroup', groupName);
      } catch (err) {
        console.error('Failed to join group:', err);
      }
    }
  };

  const leaveGroup = async (groupName: string) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('LeaveGroup', groupName);
      } catch (err) {
        console.error('Failed to leave group:', err);
      }
    }
  };

  const isUserOnline = (userId: number) => {
    return onlineUsers.includes(userId);
  };

  const requestNotificationPermission = async () => {
    if (Notification.permission === 'default') {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    }
    return Notification.permission === 'granted';
  };

  return {
    connection,
    isConnected,
    notifications,
    onlineUsers,
    addNotificationListener,
    clearNotifications,
    markNotificationAsRead,
    sendNotificationToUser,
    joinGroup,
    leaveGroup,
    isUserOnline,
    requestNotificationPermission,
  };
};