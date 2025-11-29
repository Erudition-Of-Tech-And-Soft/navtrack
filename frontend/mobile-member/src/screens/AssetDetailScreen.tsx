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
import { fetchAssetLocationsTodayOnly } from '../services/api';

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

function formatTime(dateString: string): string {
  return new Date(dateString).toLocaleTimeString('es-DO', {
    hour: '2-digit',
    minute: '2-digit',
  });
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
    queryKey: ['assetLocationsTodayOnly', asset.id],
    queryFn: () => fetchAssetLocationsTodayOnly(token!, asset.id),
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

  return (
    <ScrollView
      style={styles.container}
      refreshControl={
        <RefreshControl
          refreshing={isLoading}
          onRefresh={refetch}
          tintColor="#10b981"
        />
      }>
      {/* Asset Info Card */}
      <View style={styles.infoCard}>
        <Text style={styles.assetName}>{asset.name}</Text>

        <View style={styles.statusContainer}>
          <View style={[styles.statusBadge, { backgroundColor: asset.online ? '#10b981' : '#6b7280' }]}>
            <Text style={styles.statusText}>
              {asset.online ? 'Conectado' : 'Desconectado'}
            </Text>
          </View>
        </View>

        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Chasis:</Text>
          <Text style={styles.infoValue}>{asset.chasisNumber}</Text>
        </View>

        {asset.isDelayed && (
          <>
            <View style={styles.divider} />
            <View style={styles.delayedWarning}>
              <Text style={styles.delayedTitle}>⚠️ Estado de Pago</Text>
              <Text style={styles.delayedText}>
                Su cuenta está atrasada. Por favor póngase al día con sus pagos para evitar restricciones en el servicio.
              </Text>
            </View>
          </>
        )}
      </View>

      {/* Map */}
      <View style={styles.mapCard}>
        <Text style={styles.sectionTitle}>Ubicaciones de Hoy</Text>
        <Text style={styles.sectionSubtitle}>
          Mostrando solo las ubicaciones del día actual
        </Text>

        {isLoading && !mapRegion ? (
          <View style={styles.mapLoading}>
            <ActivityIndicator size="large" color="#10b981" />
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
                  description={`Última actualización: ${formatTime(locations[0].timestamp)}`}
                  pinColor="#10b981"
                />

                {/* Trail for today */}
                {locations.length > 1 && (
                  <Polyline
                    coordinates={locations.map(loc => ({
                      latitude: loc.latitude,
                      longitude: loc.longitude,
                    }))}
                    strokeColor="#10b981"
                    strokeWidth={3}
                  />
                )}
              </>
            )}
          </MapView>
        ) : (
          <View style={styles.mapLoading}>
            <Text style={styles.errorText}>
              No hay datos de ubicación para hoy
            </Text>
          </View>
        )}

        {locations && locations.length > 0 && (
          <View style={styles.statsRow}>
            <View style={styles.stat}>
              <Text style={styles.statLabel}>Posiciones hoy</Text>
              <Text style={styles.statValue}>{locations.length}</Text>
            </View>
            <View style={styles.stat}>
              <Text style={styles.statLabel}>Primera</Text>
              <Text style={styles.statValue}>
                {formatTime(locations[locations.length - 1].timestamp)}
              </Text>
            </View>
            <View style={styles.stat}>
              <Text style={styles.statLabel}>Última</Text>
              <Text style={styles.statValue}>
                {formatTime(locations[0].timestamp)}
              </Text>
            </View>
          </View>
        )}
      </View>

      {/* Last Position Info */}
      {locations && locations.length > 0 && (
        <View style={styles.infoCard}>
          <Text style={styles.sectionTitle}>Ubicación Actual</Text>

          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Hora:</Text>
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
    backgroundColor: '#022c22',
  },
  infoCard: {
    backgroundColor: '#064e3b',
    margin: 16,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#047857',
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
    color: '#a7f3d0',
    marginRight: 8,
    flex: 1,
  },
  infoValue: {
    fontSize: 14,
    color: '#d1fae5',
    fontWeight: '500',
    flex: 2,
    textAlign: 'right',
  },
  divider: {
    height: 1,
    backgroundColor: '#047857',
    marginVertical: 16,
  },
  delayedWarning: {
    backgroundColor: '#7f1d1d',
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#dc2626',
  },
  delayedTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fef2f2',
    marginBottom: 8,
  },
  delayedText: {
    fontSize: 14,
    color: '#fecaca',
    lineHeight: 20,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 4,
  },
  sectionSubtitle: {
    fontSize: 12,
    color: '#a7f3d0',
    marginBottom: 12,
  },
  mapCard: {
    backgroundColor: '#064e3b',
    margin: 16,
    marginTop: 0,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#047857',
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
    backgroundColor: '#047857',
    borderRadius: 8,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#a7f3d0',
  },
  errorText: {
    fontSize: 14,
    color: '#dc2626',
  },
  statsRow: {
    flexDirection: 'row',
    marginTop: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: '#047857',
  },
  stat: {
    flex: 1,
    alignItems: 'center',
  },
  statLabel: {
    fontSize: 12,
    color: '#a7f3d0',
    marginBottom: 4,
  },
  statValue: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fff',
  },
});
