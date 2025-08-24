import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import ProfileForm from '../components/ProfileForm';
import SecuritySettingsForm from '../components/SecuritySettingsForm';
import DeviceManagement from '../components/DeviceManagement';
import '../styles/profile.css';

interface ProfilePageState {
  activeTab: 'profile' | 'security' | 'devices';
  isLoading: boolean;
  error: string | null;
  successMessage: string | null;
}

const ProfilePage: React.FC = () => {
  const { session } = useAuth();
  const navigate = useNavigate();
  const [state, setState] = useState<ProfilePageState>({
    activeTab: 'profile',
    isLoading: false,
    error: null,
    successMessage: null
  });

  useEffect(() => {
    if (!session?.isAuthenticated) {
      navigate('/login');
    }
  }, [session, navigate]);

  const handleTabChange = (tab: 'profile' | 'security' | 'devices') => {
    setState(prev => ({
      ...prev,
      activeTab: tab,
      error: null,
      successMessage: null
    }));
  };

  const handleSuccess = (message: string) => {
    setState(prev => ({
      ...prev,
      successMessage: message,
      error: null
    }));

    // Auto-hide success message after 5 seconds
    setTimeout(() => {
      setState(prev => ({
        ...prev,
        successMessage: null
      }));
    }, 5000);
  };

  const handleError = (error: string) => {
    setState(prev => ({
      ...prev,
      error: error,
      successMessage: null
    }));
  };

  const clearMessages = () => {
    setState(prev => ({
      ...prev,
      error: null,
      successMessage: null
    }));
  };

  if (!session?.isAuthenticated) {
    return <div>Redirecting...</div>;
  }

  return (
    <div className="form-container">
      <div className="dashboard-wrapper">
        {/* Header */}
        <div className="dashboard-header">
          <h1 className="form-title">Minha Conta</h1>
          <p className="form-description">
            Gerencie suas informações pessoais e configurações de segurança
          </p>
        </div>

        {/* Success/Error Messages */}
        {state.successMessage && (
          <div className="success-alert">
            <div className="success-content">
              <div className="success-icon">✅</div>
              <div className="success-text">
                <h4>Operação Realizada com Sucesso!</h4>
                <p>{state.successMessage}</p>
              </div>
              <button 
                onClick={clearMessages}
                className="alert-close-btn"
                aria-label="Fechar mensagem"
              >
                ×
              </button>
            </div>
          </div>
        )}

        {state.error && (
          <div className="error-alert">
            <p>{state.error}</p>
            <button 
              onClick={clearMessages}
              className="btn btn-secondary btn-small"
            >
              Tentar Novamente
            </button>
          </div>
        )}

        {/* Navigation Tabs */}
        <div className="profile-tabs">
          <button
            className={`tab-button ${state.activeTab === 'profile' ? 'active' : ''}`}
            onClick={() => handleTabChange('profile')}
          >
            <span className="tab-icon">👤</span>
            Informações Pessoais
          </button>
          <button
            className={`tab-button ${state.activeTab === 'security' ? 'active' : ''}`}
            onClick={() => handleTabChange('security')}
          >
            <span className="tab-icon">🔒</span>
            Segurança
          </button>
          <button
            className={`tab-button ${state.activeTab === 'devices' ? 'active' : ''}`}
            onClick={() => handleTabChange('devices')}
          >
            <span className="tab-icon">📱</span>
            Dispositivos
          </button>
        </div>

        {/* Tab Content */}
        <div className="profile-content">
          {state.activeTab === 'profile' && (
            <ProfileForm 
              onSuccess={handleSuccess}
              onError={handleError}
            />
          )}
          
          {state.activeTab === 'security' && (
            <SecuritySettingsForm 
              onSuccess={handleSuccess}
              onError={handleError}
            />
          )}
          
          {state.activeTab === 'devices' && (
            <DeviceManagement 
              onSuccess={handleSuccess}
              onError={handleError}
            />
          )}
        </div>
      </div>
    </div>
  );
};

export default ProfilePage;
