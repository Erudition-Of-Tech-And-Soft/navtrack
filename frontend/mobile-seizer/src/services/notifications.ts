import messaging from '@react-native-firebase/messaging';
import { Alert, Platform } from 'react-native';

export async function requestUserPermission(): Promise<boolean> {
  const authStatus = await messaging().requestPermission();
  const enabled =
    authStatus === messaging.AuthorizationStatus.AUTHORIZED ||
    authStatus === messaging.AuthorizationStatus.PROVISIONAL;

  if (enabled) {
    console.log('Authorization status:', authStatus);
  }

  return enabled;
}

export async function getFCMToken(): Promise<string | null> {
  try {
    const token = await messaging().getToken();
    console.log('FCM Token:', token);
    return token;
  } catch (error) {
    console.error('Error getting FCM token:', error);
    return null;
  }
}

export function setupNotificationListeners() {
  // Handle background messages
  messaging().setBackgroundMessageHandler(async remoteMessage => {
    console.log('Message handled in the background!', remoteMessage);
  });

  // Handle foreground messages
  messaging().onMessage(async remoteMessage => {
    console.log('Foreground notification received:', remoteMessage);

    if (remoteMessage.notification) {
      Alert.alert(
        remoteMessage.notification.title || 'Notificación',
        remoteMessage.notification.body || '',
        [{ text: 'OK' }]
      );
    }
  });

  // Handle notification opened app
  messaging().onNotificationOpenedApp(remoteMessage => {
    console.log(
      'Notification caused app to open from background state:',
      remoteMessage
    );
    // Navigate to specific screen based on notification data
  });

  // Check if app was opened by a notification
  messaging()
    .getInitialNotification()
    .then(remoteMessage => {
      if (remoteMessage) {
        console.log(
          'Notification caused app to open from quit state:',
          remoteMessage
        );
        // Navigate to specific screen based on notification data
      }
    });

  // Handle token refresh
  messaging().onTokenRefresh(token => {
    console.log('FCM token refreshed:', token);
    // TODO: Send updated token to backend
  });
}

export interface NotificationPayload {
  assetId: string;
  assetName: string;
  type: 'seizure_expiring' | 'asset_moved' | 'asset_offline';
  message: string;
}

// This would be called from the backend to send notifications
// Included here for reference
export const NOTIFICATION_TYPES = {
  SEIZURE_EXPIRING: 'seizure_expiring', // 24 hours before expiration
  ASSET_MOVED: 'asset_moved',           // When seized asset moves
  ASSET_OFFLINE: 'asset_offline',       // When seized asset goes offline
};
