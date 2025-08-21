// API configuration
const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7000';

// API response types matching backend DTOs
export interface UserRegistrationResponse {
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  cpf: string;
  dateOfBirth: string;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  createdAt: string;
}

export interface RegistrationCompleteResponse {
  user: {
    userId: string;
    fullName: string;
    email: string;
    phone: string;
    cpf: string;
    dateOfBirth: string;
    isEmailVerified: boolean;
    isPhoneVerified: boolean;
    createdAt: string;
  };
  account: {
    accountId: string;
    userId: string;
    accountNumber: string;
    branchCode: string;
    accountType: string;
    balance: number;
    dailyLimit: number;
    monthlyLimit: number;
    isActive: boolean;
    createdAt: string;
  };
  securityConfigured: string;
  mfaOption: string;
  passwordStrength: number;
  completedAt: string;
}

export interface ValidationResponse {
  available: boolean;
}

export interface ApiError {
  message: string;
  errors?: { [key: string]: string[] };
}

// Personal info request type matching backend DTO
export interface RegisterUserRequest {
  fullName: string;
  email: string;
  phone: string;
  cpf: string;
  dateOfBirth: string; // ISO format: YYYY-MM-DD
}

// Security setup request type matching backend DTO
export interface SetupSecurityRequest {
  userId: string;
  password: string;
  securityQuestion: string;
  securityAnswer: string;
  mfaOption: string;
  passwordStrength: number;
}

class RegistrationApiService {
  private async makeRequest<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    
    try {
      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          ...options.headers,
        },
        ...options,
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`API request failed: ${endpoint}`, error);
      throw error;
    }
  }

  // Register user with personal information
  async registerUser(personalInfo: RegisterUserRequest): Promise<UserRegistrationResponse> {
    return this.makeRequest<UserRegistrationResponse>('/api/registration/register', {
      method: 'POST',
      body: JSON.stringify(personalInfo),
    });
  }

  // Setup security information for a user
  async setupSecurity(userId: string, securityInfo: SetupSecurityRequest): Promise<RegistrationCompleteResponse> {
    // Add userId to the request body to match backend DTO expectations
    const requestBody = {
      ...securityInfo,
      userId: userId
    };
    
    return this.makeRequest<RegistrationCompleteResponse>(`/api/registration/${userId}/security`, {
      method: 'POST',
      body: JSON.stringify(requestBody),
    });
  }

  // Validation endpoints
  async validateEmail(email: string): Promise<ValidationResponse> {
    const encodedEmail = encodeURIComponent(email);
    return this.makeRequest<ValidationResponse>(`/api/registration/validate/email?email=${encodedEmail}`);
  }

  async validateCPF(cpf: string): Promise<ValidationResponse> {
    const encodedCPF = encodeURIComponent(cpf);
    return this.makeRequest<ValidationResponse>(`/api/registration/validate/cpf?cpf=${encodedCPF}`);
  }

  async validatePhone(phone: string): Promise<ValidationResponse> {
    const encodedPhone = encodeURIComponent(phone);
    return this.makeRequest<ValidationResponse>(`/api/registration/validate/phone?phone=${encodedPhone}`);
  }
}

// Export singleton instance
export const registrationApi = new RegistrationApiService();

// Utility function to convert frontend date format to backend format
export const convertDateToISO = (dateString: string): string => {
  // Convert from DD/MM/YYYY to YYYY-MM-DD
  const [day, month, year] = dateString.split('/');
  return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
};

// Utility function to get password strength string
export const getPasswordStrengthString = (score: number): string => {
  switch (score) {
    case 0:
    case 1:
      return 'Weak';
    case 2:
      return 'Fair';
    case 3:
      return 'Good';
    case 4:
      return 'Strong';
    default:
      return 'Weak';
  }
};
