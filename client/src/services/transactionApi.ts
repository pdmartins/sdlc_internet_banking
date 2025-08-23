import { API_BASE_URL } from '../config/api';

export interface TransactionRequest {
  type: 'CREDIT' | 'DEBIT';
  category: string;
  amount: number;
  description: string;
  reference?: string;
  recipientAccount?: string;
  recipientName?: string;
}

export interface TransactionResponse {
  transactionId: string;
  accountId: string;
  type: string;
  category: string;
  amount: number;
  balanceAfter: number;
  description: string;
  reference: string;
  recipientAccount?: string;
  recipientName?: string;
  status: string;
  fee: number;
  createdAt: string;
  processedAt?: string;
}

export interface TransactionProcessResult {
  isSuccessful: boolean;
  transaction?: TransactionResponse;
  errorMessage: string;
  errorCode: string;
  validationErrors: string[];
  accountBalance: number;
  dailyLimit: number;
  dailyUsed: number;
  monthlyLimit: number;
  monthlyUsed: number;
  estimatedFee: number;
}

export interface ApiError {
  message: string;
  errors?: { [key: string]: string[] };
}

class TransactionApiService {
  private async getAuthHeaders(): Promise<HeadersInit> {
    // Get the token from the user session stored by AuthContext
    try {
      const userData = localStorage.getItem('user');
      const token = userData ? JSON.parse(userData)?.token : null;
      
      console.log('TransactionAPI - Auth debug:', {
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

  async createTransaction(request: TransactionRequest): Promise<TransactionResponse> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/transactions/process`, {
        method: 'POST',
        headers: await this.getAuthHeaders(),
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to create transaction');
      }

      const result: TransactionProcessResult = await response.json();
      
      if (!result.isSuccessful) {
        throw new Error(result.errorMessage || 'Transaction failed');
      }
      
      if (!result.transaction) {
        throw new Error('Transaction processed but no transaction data returned');
      }

      return result.transaction;
    } catch (error) {
      console.error('Transaction API error:', error);
      throw error;
    }
  }

  async getTransactionHistory(
    pageNumber: number = 1,
    pageSize: number = 10,
    fromDate?: string,
    toDate?: string,
    type?: string
  ): Promise<TransactionResponse[]> {
    try {
      const params = new URLSearchParams({
        pageNumber: pageNumber.toString(),
        pageSize: pageSize.toString(),
      });

      if (fromDate) params.append('fromDate', fromDate);
      if (toDate) params.append('toDate', toDate);
      if (type) params.append('type', type);

      const response = await fetch(`${API_BASE_URL}/api/transactions/history?${params}`, {
        method: 'GET',
        headers: await this.getAuthHeaders(),
      });

      if (!response.ok) {
        const errorData: ApiError = await response.json();
        throw new Error(errorData.message || 'Failed to fetch transaction history');
      }

      return await response.json();
    } catch (error) {
      console.error('Transaction history API error:', error);
      throw error;
    }
  }

  async getTransactionById(id: string): Promise<TransactionResponse> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/transactions/${id}`, {
        method: 'GET',
        headers: await this.getAuthHeaders(),
      });

      if (!response.ok) {
        const errorData: ApiError = await response.json();
        throw new Error(errorData.message || 'Failed to fetch transaction');
      }

      return await response.json();
    } catch (error) {
      console.error('Transaction fetch API error:', error);
      throw error;
    }
  }
}

// Export singleton instance
export const transactionApi = new TransactionApiService();
