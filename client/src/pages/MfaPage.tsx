import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import MfaVerification from '../components/MfaVerification';

interface MfaPageState {
  userId: string;
  email: string;
  mfaMethod: string;
}

const MfaPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as MfaPageState;

  useEffect(() => {
    // Redirect if no MFA state is available
    if (!state || !state.email || !state.mfaMethod) {
      navigate('/login', { replace: true });
    }
  }, [state, navigate]);

  const handleMfaSuccess = (token: string) => {
    // Store user session data with the new token
    localStorage.setItem('user', JSON.stringify({
      userId: state.userId,
      email: state.email,
      token: token,
      tokenExpiresAt: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(), // 8 hours
      mfaVerified: true,
    }));

    // Navigate to dashboard
    navigate('/dashboard', { replace: true });
  };

  const handleMfaCancel = () => {
    // Return to login page
    navigate('/login', { replace: true });
  };

  if (!state) {
    return null; // Will redirect in useEffect
  }

  return (
    <div className="mfa-page">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-5">
            <div className="logo-section">
              <h1 className="app-title">Contoso Bank</h1>
              <p className="app-subtitle">Internet Banking Seguro</p>
            </div>
            
            <MfaVerification
              email={state.email}
              mfaMethod={state.mfaMethod}
              onSuccess={handleMfaSuccess}
              onCancel={handleMfaCancel}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

export default MfaPage;
