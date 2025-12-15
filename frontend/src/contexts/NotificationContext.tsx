import React, { createContext, useContext, useEffect, useState } from 'react';
import { NotificationToast } from '../components/NotificationToast';
import { useSignalR, SignalRNotification } from '../hooks/useSignalR';

interface NotificationContextType {
  showToast: (notification: Omit<SignalRNotification, 'timestamp'>) => void;
  requestPermission: () => Promise<boolean>;
  isConnected: boolean;
  onlineUsers: number[];
  isUserOnline: (userId: number) => boolean;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

interface NotificationProviderProps {
  children: React.ReactNode;
}

interface ActiveToast extends SignalRNotification {
  id: string;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const [activeToasts, setActiveToasts] = useState<ActiveToast[]>([]);
  const signalR = useSignalR();

  // Listen for SignalR notifications
  useEffect(() => {
    const removeListener = signalR.addNotificationListener((notification) => {
      const id = `${Date.now()}-${Math.random()}`;
      const toast: ActiveToast = { ...notification, id };
      
      setActiveToasts(prev => [...prev, toast]);
    });

    return removeListener;
  }, [signalR.addNotificationListener]);

  const showToast = (notification: Omit<SignalRNotification, 'timestamp'>) => {
    const id = `${Date.now()}-${Math.random()}`;
    const toast: ActiveToast = {
      ...notification,
      timestamp: new Date().toISOString(),
      id,
    };
    
    setActiveToasts(prev => [...prev, toast]);
  };

  const removeToast = (id: string) => {
    setActiveToasts(prev => prev.filter(toast => toast.id !== id));
  };

  const requestPermission = async () => {
    return await signalR.requestNotificationPermission();
  };

  return (
    <NotificationContext.Provider
      value={{
        showToast,
        requestPermission,
        isConnected: signalR.isConnected,
        onlineUsers: signalR.onlineUsers,
        isUserOnline: signalR.isUserOnline,
      }}
    >
      {children}
      
      {/* Render active toasts */}
      <div className="fixed top-20 left-1/2 -translate-x-1/2 z-50 space-y-2">
        {activeToasts.map((toast, index) => (
          <div
            key={toast.id}
            style={{
              transform: `translateY(${index * 10}px)`,
              zIndex: 1000 - index,
            }}
          >
            <NotificationToast
              notification={toast}
              onClose={() => removeToast(toast.id)}
            />
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
};