import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { API_ENDPOINTS } from '../config/api';

// Validation schema for reset password form
const resetPasswordSchema = yup.object({
  newPassword: yup
    .string()
    .required('Nova senha é obrigatória')
    .min(8, 'A senha deve ter pelo menos 8 caracteres')
    .max(100, 'A senha não pode exceder 100 caracteres')
    .matches(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
      'A senha deve conter pelo menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial'
    ),
  confirmPassword: yup
    .string()
    .required('Confirmação de senha é obrigatória')
    .oneOf([yup.ref('newPassword')], 'As senhas não coincidem'),
  securityAnswer: yup
    .string()
    .required('Resposta de segurança é obrigatória')
    .min(2, 'Resposta deve ter pelo menos 2 caracteres')
    .max(100, 'Resposta não pode exceder 100 caracteres'),
});

type ResetPasswordFormData = yup.InferType<typeof resetPasswordSchema>;

interface TokenValidation {
  isValid: boolean;
  securityQuestion?: string;
  expiresAt?: string;
  remainingAttempts?: number;
  message?: string;
}

const ResetPassword: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string>('');
  const [isSuccess, setIsSuccess] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  
  // Token validation state
  const [isValidatingToken, setIsValidatingToken] = useState(true);
  const [tokenValidation, setTokenValidation] = useState<TokenValidation>({ isValid: false });

  const {
    register,
    handleSubmit,
    formState: { errors, isValid, touchedFields },
    watch,
  } = useForm<ResetPasswordFormData>({
    resolver: yupResolver(resetPasswordSchema),
    mode: 'onChange',
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
      securityAnswer: '',
    },
  });

  const watchedFields = watch();

  useEffect(() => {
    if (!token) {
      setTokenValidation({ isValid: false, message: 'Token de redefinição não encontrado' });
      setIsValidatingToken(false);
      return;
    }

    validateToken();
  }, [token]);

  const validateToken = async () => {
    try {
      const response = await fetch(API_ENDPOINTS.PASSWORD_RESET_VALIDATE(token!), {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result = await response.json();

      if (response.ok && result.success) {
        setTokenValidation({
          isValid: true,
          securityQuestion: result.securityQuestion,
          expiresAt: result.expiresAt,
          remainingAttempts: result.remainingAttempts,
        });
      } else {
        setTokenValidation({
          isValid: false,
          message: result.message || 'Token inválido ou expirado',
        });
      }
    } catch (error) {
      console.error('Token validation failed:', error);
      setTokenValidation({
        isValid: false,
        message: 'Erro ao validar token de redefinição',
      });
    } finally {
      setIsValidatingToken(false);
    }
  };

  const getFieldStatus = (fieldName: keyof ResetPasswordFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  const getPasswordStrength = (password: string): { strength: number; label: string; color: string } => {
    if (password.length === 0) return { strength: 0, label: '', color: '' };

    let strength = 0;
    if (password.length >= 8) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/\d/.test(password)) strength++;
    if (/[@$!%*?&]/.test(password)) strength++;

    const labels = ['Muito Fraca', 'Fraca', 'Regular', 'Boa', 'Forte'];
    const colors = ['#ff4757', '#ff6b7d', '#ffa502', '#2ed573', '#1e90ff'];

    return {
      strength: (strength / 5) * 100,
      label: labels[strength - 1] || 'Muito Fraca',
      color: colors[strength - 1] || '#ff4757',
    };
  };

  const passwordStrength = getPasswordStrength(watchedFields.newPassword || '');

  const handleFormSubmit = async (data: ResetPasswordFormData) => {
    if (isSubmitting || !token) return;

    setIsSubmitting(true);
    setSubmitError('');

    try {
      const requestBody = {
        Token: token,
        NewPassword: data.newPassword,
        ConfirmPassword: data.confirmPassword,
        SecurityAnswer: data.securityAnswer,
      };

      console.log('Password reset request:', {
        url: API_ENDPOINTS.PASSWORD_RESET_CONFIRM,
        body: { ...requestBody, NewPassword: '[HIDDEN]', ConfirmPassword: '[HIDDEN]' }
      });

      const response = await fetch(API_ENDPOINTS.PASSWORD_RESET_CONFIRM, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody),
      });

      const result = await response.json();
      console.log('Password reset response:', { status: response.status, result });

      if (!response.ok) {
        // Log the full error details for debugging
        console.error('Password reset API error:', {
          status: response.status,
          statusText: response.statusText,
          result
        });
        throw new Error(result.message || 'Erro ao redefinir senha');
      }

      setIsSuccess(true);
    } catch (error) {
      console.error('Password reset failed:', error);
      setSubmitError(error instanceof Error ? error.message : 'Erro inesperado ao redefinir senha');
    } finally {
      setIsSubmitting(false);
    }
  };

  const clearError = () => {
    setSubmitError('');
  };

  const handleBackToLogin = () => {
    navigate('/login');
  };

  // Loading state while validating token
  if (isValidatingToken) {
    return (
      <div className="reset-password-container">
        <div className="reset-password-card">
          <div className="form-header">
            <div className="logo-container" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
              <svg 
                className="logo" 
                viewBox="0 0 120 40" 
                style={{ width: '120px', height: '40px' }}
                aria-label="Contoso Bank Logo"
                role="img"
              >
                <rect width="120" height="40" fill="#0078D4" rx="4"/>
                <text 
                  x="60" 
                  y="25" 
                  textAnchor="middle" 
                  fill="white" 
                  fontSize="14" 
                  fontWeight="600"
                >
                  Contoso
                </text>
              </svg>
            </div>
            <h1 className="form-title">Validando...</h1>
            <div style={{ textAlign: 'center' }}>
              <span className="loading-spinner" style={{ fontSize: '2rem' }}></span>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Invalid token state
  if (!tokenValidation.isValid) {
    return (
      <div className="reset-password-container">
        <div className="reset-password-card">
          <div className="form-header">
            <div className="logo-container" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
              <svg 
                className="logo" 
                viewBox="0 0 120 40" 
                style={{ width: '120px', height: '40px' }}
                aria-label="Contoso Bank Logo"
                role="img"
              >
                <rect width="120" height="40" fill="#0078D4" rx="4"/>
                <text 
                  x="60" 
                  y="25" 
                  textAnchor="middle" 
                  fill="white" 
                  fontSize="14" 
                  fontWeight="600"
                >
                  Contoso
                </text>
              </svg>
            </div>
            <h1 className="form-title">Link inválido</h1>
            <p className="form-description">
              {tokenValidation.message}
            </p>
          </div>

          <div className="error-content" style={{ textAlign: 'center', margin: '2rem 0' }}>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>❌</div>
            <p>Este link pode ter expirado ou já ter sido usado.</p>
          </div>

          <div className="form-actions">
            <Link to="/forgot-password" className="btn btn-primary btn-full-width" style={{ marginBottom: '1rem' }}>
              Solicitar novo link
            </Link>
            <button onClick={handleBackToLogin} className="btn btn-secondary btn-full-width" type="button">
              Voltar ao login
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Success state
  if (isSuccess) {
    return (
      <div className="reset-password-container">
        <div className="reset-password-card">
          <div className="form-header">
            <div className="logo-container" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
              <svg 
                className="logo" 
                viewBox="0 0 120 40" 
                style={{ width: '120px', height: '40px' }}
                aria-label="Contoso Bank Logo"
                role="img"
              >
                <rect width="120" height="40" fill="#0078D4" rx="4"/>
                <text 
                  x="60" 
                  y="25" 
                  textAnchor="middle" 
                  fill="white" 
                  fontSize="14" 
                  fontWeight="600"
                >
                  Contoso
                </text>
              </svg>
            </div>
            <h1 className="form-title">Senha redefinida!</h1>
            <p className="form-description">
              Sua senha foi redefinida com sucesso.
            </p>
          </div>

          <div className="success-content" style={{ textAlign: 'center', margin: '2rem 0' }}>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>✅</div>
            <p>Você pode agora fazer login com sua nova senha.</p>
          </div>

          <div className="form-actions">
            <button onClick={handleBackToLogin} className="btn btn-primary btn-full-width" type="button">
              Fazer login
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Reset password form
  return (
    <div className="reset-password-container">
      <div className="reset-password-card">
        {/* Header */}
        <div className="form-header">
          <div className="logo-container" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
            <svg 
              className="logo" 
              viewBox="0 0 120 40" 
              style={{ width: '120px', height: '40px' }}
              aria-label="Contoso Bank Logo"
              role="img"
            >
              <rect width="120" height="40" fill="#0078D4" rx="4"/>
              <text 
                x="60" 
                y="25" 
                textAnchor="middle" 
                fill="white" 
                fontSize="14" 
                fontWeight="600"
              >
                Contoso
              </text>
            </svg>
          </div>

          <h1 className="form-title">Redefinir senha</h1>
          <p className="form-description">
            Crie uma nova senha segura para sua conta
          </p>
          {tokenValidation.remainingAttempts && (
            <p className="form-description" style={{ color: '#ff6b7d' }}>
              Tentativas restantes: {tokenValidation.remainingAttempts}
            </p>
          )}
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(handleFormSubmit)} className="reset-password-form">
          {/* Security Question */}
          <div className="form-group">
            <label className="form-label">
              Pergunta de segurança
            </label>
            <div className="security-question-display">
              {tokenValidation.securityQuestion}
            </div>
          </div>

          {/* Security Answer */}
          <div className="form-group">
            <label htmlFor="securityAnswer" className="form-label">
              Resposta de segurança *
            </label>
            <input
              {...register('securityAnswer')}
              type="text"
              id="securityAnswer"
              className={`form-input ${getFieldStatus('securityAnswer')}`}
              placeholder="Digite sua resposta"
              autoComplete="off"
              aria-describedby="securityAnswer-error"
            />
            {errors.securityAnswer && (
              <div className="field-feedback error" id="securityAnswer-error">
                <span className="feedback-icon">❌</span>
                {errors.securityAnswer.message}
              </div>
            )}
          </div>

          {/* New Password */}
          <div className="form-group">
            <label htmlFor="newPassword" className="form-label">
              Nova senha *
            </label>
            <div className="password-input-container">
              <input
                {...register('newPassword')}
                type={showPassword ? 'text' : 'password'}
                id="newPassword"
                className={`form-input ${getFieldStatus('newPassword')}`}
                placeholder="Digite sua nova senha"
                autoComplete="new-password"
                aria-describedby="newPassword-error"
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword(!showPassword)}
                aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
              >
                {showPassword ? '👁️' : '👁️‍🗨️'}
              </button>
            </div>
            
            {/* Password Strength Indicator */}
            {watchedFields.newPassword && (
              <div className="password-strength">
                <div className="strength-bar">
                  <div 
                    className="strength-fill" 
                    style={{ 
                      width: `${passwordStrength.strength}%`,
                      backgroundColor: passwordStrength.color 
                    }}
                  ></div>
                </div>
                <span className="strength-label" style={{ color: passwordStrength.color }}>
                  {passwordStrength.label}
                </span>
              </div>
            )}

            {errors.newPassword && (
              <div className="field-feedback error" id="newPassword-error">
                <span className="feedback-icon">❌</span>
                {errors.newPassword.message}
              </div>
            )}
          </div>

          {/* Confirm Password */}
          <div className="form-group">
            <label htmlFor="confirmPassword" className="form-label">
              Confirmar nova senha *
            </label>
            <div className="password-input-container">
              <input
                {...register('confirmPassword')}
                type={showConfirmPassword ? 'text' : 'password'}
                id="confirmPassword"
                className={`form-input ${getFieldStatus('confirmPassword')}`}
                placeholder="Confirme sua nova senha"
                autoComplete="new-password"
                aria-describedby="confirmPassword-error"
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                aria-label={showConfirmPassword ? 'Ocultar senha' : 'Mostrar senha'}
              >
                {showConfirmPassword ? '👁️' : '👁️‍🗨️'}
              </button>
            </div>
            {getFieldStatus('confirmPassword') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                Senhas coincidem
              </div>
            )}
            {errors.confirmPassword && (
              <div className="field-feedback error" id="confirmPassword-error">
                <span className="feedback-icon">❌</span>
                {errors.confirmPassword.message}
              </div>
            )}
          </div>

          {/* Error Display */}
          {submitError && (
            <div className="form-error" role="alert">
              <span className="error-icon">❌</span>
              <span className="error-message">{submitError}</span>
              <button 
                type="button" 
                onClick={clearError}
                className="error-close"
                aria-label="Fechar erro"
              >
                ×
              </button>
            </div>
          )}

          {/* Form Actions */}
          <div className="form-actions">
            <button
              type="submit"
              disabled={!isValid || isSubmitting}
              className="btn btn-primary btn-full-width"
            >
              {isSubmitting ? (
                <>
                  <span className="loading-spinner" aria-hidden="true"></span>
                  Redefinindo...
                </>
              ) : (
                'Redefinir senha'
              )}
            </button>
          </div>
        </form>

        {/* Security note */}
        <div className="security-note">
          <p>
            🔒 Sua nova senha será criptografada e armazenada com segurança
          </p>
        </div>
      </div>
    </div>
  );
};

export default ResetPassword;
