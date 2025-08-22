import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { API_ENDPOINTS } from '../config/api';

interface MfaVerificationProps {
  email: string;
  mfaMethod: string;
  onSuccess: (token: string) => void;
  onCancel: () => void;
}

interface MfaFormData {
  code: string;
}

interface MfaState {
  sessionId: string | null;
  timeRemaining: number;
  canResend: boolean;
  remainingAttempts: number;
  isLoading: boolean;
  message: string;
  messageType: 'success' | 'error' | 'info';
}

const schema = yup.object({
  code: yup
    .string()
    .required('Código MFA é obrigatório')
    .matches(/^\d{6}$/, 'Código deve ter exatamente 6 dígitos'),
});

const MfaVerification: React.FC<MfaVerificationProps> = ({
  email,
  mfaMethod,
  onSuccess,
  onCancel,
}) => {
  const [mfaState, setMfaState] = useState<MfaState>({
    sessionId: null,
    timeRemaining: 0,
    canResend: false,
    remainingAttempts: 3,
    isLoading: false,
    message: '',
    messageType: 'info',
  });

  // Use ref to track if initial code has been sent
  const hasInitialCodeSent = useRef(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<MfaFormData>({
    resolver: yupResolver(schema),
  });

  const sendMfaCode = useCallback(async () => {
    setMfaState(prev => ({ 
      ...prev, 
      isLoading: true, 
      message: ''
    }));

    try {

      //log
      console.log(`MFA code sent to ${email} via ${mfaMethod}`);

      const response = await fetch(API_ENDPOINTS.MFA_SEND_CODE, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email,
          mfaMethod,
        }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        const expiresAt = new Date(data.expiresAt);
        const timeRemaining = Math.max(0, Math.floor((expiresAt.getTime() - Date.now()) / 1000));

        setMfaState(prev => ({
          ...prev,
          sessionId: data.sessionId,
          timeRemaining,
          canResend: false,
          remainingAttempts: data.remainingAttempts,
          isLoading: false,
          message: data.message,
          messageType: 'success',
        }));
      } else {
        throw new Error(data.message || 'Erro ao enviar código MFA');
      }
    } catch (error) {
      setMfaState(prev => ({
        ...prev,
        isLoading: false,
        message: error instanceof Error ? error.message : 'Erro ao enviar código MFA',
        messageType: 'error',
      }));
    }
  }, [email, mfaMethod]); // Dependencies that could change

  // Send initial MFA code (only once)
  useEffect(() => {
    if (!hasInitialCodeSent.current) {
      hasInitialCodeSent.current = true;
      sendMfaCode();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Intentionally empty - we only want this to run once on mount

  // Timer for code expiration
  useEffect(() => {
    if (mfaState.timeRemaining > 0) {
      const timer = setTimeout(() => {
        setMfaState(prev => ({
          ...prev,
          timeRemaining: prev.timeRemaining - 1,
          canResend: prev.timeRemaining <= 1,
        }));
      }, 1000);

      return () => clearTimeout(timer);
    }
  }, [mfaState.timeRemaining]);

  const verifyMfaCode = async (data: MfaFormData) => {
    if (!mfaState.sessionId) {
      setMfaState(prev => ({
        ...prev,
        message: 'Sessão MFA inválida. Tente novamente.',
        messageType: 'error',
      }));
      return;
    }

    try {
      const response = await fetch(API_ENDPOINTS.MFA_VERIFY_CODE, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email,
          code: data.code,
          sessionId: mfaState.sessionId,
        }),
      });

      const result = await response.json();

      if (response.ok && result.success) {
        setMfaState(prev => ({
          ...prev,
          message: 'MFA verificado com sucesso!',
          messageType: 'success',
        }));
        onSuccess(result.accessToken);
      } else {
        setMfaState(prev => ({
          ...prev,
          remainingAttempts: result.remainingAttempts || 0,
          message: result.message || 'Código MFA inválido',
          messageType: 'error',
        }));
        reset();

        if (result.isLocked) {
          setTimeout(() => {
            onCancel();
          }, 3000);
        }
      }
    } catch (error) {
      setMfaState(prev => ({
        ...prev,
        message: error instanceof Error ? error.message : 'Erro ao verificar código MFA',
        messageType: 'error',
      }));
      reset();
    }
  };

  const resendCode = async () => {
    if (!mfaState.sessionId || !mfaState.canResend) return;

    setMfaState(prev => ({ ...prev, isLoading: true, message: '' }));

    try {
      const response = await fetch(API_ENDPOINTS.MFA_RESEND_CODE(mfaState.sessionId), {
        method: 'POST',
      });

      const data = await response.json();

      if (response.ok && data.success) {
        const expiresAt = new Date(data.expiresAt);
        const timeRemaining = Math.max(0, Math.floor((expiresAt.getTime() - Date.now()) / 1000));

        setMfaState(prev => ({
          ...prev,
          timeRemaining,
          canResend: false,
          remainingAttempts: data.remainingAttempts,
          isLoading: false,
          message: 'Código reenviado com sucesso!',
          messageType: 'success',
        }));
        reset();
      } else {
        throw new Error(data.message || 'Erro ao reenviar código');
      }
    } catch (error) {
      setMfaState(prev => ({
        ...prev,
        isLoading: false,
        message: error instanceof Error ? error.message : 'Erro ao reenviar código',
        messageType: 'error',
      }));
    }
  };

  const formatTime = (seconds: number) => {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  };

  const getMethodDescription = () => {
    switch (mfaMethod.toLowerCase()) {
      case 'sms':
        return 'SMS';
      case 'email':
        return 'email';
      default:
        return 'método configurado';
    }
  };

  return (
    <div className="mfa-verification">
      <div className="mfa-card">
        <div className="mfa-header">
          <h2>Verificação de Segurança</h2>
          <p>
            Um código de verificação foi enviado via {getMethodDescription()} para {email}
          </p>
        </div>

        {mfaState.message && (
          <div className={`alert alert-${mfaState.messageType}`}>
            {mfaState.message}
          </div>
        )}

        <form onSubmit={handleSubmit(verifyMfaCode)} className="mfa-form">
          <div className="form-group">
            <label htmlFor="code">Código de Verificação *</label>
            <input
              id="code"
              type="text"
              maxLength={6}
              placeholder="000000"
              {...register('code')}
              className={`form-control ${errors.code ? 'error' : ''}`}
              disabled={isSubmitting || mfaState.isLoading}
              autoComplete="one-time-code"
              autoFocus
            />
            {errors.code && (
              <span className="error-message">{errors.code.message}</span>
            )}
          </div>

          <div className="mfa-info">
            {mfaState.timeRemaining > 0 && (
              <p className="time-remaining">
                Código expira em: <strong>{formatTime(mfaState.timeRemaining)}</strong>
              </p>
            )}
            <p className="attempts-remaining">
              Tentativas restantes: <strong>{mfaState.remainingAttempts}</strong>
            </p>
          </div>

          <div className="form-actions">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting || mfaState.isLoading || mfaState.remainingAttempts <= 0}
            >
              {isSubmitting ? 'Verificando...' : 'Verificar Código'}
            </button>

            <button
              type="button"
              onClick={resendCode}
              className="btn btn-secondary"
              disabled={!mfaState.canResend || mfaState.isLoading}
            >
              {mfaState.isLoading ? 'Reenviando...' : 'Reenviar Código'}
            </button>

            <button
              type="button"
              onClick={onCancel}
              className="btn btn-outline"
              disabled={isSubmitting || mfaState.isLoading}
            >
              Cancelar
            </button>
          </div>
        </form>

        <div className="mfa-help">
          <p>Não recebeu o código?</p>
          <ul>
            <li>Verifique sua pasta de spam (se via email)</li>
            <li>Aguarde alguns minutos para recebimento</li>
            <li>Certifique-se de que seu {getMethodDescription()} está funcionando</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default MfaVerification;
