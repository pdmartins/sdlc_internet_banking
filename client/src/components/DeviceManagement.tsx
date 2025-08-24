import React, { useState, useEffect } from 'react';
import { userProfileApi, DeviceInfo } from '../services/userProfileApi';
import { useAuth } from '../contexts/AuthContext';

interface DeviceManagementProps {
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
}

const DeviceManagement: React.FC<DeviceManagementProps> = ({ onSuccess, onError }) => {
  const { logoutAllDevices } = useAuth();
  const [devices, setDevices] = useState<DeviceInfo[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [revokeLoading, setRevokeLoading] = useState<string | null>(null);
  const [revokeAllLoading, setRevokeAllLoading] = useState(false);

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    try {
      setIsLoading(true);
      const activeDevices = await userProfileApi.getActiveDevices();
      setDevices(activeDevices);
    } catch (error) {
      console.error('Error loading devices:', error);
      onError('Erro ao carregar dispositivos ativos');
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getDeviceIcon = (deviceInfo: string): string => {
    const lowerDevice = deviceInfo.toLowerCase();
    if (lowerDevice.includes('mobile') || lowerDevice.includes('android') || lowerDevice.includes('iphone')) {
      return '📱';
    } else if (lowerDevice.includes('tablet') || lowerDevice.includes('ipad')) {
      return '📲';
    } else if (lowerDevice.includes('chrome') || lowerDevice.includes('firefox') || lowerDevice.includes('safari') || lowerDevice.includes('edge')) {
      return '💻';
    }
    return '🖥️';
  };

  const getLocationFlag = (location: string): string => {
    // Simple location to flag mapping - in production this would be more sophisticated
    const lowerLocation = location.toLowerCase();
    if (lowerLocation.includes('brazil') || lowerLocation.includes('brasil')) {
      return '🇧🇷';
    } else if (lowerLocation.includes('united states') || lowerLocation.includes('usa')) {
      return '🇺🇸';
    } else if (lowerLocation.includes('canada')) {
      return '🇨🇦';
    } else if (lowerLocation.includes('unknown')) {
      return '🌍';
    }
    return '🌍';
  };

  const handleRevokeDevice = async (deviceId: string) => {
    if (revokeLoading) return;

    const device = devices.find(d => d.id === deviceId);
    if (!device) return;

    const confirmMessage = `Tem certeza que deseja revogar o acesso do dispositivo:\n${device.deviceInfo}?\n\nEste dispositivo precisará fazer login novamente.`;
    
    if (!window.confirm(confirmMessage)) {
      return;
    }

    try {
      setRevokeLoading(deviceId);
      await userProfileApi.revokeDevice(deviceId);
      
      // Remove device from list
      setDevices(prev => prev.filter(d => d.id !== deviceId));
      
      onSuccess('Dispositivo revogado com sucesso!');
    } catch (error) {
      console.error('Error revoking device:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao revogar dispositivo';
      onError(errorMessage);
    } finally {
      setRevokeLoading(null);
    }
  };

  const handleRevokeAllDevices = async () => {
    if (revokeAllLoading) return;

    const confirmMessage = `Tem certeza que deseja revogar todos os outros dispositivos?\n\nTodos os outros dispositivos precisarão fazer login novamente.`;
    
    if (!window.confirm(confirmMessage)) {
      return;
    }

    try {
      setRevokeAllLoading(true);
      
      // Use the auth context method which handles the API call and local logout
      const success = await logoutAllDevices();
      
      if (success) {
        // Reload devices to get updated list
        await loadDevices();
        onSuccess('Todos os outros dispositivos foram desconectados com sucesso!');
      } else {
        onError('Erro ao desconectar outros dispositivos');
      }
    } catch (error) {
      console.error('Error revoking all devices:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro ao desconectar todos os dispositivos';
      onError(errorMessage);
    } finally {
      setRevokeAllLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="device-management-loading">
        <div className="loading-spinner"></div>
        <p>Carregando dispositivos ativos...</p>
      </div>
    );
  }

  return (
    <div className="device-management-container">
      <div className="device-management-header">
        <h2 className="form-title">Gerenciamento de Dispositivos</h2>
        <p className="form-description">
          Visualize e gerencie os dispositivos que têm acesso à sua conta
        </p>
      </div>

      {/* Action Buttons */}
      <div className="device-actions">
        <button
          className="btn btn-danger"
          onClick={handleRevokeAllDevices}
          disabled={revokeAllLoading || devices.length <= 1}
        >
          {revokeAllLoading ? (
            <>
              <span className="btn-spinner"></span>
              Desconectando...
            </>
          ) : (
            <>
              <span className="btn-icon">🚪</span>
              Desconectar Outros Dispositivos
            </>
          )}
        </button>
        <button
          className="btn btn-secondary"
          onClick={loadDevices}
          disabled={isLoading}
        >
          <span className="btn-icon">🔄</span>
          Atualizar Lista
        </button>
      </div>

      {/* Device List */}
      {devices.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📱</div>
          <h3>Nenhum dispositivo ativo</h3>
          <p>Não foram encontrados dispositivos ativos para sua conta.</p>
        </div>
      ) : (
        <div className="device-list">
          {devices.map((device) => (
            <div key={device.id} className={`device-card ${device.isCurrentSession ? 'current-device' : ''}`}>
              <div className="device-info">
                <div className="device-header">
                  <div className="device-icon">
                    {getDeviceIcon(device.deviceInfo)}
                  </div>
                  <div className="device-details">
                    <h3 className="device-name">
                      {device.deviceInfo || 'Dispositivo Desconhecido'}
                      {device.isCurrentSession && (
                        <span className="current-badge">Este dispositivo</span>
                      )}
                      {device.isTrustedDevice && (
                        <span className="trusted-badge">Confiável</span>
                      )}
                    </h3>
                    <div className="device-meta">
                      <span className="device-location">
                        {getLocationFlag(device.location)} {device.location || 'Localização Desconhecida'}
                      </span>
                      <span className="device-ip">
                        IP: {device.ipAddress || 'Desconhecido'}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="device-timestamps">
                  <div className="timestamp-item">
                    <span className="timestamp-label">Primeiro acesso:</span>
                    <span className="timestamp-value">{formatDate(device.createdAt)}</span>
                  </div>
                  {device.lastActivityAt && (
                    <div className="timestamp-item">
                      <span className="timestamp-label">Última atividade:</span>
                      <span className="timestamp-value">{formatDate(device.lastActivityAt)}</span>
                    </div>
                  )}
                </div>
              </div>

              <div className="device-actions">
                {!device.isCurrentSession && (
                  <button
                    className="btn btn-outline btn-danger"
                    onClick={() => handleRevokeDevice(device.id)}
                    disabled={revokeLoading === device.id}
                  >
                    {revokeLoading === device.id ? (
                      <>
                        <span className="btn-spinner"></span>
                        Revogando...
                      </>
                    ) : (
                      <>
                        <span className="btn-icon">❌</span>
                        Revogar Acesso
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Security Notice */}
      <div className="security-notice">
        <div className="notice-icon">⚠️</div>
        <div className="notice-content">
          <h4>Importante para sua Segurança</h4>
          <ul>
            <li>Se você encontrar um dispositivo que não reconhece, revogue o acesso imediatamente</li>
            <li>Sempre faça logout em dispositivos públicos ou compartilhados</li>
            <li>Dispositivos marcados como "Confiável" não solicitarão verificação adicional</li>
            <li>Monitore regularmente esta lista para identificar atividades suspeitas</li>
          </ul>
        </div>
      </div>

      {/* Device Statistics */}
      <div className="device-stats">
        <div className="stat-card">
          <div className="stat-icon">📱</div>
          <div className="stat-info">
            <span className="stat-number">{devices.length}</span>
            <span className="stat-label">Dispositivos Ativos</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon">✅</div>
          <div className="stat-info">
            <span className="stat-number">{devices.filter(d => d.isTrustedDevice).length}</span>
            <span className="stat-label">Dispositivos Confiáveis</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon">🕒</div>
          <div className="stat-info">
            <span className="stat-number">
              {devices.filter(d => d.lastActivityAt && 
                new Date(d.lastActivityAt) > new Date(Date.now() - 24 * 60 * 60 * 1000)
              ).length}
            </span>
            <span className="stat-label">Ativos Hoje</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DeviceManagement;
