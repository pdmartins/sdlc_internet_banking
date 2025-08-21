import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useRegistration, maskEmail, maskPhone, maskAccountNumber, maskCPF, formatCurrency } from '../contexts/RegistrationContext';

interface TutorialStep {
  id: number;
  title: string;
  content: string;
  icon: string;
  type: 'navigation' | 'security' | 'feature';
}

const TUTORIAL_STEPS: TutorialStep[] = [
  {
    id: 1,
    title: 'Navegação Segura',
    content: 'Use sempre o menu principal para navegar. Nunca digite URLs manualmente ou clique em links externos para acessar sua conta.',
    icon: '🧭',
    type: 'navigation'
  },
  {
    id: 2,
    title: 'Verificação de Sessão',
    content: 'Sempre faça logout ao terminar. Sua sessão expira automaticamente após 15 minutos de inatividade por segurança.',
    icon: '🔒',
    type: 'security'
  },
  {
    id: 3,
    title: 'Monitoramento de Conta',
    content: 'Verifique regularmente seu extrato e configure alertas para transações. Reporte atividades suspeitas imediatamente.',
    icon: '👁️',
    type: 'security'
  },
  {
    id: 4,
    title: 'Central de Ajuda',
    content: 'Acesse a Central de Ajuda no menu para dúvidas, tutoriais detalhados e contato com suporte técnico.',
    icon: '💬',
    type: 'feature'
  },
  {
    id: 5,
    title: 'Configurações de Segurança',
    content: 'Revise suas configurações de segurança regularmente. Você pode alterar sua senha e métodos de MFA a qualquer momento.',
    icon: '⚙️',
    type: 'security'
  }
];

export const ConfirmationTutorial: React.FC = () => {
  const [currentStep, setCurrentStep] = useState(0);
  const [showAccountSummary, setShowAccountSummary] = useState(true);
  const navigate = useNavigate();
  const { registrationData } = useRegistration();

  // Extract user data from registration context
  const userData = {
    name: registrationData.personalInfo?.fullName || 'Usuário',
    email: registrationData.personalInfo?.email || '',
    phone: registrationData.personalInfo?.phone || '',
    dateOfBirth: registrationData.personalInfo?.dateOfBirth || '',
    cpf: registrationData.personalInfo?.cpf || '',
    accountNumber: registrationData.accountInfo?.accountNumber || '',
    branchCode: registrationData.accountInfo?.branchCode || '',
    accountType: registrationData.accountInfo?.accountType || '',
    balance: registrationData.accountInfo?.balance || 0,
    dailyLimit: registrationData.accountInfo?.dailyLimit || 0,
    monthlyLimit: registrationData.accountInfo?.monthlyLimit || 0,
    mfaOption: registrationData.securityInfo?.mfaOption || 'sms',
    passwordStrength: registrationData.securityInfo?.passwordStrength || 0,
    createdAt: registrationData.registrationDate?.toLocaleDateString('pt-BR') || new Date().toLocaleDateString('pt-BR')
  };

  useEffect(() => {
    // Auto-advance tutorial steps every 5 seconds if not manually controlled
    const timer = setTimeout(() => {
      if (currentStep < TUTORIAL_STEPS.length - 1) {
        setCurrentStep(currentStep + 1);
      }
    }, 5000);

    return () => clearTimeout(timer);
  }, [currentStep]);

  const handleNextStep = () => {
    if (currentStep < TUTORIAL_STEPS.length - 1) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handlePrevStep = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleSkipTutorial = () => {
    navigate('/dashboard');
  };

  const handleFinishTutorial = () => {
    navigate('/dashboard');
  };

  const currentTutorialStep = TUTORIAL_STEPS[currentStep];

  return (
    <div className="confirmation-tutorial-container">
      {/* Success Alert */}
      <div className="success-alert">
        <div className="success-icon">✅</div>
        <div className="success-content">
          <h2>Conta criada com sucesso!</h2>
          <p>Bem-vindo(a) ao Contoso Bank, {userData.name.split(' ')[0]}! Sua conta foi configurada e está pronta para uso.</p>
        </div>
      </div>

      {/* Account Summary */}
      {showAccountSummary && (
        <div className="account-summary-card">
          <div className="card-header">
            <h3>Resumo da Conta</h3>
            <button 
              className="close-button"
              onClick={() => setShowAccountSummary(false)}
              aria-label="Fechar resumo da conta"
            >
              ×
            </button>
          </div>
          <div className="account-details">
            <div className="detail-row">
              <span className="label">Nome:</span>
              <span className="value">{userData.name}</span>
            </div>
            <div className="detail-row">
              <span className="label">E-mail:</span>
              <span className="value">{maskEmail(userData.email)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Telefone:</span>
              <span className="value">{maskPhone(userData.phone)}</span>
            </div>
            <div className="detail-row">
              <span className="label">CPF:</span>
              <span className="value">{maskCPF(userData.cpf)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Conta:</span>
              <span className="value">{maskAccountNumber(userData.accountNumber)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Agência:</span>
              <span className="value">{userData.branchCode}</span>
            </div>
            <div className="detail-row">
              <span className="label">Tipo de Conta:</span>
              <span className="value">{userData.accountType}</span>
            </div>
            <div className="detail-row">
              <span className="label">Saldo Inicial:</span>
              <span className="value">{formatCurrency(userData.balance)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Limite Diário:</span>
              <span className="value">{formatCurrency(userData.dailyLimit)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Limite Mensal:</span>
              <span className="value">{formatCurrency(userData.monthlyLimit)}</span>
            </div>
            <div className="detail-row">
              <span className="label">Data de Criação:</span>
              <span className="value">{userData.createdAt}</span>
            </div>
            <div className="detail-row">
              <span className="label">MFA Configurado:</span>
              <span className="value">
                {userData.mfaOption === 'sms' ? '📱 SMS' : '🔐 Authenticator'}
              </span>
            </div>
            <div className="detail-row">
              <span className="label">Força da Senha:</span>
              <span className="value">
                {'★'.repeat(userData.passwordStrength)} {'☆'.repeat(5 - userData.passwordStrength)}
              </span>
            </div>
          </div>
          <div className="security-notice">
            <span className="security-icon">🔐</span>
            <span>Seus dados estão protegidos com criptografia de nível bancário</span>
          </div>
        </div>
      )}

      {/* Tutorial Card */}
      <div className="tutorial-card">
        <div className="tutorial-header">
          <h3>Tutorial de Segurança</h3>
          <div className="tutorial-progress">
            <span>{currentStep + 1} de {TUTORIAL_STEPS.length}</span>
            <div className="progress-bar">
              <div 
                className="progress-fill"
                style={{ width: `${((currentStep + 1) / TUTORIAL_STEPS.length) * 100}%` }}
              />
            </div>
          </div>
        </div>

        <div className="tutorial-content">
          <div className="tutorial-step">
            <div className="step-icon">{currentTutorialStep.icon}</div>
            <div className="step-content">
              <h4>{currentTutorialStep.title}</h4>
              <p>{currentTutorialStep.content}</p>
            </div>
            <div className={`step-type step-type-${currentTutorialStep.type}`}>
              {currentTutorialStep.type === 'navigation' && 'Navegação'}
              {currentTutorialStep.type === 'security' && 'Segurança'}
              {currentTutorialStep.type === 'feature' && 'Funcionalidade'}
            </div>
          </div>
        </div>

        <div className="tutorial-controls">
          <button
            className="skip-button"
            onClick={handleSkipTutorial}
          >
            Pular Tutorial
          </button>

          <div className="navigation-buttons">
            <button
              className="prev-button"
              onClick={handlePrevStep}
              disabled={currentStep === 0}
            >
              Anterior
            </button>
            
            {currentStep < TUTORIAL_STEPS.length - 1 ? (
              <button
                className="next-button"
                onClick={handleNextStep}
              >
                Próximo
              </button>
            ) : (
              <button
                className="finish-button"
                onClick={handleFinishTutorial}
              >
                Finalizar Tutorial
              </button>
            )}
          </div>
        </div>

        {/* Tutorial Indicators */}
        <div className="tutorial-indicators">
          {TUTORIAL_STEPS.map((_, index) => (
            <button
              key={index}
              className={`indicator ${index === currentStep ? 'active' : ''} ${index < currentStep ? 'completed' : ''}`}
              onClick={() => setCurrentStep(index)}
              aria-label={`Ir para passo ${index + 1}`}
            />
          ))}
        </div>
      </div>

      {/* Additional Security Tips */}
      <div className="security-tips">
        <h4>Lembre-se sempre:</h4>
        <ul>
          <li>🔒 Nunca compartilhe suas credenciais com terceiros</li>
          <li>📱 Use sempre o app oficial ou site seguro do Contoso Bank</li>
          <li>🚨 Reporte imediatamente qualquer atividade suspeita</li>
          <li>⚡ Mantenha seu app sempre atualizado</li>
        </ul>
      </div>
    </div>
  );
};

export default ConfirmationTutorial;
