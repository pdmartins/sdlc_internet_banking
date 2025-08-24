import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { 
  userProfileApi, 
  SecuritySettings, 
  UpdatePasswordRequest, 
  UpdateSecurityQuestionRequest, 
  UpdateMfaRequest 
} from '../services/userProfileApi';

// Security questions
const SECURITY_QUESTIONS = [
  { value: 'mother_maiden_name', label: 'Qual é o nome de solteira da sua mãe?' },
  { value: 'first_pet', label: 'Qual era o nome do seu primeiro animal de estimação?' },
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

// Password schema
const passwordSchema = yup.object({
  currentPassword: yup
    .string()
    .required('Senha atual é obrigatória')
    .min(8, 'Senha deve ter pelo menos 8 caracteres'),
  newPassword: yup
    .string()
    .required('Nova senha é obrigatória')
    .min(8, 'Nova senha deve ter pelo menos 8 caracteres')
    .matches(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/, 
      'Nova senha deve conter pelo menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial'),
  confirmPassword: yup
    .string()
    .required('Confirmação de senha é obrigatória')
    .oneOf([yup.ref('newPassword')], 'Senhas não coincidem'),
});

// Security question schema
const securityQuestionSchema = yup.object({
  currentPassword: yup
    .string()
    .required('Senha atual é obrigatória')
    .min(8, 'Senha deve ter pelo menos 8 caracteres'),
  securityQuestion: yup
    .string()
    .required('Pergunta de segurança é obrigatória'),
  securityAnswer: yup
    .string()
    .required('Resposta de segurança é obrigatória')
    .min(3, 'Resposta deve ter pelo menos 3 caracteres')
    .max(500, 'Resposta não pode exceder 500 caracteres'),
});

// MFA schema
const mfaSchema = yup.object({
  currentPassword: yup
    .string()
    .required('Senha atual é obrigatória')
    .min(8, 'Senha deve ter pelo menos 8 caracteres'),
  mfaOption: yup
    .string()
    .required('Opção de MFA é obrigatória')
    .oneOf(['sms', 'authenticator'], 'Opção de MFA inválida'),
});

type PasswordFormData = yup.InferType<typeof passwordSchema>;
type SecurityQuestionFormData = yup.InferType<typeof securityQuestionSchema>;
type MfaFormData = yup.InferType<typeof mfaSchema>;

interface SecuritySettingsFormProps {
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
}

type SettingsSection = 'password' | 'security-question' | 'mfa';

const SecuritySettingsForm: React.FC<SecuritySettingsFormProps> = ({ onSuccess, onError }) => {
  const [settings, setSettings] = useState<SecuritySettings | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [activeSection, setActiveSection] = useState<SettingsSection | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form instances
  const passwordForm = useForm<PasswordFormData>({
    resolver: yupResolver(passwordSchema),
    mode: 'onChange',
  });

  const securityQuestionForm = useForm<SecurityQuestionFormData>({
    resolver: yupResolver(securityQuestionSchema),
    mode: 'onChange',
  });

  const mfaForm = useForm<MfaFormData>({
    resolver: yupResolver(mfaSchema),
    mode: 'onChange',
  });

  useEffect(() => {
    loadSecuritySettings();
  }, []);

  const loadSecuritySettings = async () => {
    try {
      setIsLoading(true);
      const securitySettings = await userProfileApi.getSecuritySettings();
      setSettings(securitySettings);
      
      // Pre-populate current MFA option
      mfaForm.setValue('mfaOption', securitySettings.mfaOption as 'sms' | 'authenticator');
    } catch (error) {
      console.error('Error loading security settings:', error);
      onError('Erro ao carregar configurações de segurança');
    } finally {
      setIsLoading(false);
    }
  };

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

    // Lowercase check
    if (/[a-z]/.test(password)) {
      score += 1;
    } else {
      feedback.push('Letra minúscula');
    }

    // Uppercase check
    if (/[A-Z]/.test(password)) {
      score += 1;
    } else {
      feedback.push('Letra maiúscula');
    }

    // Number check
    if (/\d/.test(password)) {
      score += 1;
    } else {
      feedback.push('Número');
    }

    // Special character check
    if (/[@$!%*?&]/.test(password)) {
      score += 1;
    } else {
      feedback.push('Caractere especial (@$!%*?&)');
    }

    // Determine color and text
    let color = '#C8C8C8';
    let strengthText = 'Muito fraca';

    if (score >= 4) {
      color = '#0078D4';
      strengthText = 'Forte';
    } else if (score >= 3) {
      color = '#F7A800';
      strengthText = 'Média';
    } else if (score >= 2) {
      color = '#FF6B35';
      strengthText = 'Fraca';
    }

    return { score, feedback, color, strengthText };
  };

  const handlePasswordSubmit = async (data: PasswordFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      const request: UpdatePasswordRequest = {
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      };

      await userProfileApi.updatePassword(request);
      passwordForm.reset();
      setActiveSection(null);
      onSuccess('Senha atualizada com sucesso!');
    } catch (error) {
      console.error('Error updating password:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao atualizar senha';
      onError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSecurityQuestionSubmit = async (data: SecurityQuestionFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      const request: UpdateSecurityQuestionRequest = {
        currentPassword: data.currentPassword,
        securityQuestion: data.securityQuestion,
        securityAnswer: data.securityAnswer,
      };

      await userProfileApi.updateSecurityQuestion(request);
      securityQuestionForm.reset();
      setActiveSection(null);
      await loadSecuritySettings(); // Reload to get updated question
      onSuccess('Pergunta de segurança atualizada com sucesso!');
    } catch (error) {
      console.error('Error updating security question:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao atualizar pergunta de segurança';
      onError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleMfaSubmit = async (data: MfaFormData) => {
    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      const request: UpdateMfaRequest = {
        currentPassword: data.currentPassword,
        mfaOption: data.mfaOption as 'sms' | 'authenticator',
      };

      await userProfileApi.updateMfaOption(request);
      mfaForm.reset();
      setActiveSection(null);
      await loadSecuritySettings(); // Reload to get updated MFA option
      onSuccess('Opção de MFA atualizada com sucesso!');
    } catch (error) {
      console.error('Error updating MFA option:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao atualizar MFA';
      onError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const cancelEdit = (section: SettingsSection) => {
    setActiveSection(null);
    switch (section) {
      case 'password':
        passwordForm.reset();
        break;
      case 'security-question':
        securityQuestionForm.reset();
        break;
      case 'mfa':
        mfaForm.reset();
        if (settings) {
          mfaForm.setValue('mfaOption', settings.mfaOption as 'sms' | 'authenticator');
        }
        break;
    }
  };

  const getPasswordStrengthBar = () => {
    if (!settings) return null;
    
    const strength = settings.passwordStrength;
    const percentage = (strength / 5) * 100;
    
    let color = '#C8C8C8';
    if (strength >= 4) color = '#0078D4';
    else if (strength >= 3) color = '#F7A800';
    else if (strength >= 2) color = '#FF6B35';
    
    return (
      <div className="password-strength-indicator">
        <div className="strength-bar">
          <div 
            className="strength-fill" 
            style={{ width: `${percentage}%`, backgroundColor: color }}
          ></div>
        </div>
        <span className="strength-text" style={{ color }}>
          Força: {strength >= 4 ? 'Forte' : strength >= 3 ? 'Média' : strength >= 2 ? 'Fraca' : 'Muito fraca'}
        </span>
      </div>
    );
  };

  if (isLoading) {
    return (
      <div className="security-settings-loading">
        <div className="loading-spinner"></div>
        <p>Carregando configurações de segurança...</p>
      </div>
    );
  }

  return (
    <div className="security-settings-container">
      <div className="security-settings-header">
        <h2 className="form-title">Configurações de Segurança</h2>
        <p className="form-description">
          Mantenha sua conta segura configurando senha forte e autenticação em duas etapas
        </p>
      </div>

      <div className="security-settings-grid">
        {/* Password Section */}
        <div className="security-card">
          <div className="security-card-header">
            <div className="security-card-icon">🔑</div>
            <div className="security-card-info">
              <h3>Senha</h3>
              <p>Altere sua senha regularmente para manter a conta segura</p>
              {getPasswordStrengthBar()}
            </div>
            {activeSection !== 'password' && (
              <button
                className="btn btn-outline"
                onClick={() => setActiveSection('password')}
              >
                Alterar
              </button>
            )}
          </div>

          {activeSection === 'password' && (
            <form onSubmit={passwordForm.handleSubmit(handlePasswordSubmit)} className="security-form">
              <div className="form-group">
                <label htmlFor="currentPassword" className="form-label">
                  Senha Atual *
                </label>
                <input
                  {...passwordForm.register('currentPassword')}
                  type="password"
                  id="currentPassword"
                  className="form-input"
                  placeholder="Digite sua senha atual"
                />
                {passwordForm.formState.errors.currentPassword && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {passwordForm.formState.errors.currentPassword.message}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="newPassword" className="form-label">
                  Nova Senha *
                </label>
                <input
                  {...passwordForm.register('newPassword')}
                  type="password"
                  id="newPassword"
                  className="form-input"
                  placeholder="Digite sua nova senha"
                />
                {passwordForm.watch('newPassword') && (
                  <div className="password-strength">
                    {(() => {
                      const strength = calculatePasswordStrength(passwordForm.watch('newPassword'));
                      return (
                        <div>
                          <div className="strength-bar">
                            <div 
                              className="strength-fill" 
                              style={{ 
                                width: `${(strength.score / 5) * 100}%`, 
                                backgroundColor: strength.color 
                              }}
                            ></div>
                          </div>
                          <span style={{ color: strength.color }}>{strength.strengthText}</span>
                          {strength.feedback.length > 0 && (
                            <div className="strength-feedback">
                              Adicione: {strength.feedback.join(', ')}
                            </div>
                          )}
                        </div>
                      );
                    })()}
                  </div>
                )}
                {passwordForm.formState.errors.newPassword && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {passwordForm.formState.errors.newPassword.message}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="confirmPassword" className="form-label">
                  Confirmar Nova Senha *
                </label>
                <input
                  {...passwordForm.register('confirmPassword')}
                  type="password"
                  id="confirmPassword"
                  className="form-input"
                  placeholder="Confirme sua nova senha"
                />
                {passwordForm.formState.errors.confirmPassword && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {passwordForm.formState.errors.confirmPassword.message}
                  </div>
                )}
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Atualizando...' : 'Salvar Nova Senha'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => cancelEdit('password')}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}
        </div>

        {/* Security Question Section */}
        <div className="security-card">
          <div className="security-card-header">
            <div className="security-card-icon">❓</div>
            <div className="security-card-info">
              <h3>Pergunta de Segurança</h3>
              <p>
                {settings?.securityQuestion || 'Nenhuma pergunta configurada'}
              </p>
            </div>
            {activeSection !== 'security-question' && (
              <button
                className="btn btn-outline"
                onClick={() => setActiveSection('security-question')}
              >
                {settings?.securityQuestion ? 'Alterar' : 'Configurar'}
              </button>
            )}
          </div>

          {activeSection === 'security-question' && (
            <form onSubmit={securityQuestionForm.handleSubmit(handleSecurityQuestionSubmit)} className="security-form">
              <div className="form-group">
                <label htmlFor="securityQuestionPassword" className="form-label">
                  Senha Atual *
                </label>
                <input
                  {...securityQuestionForm.register('currentPassword')}
                  type="password"
                  id="securityQuestionPassword"
                  className="form-input"
                  placeholder="Digite sua senha atual"
                />
                {securityQuestionForm.formState.errors.currentPassword && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {securityQuestionForm.formState.errors.currentPassword.message}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="securityQuestion" className="form-label">
                  Pergunta de Segurança *
                </label>
                <select
                  {...securityQuestionForm.register('securityQuestion')}
                  id="securityQuestion"
                  className="form-select"
                >
                  <option value="">Selecione uma pergunta</option>
                  {SECURITY_QUESTIONS.map((question) => (
                    <option key={question.value} value={question.value}>
                      {question.label}
                    </option>
                  ))}
                </select>
                {securityQuestionForm.formState.errors.securityQuestion && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {securityQuestionForm.formState.errors.securityQuestion.message}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="securityAnswer" className="form-label">
                  Resposta *
                </label>
                <input
                  {...securityQuestionForm.register('securityAnswer')}
                  type="text"
                  id="securityAnswer"
                  className="form-input"
                  placeholder="Digite sua resposta"
                />
                {securityQuestionForm.formState.errors.securityAnswer && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {securityQuestionForm.formState.errors.securityAnswer.message}
                  </div>
                )}
                <div className="field-help">
                  Escolha uma resposta que você sempre lembrará
                </div>
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Salvando...' : 'Salvar Pergunta'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => cancelEdit('security-question')}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}
        </div>

        {/* MFA Section */}
        <div className="security-card">
          <div className="security-card-header">
            <div className="security-card-icon">📱</div>
            <div className="security-card-info">
              <h3>Autenticação em Duas Etapas (MFA)</h3>
              <p>
                {settings?.mfaOption === 'sms' ? 'SMS configurado' : 
                 settings?.mfaOption === 'authenticator' ? 'Aplicativo autenticador configurado' : 
                 'Não configurado'}
              </p>
            </div>
            {activeSection !== 'mfa' && (
              <button
                className="btn btn-outline"
                onClick={() => setActiveSection('mfa')}
              >
                Alterar
              </button>
            )}
          </div>

          {activeSection === 'mfa' && (
            <form onSubmit={mfaForm.handleSubmit(handleMfaSubmit)} className="security-form">
              <div className="form-group">
                <label htmlFor="mfaPassword" className="form-label">
                  Senha Atual *
                </label>
                <input
                  {...mfaForm.register('currentPassword')}
                  type="password"
                  id="mfaPassword"
                  className="form-input"
                  placeholder="Digite sua senha atual"
                />
                {mfaForm.formState.errors.currentPassword && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {mfaForm.formState.errors.currentPassword.message}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label className="form-label">Método de MFA *</label>
                <div className="mfa-options">
                  {MFA_OPTIONS.map((option) => (
                    <label key={option.value} className="mfa-option">
                      <input
                        {...mfaForm.register('mfaOption')}
                        type="radio"
                        value={option.value}
                        className="mfa-radio"
                      />
                      <div className="mfa-option-content">
                        <h4>{option.label}</h4>
                        <p>{option.description}</p>
                      </div>
                    </label>
                  ))}
                </div>
                {mfaForm.formState.errors.mfaOption && (
                  <div className="field-feedback error">
                    <span className="feedback-icon">❌</span>
                    {mfaForm.formState.errors.mfaOption.message}
                  </div>
                )}
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Salvando...' : 'Salvar MFA'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => cancelEdit('mfa')}
                  disabled={isSubmitting}
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}
        </div>
      </div>

      {/* Security Statistics */}
      <div className="security-stats">
        <div className="security-stat-item">
          <span className="stat-label">Tentativas de login falharam:</span>
          <span className="stat-value">{settings?.failedLoginAttempts || 0}</span>
        </div>
        {settings?.lastFailedLoginAt && (
          <div className="security-stat-item">
            <span className="stat-label">Última tentativa falha:</span>
            <span className="stat-value">
              {new Date(settings.lastFailedLoginAt).toLocaleString('pt-BR')}
            </span>
          </div>
        )}
        {settings?.lastPasswordChange && (
          <div className="security-stat-item">
            <span className="stat-label">Última alteração de senha:</span>
            <span className="stat-value">
              {new Date(settings.lastPasswordChange).toLocaleString('pt-BR')}
            </span>
          </div>
        )}
      </div>
    </div>
  );
};

export default SecuritySettingsForm;
