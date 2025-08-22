import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import MfaVerification from '../components/MfaVerification';

// Mock API responses
const mockFetch = jest.fn();
global.fetch = mockFetch;

// Mock react-router-dom
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

describe('MfaVerification Component', () => {
  const defaultProps = {
    email: 'test@example.com',
    mfaMethod: 'sms',
    onSuccess: jest.fn(),
    onCancel: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockFetch.mockClear();
  });

  const renderComponent = (props = {}) => {
    return render(
      <BrowserRouter>
        <MfaVerification {...defaultProps} {...props} />
      </BrowserRouter>
    );
  };

  describe('User Story 2.1.2 - MFA Verification Flow', () => {
    test('displays MFA verification form with correct email and method', async () => {
      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(), // 10 minutes
          remainingAttempts: 3,
          message: 'Código MFA enviado via SMS. Verifique suas mensagens.',
        }),
      });

      renderComponent();

      // Check header content
      expect(screen.getByText('Verificação de Segurança')).toBeInTheDocument();
      expect(screen.getByText(/código de verificação foi enviado via SMS/i)).toBeInTheDocument();
      expect(screen.getByText(/test@example.com/)).toBeInTheDocument();

      // Check form elements
      expect(screen.getByLabelText(/código de verificação/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /verificar código/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /reenviar código/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /cancelar/i })).toBeInTheDocument();

      // Wait for initial API call
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/mfa/send-code'),
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              email: 'test@example.com',
              mfaMethod: 'sms',
            }),
          })
        );
      });
    });

    test('validates MFA code input format', async () => {
      const user = userEvent.setup();

      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      renderComponent();

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const codeInput = screen.getByLabelText(/código de verificação/i);
      const submitButton = screen.getByRole('button', { name: /verificar código/i });

      // Test invalid codes
      await user.type(codeInput, '123');
      await user.click(submitButton);
      expect(screen.getByText(/código deve ter exatamente 6 dígitos/i)).toBeInTheDocument();

      await user.clear(codeInput);
      await user.type(codeInput, '12345a');
      await user.click(submitButton);
      expect(screen.getByText(/código deve ter exatamente 6 dígitos/i)).toBeInTheDocument();
    });

    test('handles successful MFA verification', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      // Mock successful verification
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          message: 'MFA verificado com sucesso.',
          accessToken: 'valid-token-123',
          remainingAttempts: 2,
          isLocked: false,
        }),
      });

      renderComponent({ onSuccess: mockOnSuccess });

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const codeInput = screen.getByLabelText(/código de verificação/i);
      const submitButton = screen.getByRole('button', { name: /verificar código/i });

      await user.type(codeInput, '123456');
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/mfa/verify-code'),
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              email: 'test@example.com',
              code: '123456',
              sessionId: 'test-session-123',
            }),
          })
        );
      });

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith('valid-token-123');
      });
    });

    test('handles invalid MFA code attempts', async () => {
      const user = userEvent.setup();

      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      // Mock failed verification
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: false,
          message: 'Código MFA inválido.',
          remainingAttempts: 2,
          isLocked: false,
        }),
      });

      renderComponent();

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const codeInput = screen.getByLabelText(/código de verificação/i);
      const submitButton = screen.getByRole('button', { name: /verificar código/i });

      await user.type(codeInput, '000000');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/código mfa inválido/i)).toBeInTheDocument();
        expect(screen.getByText(/tentativas restantes: 2/i)).toBeInTheDocument();
      });

      // Check that input is cleared after failed attempt
      expect(codeInput).toHaveValue('');
    });

    test('handles MFA session lock after too many attempts', async () => {
      const user = userEvent.setup();
      const mockOnCancel = jest.fn();

      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      // Mock locked session
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: false,
          message: 'Sessão MFA bloqueada devido a muitas tentativas incorretas.',
          remainingAttempts: 0,
          isLocked: true,
          lockedUntil: new Date(Date.now() + 600000).toISOString(),
        }),
      });

      renderComponent({ onCancel: mockOnCancel });

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const codeInput = screen.getByLabelText(/código de verificação/i);
      const submitButton = screen.getByRole('button', { name: /verificar código/i });

      await user.type(codeInput, '000000');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/sessão mfa bloqueada/i)).toBeInTheDocument();
      });

      // Should redirect after 3 seconds
      await waitFor(() => {
        expect(mockOnCancel).toHaveBeenCalled();
      }, { timeout: 4000 });
    });

    test('allows resending MFA code when cooldown period expires', async () => {
      const user = userEvent.setup();

      // Mock successful initial code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          canResend: false,
          nextResendAt: new Date(Date.now() + 2000).toISOString(), // 2 seconds
          message: 'Código enviado',
        }),
      });

      renderComponent();

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const resendButton = screen.getByRole('button', { name: /reenviar código/i });
      expect(resendButton).toBeDisabled();

      // Mock successful resend
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          canResend: false,
          message: 'Código reenviado com sucesso!',
        }),
      });

      // Wait for cooldown to expire and enable resend
      await waitFor(() => {
        expect(resendButton).toBeEnabled();
      }, { timeout: 3000 });

      await user.click(resendButton);

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/mfa/resend-code/test-session-123'),
          expect.objectContaining({ method: 'POST' })
        );
      });

      await waitFor(() => {
        expect(screen.getByText(/código reenviado com sucesso/i)).toBeInTheDocument();
      });
    });

    test('displays countdown timer for code expiration', async () => {
      // Mock code send with short expiry for testing
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 5000).toISOString(), // 5 seconds
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      renderComponent();

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      // Check that timer is displayed
      expect(screen.getByText(/código expira em:/i)).toBeInTheDocument();
      expect(screen.getByText(/0:0[4-5]/)).toBeInTheDocument(); // Should show remaining seconds

      // Wait for timer to countdown
      await waitFor(() => {
        expect(screen.getByText(/0:0[0-3]/)).toBeInTheDocument();
      }, { timeout: 3000 });
    });

    test('handles cancel action', async () => {
      const user = userEvent.setup();
      const mockOnCancel = jest.fn();

      // Mock successful code send
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado',
        }),
      });

      renderComponent({ onCancel: mockOnCancel });

      await waitFor(() => {
        expect(screen.getByText(/código enviado/i)).toBeInTheDocument();
      });

      const cancelButton = screen.getByRole('button', { name: /cancelar/i });
      await user.click(cancelButton);

      expect(mockOnCancel).toHaveBeenCalled();
    });

    test('displays help information for different MFA methods', async () => {
      // Test SMS method
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-123',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado via SMS',
        }),
      });

      const { rerender } = renderComponent({ mfaMethod: 'sms' });

      await waitFor(() => {
        expect(screen.getByText(/enviado via SMS/i)).toBeInTheDocument();
      });

      expect(screen.getByText(/não recebeu o código\?/i)).toBeInTheDocument();

      // Test email method
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          sessionId: 'test-session-456',
          expiresAt: new Date(Date.now() + 600000).toISOString(),
          remainingAttempts: 3,
          message: 'Código enviado via email',
        }),
      });

      rerender(
        <BrowserRouter>
          <MfaVerification {...defaultProps} mfaMethod="email" />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText(/enviado via email/i)).toBeInTheDocument();
      });

      expect(screen.getByText(/verifique sua pasta de spam/i)).toBeInTheDocument();
    });
  });
});
