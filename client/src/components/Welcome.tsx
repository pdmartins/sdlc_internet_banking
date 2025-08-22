import React from 'react';
import { useNavigate } from 'react-router-dom';

interface WelcomeProps {
  onContinue?: () => void;
}

const Welcome: React.FC<WelcomeProps> = ({ onContinue }) => {
  const navigate = useNavigate();

  const handleContinue = () => {
    if (onContinue) {
      onContinue();
    } else {
      // Navigate to registration form
      navigate('/register/personal-info');
    }
  };

  return (
    <div className="welcome-container">
      <div className="welcome-card">
        {/* Contoso Logo */}
        <div className="welcome-logo-container">
          <svg 
            className="welcome-logo" 
            viewBox="0 0 120 40" 
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
              fontFamily="Segoe UI, sans-serif"
            >
              CONTOSO
            </text>
          </svg>
        </div>

        {/* Welcome Title */}
        <h1 className="welcome-title">
          Bem-vindo ao Contoso Bank
        </h1>

        {/* Description */}
        <p className="welcome-description">
          Gerencie suas finanças com segurança e simplicidade. 
          Nossa plataforma oferece a melhor experiência em banking digital 
          com tecnologia de ponta e proteção total dos seus dados.
        </p>

        {/* Features List */}
        <ul className="welcome-features">
          <li>Transações seguras com autenticação multifator</li>
          <li>Histórico completo e relatórios detalhados</li>
          <li>Acesso 24/7 em todos os dispositivos</li>
          <li>Suporte especializado quando precisar</li>
        </ul>

        {/* Action Buttons */}
        <div className="welcome-actions">
          <button 
            className="btn btn-primary"
            onClick={handleContinue}
            aria-label="Continuar para o cadastro"
          >
            Criar conta
          </button>
        </div>

        {/* Login Link */}
        <div className="welcome-login-link" style={{ textAlign: 'center', marginTop: '1rem' }}>
          <p>
            Já tem uma conta?{' '}
            <a 
              href="/login" 
              className="link"
              style={{ color: 'var(--primary-blue)', textDecoration: 'none', fontWeight: '600' }}
            >
              Fazer login
            </a>
          </p>
        </div>

        {/* Security Information */}
        <div className="security-info" style={{ marginTop: '1.5rem', fontSize: '0.9rem', color: '#666' }}>
          <p>
            🔒 Seus dados são protegidos com criptografia de nível bancário
          </p>
        </div>
      </div>
    </div>
  );
};

export default Welcome;
