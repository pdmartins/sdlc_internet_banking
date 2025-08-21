import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import PersonalInfoForm from '../components/PersonalInfoForm';

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

describe('PersonalInfoForm Component - User Story 1.1.2', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  const renderPersonalInfoForm = (props = {}) => {
    return render(
      <BrowserRouter>
        <PersonalInfoForm {...props} />
      </BrowserRouter>
    );
  };

  describe('Acceptance Criteria: All fields required', () => {
    test('displays all required form fields', () => {
      renderPersonalInfoForm();
      
      expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/telefone/i)).toBeInTheDocument();
      
      // Check required indicators
      expect(screen.getByText(/nome completo \*/i)).toBeInTheDocument();
      expect(screen.getByText(/email \*/i)).toBeInTheDocument();
      expect(screen.getByText(/telefone \*/i)).toBeInTheDocument();
    });

    test('shows validation errors when fields are empty and touched', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      const emailInput = screen.getByLabelText(/email/i);
      const phoneInput = screen.getByLabelText(/telefone/i);
      
      // Touch fields and blur
      await user.click(nameInput);
      await user.tab();
      await user.click(emailInput);
      await user.tab();
      await user.click(phoneInput);
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/nome completo é obrigatório/i)).toBeInTheDocument();
        expect(screen.getByText(/email é obrigatório/i)).toBeInTheDocument();
        expect(screen.getByText(/telefone é obrigatório/i)).toBeInTheDocument();
      });
    });
  });

  describe('Acceptance Criteria: Format validation', () => {
    test('validates full name format', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      
      // Test invalid characters
      await user.type(nameInput, 'John123');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/nome deve conter apenas letras e espaços/i)).toBeInTheDocument();
      });
      
      // Test valid name
      await user.clear(nameInput);
      await user.type(nameInput, 'João Silva Santos');
      
      await waitFor(() => {
        expect(screen.getByText(/nome válido/i)).toBeInTheDocument();
      });
    });

    test('validates email format', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const emailInput = screen.getByLabelText(/email/i);
      
      // Test invalid email
      await user.type(emailInput, 'invalid-email');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/formato de email inválido/i)).toBeInTheDocument();
      });
      
      // Test valid email
      await user.clear(emailInput);
      await user.type(emailInput, 'joao@exemplo.com');
      
      await waitFor(() => {
        expect(screen.getByText(/email válido/i)).toBeInTheDocument();
      });
    });

    test('validates and formats phone number', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const phoneInput = screen.getByLabelText(/telefone/i);
      
      // Test phone formatting
      await user.type(phoneInput, '11999887766');
      
      await waitFor(() => {
        expect(phoneInput).toHaveValue('(11) 99988-7766');
      });
      
      // Test invalid format
      await user.clear(phoneInput);
      await user.type(phoneInput, '123');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/formato: \(11\) 99999-9999/i)).toBeInTheDocument();
      });
    });
  });

  describe('Acceptance Criteria: Inline error/success feedback', () => {
    test('shows success feedback for valid inputs', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      // Fill valid data
      await user.type(screen.getByLabelText(/nome completo/i), 'João Silva');
      await user.type(screen.getByLabelText(/email/i), 'joao@exemplo.com');
      await user.type(screen.getByLabelText(/telefone/i), '11999887766');
      
      await waitFor(() => {
        expect(screen.getByText(/nome válido/i)).toBeInTheDocument();
        expect(screen.getByText(/email válido/i)).toBeInTheDocument();
        expect(screen.getByText(/telefone válido/i)).toBeInTheDocument();
      });
      
      // Check success icons
      const successIcons = screen.getAllByText('✓');
      expect(successIcons).toHaveLength(3);
    });

    test('shows error feedback with error icons', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      
      await user.type(nameInput, 'A');
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/nome deve ter pelo menos 2 caracteres/i)).toBeInTheDocument();
        expect(screen.getByText('❌')).toBeInTheDocument();
      });
    });

    test('provides real-time validation feedback', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const emailInput = screen.getByLabelText(/email/i);
      
      // Type incomplete email
      await user.type(emailInput, 'test');
      await waitFor(() => {
        expect(screen.getByText(/formato de email inválido/i)).toBeInTheDocument();
      });
      
      // Complete email
      await user.type(emailInput, '@exemplo.com');
      await waitFor(() => {
        expect(screen.getByText(/email válido/i)).toBeInTheDocument();
      });
    });
  });

  describe('Acceptance Criteria: [Continuar] and [Cancelar] buttons', () => {
    test('displays both action buttons', () => {
      renderPersonalInfoForm();
      
      expect(screen.getByRole('button', { name: /continuar/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /cancelar/i })).toBeInTheDocument();
    });

    test('disables Continue button when form is invalid', () => {
      renderPersonalInfoForm();
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      expect(continueButton).toBeDisabled();
    });

    test('enables Continue button when form is valid', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      // Fill valid data
      await user.type(screen.getByLabelText(/nome completo/i), 'João Silva');
      await user.type(screen.getByLabelText(/email/i), 'joao@exemplo.com');
      await user.type(screen.getByLabelText(/telefone/i), '11999887766');
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
    });

    test('navigates to verification page when Continue is clicked', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      // Fill valid data
      await user.type(screen.getByLabelText(/nome completo/i), 'João Silva');
      await user.type(screen.getByLabelText(/email/i), 'joao@exemplo.com');
      await user.type(screen.getByLabelText(/telefone/i), '11999887766');
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
      
      await user.click(continueButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/register/verification');
    });

    test('navigates to welcome page when Cancel is clicked', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const cancelButton = screen.getByRole('button', { name: /cancelar/i });
      await user.click(cancelButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/welcome');
    });

    test('calls custom handlers when provided', async () => {
      const user = userEvent.setup();
      const mockOnSubmit = jest.fn();
      const mockOnCancel = jest.fn();
      
      renderPersonalInfoForm({ 
        onSubmit: mockOnSubmit, 
        onCancel: mockOnCancel 
      });
      
      // Test custom onCancel
      const cancelButton = screen.getByRole('button', { name: /cancelar/i });
      await user.click(cancelButton);
      expect(mockOnCancel).toHaveBeenCalled();
      
      // Test custom onSubmit
      await user.type(screen.getByLabelText(/nome completo/i), 'João Silva');
      await user.type(screen.getByLabelText(/email/i), 'joao@exemplo.com');
      await user.type(screen.getByLabelText(/telefone/i), '11999887766');
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      
      await waitFor(() => {
        expect(continueButton).toBeEnabled();
      });
      
      await user.click(continueButton);
      
      expect(mockOnSubmit).toHaveBeenCalledWith({
        fullName: 'João Silva',
        email: 'joao@exemplo.com',
        phone: '(11) 99988-7766'
      });
    });
  });

  describe('Accessibility and User Experience', () => {
    test('has proper form structure and labels', () => {
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      const emailInput = screen.getByLabelText(/email/i);
      const phoneInput = screen.getByLabelText(/telefone/i);
      
      expect(nameInput).toHaveAttribute('aria-describedby');
      expect(emailInput).toHaveAttribute('aria-describedby');
      expect(phoneInput).toHaveAttribute('aria-describedby');
    });

    test('displays progress indicator', () => {
      renderPersonalInfoForm();
      
      expect(screen.getByText('1')).toBeInTheDocument();
      expect(screen.getByText('2')).toBeInTheDocument();
      expect(screen.getByText('3')).toBeInTheDocument();
      expect(screen.getByText('4')).toBeInTheDocument();
    });

    test('shows helpful field descriptions', () => {
      renderPersonalInfoForm();
      
      expect(screen.getByText(/use seu nome completo como aparece nos documentos/i)).toBeInTheDocument();
      expect(screen.getByText(/usaremos este email para comunicações importantes/i)).toBeInTheDocument();
      expect(screen.getByText(/incluir código de área/i)).toBeInTheDocument();
    });

    test('displays LGPD compliance message', () => {
      renderPersonalInfoForm();
      
      expect(screen.getByText(/suas informações são criptografadas e protegidas de acordo com a lgpd/i)).toBeInTheDocument();
    });
  });

  describe('Form Validation Edge Cases', () => {
    test('handles maximum length validation', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      const longName = 'A'.repeat(101);
      
      await user.type(nameInput, longName);
      await user.tab();
      
      await waitFor(() => {
        expect(screen.getByText(/nome não pode exceder 100 caracteres/i)).toBeInTheDocument();
      });
    });

    test('handles special characters in name validation', async () => {
      const user = userEvent.setup();
      renderPersonalInfoForm();
      
      const nameInput = screen.getByLabelText(/nome completo/i);
      
      // Test accented characters (should be valid)
      await user.type(nameInput, 'José María Ñuñez');
      
      await waitFor(() => {
        expect(screen.getByText(/nome válido/i)).toBeInTheDocument();
      });
    });
  });
});
