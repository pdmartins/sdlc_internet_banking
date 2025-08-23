import { API_BASE_URL } from '../config/api';

export interface TransactionRequest {
  type: 'CREDIT' | 'DEBIT';
  amount: number;
  recipientAccount?: string;
  description: string;
}

export interface TransactionResponse {
  id: string;
  type: string;
  amount: number;
  description: string;
  recipientAccount?: string;
  balanceAfter: number;
  status: string;
  createdAt: string;
  processedAt?: string;
  fee: number;
}

export interface ApiError {
  message: string;
  errors?: { [key: string]: string[] };
}

class TransactionApiService {
  private async getAuthHeaders(): Promise<HeadersInit> {
    // In a real implementation, you would get the token from your auth context or storage
    const token = localStorage.getItem('authToken');
    return {
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : '',
    };
  }

  async createTransaction(request: TransactionRequest): Promise<TransactionResponse> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/transactions`, {
        method: 'POST',
        headers: await this.getAuthHeaders(),
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        const errorData: ApiError = await response.json();
        throw new Error(errorData.message || 'Failed to create transaction');
      }

      return await response.json();
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
  ): Promise<{
    transactions: TransactionResponse[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
  }> {
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
