import React, { createContext, useContext, useState, ReactNode } from 'react';
import { 
  registrationApi, 
  UserRegistrationResponse, 
  RegistrationCompleteResponse,
  convertDateToISO
} from '../services/registrationApi';

export interface PersonalInfo {
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  cpf: string;
}

export interface SecurityInfo {
  securityQuestion: string;
  securityAnswer: string;
  mfaOption: 'sms' | 'authenticator';
  passwordStrength: number;
  password?: string; // Temporarily store for API call
}

export interface AccountInfo {
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
}

export interface RegistrationData {
  personalInfo: PersonalInfo | null;
  securityInfo: SecurityInfo | null;
  accountInfo: AccountInfo | null;
  userId: string | null;
  registrationDate: Date | null;
  isLoading: boolean;
  error: string | null;
}

interface RegistrationContextType {
  registrationData: RegistrationData;
  registerUser: (info: PersonalInfo) => Promise<void>;
  setupSecurity: (info: SecurityInfo) => Promise<void>;
  validateEmail: (email: string) => Promise<boolean>;
  validateCPF: (cpf: string) => Promise<boolean>;
  validatePhone: (phone: string) => Promise<boolean>;
  clearRegistrationData: () => void;
  clearError: () => void;
}

const RegistrationContext = createContext<RegistrationContextType | undefined>(undefined);

export const useRegistration = () => {
  const context = useContext(RegistrationContext);
  if (context === undefined) {
    throw new Error('useRegistration must be used within a RegistrationProvider');
  }
  return context;
};

interface RegistrationProviderProps {
  children: ReactNode;
}

export const RegistrationProvider: React.FC<RegistrationProviderProps> = ({ children }) => {
  const [registrationData, setRegistrationData] = useState<RegistrationData>({
    personalInfo: null,
    securityInfo: null,
    accountInfo: null,
    userId: null,
    registrationDate: null,
    isLoading: false,
    error: null,
  });

  const registerUser = async (info: PersonalInfo): Promise<void> => {
    setRegistrationData(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const request = {
        fullName: info.fullName,
        email: info.email,
        phone: info.phone,
        cpf: info.cpf,
        dateOfBirth: convertDateToISO(info.dateOfBirth), // Convert DD/MM/YYYY to YYYY-MM-DD
      };

      const response: UserRegistrationResponse = await registrationApi.registerUser(request);
      
      setRegistrationData(prev => ({
        ...prev,
        personalInfo: info,
        userId: response.userId,
        isLoading: false,
      }));
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Erro ao registrar usuário';
      setRegistrationData(prev => ({
        ...prev,
        isLoading: false,
        error: errorMessage,
      }));
      throw error; // Re-throw so forms can handle it
    }
  };

  const setupSecurity = async (info: SecurityInfo): Promise<void> => {
    if (!registrationData.userId) {
      throw new Error('Usuário não foi registrado. Complete o cadastro pessoal primeiro.');
    }

    setRegistrationData(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const request = {
        userId: registrationData.userId, // Include userId in request body
        password: info.password || '',
        securityQuestion: info.securityQuestion,
        securityAnswer: info.securityAnswer,
        mfaOption: info.mfaOption,
        passwordStrength: info.passwordStrength, // Send as number, not string
      };

      const response: RegistrationCompleteResponse = await registrationApi.setupSecurity(
        registrationData.userId,
        request
      );
      
      // Remove password from stored security info for security
      const securityInfoToStore = { ...info };
      delete securityInfoToStore.password;
      
      setRegistrationData(prev => ({
        ...prev,
        securityInfo: securityInfoToStore,
        accountInfo: response.account,
        registrationDate: response.completedAt ? new Date(response.completedAt) : new Date(),
        isLoading: false,
      }));
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Erro ao configurar segurança';
      setRegistrationData(prev => ({
        ...prev,
        isLoading: false,
        error: errorMessage,
      }));
      throw error; // Re-throw so forms can handle it
    }
  };

  const validateEmail = async (email: string): Promise<boolean> => {
    try {
      const response = await registrationApi.validateEmail(email);
      return response.available;
    } catch (error) {
      console.error('Error validating email:', error);
      return false; // Assume not available on error for safety
    }
  };

  const validateCPF = async (cpf: string): Promise<boolean> => {
    try {
      const response = await registrationApi.validateCPF(cpf);
      return response.available;
    } catch (error) {
      console.error('Error validating CPF:', error);
      return false; // Assume not available on error for safety
    }
  };

  const validatePhone = async (phone: string): Promise<boolean> => {
    try {
      const response = await registrationApi.validatePhone(phone);
      return response.available;
    } catch (error) {
      console.error('Error validating phone:', error);
      return false; // Assume not available on error for safety
    }
  };

  const clearError = () => {
    setRegistrationData(prev => ({ ...prev, error: null }));
  };

  const clearRegistrationData = () => {
    setRegistrationData({
      personalInfo: null,
      securityInfo: null,
      accountInfo: null,
      userId: null,
      registrationDate: null,
      isLoading: false,
      error: null,
    });
  };

  const value = {
    registrationData,
    registerUser,
    setupSecurity,
    validateEmail,
    validateCPF,
    validatePhone,
    clearRegistrationData,
    clearError,
  };

  return (
    <RegistrationContext.Provider value={value}>
      {children}
    </RegistrationContext.Provider>
  );
};

// Utility functions for data masking
export const maskEmail = (email: string): string => {
  if (!email) return '';
  const [username, domain] = email.split('@');
  if (!username || !domain) return email;
  
  const maskedUsername = username.length > 2 
    ? username[0] + '*'.repeat(username.length - 2) + username[username.length - 1]
    : username[0] + '*';
  
  const [domainName, extension] = domain.split('.');
  const maskedDomain = domainName.length > 2 
    ? domainName[0] + '*'.repeat(domainName.length - 2) + domainName[domainName.length - 1]
    : domainName[0] + '*';
  
  return `${maskedUsername}@${maskedDomain}.${extension}`;
};

export const maskPhone = (phone: string): string => {
  if (!phone) return '';
  // Remove any formatting and keep only digits
  const digits = phone.replace(/\D/g, '');
  
  if (digits.length === 11) {
    // Format: (11) 9****-****
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 3)}${'*'.repeat(4)}-${'*'.repeat(4)}`;
  } else if (digits.length === 10) {
    // Format: (11) ****-****
    return `(${digits.slice(0, 2)}) ${'*'.repeat(4)}-${'*'.repeat(4)}`;
  }
  
  return phone; // Return original if format is unexpected
};

export const maskAccountNumber = (accountNumber: string): string => {
  if (!accountNumber) return '';
  // For account numbers like "0001-123456-7"
  const parts = accountNumber.split('-');
  if (parts.length === 3) {
    return `${'*'.repeat(4)}-${'*'.repeat(parts[1].length)}-${parts[2]}`;
  }
  // For other formats, mask middle part
  if (accountNumber.length > 4) {
    const start = accountNumber.slice(0, 2);
    const end = accountNumber.slice(-2);
    const middle = '*'.repeat(accountNumber.length - 4);
    return `${start}${middle}${end}`;
  }
  return accountNumber;
};

export const maskCPF = (cpf: string): string => {
  if (!cpf) return '';
  const digits = cpf.replace(/\D/g, '');
  if (digits.length === 11) {
    return `${'*'.repeat(3)}.${digits.slice(3, 6)}.${'*'.repeat(3)}-${digits.slice(9)}`;
  }
  return cpf;
};

export const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL'
  }).format(amount);
};
