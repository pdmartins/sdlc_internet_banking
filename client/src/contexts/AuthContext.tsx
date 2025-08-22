import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { API_ENDPOINTS } from '../config/api';

export interface SessionInfo {
  id: string;
  deviceInfo: string;
  location: string;
  ipAddress: string;
  createdAt: string;
  lastActivityAt?: string;
  isTrustedDevice: boolean;
  isCurrentSession: boolean;
}

export interface UserSession {
  userId: string;
  email: string;
  fullName: string;
  token: string;
  tokenExpiresAt: string;
  isAuthenticated: boolean;
  inactivityTimeoutMinutes: number;
}

interface AuthContextType {
  session: UserSession | null;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string, rememberDevice?: boolean) => Promise<boolean>;
  logout: () => Promise<void>;
  logoutAllDevices: () => Promise<boolean>;
  validateSession: () => Promise<boolean>;
  updateActivity: () => Promise<void>;
  getActiveSessions: () => Promise<SessionInfo[]>;
  clearError: () => void;
  lastActivity: Date | null;
  timeUntilTimeout: number | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [session, setSession] = useState<UserSession | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastActivity, setLastActivity] = useState<Date | null>(null);
  const [timeUntilTimeout, setTimeUntilTimeout] = useState<number | null>(null);

  // Session validation interval (check every 60 seconds)
  const SESSION_CHECK_INTERVAL = 60000;
  // Activity heartbeat interval (send every 5 minutes)
  const HEARTBEAT_INTERVAL = 300000;
  // Inactivity warning threshold (5 minutes before timeout)
  const INACTIVITY_WARNING_THRESHOLD = 5;

  useEffect(() => {
    // Load session from localStorage on startup
    loadSessionFromStorage();
  }, []);

  useEffect(() => {
    if (session?.isAuthenticated) {
      // Start session monitoring
      const sessionInterval = setInterval(checkSessionValidity, SESSION_CHECK_INTERVAL);
      const heartbeatInterval = setInterval(sendHeartbeat, HEARTBEAT_INTERVAL);
      const activityListener = setupActivityListener();

      return () => {
        clearInterval(sessionInterval);
        clearInterval(heartbeatInterval);
        cleanupActivityListener(activityListener);
      };
    }
  }, [session?.isAuthenticated]);

  const loadSessionFromStorage = () => {
    try {
      const stored = localStorage.getItem('user');
      if (stored) {
        const userData = JSON.parse(stored);
        if (userData.token && userData.tokenExpiresAt) {
          const expiresAt = new Date(userData.tokenExpiresAt);
          if (expiresAt > new Date()) {
            setSession({
              ...userData,
              isAuthenticated: true,
              inactivityTimeoutMinutes: 30 // Default timeout
            });
            setLastActivity(new Date());
          } else {
            // Token expired, clear storage
            localStorage.removeItem('user');
          }
        }
      }
    } catch (error) {
      console.error('Error loading session from storage:', error);
      localStorage.removeItem('user');
    }
  };

  const saveSessionToStorage = (sessionData: UserSession) => {
    try {
      localStorage.setItem('user', JSON.stringify({
        userId: sessionData.userId,
        email: sessionData.email,
        fullName: sessionData.fullName,
        token: sessionData.token,
        tokenExpiresAt: sessionData.tokenExpiresAt
      }));
    } catch (error) {
      console.error('Error saving session to storage:', error);
    }
  };

  const clearSessionFromStorage = () => {
    localStorage.removeItem('user');
  };

  const login = async (email: string, password: string, rememberDevice = false): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(API_ENDPOINTS.LOGIN, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email,
          password,
          rememberDevice
        }),
      });

      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.message || 'Erro durante o login');
      }

      // Check if MFA is required
      if (result.requiresMfa) {
        // For MFA flow, we don't set session yet
        return false; // Indicates MFA is required
      }

      const sessionData: UserSession = {
        userId: result.userId,
        email: result.email,
        fullName: result.fullName,
        token: result.token,
        tokenExpiresAt: result.tokenExpiresAt,
        isAuthenticated: true,
        inactivityTimeoutMinutes: 30
      };

      setSession(sessionData);
      saveSessionToStorage(sessionData);
      setLastActivity(new Date());

      return true;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erro inesperado durante o login');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async (): Promise<void> => {
    setIsLoading(true);

    try {
      if (session?.token) {
        await fetch(API_ENDPOINTS.SESSION_LOGOUT, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${session.token}`,
            'Content-Type': 'application/json',
          },
        });
      }
    } catch (error) {
      console.error('Error during logout:', error);
    } finally {
      setSession(null);
      clearSessionFromStorage();
      setLastActivity(null);
      setTimeUntilTimeout(null);
      setIsLoading(false);
    }
  };

  const logoutAllDevices = async (): Promise<boolean> => {
    if (!session?.userId || !session?.token) {
      return false;
    }

    setIsLoading(true);

    try {
      const response = await fetch(API_ENDPOINTS.SESSION_LOGOUT_ALL_DEVICES(session.userId), {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          confirmLogoutAll: true
        }),
      });

      const result = await response.json();

      if (response.ok && result.success) {
        return true;
      } else {
        setError(result.message || 'Erro ao fazer logout de todos os dispositivos');
        return false;
      }
    } catch (error) {
      setError('Erro ao fazer logout de todos os dispositivos');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const validateSession = async (): Promise<boolean> => {
    if (!session?.token) {
      return false;
    }

    try {
      const response = await fetch(API_ENDPOINTS.SESSION_VALIDATE, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.token}`,
          'Content-Type': 'application/json',
        },
      });

      const result = await response.json();

      if (response.ok && result.isValid) {
        setTimeUntilTimeout(result.minutesUntilTimeout);
        return true;
      } else {
        // Session invalid, logout user
        await logout();
        return false;
      }
    } catch (error) {
      console.error('Error validating session:', error);
      return false;
    }
  };

  const updateActivity = async (): Promise<void> => {
    if (!session?.token) {
      return;
    }

    try {
      await fetch(API_ENDPOINTS.SESSION_HEARTBEAT, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${session.token}`,
          'Content-Type': 'application/json',
        },
      });

      setLastActivity(new Date());
    } catch (error) {
      console.error('Error updating activity:', error);
    }
  };

  const getActiveSessions = async (): Promise<SessionInfo[]> => {
    if (!session?.userId || !session?.token) {
      return [];
    }

    try {
      const response = await fetch(API_ENDPOINTS.SESSION_ACTIVE(session.userId), {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${session.token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        return await response.json();
      }
    } catch (error) {
      console.error('Error getting active sessions:', error);
    }

    return [];
  };

  const checkSessionValidity = async () => {
    if (session?.isAuthenticated) {
      const isValid = await validateSession();
      if (!isValid) {
        setError('Sua sessão expirou. Faça login novamente.');
      }
    }
  };

  const sendHeartbeat = async () => {
    if (session?.isAuthenticated) {
      await updateActivity();
    }
  };

  const setupActivityListener = () => {
    const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
    
    const activityHandler = () => {
      setLastActivity(new Date());
    };

    events.forEach(event => {
      document.addEventListener(event, activityHandler, true);
    });

    return () => {
      events.forEach(event => {
        document.removeEventListener(event, activityHandler, true);
      });
    };
  };

  const cleanupActivityListener = (cleanup: () => void) => {
    cleanup();
  };

  const clearError = () => {
    setError(null);
  };

  return (
    <AuthContext.Provider value={{
      session,
      isLoading,
      error,
      login,
      logout,
      logoutAllDevices,
      validateSession,
      updateActivity,
      getActiveSessions,
      clearError,
      lastActivity,
      timeUntilTimeout
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
