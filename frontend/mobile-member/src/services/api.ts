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
  isDelayed: boolean;
  online: boolean;
  lastPositionTime?: string;
  users?: Array<{
    userId: string;
    email: string;
    role: string;
  }>;
}

export interface Location {
  latitude: number;
  longitude: number;
  altitude: number;
  speed: number;
  heading: number;
  timestamp: string;
}

export async function fetchMyAssets(
  token: string,
  userId: string,
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

      // Filter only assets assigned to this member
      const myAssets = orgAssets.filter((asset: Asset) => {
        return asset.users?.some(user => user.userId === userId);
      });

      allAssets.push(...myAssets);
    }

    // Sort by name
    allAssets.sort((a, b) => a.name.localeCompare(b.name));

    return allAssets;
  } catch (error) {
    console.error('Error fetching my assets:', error);
    throw error;
  }
}

export async function fetchAssetLocationsTodayOnly(
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
