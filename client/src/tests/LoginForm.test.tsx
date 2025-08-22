import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import LoginForm from '../components/LoginForm';

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

// Mock fetch
global.fetch = jest.fn();

describe('LoginForm Component - User Story 2.1.1', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    (fetch as jest.Mock).mockClear();
  });

  const renderLoginForm = (props = {}) => {
    return render(
      <BrowserRouter>
        <LoginForm {...props} />
      </BrowserRouter>
    );
  };

  describe('Acceptance Criteria: Form with validation', () => {
    test('displays all required form fields', () => {
      renderLoginForm();
      
      expect(screen.getByLabelText(/^email \*/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/^senha \*/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/lembrar dispositivo/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /entrar na conta/i })).toBeInTheDocument();
    });

    test('shows validation errors for empty required fields', async () => {
      const user = userEvent.setup();
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Type something and then clear to trigger validation
      await user.type(emailInput, 'test');
      await user.clear(emailInput);
      await user.type(passwordInput, 'test');
      await user.clear(passwordInput);
      
      // Submit the form to trigger validation
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(screen.getByText(/email é obrigatório/i)).toBeInTheDocument();
        expect(screen.getByText(/senha é obrigatória/i)).toBeInTheDocument();
      });
    });

    test('validates email format', async () => {
      const user = userEvent.setup();
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      
      // Enter invalid email
      await user.type(emailInput, 'invalid-email');
      await user.tab(); // Trigger validation
      
      await waitFor(() => {
        expect(screen.getByText(/formato de email inválido/i)).toBeInTheDocument();
      });
    });

    test('enables submit button when form is valid', async () => {
      const user = userEvent.setup();
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Submit button should be disabled initially
      expect(submitButton).toBeDisabled();
      
      // Fill in valid data
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      
      await waitFor(() => {
        expect(submitButton).toBeEnabled();
      });
    });
  });

  describe('Acceptance Criteria: Option to remember device', () => {
    test('includes remember device checkbox with proper labeling', () => {
      renderLoginForm();
      
      const rememberCheckbox = screen.getByLabelText(/lembrar dispositivo/i);
      expect(rememberCheckbox).toBeInTheDocument();
      expect(rememberCheckbox).not.toBeChecked();
      
      // Check for security warning
      expect(screen.getByText(/não marque esta opção em computadores compartilhados/i)).toBeInTheDocument();
    });

    test('allows toggling remember device option', async () => {
      const user = userEvent.setup();
      renderLoginForm();
      
      const rememberCheckbox = screen.getByLabelText(/lembrar dispositivo/i);
      
      // Toggle checkbox
      await user.click(rememberCheckbox);
      expect(rememberCheckbox).toBeChecked();
      
      await user.click(rememberCheckbox);
      expect(rememberCheckbox).not.toBeChecked();
    });
  });

  describe('Acceptance Criteria: Error feedback for failed attempts', () => {
    test('displays error message on failed login', async () => {
      const user = userEvent.setup();
      
      // Mock failed login response
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Email ou senha inválidos.' }),
      });
      
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Fill form and submit
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'wrongpassword');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(screen.getByText(/email ou senha inválidos/i)).toBeInTheDocument();
      });
    });

    test('allows dismissing error messages', async () => {
      const user = userEvent.setup();
      
      // Mock failed login response
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Email ou senha inválidos.' }),
      });
      
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Fill form and submit
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'wrongpassword');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(screen.getByText(/email ou senha inválidos/i)).toBeInTheDocument();
      });
      
      // Dismiss error
      const closeButton = screen.getByLabelText(/fechar erro/i);
      await user.click(closeButton);
      
      expect(screen.queryByText(/email ou senha inválidos/i)).not.toBeInTheDocument();
    });
  });

  describe('Login Success Flow', () => {
    test('navigates to dashboard on successful login without MFA', async () => {
      const user = userEvent.setup();
      
      // Mock successful login response
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: 'user-id',
          email: 'test@example.com',
          fullName: 'Test User',
          token: 'jwt-token',
          tokenExpiresAt: '2025-08-22T12:00:00Z',
          requiresMfa: false,
          account: { accountNumber: '123456' },
        }),
      });

      // Mock localStorage
      const mockLocalStorage = {
        setItem: jest.fn(),
      };
      Object.defineProperty(window, 'localStorage', {
        value: mockLocalStorage,
      });
      
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Fill form and submit
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
        expect(mockLocalStorage.setItem).toHaveBeenCalledWith(
          'user',
          expect.stringContaining('test@example.com')
        );
      });
    });

    test('navigates to MFA verification when MFA is required', async () => {
      const user = userEvent.setup();
      
      // Mock successful login response with MFA required
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          userId: 'user-id',
          email: 'test@example.com',
          fullName: 'Test User',
          requiresMfa: true,
          mfaMethod: 'sms',
        }),
      });
      
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Fill form and submit
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/login/mfa', {
          state: {
            userId: 'user-id',
            email: 'test@example.com',
            mfaMethod: 'sms',
          },
        });
      });
    });
  });

  describe('Password Visibility Toggle', () => {
    test('toggles password visibility', async () => {
      const user = userEvent.setup();
      renderLoginForm();
      
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const toggleButton = screen.getByRole('button', { name: /mostrar senha/i });
      
      // Initially password should be hidden
      expect(passwordInput).toHaveAttribute('type', 'password');
      
      // Click toggle to show password
      await user.click(toggleButton);
      expect(passwordInput).toHaveAttribute('type', 'text');
      expect(screen.getByRole('button', { name: /ocultar senha/i })).toBeInTheDocument();
      
      // Click toggle to hide password again
      const hideToggleButton = screen.getByRole('button', { name: /ocultar senha/i });
      await user.click(hideToggleButton);
      expect(passwordInput).toHaveAttribute('type', 'password');
    });
  });

  describe('Form Navigation', () => {
    test('includes link to password recovery', () => {
      renderLoginForm();
      
      const forgotPasswordLink = screen.getByRole('link', { name: /esqueci minha senha/i });
      expect(forgotPasswordLink).toBeInTheDocument();
      expect(forgotPasswordLink).toHaveAttribute('href', '/forgot-password');
    });

    test('includes link to registration', () => {
      renderLoginForm();
      
      const createAccountLink = screen.getByRole('link', { name: /criar conta/i });
      expect(createAccountLink).toBeInTheDocument();
      expect(createAccountLink).toHaveAttribute('href', '/welcome');
    });
  });

  describe('Accessibility', () => {
    test('has proper form labels and ARIA attributes', () => {
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      
      expect(emailInput).toHaveAttribute('aria-describedby');
      expect(passwordInput).toHaveAttribute('aria-describedby');
      expect(emailInput).toHaveAttribute('autoComplete', 'email');
      expect(passwordInput).toHaveAttribute('autoComplete', 'current-password');
    });

    test('displays security note for user confidence', () => {
      renderLoginForm();
      
      expect(screen.getByText(/seus dados são protegidos com criptografia de nível bancário/i)).toBeInTheDocument();
    });
  });

  describe('Loading States', () => {
    test('shows loading state during submission', async () => {
      const user = userEvent.setup();
      
      // Mock delayed response
      (fetch as jest.Mock).mockImplementationOnce(() => 
        new Promise(resolve => setTimeout(() => resolve({
          ok: true,
          json: async () => ({ requiresMfa: false }),
        }), 100))
      );
      
      renderLoginForm();
      
      const emailInput = screen.getByLabelText(/^email \*/i);
      const passwordInput = screen.getByLabelText(/senha \*/i);
      const submitButton = screen.getByRole('button', { name: /entrar na conta/i });
      
      // Fill form and submit
      await user.type(emailInput, 'test@example.com');
      await user.type(passwordInput, 'password123');
      await user.click(submitButton);
      
      // Check loading state
      expect(screen.getByText(/entrando\.\.\./i)).toBeInTheDocument();
      expect(submitButton).toBeDisabled();
    });
  });
});
