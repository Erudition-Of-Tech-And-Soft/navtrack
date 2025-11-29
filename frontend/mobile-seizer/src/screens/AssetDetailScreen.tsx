import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useRoute } from '@react-navigation/native';
import MapView, { Marker, Polyline } from 'react-native-maps';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { fetchAssetLocations } from '../services/api';

interface Location {
  latitude: number;
  longitude: number;
  altitude: number;
  speed: number;
  heading: number;
  timestamp: string;
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleString('es-DO', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatTimeRemaining(expirationDate: string): string {
  const now = new Date();
  const expiry = new Date(expirationDate);
  const diff = expiry.getTime() - now.getTime();

  if (diff <= 0) {
    return 'Expirado';
  }

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

  if (days > 0) {
    return `${days} día${days > 1 ? 's' : ''}, ${hours} hora${hours > 1 ? 's' : ''}`;
  } else if (hours > 0) {
    return `${hours} hora${hours > 1 ? 's' : ''}, ${minutes} minuto${minutes > 1 ? 's' : ''}`;
  } else {
    return `${minutes} minuto${minutes > 1 ? 's' : ''}`;
  }
}

export default function AssetDetailScreen() {
  const route = useRoute();
  const { asset } = route.params as any;
  const { token } = useAuth();
  const [mapRegion, setMapRegion] = useState<any>(null);

  const {
    data: locations,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['assetLocations', asset.id],
    queryFn: () => fetchAssetLocations(token!, asset.id),
    enabled: !!token,
    refetchInterval: 10000, // Refresh every 10 seconds
  });

  useEffect(() => {
    if (locations && locations.length > 0) {
      const lastLocation = locations[0];
      setMapRegion({
        latitude: lastLocation.latitude,
        longitude: lastLocation.longitude,
        latitudeDelta: 0.01,
        longitudeDelta: 0.01,
      });
    }
  }, [locations]);

  const timeRemaining = formatTimeRemaining(asset.seizureExpirationDate);
  const expirationDate = formatDate(asset.seizureExpirationDate);

  return (
    <ScrollView
      style={styles.container}
      refreshControl={
        <RefreshControl refreshing={isLoading} onRefresh={refetch} />
      }>
      {/* Asset Info Card */}
      <View style={styles.infoCard}>
        <Text style={styles.assetName}>{asset.name}</Text>

        <View style={styles.statusContainer}>
          <View style={[styles.statusBadge, { backgroundColor: asset.online ? '#22c55e' : '#6b7280' }]}>
            <Text style={styles.statusText}>
              {asset.online ? 'Conectado' : 'Desconectado'}
            </Text>
          </View>
        </View>

        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Chasis:</Text>
          <Text style={styles.infoValue}>{asset.chasisNumber}</Text>
        </View>

        <View style={styles.divider} />

        <Text style={styles.sectionTitle}>Información del Incaute</Text>

        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Expira:</Text>
          <Text style={styles.infoValue}>{expirationDate}</Text>
        </View>

        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Tiempo restante:</Text>
          <Text style={[styles.infoValue, styles.timeRemaining]}>
            {timeRemaining}
          </Text>
        </View>
      </View>

      {/* Map */}
      <View style={styles.mapCard}>
        <Text style={styles.sectionTitle}>Ubicación Actual</Text>

        {isLoading && !mapRegion ? (
          <View style={styles.mapLoading}>
            <ActivityIndicator size="large" color="#3b82f6" />
            <Text style={styles.loadingText}>Cargando ubicación...</Text>
          </View>
        ) : error ? (
          <View style={styles.mapLoading}>
            <Text style={styles.errorText}>Error al cargar ubicación</Text>
          </View>
        ) : mapRegion ? (
          <MapView
            style={styles.map}
            region={mapRegion}
            onRegionChangeComplete={setMapRegion}>
            {locations && locations.length > 0 && (
              <>
                {/* Current position marker */}
                <Marker
                  coordinate={{
                    latitude: locations[0].latitude,
                    longitude: locations[0].longitude,
                  }}
                  title={asset.name}
                  description={`Última actualización: ${new Date(locations[0].timestamp).toLocaleTimeString('es-DO')}`}
                  pinColor="#3b82f6"
                />

                {/* Trail */}
                {locations.length > 1 && (
                  <Polyline
                    coordinates={locations.map(loc => ({
                      latitude: loc.latitude,
                      longitude: loc.longitude,
                    }))}
                    strokeColor="#3b82f6"
                    strokeWidth={3}
                  />
                )}
              </>
            )}
          </MapView>
        ) : (
          <View style={styles.mapLoading}>
            <Text style={styles.errorText}>
              No hay datos de ubicación disponibles
            </Text>
          </View>
        )}
      </View>

      {/* Last Position Info */}
      {locations && locations.length > 0 && (
        <View style={styles.infoCard}>
          <Text style={styles.sectionTitle}>Última Posición</Text>

          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Fecha/Hora:</Text>
            <Text style={styles.infoValue}>
              {formatDate(locations[0].timestamp)}
            </Text>
          </View>

          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Velocidad:</Text>
            <Text style={styles.infoValue}>{locations[0].speed.toFixed(1)} km/h</Text>
          </View>

          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Altitud:</Text>
            <Text style={styles.infoValue}>{locations[0].altitude.toFixed(0)} m</Text>
          </View>

          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Coordenadas:</Text>
            <Text style={styles.infoValue}>
              {locations[0].latitude.toFixed(6)}, {locations[0].longitude.toFixed(6)}
            </Text>
          </View>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#111827',
  },
  infoCard: {
    backgroundColor: '#1f2937',
    margin: 16,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#374151',
  },
  assetName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 12,
  },
  statusContainer: {
    flexDirection: 'row',
    marginBottom: 16,
  },
  statusBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
  },
  statusText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  infoRow: {
    flexDirection: 'row',
    marginBottom: 12,
  },
  infoLabel: {
    fontSize: 14,
    color: '#9ca3af',
    marginRight: 8,
    flex: 1,
  },
  infoValue: {
    fontSize: 14,
    color: '#e5e7eb',
    fontWeight: '500',
    flex: 2,
    textAlign: 'right',
  },
  timeRemaining: {
    color: '#f59e0b',
    fontWeight: 'bold',
  },
  divider: {
    height: 1,
    backgroundColor: '#374151',
    marginVertical: 16,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 12,
  },
  mapCard: {
    backgroundColor: '#1f2937',
    margin: 16,
    marginTop: 0,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#374151',
  },
  map: {
    height: 300,
    borderRadius: 8,
    overflow: 'hidden',
  },
  mapLoading: {
    height: 300,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#374151',
    borderRadius: 8,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#9ca3af',
  },
  errorText: {
    fontSize: 14,
    color: '#ef4444',
  },
});
