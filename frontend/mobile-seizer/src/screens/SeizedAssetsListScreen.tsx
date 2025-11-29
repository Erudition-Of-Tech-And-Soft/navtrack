import React, { useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { fetchSeizedAssets } from '../services/api';

interface Asset {
  id: string;
  name: string;
  chasisNumber: string;
  hasActiveSeizure: boolean;
  seizureExpirationDate: string;
  online: boolean;
  lastPositionTime?: string;
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
    return `${days}d ${hours}h`;
  } else if (hours > 0) {
    return `${hours}h ${minutes}m`;
  } else {
    return `${minutes}m`;
  }
}

function getExpirationColor(expirationDate: string): string {
  const now = new Date();
  const expiry = new Date(expirationDate);
  const diff = expiry.getTime() - now.getTime();
  const hoursRemaining = diff / (1000 * 60 * 60);

  if (hoursRemaining <= 0) {
    return '#ef4444'; // red
  } else if (hoursRemaining <= 24) {
    return '#f59e0b'; // amber
  } else if (hoursRemaining <= 72) {
    return '#eab308'; // yellow
  } else {
    return '#22c55e'; // green
  }
}

export default function SeizedAssetsListScreen() {
  const navigation = useNavigation();
  const { user, token, logout } = useAuth();

  const {
    data: assets,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['seizedAssets', user?.organizations],
    queryFn: () => fetchSeizedAssets(token!, user!.organizations),
    enabled: !!token && !!user,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const renderAssetItem = ({ item }: { item: Asset }) => {
    const timeRemaining = formatTimeRemaining(item.seizureExpirationDate);
    const expirationColor = getExpirationColor(item.seizureExpirationDate);

    return (
      <TouchableOpacity
        style={styles.assetCard}
        onPress={() => navigation.navigate('AssetDetail' as never, { asset: item } as never)}>
        <View style={styles.assetHeader}>
          <View style={styles.assetTitleContainer}>
            <Text style={styles.assetName}>{item.name}</Text>
            <View style={[styles.statusBadge, { backgroundColor: item.online ? '#22c55e' : '#6b7280' }]}>
              <Text style={styles.statusText}>
                {item.online ? 'Conectado' : 'Desconectado'}
              </Text>
            </View>
          </View>
        </View>

        <View style={styles.assetInfo}>
          <Text style={styles.infoLabel}>Chasis:</Text>
          <Text style={styles.infoValue}>{item.chasisNumber}</Text>
        </View>

        <View style={styles.expirationContainer}>
          <Text style={styles.expirationLabel}>Tiempo restante:</Text>
          <View style={[styles.expirationBadge, { backgroundColor: expirationColor }]}>
            <Text style={styles.expirationText}>{timeRemaining}</Text>
          </View>
        </View>

        {item.lastPositionTime && (
          <View style={styles.assetInfo}>
            <Text style={styles.infoLabel}>Última ubicación:</Text>
            <Text style={styles.infoValue}>
              {new Date(item.lastPositionTime).toLocaleString('es-DO')}
            </Text>
          </View>
        )}
      </TouchableOpacity>
    );
  };

  if (isLoading) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#3b82f6" />
        <Text style={styles.loadingText}>Cargando vehículos...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.centerContainer}>
        <Text style={styles.errorText}>Error al cargar vehículos</Text>
        <TouchableOpacity style={styles.retryButton} onPress={() => refetch()}>
          <Text style={styles.retryButtonText}>Reintentar</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>
          {assets?.length || 0} Vehículo{assets?.length !== 1 ? 's' : ''} Incautado{assets?.length !== 1 ? 's' : ''}
        </Text>
        <TouchableOpacity onPress={logout} style={styles.logoutButton}>
          <Text style={styles.logoutText}>Salir</Text>
        </TouchableOpacity>
      </View>

      {assets && assets.length > 0 ? (
        <FlatList
          data={assets}
          renderItem={renderAssetItem}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContainer}
          refreshControl={
            <RefreshControl refreshing={isLoading} onRefresh={refetch} />
          }
        />
      ) : (
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>
            No hay vehículos incautados asignados
          </Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#111827',
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#111827',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#1f2937',
    borderBottomWidth: 1,
    borderBottomColor: '#374151',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fff',
  },
  logoutButton: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: '#ef4444',
    borderRadius: 6,
  },
  logoutText: {
    color: '#fff',
    fontWeight: '600',
  },
  listContainer: {
    padding: 16,
  },
  assetCard: {
    backgroundColor: '#1f2937',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#374151',
  },
  assetHeader: {
    marginBottom: 12,
  },
  assetTitleContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  assetName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fff',
    flex: 1,
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    marginLeft: 8,
  },
  statusText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  assetInfo: {
    flexDirection: 'row',
    marginBottom: 8,
  },
  infoLabel: {
    fontSize: 14,
    color: '#9ca3af',
    marginRight: 8,
  },
  infoValue: {
    fontSize: 14,
    color: '#e5e7eb',
    fontWeight: '500',
  },
  expirationContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#374151',
  },
  expirationLabel: {
    fontSize: 14,
    color: '#9ca3af',
    marginRight: 12,
  },
  expirationBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  expirationText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: 'bold',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: '#9ca3af',
  },
  errorText: {
    fontSize: 16,
    color: '#ef4444',
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: '#3b82f6',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  emptyText: {
    fontSize: 16,
    color: '#9ca3af',
    textAlign: 'center',
  },
});
