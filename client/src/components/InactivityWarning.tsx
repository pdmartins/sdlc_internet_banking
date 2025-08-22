import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';

interface InactivityWarningProps {
  warningThresholdMinutes?: number;
}

/**
 * Component that displays an inactivity warning before automatic logout
 * Implements part of User Story 2.3.1: Automatic logout after inactivity
 */
const InactivityWarning: React.FC<InactivityWarningProps> = ({ 
  warningThresholdMinutes = 5 
}) => {
  const { session, timeUntilTimeout, updateActivity, logout } = useAuth();
  const [showWarning, setShowWarning] = useState(false);
  const [countdown, setCountdown] = useState<number>(0);

  useEffect(() => {
    if (session?.isAuthenticated && timeUntilTimeout !== null) {
      // Show warning if time remaining is less than threshold
      if (timeUntilTimeout <= warningThresholdMinutes && timeUntilTimeout > 0) {
        setShowWarning(true);
        setCountdown(timeUntilTimeout);
      } else if (timeUntilTimeout <= 0) {
        // Auto logout when time expires
        handleAutoLogout();
      } else {
        setShowWarning(false);
      }
    }
  }, [session?.isAuthenticated, timeUntilTimeout, warningThresholdMinutes]);

  useEffect(() => {
    let countdownInterval: NodeJS.Timeout;

    if (showWarning && countdown > 0) {
      countdownInterval = setInterval(() => {
        setCountdown(prev => {
          if (prev <= 1) {
            handleAutoLogout();
            return 0;
          }
          return prev - 1;
        });
      }, 60000); // Update every minute
    }

    return () => {
      if (countdownInterval) {
        clearInterval(countdownInterval);
      }
    };
  }, [showWarning, countdown]);

  const handleAutoLogout = async () => {
    setShowWarning(false);
    await logout();
  };

  const handleStayLoggedIn = async () => {
    await updateActivity();
    setShowWarning(false);
  };

  const handleLogoutNow = async () => {
    setShowWarning(false);
    await logout();
  };

  if (!showWarning || !session?.isAuthenticated) {
    return null;
  }

  return (
    <div className="inactivity-warning-overlay">
      <div className="inactivity-warning-modal">
        <div className="warning-icon">
          ⚠️
        </div>
        
        <h3 className="warning-title">
          Sua sessão irá expirar em breve
        </h3>
        
        <p className="warning-message">
          Você será desconectado automaticamente em{' '}
          <strong>{countdown} minuto{countdown !== 1 ? 's' : ''}</strong> devido à inatividade.
        </p>
        
        <div className="warning-actions">
          <button
            onClick={handleStayLoggedIn}
            className="btn btn-primary"
          >
            Continuar conectado
          </button>
          
          <button
            onClick={handleLogoutNow}
            className="btn btn-secondary"
          >
            Sair agora
          </button>
        </div>
        
        <div className="warning-note">
          <p>
            🔒 Esta é uma medida de segurança para proteger sua conta.
          </p>
        </div>
      </div>
    </div>
  );
};

export default InactivityWarning;
