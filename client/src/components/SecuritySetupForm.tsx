import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate } from 'react-router-dom';
import { useRegistration } from '../contexts/RegistrationContext';

// Security questions options
const SECURITY_QUESTIONS = [
  { value: '', label: 'Selecione uma pergunta de segurança' },
  { value: 'first_pet', label: 'Qual era o nome do seu primeiro animal de estimação?' },
  { value: 'mother_maiden', label: 'Qual é o nome de solteira da sua mãe?' },
  { value: 'first_school', label: 'Qual foi a primeira escola que você frequentou?' },
  { value: 'childhood_friend', label: 'Qual é o nome do seu melhor amigo de infância?' },
  { value: 'birth_city', label: 'Em qual cidade você nasceu?' },
  { value: 'first_car', label: 'Qual foi o modelo do seu primeiro carro?' },
  { value: 'favorite_teacher', label: 'Qual era o nome do seu professor favorito?' },
  { value: 'street_grew_up', label: 'Em qual rua você cresceu?' },
];

// MFA options
const MFA_OPTIONS = [
  { value: 'sms', label: 'SMS (Mensagem de texto)', description: 'Receba códigos via SMS no seu celular' },
  { value: 'authenticator', label: 'Aplicativo Autenticador', description: 'Use Google Authenticator ou Microsoft Authenticator' },
];

// Password strength calculation
const calculatePasswordStrength = (password: string) => {
  let score = 0;
  const feedback = [];

  if (!password) return { score: 0, feedback: [], color: '#C8C8C8' };

  // Length check
  if (password.length >= 8) {
    score += 1;
  } else {
    feedback.push('Pelo menos 8 caracteres');
  }

  // Uppercase check
  if (/[A-Z]/.test(password)) {
    score += 1;
  } else {
    feedback.push('Pelo menos 1 letra maiúscula');
  }

  // Lowercase check
  if (/[a-z]/.test(password)) {
    score += 1;
  } else {
    feedback.push('Pelo menos 1 letra minúscula');
  }

  // Number check
  if (/\d/.test(password)) {
    score += 1;
  } else {
    feedback.push('Pelo menos 1 número');
  }

  // Special character check
  if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
    score += 1;
  } else {
    feedback.push('Pelo menos 1 caractere especial (!@#$%^&*)');
  }

  // Determine strength level and color
  let strength = '';
  let color = '';
  
  if (score === 0) {
    strength = 'Muito Fraca';
    color = '#D13438';
  } else if (score <= 2) {
    strength = 'Fraca';
    color = '#FF8C00';
  } else if (score <= 3) {
    strength = 'Média';
    color = '#FFD700';
  } else if (score <= 4) {
    strength = 'Forte';
    color = '#9ACD32';
  } else {
    strength = 'Muito Forte';
    color = '#107C10';
  }

  return { score, feedback, strength, color };
};

// Validation schema
const securitySetupSchema = yup.object({
  password: yup
    .string()
    .required('Senha é obrigatória')
    .min(8, 'Senha deve ter pelo menos 8 caracteres')
    .matches(/[A-Z]/, 'Senha deve conter pelo menos 1 letra maiúscula')
    .matches(/[a-z]/, 'Senha deve conter pelo menos 1 letra minúscula')
    .matches(/\d/, 'Senha deve conter pelo menos 1 número')
    .matches(/[!@#$%^&*(),.?":{}|<>]/, 'Senha deve conter pelo menos 1 caractere especial'),
  confirmPassword: yup
    .string()
    .required('Confirmação de senha é obrigatória')
    .oneOf([yup.ref('password')], 'Senhas não coincidem'),
  securityQuestion: yup
    .string()
    .required('Pergunta de segurança é obrigatória')
    .notOneOf([''], 'Selecione uma pergunta de segurança'),
  securityAnswer: yup
    .string()
    .required('Resposta de segurança é obrigatória')
    .min(2, 'Resposta deve ter pelo menos 2 caracteres')
    .max(100, 'Resposta não pode exceder 100 caracteres'),
  mfaOption: yup
    .string()
    .required('Selecione uma opção de autenticação multifator')
    .oneOf(['sms', 'authenticator'], 'Opção de MFA inválida'),
  termsAccepted: yup
    .boolean()
    .oneOf([true], 'Você deve aceitar os termos e condições'),
});

type SecuritySetupFormData = yup.InferType<typeof securitySetupSchema>;

interface SecuritySetupFormProps {
  onSubmit?: (data: SecuritySetupFormData) => void;
  onCancel?: () => void;
}

const SecuritySetupForm: React.FC<SecuritySetupFormProps> = ({ 
  onSubmit, 
  onCancel 
}) => {
  const navigate = useNavigate();
  const { setupSecurity, registrationData, clearError } = useRegistration();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors, isValid, touchedFields },
    watch,
  } = useForm<SecuritySetupFormData>({
    resolver: yupResolver(securitySetupSchema),
    mode: 'onChange',
  });

  const watchedFields = watch();
  const passwordStrength = calculatePasswordStrength(watchedFields.password || '');

  const getFieldStatus = (fieldName: keyof SecuritySetupFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  const handleFormSubmit = async (data: SecuritySetupFormData) => {
    if (isSubmitting) return;
    
    // Check if user is registered
    if (!registrationData.userId) {
      clearError();
      navigate('/register/personal-info');
      return;
    }
    
    setIsSubmitting(true);
    clearError(); // Clear any previous errors

    try {
      // Prepare security information for API call
      const securityInfo = {
        securityQuestion: data.securityQuestion,
        securityAnswer: data.securityAnswer,
        mfaOption: data.mfaOption as 'sms' | 'authenticator',
        passwordStrength: calculatePasswordStrength(data.password).score,
        password: data.password, // Include password for API call
      };
      
      // Call backend API to setup security
      await setupSecurity(securityInfo);
      
      if (onSubmit) {
        onSubmit(data);
      } else {
        // Navigate to confirmation page
        navigate('/register/confirmation');
      }
    } catch (error) {
      console.error('Security setup failed:', error);
      // Error is already set in the context by setupSecurity
      // Form will show the error message from registrationData.error
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    if (onCancel) {
      onCancel();
    } else {
      navigate('/register/personal-info');
    }
  };

  return (
    <div className="security-setup-container">
      <div className="security-setup-card">
        {/* Header */}
        <div className="form-header">
          <h1 className="form-title">Configuração de Segurança</h1>
          <p className="form-description">
            Configure sua senha e métodos de autenticação para proteger sua conta
          </p>
          <div className="progress-indicator">
            <div className="progress-step completed">1</div>
            <div className="progress-line completed"></div>
            <div className="progress-step completed">2</div>
            <div className="progress-line active"></div>
            <div className="progress-step active">3</div>
            <div className="progress-line"></div>
            <div className="progress-step">4</div>
          </div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(handleFormSubmit)} className="security-setup-form">
          {/* Password Section */}
          <div className="form-section">
            <h2 className="section-title">Criar Senha</h2>
            
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
                  placeholder="Digite uma senha forte"
                  autoComplete="new-password"
                  aria-describedby="password-error password-help password-strength"
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
              
              {/* Password Strength Meter */}
              {watchedFields.password && (
                <div className="password-strength" id="password-strength">
                  <div className="strength-meter">
                    <div 
                      className="strength-bar"
                      style={{ 
                        width: `${(passwordStrength.score / 5) * 100}%`,
                        backgroundColor: passwordStrength.color 
                      }}
                    ></div>
                  </div>
                  <div className="strength-text" style={{ color: passwordStrength.color }}>
                    Força da senha: {passwordStrength.strength}
                  </div>
                  {passwordStrength.feedback.length > 0 && (
                    <ul className="strength-feedback">
                      {passwordStrength.feedback.map((item, index) => (
                        <li key={index}>{item}</li>
                      ))}
                    </ul>
                  )}
                </div>
              )}
              
              {errors.password && (
                <div className="field-feedback error" id="password-error">
                  <span className="feedback-icon">❌</span>
                  {errors.password.message}
                </div>
              )}
              <div className="field-help" id="password-help">
                Use uma combinação de letras, números e símbolos
              </div>
            </div>

            {/* Confirm Password Field */}
            <div className="form-group">
              <label htmlFor="confirmPassword" className="form-label">
                Confirmar Senha *
              </label>
              <div className="password-input-container">
                <input
                  {...register('confirmPassword')}
                  type={showConfirmPassword ? 'text' : 'password'}
                  id="confirmPassword"
                  className={`form-input ${getFieldStatus('confirmPassword')}`}
                  placeholder="Digite a senha novamente"
                  autoComplete="new-password"
                  aria-describedby="confirmPassword-error"
                />
                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  aria-label={showConfirmPassword ? 'Ocultar confirmação' : 'Mostrar confirmação'}
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
          </div>

          {/* Security Question Section */}
          <div className="form-section">
            <h2 className="section-title">Pergunta de Segurança</h2>
            
            <div className="form-group">
              <label htmlFor="securityQuestion" className="form-label">
                Pergunta *
              </label>
              <select
                {...register('securityQuestion')}
                id="securityQuestion"
                className={`form-select ${getFieldStatus('securityQuestion')}`}
                aria-describedby="securityQuestion-error securityQuestion-help"
              >
                {SECURITY_QUESTIONS.map((question) => (
                  <option key={question.value} value={question.value}>
                    {question.label}
                  </option>
                ))}
              </select>
              {errors.securityQuestion && (
                <div className="field-feedback error" id="securityQuestion-error">
                  <span className="feedback-icon">❌</span>
                  {errors.securityQuestion.message}
                </div>
              )}
              <div className="field-help" id="securityQuestion-help">
                Escolha uma pergunta que só você saiba responder
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="securityAnswer" className="form-label">
                Resposta *
              </label>
              <input
                {...register('securityAnswer')}
                type="text"
                id="securityAnswer"
                className={`form-input ${getFieldStatus('securityAnswer')}`}
                placeholder="Digite sua resposta"
                autoComplete="off"
                aria-describedby="securityAnswer-error securityAnswer-help"
              />
              {getFieldStatus('securityAnswer') === 'success' && (
                <div className="field-feedback success">
                  <span className="feedback-icon">✓</span>
                  Resposta válida
                </div>
              )}
              {errors.securityAnswer && (
                <div className="field-feedback error" id="securityAnswer-error">
                  <span className="feedback-icon">❌</span>
                  {errors.securityAnswer.message}
                </div>
              )}
              <div className="field-help" id="securityAnswer-help">
                Sua resposta será usada para recuperação de conta
              </div>
            </div>
          </div>

          {/* MFA Section */}
          <div className="form-section">
            <h2 className="section-title">Autenticação Multifator (MFA)</h2>
            <p className="section-description">
              Adicione uma camada extra de segurança à sua conta
            </p>
            
            <div className="mfa-options">
              {MFA_OPTIONS.map((option) => (
                <div key={option.value} className="mfa-option">
                  <label className="mfa-label">
                    <input
                      {...register('mfaOption')}
                      type="radio"
                      value={option.value}
                      className="mfa-radio"
                    />
                    <div className="mfa-content">
                      <div className="mfa-title">{option.label}</div>
                      <div className="mfa-description">{option.description}</div>
                    </div>
                  </label>
                </div>
              ))}
            </div>
            {errors.mfaOption && (
              <div className="field-feedback error">
                <span className="feedback-icon">❌</span>
                {errors.mfaOption.message}
              </div>
            )}
          </div>

          {/* Terms and Conditions */}
          <div className="form-section">
            <div className="checkbox-group">
              <label className="checkbox-label">
                <input
                  {...register('termsAccepted')}
                  type="checkbox"
                  className="checkbox-input"
                />
                <span className="checkbox-custom"></span>
                <span className="checkbox-text">
                  Eu li e aceito os{' '}
                  <a href="/terms" target="_blank" rel="noopener noreferrer" className="link">
                    Termos e Condições
                  </a>{' '}
                  e a{' '}
                  <a href="/privacy" target="_blank" rel="noopener noreferrer" className="link">
                    Política de Privacidade
                  </a>
                </span>
              </label>
              {errors.termsAccepted && (
                <div className="field-feedback error">
                  <span className="feedback-icon">❌</span>
                  {errors.termsAccepted.message}
                </div>
              )}
            </div>
          </div>

          {/* Error Display */}
          {registrationData.error && (
            <div className="form-error" role="alert">
              <span className="error-icon">❌</span>
              <span className="error-message">{registrationData.error}</span>
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
              type="button"
              onClick={handleCancel}
              className="btn btn-secondary"
              disabled={isSubmitting}
              aria-label="Voltar para informações pessoais"
            >
              Voltar
            </button>
            <button
              type="submit"
              disabled={!isValid || isSubmitting || registrationData.isLoading}
              className="btn btn-primary"
              aria-label="Continuar para confirmação"
            >
              {isSubmitting || registrationData.isLoading ? (
                <>
                  <span className="loading-spinner" aria-hidden="true"></span>
                  Configurando...
                </>
              ) : (
                'Continuar'
              )}
            </button>
          </div>
        </form>

        {/* Security Note */}
        <div className="security-note">
          <p>
            🔒 Suas configurações de segurança são criptografadas e nunca compartilhadas
          </p>
        </div>
      </div>
    </div>
  );
};

export default SecuritySetupForm;
