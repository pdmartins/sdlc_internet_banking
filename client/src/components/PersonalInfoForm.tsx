import React from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate } from 'react-router-dom';
import { useRegistration } from '../contexts/RegistrationContext';

// Validation schema
const personalInfoSchema = yup.object({
  fullName: yup
    .string()
    .required('Nome completo é obrigatório')
    .min(2, 'Nome deve ter pelo menos 2 caracteres')
    .max(100, 'Nome não pode exceder 100 caracteres')
    .matches(/^[a-zA-ZÀ-ÿ\s]+$/, 'Nome deve conter apenas letras e espaços'),
  email: yup
    .string()
    .required('Email é obrigatório')
    .email('Formato de email inválido')
    .max(255, 'Email não pode exceder 255 caracteres'),
  phone: yup
    .string()
    .required('Telefone é obrigatório')
    .matches(
      /^\(\d{2}\)\s\d{4,5}-\d{4}$/,
      'Formato: (11) 99999-9999 ou (11) 9999-9999'
    ),
  dateOfBirth: yup
    .string()
    .required('Data de nascimento é obrigatória')
    .matches(
      /^\d{2}\/\d{2}\/\d{4}$/,
      'Formato: DD/MM/AAAA'
    )
    .test('age', 'Você deve ter pelo menos 18 anos', function(value) {
      if (!value) return false;
      const [day, month, year] = value.split('/').map(Number);
      const birthDate = new Date(year, month - 1, day);
      const today = new Date();
      const age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();
      
      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        return age - 1 >= 18;
      }
      return age >= 18;
    }),
  cpf: yup
    .string()
    .required('CPF é obrigatório')
    .matches(
      /^\d{3}\.\d{3}\.\d{3}-\d{2}$/,
      'Formato: 000.000.000-00'
    )
    .test('cpf', 'CPF inválido', function(value) {
      if (!value) return false;
      const cpf = value.replace(/\D/g, '');
      
      // Basic CPF validation
      if (cpf.length !== 11) return false;
      if (/^(\d)\1{10}$/.test(cpf)) return false; // All same digits
      
      // Calculate verification digits
      let sum = 0;
      for (let i = 0; i < 9; i++) {
        sum += parseInt(cpf.charAt(i)) * (10 - i);
      }
      let digit1 = 11 - (sum % 11);
      if (digit1 > 9) digit1 = 0;
      
      sum = 0;
      for (let i = 0; i < 10; i++) {
        sum += parseInt(cpf.charAt(i)) * (11 - i);
      }
      let digit2 = 11 - (sum % 11);
      if (digit2 > 9) digit2 = 0;
      
      return parseInt(cpf.charAt(9)) === digit1 && parseInt(cpf.charAt(10)) === digit2;
    }),
});

type PersonalInfoFormData = yup.InferType<typeof personalInfoSchema>;

interface PersonalInfoFormProps {
  onSubmit?: (data: PersonalInfoFormData) => void;
  onCancel?: () => void;
}

const PersonalInfoForm: React.FC<PersonalInfoFormProps> = ({ 
  onSubmit, 
  onCancel 
}) => {
  const navigate = useNavigate();
  const { registerUser, registrationData, clearError } = useRegistration();
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors, isValid, touchedFields },
    watch,
    setValue,
  } = useForm<PersonalInfoFormData>({
    resolver: yupResolver(personalInfoSchema),
    mode: 'onChange', // Real-time validation
  });

  // Watch all fields for real-time feedback
  const watchedFields = watch();

  // Phone number formatting
  const formatPhoneNumber = (value: string) => {
    // Remove all non-digits
    const digits = value.replace(/\D/g, '');
    
    // Apply formatting based on length
    if (digits.length <= 2) {
      return `(${digits}`;
    } else if (digits.length <= 6) {
      return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    } else if (digits.length <= 10) {
      return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
    } else {
      return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7, 11)}`;
    }
  };

  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatPhoneNumber(e.target.value);
    setValue('phone', formatted, { shouldValidate: true });
  };

  // Date formatting (DD/MM/YYYY)
  const formatDate = (value: string) => {
    const digits = value.replace(/\D/g, '');
    
    if (digits.length <= 2) {
      return digits;
    } else if (digits.length <= 4) {
      return `${digits.slice(0, 2)}/${digits.slice(2)}`;
    } else {
      return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4, 8)}`;
    }
  };

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatDate(e.target.value);
    setValue('dateOfBirth', formatted, { shouldValidate: true });
  };

  // CPF formatting (000.000.000-00)
  const formatCPF = (value: string) => {
    const digits = value.replace(/\D/g, '');
    
    if (digits.length <= 3) {
      return digits;
    } else if (digits.length <= 6) {
      return `${digits.slice(0, 3)}.${digits.slice(3)}`;
    } else if (digits.length <= 9) {
      return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`;
    } else {
      return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9, 11)}`;
    }
  };

  const handleCPFChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatCPF(e.target.value);
    setValue('cpf', formatted, { shouldValidate: true });
  };

  const handleFormSubmit = async (data: PersonalInfoFormData) => {
    if (isSubmitting) return;
    
    setIsSubmitting(true);
    clearError(); // Clear any previous errors

    try {
      // Call backend API to register user
      await registerUser(data);
      
      if (onSubmit) {
        onSubmit(data);
      } else {
        // Navigate to next step in registration (security setup)
        navigate('/register/security');
      }
    } catch (error) {
      console.error('Registration failed:', error);
      // Error is already set in the context by registerUser
      // Form will show the error message from registrationData.error
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

  const getFieldStatus = (fieldName: keyof PersonalInfoFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  return (
    <div className="personal-info-container">
      <div className="personal-info-card">
        {/* Header */}
        <div className="form-header">
          <h1 className="form-title">Informações Pessoais</h1>
          <p className="form-description">
            Precisamos de algumas informações básicas para criar sua conta
          </p>
          <div className="progress-indicator">
            <div className="progress-step completed">1</div>
            <div className="progress-line active"></div>
            <div className="progress-step active">2</div>
            <div className="progress-line"></div>
            <div className="progress-step">3</div>
            <div className="progress-line"></div>
            <div className="progress-step">4</div>
          </div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(handleFormSubmit)} className="personal-info-form">
          {/* Full Name Field */}
          <div className="form-group">
            <label htmlFor="fullName" className="form-label">
              Nome Completo *
            </label>
            <input
              {...register('fullName')}
              type="text"
              id="fullName"
              className={`form-input ${getFieldStatus('fullName')}`}
              placeholder="Digite seu nome completo"
              autoComplete="name"
              aria-describedby="fullName-error fullName-help"
            />
            {getFieldStatus('fullName') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                Nome válido
              </div>
            )}
            {errors.fullName && (
              <div className="field-feedback error" id="fullName-error">
                <span className="feedback-icon">❌</span>
                {errors.fullName.message}
              </div>
            )}
            <div className="field-help" id="fullName-help">
              Use seu nome completo como aparece nos documentos
            </div>
          </div>

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
              placeholder="seuemail@exemplo.com"
              autoComplete="email"
              aria-describedby="email-error email-help"
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
            <div className="field-help" id="email-help">
              Usaremos este email para comunicações importantes
            </div>
          </div>

          {/* Phone Field */}
          <div className="form-group">
            <label htmlFor="phone" className="form-label">
              Telefone *
            </label>
            <input
              {...register('phone')}
              type="tel"
              id="phone"
              className={`form-input ${getFieldStatus('phone')}`}
              placeholder="(11) 99999-9999"
              autoComplete="tel"
              onChange={handlePhoneChange}
              aria-describedby="phone-error phone-help"
            />
            {getFieldStatus('phone') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                Telefone válido
              </div>
            )}
            {errors.phone && (
              <div className="field-feedback error" id="phone-error">
                <span className="feedback-icon">❌</span>
                {errors.phone.message}
              </div>
            )}
            <div className="field-help" id="phone-help">
              Incluir código de área. Ex: (11) 99999-9999
            </div>
          </div>

          {/* Date of Birth Field */}
          <div className="form-group">
            <label htmlFor="dateOfBirth" className="form-label">
              Data de Nascimento *
            </label>
            <input
              {...register('dateOfBirth')}
              type="text"
              id="dateOfBirth"
              className={`form-input ${getFieldStatus('dateOfBirth')}`}
              placeholder="DD/MM/AAAA"
              onChange={handleDateChange}
              maxLength={10}
              aria-describedby="dateOfBirth-error dateOfBirth-help"
            />
            {getFieldStatus('dateOfBirth') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                Data válida
              </div>
            )}
            {errors.dateOfBirth && (
              <div className="field-feedback error" id="dateOfBirth-error">
                <span className="feedback-icon">❌</span>
                {errors.dateOfBirth.message}
              </div>
            )}
            <div className="field-help" id="dateOfBirth-help">
              Você deve ter pelo menos 18 anos para abrir uma conta
            </div>
          </div>

          {/* CPF Field */}
          <div className="form-group">
            <label htmlFor="cpf" className="form-label">
              CPF *
            </label>
            <input
              {...register('cpf')}
              type="text"
              id="cpf"
              className={`form-input ${getFieldStatus('cpf')}`}
              placeholder="000.000.000-00"
              onChange={handleCPFChange}
              maxLength={14}
              aria-describedby="cpf-error cpf-help"
            />
            {getFieldStatus('cpf') === 'success' && (
              <div className="field-feedback success">
                <span className="feedback-icon">✓</span>
                CPF válido
              </div>
            )}
            {errors.cpf && (
              <div className="field-feedback error" id="cpf-error">
                <span className="feedback-icon">❌</span>
                {errors.cpf.message}
              </div>
            )}
            <div className="field-help" id="cpf-help">
              Documento necessário para verificação de identidade
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
              aria-label="Cancelar cadastro e voltar"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={!isValid || isSubmitting || registrationData.isLoading}
              className="btn btn-primary"
              aria-label="Continuar para próxima etapa"
            >
              {isSubmitting || registrationData.isLoading ? (
                <>
                  <span className="loading-spinner" aria-hidden="true"></span>
                  Registrando...
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
            🔒 Suas informações são criptografadas e protegidas de acordo com a LGPD
          </p>
        </div>
      </div>
    </div>
  );
};

export default PersonalInfoForm;
