import React, { useState, useEffect } from 'react';
import { useAuth, SessionInfo } from '../contexts/AuthContext';

/**
 * Component to display and manage active user sessions
 * Implements the "logout all devices" feature from User Story 2.3.1
 */
const SessionManager: React.FC = () => {
  const { getActiveSessions, logoutAllDevices, isLoading, error } = useAuth();
  const [sessions, setSessions] = useState<SessionInfo[]>([]);
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadActiveSessions();
  }, []);

  const loadActiveSessions = async () => {
    try {
      setLocalError(null);
      const activeSessions = await getActiveSessions();
      setSessions(activeSessions);
    } catch (error) {
      setLocalError('Erro ao carregar sessões ativas');
    }
  };

  const handleLogoutAllDevices = async () => {
    try {
      setLocalError(null);
      setSuccessMessage(null);
      
      const success = await logoutAllDevices();
      
      if (success) {
        setSuccessMessage('Logout realizado em todos os outros dispositivos');
        await loadActiveSessions(); // Refresh the list
      } else {
        setLocalError('Erro ao fazer logout de todos os dispositivos');
      }
    } catch (error) {
      setLocalError('Erro inesperado ao fazer logout de todos os dispositivos');
    } finally {
      setShowLogoutConfirm(false);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getDeviceIcon = (deviceInfo: string) => {
    if (deviceInfo.includes('móvel') || deviceInfo.includes('Mobile')) return '📱';
    if (deviceInfo.includes('Tablet')) return '📱';
    if (deviceInfo.includes('Windows')) return '💻';
    if (deviceInfo.includes('Mac')) return '💻';
    return '🖥️';
  };

  return (
    <div className="session-manager">
      <div className="session-header">
        <h3>Dispositivos Conectados</h3>
        <p className="session-description">
          Gerencie os dispositivos que têm acesso à sua conta
        </p>
      </div>

      {(localError || error) && (
        <div className="alert alert-error">
          <span className="alert-icon">❌</span>
          {localError || error}
        </div>
      )}

      {successMessage && (
        <div className="alert alert-success">
          <span className="alert-icon">✅</span>
          {successMessage}
        </div>
      )}

      {isLoading ? (
        <div className="loading-state">
          <div className="loading-spinner"></div>
          <p>Carregando sessões ativas...</p>
        </div>
      ) : (
        <>
          <div className="sessions-list">
            {sessions.length === 0 ? (
              <div className="empty-state">
                <p>Nenhuma sessão ativa encontrada</p>
              </div>
            ) : (
              sessions.map((session) => (
                <div key={session.id} className={`session-card ${session.isCurrentSession ? 'current-session' : ''}`}>
                  <div className="session-info">
                    <div className="session-device">
                      <span className="device-icon">{getDeviceIcon(session.deviceInfo)}</span>
                      <div className="device-details">
                        <span className="device-name">{session.deviceInfo}</span>
                        {session.isCurrentSession && (
                          <span className="current-label">Dispositivo atual</span>
                        )}
                      </div>
                    </div>
                    
                    <div className="session-metadata">
                      <div className="session-detail">
                        <span className="detail-label">IP:</span>
                        <span className="detail-value">{session.ipAddress}</span>
                      </div>
                      
                      {session.location && (
                        <div className="session-detail">
                          <span className="detail-label">Local:</span>
                          <span className="detail-value">{session.location}</span>
                        </div>
                      )}
                      
                      <div className="session-detail">
                        <span className="detail-label">Conectado em:</span>
                        <span className="detail-value">{formatDate(session.createdAt)}</span>
                      </div>
                      
                      {session.lastActivityAt && (
                        <div className="session-detail">
                          <span className="detail-label">Última atividade:</span>
                          <span className="detail-value">{formatDate(session.lastActivityAt)}</span>
                        </div>
                      )}
                    </div>
                  </div>
                  
                  {session.isTrustedDevice && (
                    <div className="trusted-badge">
                      🔒 Dispositivo confiável
                    </div>
                  )}
                </div>
              ))
            )}
          </div>

          {sessions.length > 1 && (
            <div className="session-actions">
              <button
                onClick={() => setShowLogoutConfirm(true)}
                className="btn btn-danger"
                disabled={isLoading}
              >
                Sair de todos os outros dispositivos
              </button>
              
              <p className="action-note">
                Isso irá desconectar todos os outros dispositivos, mantendo apenas este ativo.
              </p>
            </div>
          )}
        </>
      )}

      {/* Logout Confirmation Modal */}
      {showLogoutConfirm && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h4>Confirmar logout de todos os dispositivos</h4>
            <p>
              Tem certeza que deseja sair de todos os outros dispositivos? 
              Esta ação irá desconectar {sessions.filter(s => !s.isCurrentSession).length} dispositivo(s).
            </p>
            
            <div className="modal-actions">
              <button
                onClick={handleLogoutAllDevices}
                className="btn btn-danger"
                disabled={isLoading}
              >
                {isLoading ? 'Processando...' : 'Sim, sair de todos'}
              </button>
              
              <button
                onClick={() => setShowLogoutConfirm(false)}
                className="btn btn-secondary"
                disabled={isLoading}
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SessionManager;
