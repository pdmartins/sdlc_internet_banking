import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { userProfileApi, UserProfile, UpdateProfileRequest } from '../services/userProfileApi';

// Validation schema
const profileSchema = yup.object({
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
    .matches(/^\(\d{2}\)\s\d{4,5}-\d{4}$/, 'Formato: (11) 99999-9999'),
  dateOfBirth: yup
    .string()
    .required('Data de nascimento é obrigatória')
    .matches(/^\d{2}\/\d{2}\/\d{4}$/, 'Formato: DD/MM/AAAA'),
  currentPassword: yup
    .string()
    .required('Senha atual é obrigatória para confirmar alterações')
    .min(8, 'Senha deve ter pelo menos 8 caracteres'),
});

type ProfileFormData = yup.InferType<typeof profileSchema>;

interface ProfileFormProps {
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
}

const ProfileForm: React.FC<ProfileFormProps> = ({ onSuccess, onError }) => {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, touchedFields },
    setValue,
    watch,
    reset,
  } = useForm<ProfileFormData>({
    resolver: yupResolver(profileSchema),
    mode: 'onChange',
  });

  const watchedFields = watch();

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    try {
      setIsLoading(true);
      const userProfile = await userProfileApi.getUserProfile();
      setProfile(userProfile);
      
      // Format date from YYYY-MM-DD to DD/MM/YYYY
      const formattedDate = formatDateToBrazilian(userProfile.dateOfBirth);
      
      // Populate form with current profile data
      reset({
        fullName: userProfile.fullName,
        email: userProfile.email,
        phone: userProfile.phone,
        dateOfBirth: formattedDate,
        currentPassword: '',
      });
    } catch (error) {
      console.error('Error loading profile:', error);
      onError('Erro ao carregar dados do perfil');
    } finally {
      setIsLoading(false);
    }
  };

  const formatDateToBrazilian = (isoDate: string): string => {
    const date = new Date(isoDate);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  };

  const formatDateToISO = (brazilianDate: string): string => {
    const [day, month, year] = brazilianDate.split('/');
    return `${year}-${month}-${day}`;
  };

  const formatPhone = (value: string): string => {
    const digits = value.replace(/\D/g, '');
    
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

  const formatDate = (value: string): string => {
    const digits = value.replace(/\D/g, '');
    
    if (digits.length <= 2) {
      return digits;
    } else if (digits.length <= 4) {
      return `${digits.slice(0, 2)}/${digits.slice(2)}`;
    } else {
      return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4, 8)}`;
    }
  };

  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatPhone(e.target.value);
    setValue('phone', formatted, { shouldValidate: true });
  };

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatDate(e.target.value);
    setValue('dateOfBirth', formatted, { shouldValidate: true });
  };

  const onSubmit = async (data: ProfileFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      const request: UpdateProfileRequest = {
        fullName: data.fullName.trim(),
        email: data.email.trim().toLowerCase(),
        phone: data.phone.trim(),
        dateOfBirth: formatDateToISO(data.dateOfBirth),
        currentPassword: data.currentPassword,
      };

      const updatedProfile = await userProfileApi.updateProfile(request);
      setProfile(updatedProfile);
      
      // Clear password field
      setValue('currentPassword', '');
      
      onSuccess('Perfil atualizado com sucesso!');
    } catch (error) {
      console.error('Error updating profile:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao atualizar perfil';
      onError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const getFieldStatus = (fieldName: keyof ProfileFormData) => {
    const isTouched = touchedFields[fieldName];
    const hasError = errors[fieldName];
    const hasValue = watchedFields[fieldName];

    if (!isTouched) return 'default';
    if (hasError) return 'error';
    if (hasValue && !hasError) return 'success';
    return 'default';
  };

  if (isLoading) {
    return (
      <div className="profile-form-loading">
        <div className="loading-spinner"></div>
        <p>Carregando dados do perfil...</p>
      </div>
    );
  }

  return (
    <div className="profile-form-container">
      <div className="profile-form-header">
        <h2 className="form-title">Informações Pessoais</h2>
        <p className="form-description">
          Mantenha suas informações atualizadas para garantir a segurança da sua conta
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="profile-form">
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
            {profile?.isEmailVerified ? (
              <span className="verified-badge">✓ Email verificado</span>
            ) : (
              <span className="unverified-badge">⚠️ Email não verificado</span>
            )}
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
            onChange={handlePhoneChange}
            maxLength={15}
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
            {profile?.isPhoneVerified ? (
              <span className="verified-badge">✓ Telefone verificado</span>
            ) : (
              <span className="unverified-badge">⚠️ Telefone não verificado</span>
            )}
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
            Esta informação não pode ser alterada após a criação da conta
          </div>
        </div>

        {/* CPF Field (Read-only) */}
        <div className="form-group">
          <label htmlFor="cpf" className="form-label">
            CPF
          </label>
          <input
            type="text"
            id="cpf"
            className="form-input readonly"
            value={profile?.cpf || ''}
            readOnly
            aria-describedby="cpf-help"
          />
          <div className="field-help" id="cpf-help">
            CPF não pode ser alterado após a criação da conta
          </div>
        </div>

        {/* Current Password Field */}
        <div className="form-group">
          <label htmlFor="currentPassword" className="form-label">
            Senha Atual *
          </label>
          <input
            {...register('currentPassword')}
            type="password"
            id="currentPassword"
            className={`form-input ${getFieldStatus('currentPassword')}`}
            placeholder="Digite sua senha atual para confirmar"
            aria-describedby="currentPassword-error currentPassword-help"
          />
          {errors.currentPassword && (
            <div className="field-feedback error" id="currentPassword-error">
              <span className="feedback-icon">❌</span>
              {errors.currentPassword.message}
            </div>
          )}
          <div className="field-help" id="currentPassword-help">
            Sua senha atual é necessária para confirmar as alterações
          </div>
        </div>

        {/* Submit Button */}
        <div className="form-actions">
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <>
                <span className="btn-spinner"></span>
                Atualizando...
              </>
            ) : (
              <>
                <span className="btn-icon">💾</span>
                Salvar Alterações
              </>
            )}
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={loadProfile}
            disabled={isSubmitting}
          >
            <span className="btn-icon">🔄</span>
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
};

export default ProfileForm;
