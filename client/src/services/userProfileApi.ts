import { API_BASE_URL } from '../config/api';

export interface UserProfile {
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  cpf: string;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  createdAt: string;
  updatedAt?: string;
  lastLoginAt?: string;
  mfaOption: string;
  securityQuestion: string;
  passwordStrength: number;
}

export interface UpdateProfileRequest {
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  currentPassword: string;
}

export interface UpdatePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface UpdateSecurityQuestionRequest {
  currentPassword: string;
  securityQuestion: string;
  securityAnswer: string;
}

export interface UpdateMfaRequest {
  currentPassword: string;
  mfaOption: 'sms' | 'authenticator';
}

export interface SecuritySettings {
  mfaOption: string;
  securityQuestion: string;
  passwordStrength: number;
  lastPasswordChange?: string;
  failedLoginAttempts: number;
  lastFailedLoginAt?: string;
  accountLockedUntil?: string;
}

export interface DeviceInfo {
  id: string;
  deviceInfo: string;
  location: string;
  ipAddress: string;
  createdAt: string;
  lastActivityAt?: string;
  isTrustedDevice: boolean;
  isCurrentSession: boolean;
}

class UserProfileApiService {
  private async getAuthHeaders(): Promise<HeadersInit> {
    // Get the token from the user session stored by AuthContext (same as transactionApi)
    try {
      const userData = localStorage.getItem('user');
      const token = userData ? JSON.parse(userData)?.token : null;
      
      console.log('UserProfileAPI - Auth debug:', {
        userDataExists: !!userData,
        userDataLength: userData?.length,
        tokenExists: !!token,
        tokenPreview: token ? `${token.substring(0, 20)}...` : 'null'
      });
      
      return {
        'Content-Type': 'application/json',
        'Authorization': token ? `Bearer ${token}` : '',
      };
    } catch (error) {
      console.error('Error getting auth token:', error);
      return {
        'Content-Type': 'application/json',
      };
    }
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const authHeaders = await this.getAuthHeaders();
    
    const config: RequestInit = {
      headers: {
        ...authHeaders,
        ...options.headers,
      },
      ...options,
    };

    const response = await fetch(endpoint, config);
    
    if (!response.ok) {
      const errorData = await response.text();
      let errorMessage = 'Erro na operação';
      
      try {
        const errorJson = JSON.parse(errorData);
        errorMessage = errorJson.message || errorJson.title || errorMessage;
      } catch {
        errorMessage = errorData || errorMessage;
      }
      
      throw new Error(errorMessage);
    }

    // Handle empty responses
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return response.json();
    }
    
    return response.text() as unknown as T;
  }

  async getUserProfile(): Promise<UserProfile> {
    return this.request<UserProfile>(`${API_BASE_URL}/api/userprofile`);
  }

  async updateProfile(request: UpdateProfileRequest): Promise<UserProfile> {
    return this.request<UserProfile>(`${API_BASE_URL}/api/userprofile`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async updatePassword(request: UpdatePasswordRequest): Promise<{ message: string }> {
    return this.request<{ message: string }>(`${API_BASE_URL}/api/userprofile/password`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async updateSecurityQuestion(request: UpdateSecurityQuestionRequest): Promise<{ message: string }> {
    return this.request<{ message: string }>(`${API_BASE_URL}/api/userprofile/security-question`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async updateMfaOption(request: UpdateMfaRequest): Promise<{ message: string }> {
    return this.request<{ message: string }>(`${API_BASE_URL}/api/userprofile/mfa`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async getSecuritySettings(): Promise<SecuritySettings> {
    return this.request<SecuritySettings>(`${API_BASE_URL}/api/userprofile/security-settings`);
  }

  async getActiveDevices(): Promise<DeviceInfo[]> {
    return this.request<DeviceInfo[]>(`${API_BASE_URL}/api/userprofile/devices`);
  }

  async revokeDevice(deviceId: string): Promise<{ message: string }> {
    return this.request<{ message: string }>(`${API_BASE_URL}/api/userprofile/devices/${deviceId}`, {
      method: 'DELETE',
    });
  }

  async revokeAllOtherDevices(): Promise<{ message: string; revokedCount: number }> {
    return this.request<{ message: string; revokedCount: number }>(`${API_BASE_URL}/api/userprofile/revoke-all-devices`, {
      method: 'POST',
    });
  }
}

export const userProfileApi = new UserProfileApiService();
