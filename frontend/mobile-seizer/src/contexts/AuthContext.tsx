import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import axios from 'axios';
import messaging from '@react-native-firebase/messaging';

interface User {
  id: string;
  email: string;
  organizations: Array<{
    organizationId: string;
    userRole: string;
  }>;
}

interface AuthContextType {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    loadStoredAuth();
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      setupFCM();
    }
  }, [isAuthenticated]);

  const loadStoredAuth = async () => {
    try {
      const storedToken = await AsyncStorage.getItem('auth_token');
      const storedUser = await AsyncStorage.getItem('user_data');

      if (storedToken && storedUser) {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));
        setIsAuthenticated(true);
      }
    } catch (error) {
      console.error('Error loading auth:', error);
    }
  };

  const setupFCM = async () => {
    try {
      const fcmToken = await messaging().getToken();
      console.log('FCM Token:', fcmToken);

      // TODO: Send FCM token to backend to store it
      // await axios.post('/api/users/fcm-token', { token: fcmToken });

      // Handle foreground notifications
      messaging().onMessage(async remoteMessage => {
        console.log('Foreground notification:', remoteMessage);
        // TODO: Show local notification
      });
    } catch (error) {
      console.error('FCM setup error:', error);
    }
  };

  const login = async (email: string, password: string) => {
    try {
      // TODO: Replace with actual API endpoint
      const response = await axios.post('http://10.0.2.2:5000/api/account/login', {
        email,
        password,
      });

      const { accessToken, user: userData } = response.data;

      // Verify user is a Seizer
      const isSeizer = userData.organizations?.some(
        (org: any) => org.userRole === 'Seizer'
      );

      if (!isSeizer) {
        throw new Error('Usuario no autorizado. Solo incautadores pueden acceder.');
      }

      await AsyncStorage.setItem('auth_token', accessToken);
      await AsyncStorage.setItem('user_data', JSON.stringify(userData));

      setToken(accessToken);
      setUser(userData);
      setIsAuthenticated(true);
    } catch (error: any) {
      console.error('Login error:', error);
      throw new Error(
        error.response?.data?.message ||
        error.message ||
        'Error al iniciar sesión'
      );
    }
  };

  const logout = async () => {
    try {
      await AsyncStorage.removeItem('auth_token');
      await AsyncStorage.removeItem('user_data');

      setToken(null);
      setUser(null);
      setIsAuthenticated(false);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        token,
        login,
        logout,
      }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
