// API 請求與回應型別定義

export interface LoginRequest {
  username: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface ErrorResponse {
  message: string;
  statusCode: number;
  timestamp: string;
  path: string;
  errors?: Record<string, string[]>;
}

export interface User {
  id: string;
  username: string;
  roles: string[];
  createdAt: string;
}

export interface UserProfile extends User {
  claims: Claim[];
}

export interface Claim {
  type: string;
  value: string;
}

export interface UpdateProfileRequest {
  email?: string;
  displayName?: string;
  bio?: string;
}

export interface UpdateProfileResponse {
  message: string;
  updatedAt: string;
  data: {
    email?: string;
    displayName?: string;
    bio?: string;
  };
}

export interface Activity {
  id: number;
  action: string;
  timestamp: string;
  ipAddress: string;
  userAgent: string;
}

export interface ActivitiesResponse {
  totalCount: number;
  activities: Activity[];
}

export interface UsersResponse {
  totalCount: number;
  users: User[];
}

export interface StatsResponse {
  totalUsers: number;
  activeSessions: number;
  totalApiCalls: number;
  systemUptime: string;
  lastUpdated: string;
}
