import axios from 'axios';

// TODO: Replace with actual API URL
const API_BASE_URL = 'http://10.0.2.2:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

interface Organization {
  organizationId: string;
  userRole: string;
}

export interface Asset {
  id: string;
  name: string;
  chasisNumber: string;
  hasActiveSeizure: boolean;
  seizureExpirationDate: string;
  online: boolean;
  lastPositionTime?: string;
}

export interface Location {
  latitude: number;
  longitude: number;
  altitude: number;
  speed: number;
  heading: number;
  timestamp: string;
}

export async function fetchSeizedAssets(
  token: string,
  organizations: Organization[]
): Promise<Asset[]> {
  try {
    const allAssets: Asset[] = [];

    // Fetch assets from all organizations that the user belongs to
    for (const org of organizations) {
      const response = await api.get(`/organizations/${org.organizationId}/assets`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      const orgAssets = response.data.items || [];

      // Filter only seized assets with active seizure and not expired
      const seizedAssets = orgAssets.filter((asset: Asset) => {
        if (!asset.hasActiveSeizure) {
          return false;
        }

        // Check if seizure is not expired
        if (asset.seizureExpirationDate) {
          const expiryDate = new Date(asset.seizureExpirationDate);
          const now = new Date();
          if (expiryDate < now) {
            return false;
          }
        }

        return true;
      });

      allAssets.push(...seizedAssets);
    }

    // Sort by expiration date (closest to expiring first)
    allAssets.sort((a, b) => {
      const dateA = new Date(a.seizureExpirationDate).getTime();
      const dateB = new Date(b.seizureExpirationDate).getTime();
      return dateA - dateB;
    });

    return allAssets;
  } catch (error) {
    console.error('Error fetching seized assets:', error);
    throw error;
  }
}

export async function fetchAssetLocations(
  token: string,
  assetId: string
): Promise<Location[]> {
  try {
    // Get today's date range
    const now = new Date();
    const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);

    const response = await api.get(`/assets/${assetId}/locations`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      params: {
        startDate: startOfDay.toISOString(),
        endDate: endOfDay.toISOString(),
        limit: 1000,
      },
    });

    const locations = response.data.items || [];

    // Sort by timestamp (newest first)
    locations.sort((a: Location, b: Location) => {
      return new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime();
    });

    return locations;
  } catch (error) {
    console.error('Error fetching asset locations:', error);
    throw error;
  }
}
