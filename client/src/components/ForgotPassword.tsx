import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { Link, useNavigate } from 'react-router-dom';
import { API_ENDPOINTS } from '../config/api';

// Validation schema for forgot password form
const forgotPasswordSchema = yup.object({
  email: yup
    .string()
    .required('Email é obrigatório')
    .email('Formato de email inválido')
    .max(255, 'Email não pode exceder 255 caracteres'),
});

type ForgotPasswordFormData = yup.InferType<typeof forgotPasswordSchema>;

const ForgotPassword: React.FC = () => {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string>('');
  const [isSuccess, setIsSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isValid, touchedFields },
    watch,
  } = useForm<ForgotPasswordFormData>({
    resolver: yupResolver(forgotPasswordSchema),
    mode: 'onChange',
    defaultValues: {
      email: '',
    },
  });

  const watchedFields = watch();

  const getFieldStatus = (fieldName: keyof ForgotPasswordFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  const handleFormSubmit = async (data: ForgotPasswordFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    setSubmitError('');

    try {
      const response = await fetch(API_ENDPOINTS.PASSWORD_RESET_REQUEST, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });

      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.message || 'Erro ao solicitar redefinição de senha');
      }

      setIsSuccess(true);
    } catch (error) {
      console.error('Password reset request failed:', error);
      setSubmitError(error instanceof Error ? error.message : 'Erro inesperado ao solicitar redefinição');
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

  if (isSuccess) {
    return (
      <div className="forgot-password-container">
        <div className="forgot-password-card">
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

            <h1 className="form-title">Email enviado!</h1>
            <p className="form-description">
              Se o email existir em nosso sistema, você receberá instruções para redefinir sua senha.
            </p>
          </div>

          {/* Success message */}
          <div className="success-content">
            <div className="success-icon" style={{ textAlign: 'center', fontSize: '3rem', marginBottom: '1rem' }}>
              ✅
            </div>
            <div className="success-instructions">
              <h3>Próximos passos:</h3>
              <ol>
                <li>Verifique sua caixa de entrada de email</li>
                <li>Clique no link de redefinição (válido por 30 minutos)</li>
                <li>Responda à pergunta de segurança</li>
                <li>Crie uma nova senha</li>
              </ol>
            </div>
          </div>

          {/* Actions */}
          <div className="form-actions">
            <button
              onClick={handleBackToLogin}
              className="btn btn-primary btn-full-width"
              type="button"
            >
              Voltar ao Login
            </button>
          </div>

          {/* Security note */}
          <div className="security-note">
            <p>
              🔒 Por segurança, não informamos se o email existe em nosso sistema
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="forgot-password-container">
      <div className="forgot-password-card">
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

          <h1 className="form-title">Esqueci minha senha</h1>
          <p className="form-description">
            Digite seu email para receber instruções de redefinição de senha
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(handleFormSubmit)} className="forgot-password-form">
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
              style={{ marginBottom: '1rem' }}
            >
              {isSubmitting ? (
                <>
                  <span className="loading-spinner" aria-hidden="true"></span>
                  Enviando...
                </>
              ) : (
                'Enviar instruções'
              )}
            </button>

            <button
              type="button"
              onClick={handleBackToLogin}
              className="btn btn-secondary btn-full-width"
            >
              Voltar ao login
            </button>
          </div>
        </form>

        {/* Security note */}
        <div className="security-note">
          <p>
            🔒 Você receberá um email com um link seguro para redefinir sua senha
          </p>
        </div>
      </div>
    </div>
  );
};

export default ForgotPassword;
