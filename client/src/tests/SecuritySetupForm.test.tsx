import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import SecuritySetupForm from '../components/SecuritySetupForm';

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

describe('SecuritySetupForm Component - User Story 1.1.3', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  const renderSecuritySetupForm = (props = {}) => {
    return render(
      <BrowserRouter>
        <SecuritySetupForm {...props} />
      </BrowserRouter>
    );
  };

  describe('Acceptance Criteria: Password strength meter', () => {
    test('displays password strength meter when typing', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      
      await user.type(passwordInput, 'weak');
      
      await waitFor(() => {
        expect(screen.getByText(/força da senha/i)).toBeInTheDocument();
        expect(screen.getByText(/fraca/i)).toBeInTheDocument();
      });
    });

    test('shows password strength progression', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      
      // Weak password
      await user.type(passwordInput, 'abc');
      await waitFor(() => {
        expect(screen.getByText(/muito fraca/i)).toBeInTheDocument();
      });
      
      // Strong password
      await user.clear(passwordInput);
      await user.type(passwordInput, 'MyStrongPass123!');
      await waitFor(() => {
        expect(screen.getByText(/muito forte/i)).toBeInTheDocument();
      });
    });

    test('shows password requirements feedback', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      
      await user.type(passwordInput, 'weak');
      
      await waitFor(() => {
        expect(screen.getByText(/pelo menos 8 caracteres/i)).toBeInTheDocument();
        expect(screen.getByText(/pelo menos 1 letra maiúscula/i)).toBeInTheDocument();
        expect(screen.getByText(/pelo menos 1 número/i)).toBeInTheDocument();
        expect(screen.getByText(/pelo menos 1 caractere especial/i)).toBeInTheDocument();
      });
    });

    test('password show/hide toggle works', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      const toggleButton = screen.getByLabelText(/mostrar senha/i);
      
      // Initially password type
      expect(passwordInput).toHaveAttribute('type', 'password');
      
      // Click to show
      await user.click(toggleButton);
      expect(passwordInput).toHaveAttribute('type', 'text');
      
      // Click to hide
      await user.click(toggleButton);
      expect(passwordInput).toHaveAttribute('type', 'password');
    });
  });

  describe('Acceptance Criteria: Security questions', () => {
    test('displays security question dropdown with options', () => {
      renderSecuritySetupForm();
      
      const questionSelect = screen.getByLabelText(/pergunta \*/i);
      expect(questionSelect).toBeInTheDocument();
      
      // Check some options are present
      expect(screen.getByText(/qual era o nome do seu primeiro animal/i)).toBeInTheDocument();
      expect(screen.getByText(/qual é o nome de solteira da sua mãe/i)).toBeInTheDocument();
    });

    test('validates security question selection', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const questionSelect = screen.getByLabelText(/pergunta \*/i);
      const answerInput = screen.getByLabelText(/resposta \*/i);
      
      // Try to submit without selecting question
      await user.click(answerInput);
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/pergunta de segurança é obrigatória/i)).toBeInTheDocument();
      });
    });

    test('validates security answer input', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const answerInput = screen.getByLabelText(/resposta \*/i);
      
      // Test empty answer
      await user.click(answerInput);
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/resposta de segurança é obrigatória/i)).toBeInTheDocument();
      });
      
      // Test valid answer
      await user.type(answerInput, 'Minha resposta válida');
      
      await waitFor(() => {
        expect(screen.getByText(/resposta válida/i)).toBeInTheDocument();
      });
    });
  });

  describe('Acceptance Criteria: MFA options (SMS/Auth app)', () => {
    test('displays MFA options', () => {
      renderSecuritySetupForm();
      
      expect(screen.getByText(/sms \(mensagem de texto\)/i)).toBeInTheDocument();
      expect(screen.getByText(/aplicativo autenticador/i)).toBeInTheDocument();
      expect(screen.getByText(/receba códigos via sms/i)).toBeInTheDocument();
      expect(screen.getByText(/use google authenticator/i)).toBeInTheDocument();
    });

    test('allows selecting MFA option', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const smsOption = screen.getByLabelText(/sms \(mensagem de texto\)/i);
      const authOption = screen.getByLabelText(/aplicativo autenticador/i);
      
      // Select SMS
      await user.click(smsOption);
      expect(smsOption).toBeChecked();
      expect(authOption).not.toBeChecked();
      
      // Switch to Authenticator
      await user.click(authOption);
      expect(authOption).toBeChecked();
      expect(smsOption).not.toBeChecked();
    });

    test('validates MFA selection is required', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      // Fill other fields but skip MFA
      await user.type(screen.getByLabelText(/^senha \*/i), 'MyStrongPass123!');
      await user.type(screen.getByLabelText(/confirmar senha/i), 'MyStrongPass123!');
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      // Button should remain disabled without MFA selection
      await waitFor(() => {
        expect(continueButton).toBeDisabled();
      });
    });
  });

  describe('Acceptance Criteria: Backend validation; error handling', () => {
    test('validates password requirements', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      
      // Test various invalid passwords
      await user.type(passwordInput, 'short');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/senha deve ter pelo menos 8 caracteres/i)).toBeInTheDocument();
      });
    });

    test('validates password confirmation match', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      const confirmInput = screen.getByLabelText(/confirmar senha/i);
      
      await user.type(passwordInput, 'MyStrongPass123!');
      await user.type(confirmInput, 'DifferentPassword');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/senhas não coincidem/i)).toBeInTheDocument();
      });
      
      // Test matching passwords
      await user.clear(confirmInput);
      await user.type(confirmInput, 'MyStrongPass123!');
      
      await waitFor(() => {
        expect(screen.getByText(/senhas coincidem/i)).toBeInTheDocument();
      });
    });

    test('validates terms acceptance', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      // Fill all required fields except terms
      await user.type(screen.getByLabelText(/^senha \*/i), 'MyStrongPass123!');
      await user.type(screen.getByLabelText(/confirmar senha/i), 'MyStrongPass123!');
      await user.selectOptions(screen.getByLabelText(/pergunta \*/i), 'first_pet');
      await user.type(screen.getByLabelText(/resposta \*/i), 'Fluffy');
      await user.click(screen.getByLabelText(/sms \(mensagem de texto\)/i));
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      // Should still be disabled without terms acceptance
      await waitFor(() => {
        expect(continueButton).toBeDisabled();
      });
      
      // Accept terms
      await user.click(screen.getByLabelText(/eu li e aceito/i));
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
    });
  });

  describe('Form Navigation and Actions', () => {
    test('displays correct progress indicator', () => {
      renderSecuritySetupForm();
      
      const progressSteps = screen.getAllByText(/^[1-4]$/);
      expect(progressSteps).toHaveLength(4);
      
      // Step 3 should be active
      expect(progressSteps[2]).toHaveClass('active');
      // Steps 1 and 2 should be completed
      expect(progressSteps[0]).toHaveClass('completed');
      expect(progressSteps[1]).toHaveClass('completed');
    });

    test('navigates back to personal info when Voltar is clicked', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const backButton = screen.getByRole('button', { name: /voltar/i });
      await user.click(backButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/register/personal-info');
    });

    test('navigates to confirmation when valid form is submitted', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      // Fill all required fields
      await user.type(screen.getByLabelText(/^senha \*/i), 'MyStrongPass123!');
      await user.type(screen.getByLabelText(/confirmar senha/i), 'MyStrongPass123!');
      await user.selectOptions(screen.getByLabelText(/pergunta \*/i), 'first_pet');
      await user.type(screen.getByLabelText(/resposta \*/i), 'Fluffy');
      await user.click(screen.getByLabelText(/sms \(mensagem de texto\)/i));
      await user.click(screen.getByLabelText(/eu li e aceito/i));
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
      
      await user.click(continueButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/register/confirmation');
    });

    test('calls custom handlers when provided', async () => {
      const user = userEvent.setup();
      const mockOnSubmit = jest.fn();
      const mockOnCancel = jest.fn();
      
      renderSecuritySetupForm({ 
        onSubmit: mockOnSubmit, 
        onCancel: mockOnCancel 
      });
      
      // Test custom onCancel
      const backButton = screen.getByRole('button', { name: /voltar/i });
      await user.click(backButton);
      expect(mockOnCancel).toHaveBeenCalled();
      
      // Test custom onSubmit
      await user.type(screen.getByLabelText(/^senha \*/i), 'MyStrongPass123!');
      await user.type(screen.getByLabelText(/confirmar senha/i), 'MyStrongPass123!');
      await user.selectOptions(screen.getByLabelText(/pergunta \*/i), 'first_pet');
      await user.type(screen.getByLabelText(/resposta \*/i), 'Fluffy');
      await user.click(screen.getByLabelText(/sms \(mensagem de texto\)/i));
      await user.click(screen.getByLabelText(/eu li e aceito/i));
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
      
      await user.click(continueButton);
      
      expect(mockOnSubmit).toHaveBeenCalledWith({
        password: 'MyStrongPass123!',
        confirmPassword: 'MyStrongPass123!',
        securityQuestion: 'first_pet',
        securityAnswer: 'Fluffy',
        mfaOption: 'sms',
        termsAccepted: true
      });
    });
  });

  describe('Accessibility and User Experience', () => {
    test('has proper form structure and labels', () => {
      renderSecuritySetupForm();
      
      expect(screen.getByRole('heading', { name: /configuração de segurança/i })).toBeInTheDocument();
      expect(screen.getByText(/criar senha/i)).toBeInTheDocument();
      expect(screen.getByText(/pergunta de segurança/i)).toBeInTheDocument();
      expect(screen.getByText(/autenticação multifator/i)).toBeInTheDocument();
    });

    test('provides helpful field descriptions', () => {
      renderSecuritySetupForm();
      
      expect(screen.getByText(/use uma combinação de letras, números e símbolos/i)).toBeInTheDocument();
      expect(screen.getByText(/escolha uma pergunta que só você saiba responder/i)).toBeInTheDocument();
      expect(screen.getByText(/adicione uma camada extra de segurança/i)).toBeInTheDocument();
    });

    test('displays security compliance message', () => {
      renderSecuritySetupForm();
      
      expect(screen.getByText(/suas configurações de segurança são criptografadas/i)).toBeInTheDocument();
    });

    test('has proper ARIA attributes', () => {
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      const questionSelect = screen.getByLabelText(/pergunta \*/i);
      
      expect(passwordInput).toHaveAttribute('aria-describedby');
      expect(questionSelect).toHaveAttribute('aria-describedby');
    });
  });

  describe('Password Strength Calculation', () => {
    test('calculates different strength levels correctly', async () => {
      const user = userEvent.setup();
      renderSecuritySetupForm();
      
      const passwordInput = screen.getByLabelText(/^senha \*/i);
      
      // Very weak
      await user.type(passwordInput, 'a');
      await waitFor(() => {
        expect(screen.getByText(/muito fraca/i)).toBeInTheDocument();
      });
      
      // Weak
      await user.clear(passwordInput);
      await user.type(passwordInput, 'password');
      await waitFor(() => {
        expect(screen.getByText(/fraca/i)).toBeInTheDocument();
      });
      
      // Medium
      await user.clear(passwordInput);
      await user.type(passwordInput, 'Password1');
      await waitFor(() => {
        expect(screen.getByText(/média/i)).toBeInTheDocument();
      });
      
      // Strong
      await user.clear(passwordInput);
      await user.type(passwordInput, 'Password1!');
      await waitFor(() => {
        expect(screen.getByText(/forte/i)).toBeInTheDocument();
      });
      
      // Very strong
      await user.clear(passwordInput);
      await user.type(passwordInput, 'MyVeryStrong123!');
      await waitFor(() => {
        expect(screen.getByText(/muito forte/i)).toBeInTheDocument();
      });
    });
  });
});
