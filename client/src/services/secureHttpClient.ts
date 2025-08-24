// Secure HTTP Client with CSRF Protection
import { API_BASE_URL } from '../config/api';

interface CsrfTokenResponse {
  token: string;
  headerName: string;
}

class SecureHttpClient {
  private csrfToken: string | null = null;
  private csrfHeaderName: string = 'X-CSRF-TOKEN';

  // Get CSRF token from server
  private async getCsrfToken(): Promise<string> {
    if (this.csrfToken) {
      return this.csrfToken;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/security/csrf-token`, {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to get CSRF token');
      }

      const tokenData: CsrfTokenResponse = await response.json();
      this.csrfToken = tokenData.token;
      this.csrfHeaderName = tokenData.headerName;

      return this.csrfToken;
    } catch (error) {
      console.error('Error getting CSRF token:', error);
      throw error;
    }
  }

  // Create secure request headers
  private async createSecureHeaders(additionalHeaders?: Record<string, string>): Promise<Record<string, string>> {
    const csrfToken = await this.getCsrfToken();
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      [this.csrfHeaderName]: csrfToken,
      ...additionalHeaders,
    };

    return headers;
  }

  // Secure GET request
  public async get<T>(url: string, headers?: Record<string, string>): Promise<T> {
    const secureHeaders = await this.createSecureHeaders(headers);
    
    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include',
      headers: secureHeaders,
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Secure POST request
  public async post<T>(url: string, data?: any, headers?: Record<string, string>): Promise<T> {
    const secureHeaders = await this.createSecureHeaders(headers);
    
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: secureHeaders,
      body: data ? JSON.stringify(data) : undefined,
    });

    if (!response.ok) {
      if (response.status === 400 && response.headers.get('Content-Type')?.includes('application/json')) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Request failed');
      }
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Secure PUT request
  public async put<T>(url: string, data?: any, headers?: Record<string, string>): Promise<T> {
    const secureHeaders = await this.createSecureHeaders(headers);
    
    const response = await fetch(url, {
      method: 'PUT',
      credentials: 'include',
      headers: secureHeaders,
      body: data ? JSON.stringify(data) : undefined,
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Secure DELETE request
  public async delete<T>(url: string, headers?: Record<string, string>): Promise<T> {
    const secureHeaders = await this.createSecureHeaders(headers);
    
    const response = await fetch(url, {
      method: 'DELETE',
      credentials: 'include',
      headers: secureHeaders,
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Clear CSRF token (on logout)
  public clearToken(): void {
    this.csrfToken = null;
  }
}

// Input sanitization utilities
export class SecurityUtils {
  // HTML encode to prevent XSS
  public static htmlEncode(input: string): string {
    const div = document.createElement('div');
    div.textContent = input;
    return div.innerHTML;
  }

  // Remove potentially dangerous characters
  public static sanitizeInput(input: string): string {
    if (!input) return '';
    
    // Remove script tags and javascript: protocols
    let sanitized = input
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
      .replace(/javascript:/gi, '')
      .replace(/on\w+\s*=/gi, '')
      .replace(/(<\s*\w+\s+[^>]*?)on\w+\s*=\s*"[^"]*"([^>]*>)/gi, '$1$2')
      .replace(/(<\s*\w+\s+[^>]*?)on\w+\s*=\s*'[^']*'([^>]*>)/gi, '$1$2');

    return sanitized.trim();
  }

  // Validate email format
  public static isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  // Check for potentially malicious patterns
  public static containsSuspiciousContent(input: string): boolean {
    const suspiciousPatterns = [
      /<script/i,
      /javascript:/i,
      /vbscript:/i,
      /data:text\/html/i,
      /on\w+\s*=/i,
    ];

    return suspiciousPatterns.some(pattern => pattern.test(input));
  }
}

// Export singleton instance
export const secureHttpClient = new SecureHttpClient();
export default secureHttpClient;
