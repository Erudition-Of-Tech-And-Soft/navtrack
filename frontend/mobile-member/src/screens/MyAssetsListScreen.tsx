import React from 'react';
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
import { fetchMyAssets } from '../services/api';

interface Asset {
  id: string;
  name: string;
  chasisNumber: string;
  isDelayed: boolean;
  online: boolean;
  lastPositionTime?: string;
}

export default function MyAssetsListScreen() {
  const navigation = useNavigation();
  const { user, token, logout } = useAuth();

  const {
    data: assets,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['myAssets', user?.id],
    queryFn: () => fetchMyAssets(token!, user!.id, user!.organizations),
    enabled: !!token && !!user,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const renderAssetItem = ({ item }: { item: Asset }) => {
    return (
      <TouchableOpacity
        style={styles.assetCard}
        onPress={() => navigation.navigate('AssetDetail' as never, { asset: item } as never)}>
        <View style={styles.assetHeader}>
          <View style={styles.assetTitleContainer}>
            <Text style={styles.assetName}>{item.name}</Text>
            <View style={[styles.statusBadge, { backgroundColor: item.online ? '#10b981' : '#6b7280' }]}>
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

        {item.isDelayed && (
          <View style={styles.delayedContainer}>
            <View style={styles.delayedBadge}>
              <Text style={styles.delayedText}>⚠️ ATRASADO EN PAGOS</Text>
            </View>
            <Text style={styles.delayedInfo}>
              Por favor póngase al día con sus pagos
            </Text>
          </View>
        )}

        {item.lastPositionTime && (
          <View style={styles.assetInfo}>
            <Text style={styles.infoLabel}>Última ubicación:</Text>
            <Text style={styles.infoValue}>
              {new Date(item.lastPositionTime).toLocaleString('es-DO', {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              })}
            </Text>
          </View>
        )}
      </TouchableOpacity>
    );
  };

  if (isLoading) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#10b981" />
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
        <View>
          <Text style={styles.headerTitle}>
            {assets?.length || 0} Vehículo{assets?.length !== 1 ? 's' : ''}
          </Text>
          {assets && assets.some(a => a.isDelayed) && (
            <Text style={styles.headerWarning}>
              {assets.filter(a => a.isDelayed).length} atrasado{assets.filter(a => a.isDelayed).length !== 1 ? 's' : ''}
            </Text>
          )}
        </View>
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
            <RefreshControl
              refreshing={isLoading}
              onRefresh={refetch}
              tintColor="#10b981"
            />
          }
        />
      ) : (
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>
            No tiene vehículos asignados
          </Text>
          <Text style={styles.emptySubtext}>
            Contacte al administrador para más información
          </Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#022c22',
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#022c22',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#064e3b',
    borderBottomWidth: 1,
    borderBottomColor: '#047857',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fff',
  },
  headerWarning: {
    fontSize: 12,
    color: '#fbbf24',
    marginTop: 4,
  },
  logoutButton: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: '#dc2626',
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
    backgroundColor: '#064e3b',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#047857',
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
    color: '#a7f3d0',
    marginRight: 8,
  },
  infoValue: {
    fontSize: 14,
    color: '#d1fae5',
    fontWeight: '500',
  },
  delayedContainer: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#047857',
  },
  delayedBadge: {
    backgroundColor: '#dc2626',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 8,
    alignSelf: 'flex-start',
    marginBottom: 8,
  },
  delayedText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
  },
  delayedInfo: {
    fontSize: 12,
    color: '#fbbf24',
    fontStyle: 'italic',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: '#a7f3d0',
  },
  errorText: {
    fontSize: 16,
    color: '#dc2626',
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: '#10b981',
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
    fontSize: 18,
    color: '#d1fae5',
    textAlign: 'center',
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#a7f3d0',
    textAlign: 'center',
  },
});
