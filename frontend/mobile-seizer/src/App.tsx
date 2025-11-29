import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { IntlProvider } from 'react-intl';
import { translations } from '@shared/translations';
import messaging from '@react-native-firebase/messaging';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import LoginScreen from './screens/LoginScreen';
import SeizedAssetsListScreen from './screens/SeizedAssetsListScreen';
import AssetDetailScreen from './screens/AssetDetailScreen';
import { AuthProvider, useAuth } from './contexts/AuthContext';

const Stack = createNativeStackNavigator();
const queryClient = new QueryClient();

// Request permission for notifications
messaging().requestPermission();

function Navigation() {
  const { isAuthenticated } = useAuth();

  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#1f2937',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}>
      {!isAuthenticated ? (
        <Stack.Screen
          name="Login"
          component={LoginScreen}
          options={{ headerShown: false }}
        />
      ) : (
        <>
          <Stack.Screen
            name="SeizedAssetsList"
            component={SeizedAssetsListScreen}
            options={{ title: 'Vehículos Incautados' }}
          />
          <Stack.Screen
            name="AssetDetail"
            component={AssetDetailScreen}
            options={{ title: 'Detalles del Vehículo' }}
          />
        </>
      )}
    </Stack.Navigator>
  );
}

export default function App() {
  return (
    <SafeAreaProvider>
      <QueryClientProvider client={queryClient}>
        <IntlProvider locale="es" messages={translations.es}>
          <AuthProvider>
            <NavigationContainer>
              <Navigation />
            </NavigationContainer>
          </AuthProvider>
        </IntlProvider>
      </QueryClientProvider>
    </SafeAreaProvider>
  );
}
