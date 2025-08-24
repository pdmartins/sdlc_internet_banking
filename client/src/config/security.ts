// Security Configuration for Azure AD and MSAL
import { Configuration, LogLevel } from '@azure/msal-browser';

// Azure AD configuration for OAuth 2.0/OpenID Connect
export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_CLIENT_ID || 'your-client-id-here',
    authority: process.env.REACT_APP_AUTHORITY || 'https://login.microsoftonline.com/your-tenant-id',
    redirectUri: process.env.REACT_APP_REDIRECT_URI || window.location.origin,
    postLogoutRedirectUri: process.env.REACT_APP_POST_LOGOUT_REDIRECT_URI || window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage', // More secure than localStorage
    storeAuthStateInCookie: false, // Set to true for IE11 or Edge
  },
  system: {
    loggerOptions: {
      loggerCallback: (level: LogLevel, message: string) => {
        if (process.env.NODE_ENV === 'development') {
          switch (level) {
            case LogLevel.Error:
              console.error(message);
              break;
            case LogLevel.Warning:
              console.warn(message);
              break;
            case LogLevel.Info:
              console.info(message);
              break;
            default:
              console.log(message);
              break;
          }
        }
      },
      logLevel: process.env.NODE_ENV === 'development' ? LogLevel.Verbose : LogLevel.Error,
    },
  },
};

// API scopes for backend access
export const apiScopes = {
  read: ['api://your-api-client-id/read'],
  write: ['api://your-api-client-id/write'],
};

// Content Security Policy configuration
export const cspConfig = {
  defaultSrc: ["'self'"],
  scriptSrc: ["'self'", "'unsafe-inline'"], // Unsafe-inline should be removed in production
  styleSrc: ["'self'", "'unsafe-inline'"],
  imgSrc: ["'self'", 'data:', 'https:'],
  fontSrc: ["'self'"],
  connectSrc: ["'self'", 'https://login.microsoftonline.com'],
  frameSrc: ["'none'"],
  objectSrc: ["'none'"],
  mediaSrc: ["'none'"],
};

// Security headers configuration
export const securityHeaders = {
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'DENY',
  'X-XSS-Protection': '1; mode=block',
  'Referrer-Policy': 'strict-origin-when-cross-origin',
  'Permissions-Policy': 'geolocation=(), microphone=(), camera=()',
};

// Session configuration
export const sessionConfig = {
  timeout: 30 * 60 * 1000, // 30 minutes in milliseconds
  warningTime: 5 * 60 * 1000, // Show warning 5 minutes before timeout
  checkInterval: 60 * 1000, // Check every minute
};

// Input validation patterns
export const validationPatterns = {
  email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  phone: /^\+?[\d\s\-\(\)]{10,}$/,
  cpf: /^\d{3}\.\d{3}\.\d{3}-\d{2}$/,
  password: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
};

// Dangerous patterns to block
export const dangerousPatterns = [
  /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
  /javascript:/gi,
  /vbscript:/gi,
  /data:text\/html/gi,
  /on\w+\s*=/gi,
  /<iframe/gi,
  /<object/gi,
  /<embed/gi,
];

export default {
  msalConfig,
  apiScopes,
  cspConfig,
  securityHeaders,
  sessionConfig,
  validationPatterns,
  dangerousPatterns,
};
