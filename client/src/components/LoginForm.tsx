import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate, Link } from 'react-router-dom';
import { API_ENDPOINTS } from '../config/api';

// Validation schema for login form
const loginSchema = yup.object({
  email: yup
    .string()
    .required('Email é obrigatório')
    .email('Formato de email inválido')
    .max(255, 'Email não pode exceder 255 caracteres'),
  password: yup
    .string()
    .required('Senha é obrigatória')
    .min(1, 'Senha não pode estar vazia'),
  rememberDevice: yup
    .boolean()
    .optional(),
});

type LoginFormData = yup.InferType<typeof loginSchema>;

interface LoginFormProps {
  onSubmit?: (data: LoginFormData) => void;
  onCancel?: () => void;
}

const LoginForm: React.FC<LoginFormProps> = ({ onSubmit, onCancel }) => {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [loginError, setLoginError] = useState<string>('');
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isValid, touchedFields },
    watch,
  } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
    mode: 'onChange',
    defaultValues: {
      email: '',
      password: '',
      rememberDevice: false,
    },
  });

  const watchedFields = watch();

  const getFieldStatus = (fieldName: keyof LoginFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  const handleFormSubmit = async (data: LoginFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    setLoginError('');

    try {
      // Call login API
      const response = await fetch(API_ENDPOINTS.LOGIN, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });

      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.message || 'Erro durante o login');
      }

      // Check if MFA is required
      if (result.requiresMfa) {
        // Navigate to MFA verification
        navigate('/login/mfa', { 
          state: { 
            userId: result.userId, 
            email: result.email,
            mfaMethod: result.mfaMethod 
          } 
        });
      } else {
        // Store user session data (simplified - should use proper session management)
        localStorage.setItem('user', JSON.stringify({
          userId: result.userId,
          email: result.email,
          fullName: result.fullName,
          token: result.token,
          tokenExpiresAt: result.tokenExpiresAt,
        }));

        // Navigate to dashboard
        navigate('/dashboard');
      }

      if (onSubmit) {
        onSubmit(data);
      }
    } catch (error) {
      console.error('Login failed:', error);
      setLoginError(error instanceof Error ? error.message : 'Erro inesperado durante o login');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    if (onCancel) {
      onCancel();
    } else {
      navigate('/welcome');
    }
  };

  const clearError = () => {
    setLoginError('');
  };

  return (
    <div className="login-container">
      <div className="login-card">
        {/* Header */}
        <div className="form-header">
          {/* Contoso Logo */}
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

          <h1 className="form-title">Acessar sua conta</h1>
          <p className="form-description">
            Entre com seu email e senha para acessar sua conta
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(handleFormSubmit)} className="login-form">
          {/* Email Field */}
          <div className="form-group">
            <label htmlFor="email" className="form-label">
              Email *
            </label>
            <input
              {...register('email')}
              type="email"
              id="email"
              className={`form-input ${getFieldStatus('email')}`}
              placeholder="seu.email@exemplo.com"
              autoComplete="email"
              aria-describedby="email-error"
            />
            {getFieldStatus('email') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                Email válido
              </div>
            )}
            {errors.email && (
              <div className="field-feedback error" id="email-error">
                <span className="feedback-icon">❌</span>
                {errors.email.message}
              </div>
            )}
          </div>

          {/* Password Field */}
          <div className="form-group">
            <label htmlFor="password" className="form-label">
              Senha *
            </label>
            <div className="password-input-container">
              <input
                {...register('password')}
                type={showPassword ? 'text' : 'password'}
                id="password"
                className={`form-input ${getFieldStatus('password')}`}
                placeholder="Digite sua senha"
                autoComplete="current-password"
                aria-describedby="password-error"
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
            {errors.password && (
              <div className="field-feedback error" id="password-error">
                <span className="feedback-icon">❌</span>
                {errors.password.message}
              </div>
            )}
          </div>

          {/* Remember Device Checkbox */}
          <div className="form-group">
            <div className="checkbox-group">
              <label className="checkbox-label">
                <input
                  {...register('rememberDevice')}
                  type="checkbox"
                  className="checkbox-input"
                />
                <span className="checkbox-custom"></span>
                <span className="checkbox-text">
                  Lembrar dispositivo
                </span>
              </label>
            </div>
            <div className="field-help">
              Não marque esta opção em computadores compartilhados
            </div>
          </div>

          {/* Error Display */}
          {loginError && (
            <div className="form-error" role="alert">
              <span className="error-icon">❌</span>
              <span className="error-message">{loginError}</span>
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
              aria-label="Entrar na conta"
            >
              {isSubmitting ? (
                <>
                  <span className="loading-spinner" aria-hidden="true"></span>
                  Entrando...
                </>
              ) : (
                'Continuar'
              )}
            </button>
          </div>

          {/* Additional Links */}
          <div className="form-links">
            <Link 
              to="/forgot-password" 
              className="link"
              aria-label="Esqueci minha senha"
            >
              Esqueci minha senha
            </Link>
          </div>
        </form>

        {/* Registration Link */}
        <div className="form-footer">
          <p>
            Não tem uma conta?{' '}
            <Link to="/welcome" className="link">
              Criar conta
            </Link>
          </p>
        </div>

        {/* Security Note */}
        <div className="security-note">
          <p>
            🔒 Seus dados são protegidos com criptografia de nível bancário
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginForm;
