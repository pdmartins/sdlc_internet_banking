import React, { useEffect, useState } from 'react';
import { SecurityUtils } from '../services/secureHttpClient';
import { validationPatterns, dangerousPatterns } from '../config/security';

interface SecureFormProps {
  onSubmit: (data: any) => void;
  children: React.ReactNode;
  className?: string;
}

interface FormErrors {
  [key: string]: string;
}

/**
 * Secure form component with XSS protection and input validation
 */
export const SecureForm: React.FC<SecureFormProps> = ({ onSubmit, children, className }) => {
  const [errors, setErrors] = useState<FormErrors>({});

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    
    const formData = new FormData(event.currentTarget);
    const data: { [key: string]: any } = {};
    const newErrors: FormErrors = {};

    // Process and validate all form fields
    Array.from(formData.entries()).forEach(([key, value]) => {
      const stringValue = value.toString();
      
      // Check for suspicious content
      if (SecurityUtils.containsSuspiciousContent(stringValue)) {
        newErrors[key] = 'Invalid input detected';
        return;
      }

      // Sanitize input
      const sanitizedValue = SecurityUtils.sanitizeInput(stringValue);
      
      // Validate specific field types
      if (key === 'email' && !SecurityUtils.isValidEmail(sanitizedValue)) {
        newErrors[key] = 'Invalid email format';
        return;
      }

      if (key === 'password' && !validationPatterns.password.test(sanitizedValue)) {
        newErrors[key] = 'Password must be at least 8 characters with uppercase, lowercase, number, and special character';
        return;
      }

      if (key === 'phone' && !validationPatterns.phone.test(sanitizedValue)) {
        newErrors[key] = 'Invalid phone number format';
        return;
      }

      data[key] = sanitizedValue;
    });

    setErrors(newErrors);

    // Only submit if no errors
    if (Object.keys(newErrors).length === 0) {
      onSubmit(data);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={className} noValidate>
      {children}
      {Object.keys(errors).length > 0 && (
        <div className="security-errors" role="alert">
          {Object.entries(errors).map(([field, error]) => (
            <div key={field} className="error-message">
              {error}
            </div>
          ))}
        </div>
      )}
    </form>
  );
};

interface SecureInputProps {
  name: string;
  type?: string;
  placeholder?: string;
  required?: boolean;
  className?: string;
  maxLength?: number;
  pattern?: string;
  'aria-label'?: string;
}

/**
 * Secure input component with built-in validation and sanitization
 */
export const SecureInput: React.FC<SecureInputProps> = ({
  name,
  type = 'text',
  placeholder,
  required = false,
  className,
  maxLength = 255,
  pattern,
  ...props
}) => {
  const [value, setValue] = useState('');
  const [error, setError] = useState('');

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const inputValue = event.target.value;
    
    // Check for dangerous patterns
    const hasDangerousContent = dangerousPatterns.some(pattern => pattern.test(inputValue));
    
    if (hasDangerousContent) {
      setError('Invalid characters detected');
      return;
    }

    // Clear error if input is valid
    setError('');
    setValue(inputValue);
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    const inputValue = event.target.value;
    
    // Validate on blur based on input type
    if (type === 'email' && inputValue && !SecurityUtils.isValidEmail(inputValue)) {
      setError('Invalid email format');
    } else if (type === 'password' && inputValue && !validationPatterns.password.test(inputValue)) {
      setError('Password does not meet security requirements');
    } else {
      setError('');
    }
  };

  return (
    <div className="secure-input-container">
      <input
        name={name}
        type={type}
        value={value}
        onChange={handleChange}
        onBlur={handleBlur}
        placeholder={placeholder}
        required={required}
        className={`${className} ${error ? 'error' : ''}`}
        maxLength={maxLength}
        pattern={pattern}
        aria-invalid={error ? 'true' : 'false'}
        aria-describedby={error ? `${name}-error` : undefined}
        {...props}
      />
      {error && (
        <div id={`${name}-error`} className="input-error" role="alert">
          {error}
        </div>
      )}
    </div>
  );
};

/**
 * Component to display security status and headers
 */
export const SecurityStatus: React.FC = () => {
  const [securityInfo, setSecurityInfo] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkSecurityHeaders = async () => {
      try {
        const response = await fetch('/api/security/headers');
        const data = await response.json();
        setSecurityInfo(data);
      } catch (error) {
        console.error('Failed to check security headers:', error);
      } finally {
        setLoading(false);
      }
    };

    checkSecurityHeaders();
  }, []);

  if (loading) {
    return <div>Checking security status...</div>;
  }

  return (
    <div className="security-status">
      <h3>Security Status</h3>
      <div className="security-checks">
        <div className={`check ${securityInfo?.isHttps ? 'pass' : 'fail'}`}>
          HTTPS: {securityInfo?.isHttps ? '✓ Enabled' : '✗ Disabled'}
        </div>
        <div className={`check ${securityInfo?.headers?.xFrameOptions ? 'pass' : 'fail'}`}>
          X-Frame-Options: {securityInfo?.headers?.xFrameOptions ? '✓ Set' : '✗ Missing'}
        </div>
        <div className={`check ${securityInfo?.headers?.xContentTypeOptions ? 'pass' : 'fail'}`}>
          X-Content-Type-Options: {securityInfo?.headers?.xContentTypeOptions ? '✓ Set' : '✗ Missing'}
        </div>
        <div className={`check ${securityInfo?.headers?.contentSecurityPolicy ? 'pass' : 'fail'}`}>
          Content-Security-Policy: {securityInfo?.headers?.contentSecurityPolicy ? '✓ Set' : '✗ Missing'}
        </div>
      </div>
    </div>
  );
};

export default { SecureForm, SecureInput, SecurityStatus };
