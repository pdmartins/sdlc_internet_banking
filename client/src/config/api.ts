// API Configuration
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

export const API_ENDPOINTS = {
  LOGIN: `${API_BASE_URL}/api/authentication/login`,
  REGISTER: `${API_BASE_URL}/api/registration/register`,
  SETUP_SECURITY: `${API_BASE_URL}/api/registration/setup-security`,
  COMPLETE_REGISTRATION: `${API_BASE_URL}/api/registration/complete`,
  MFA_SEND_CODE: `${API_BASE_URL}/api/mfa/send-code`,
  MFA_VERIFY_CODE: `${API_BASE_URL}/api/mfa/verify-code`,
  MFA_RESEND_CODE: (sessionId: string) => `${API_BASE_URL}/api/mfa/resend-code/${sessionId}`,
  MFA_SESSION_STATUS: (sessionId: string) => `${API_BASE_URL}/api/mfa/session/${sessionId}/status`,
  PASSWORD_RESET_REQUEST: `${API_BASE_URL}/api/PasswordReset/request`,
  PASSWORD_RESET_VALIDATE: (token: string) => `${API_BASE_URL}/api/PasswordReset/validate/${token}`,
  PASSWORD_RESET_CONFIRM: `${API_BASE_URL}/api/PasswordReset/reset`,
} as const;

export { API_BASE_URL };
