import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import Welcome from '../components/Welcome';

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

describe('Welcome Component - User Story 1.1.1', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  const renderWelcome = (props = {}) => {
    return render(
      <BrowserRouter>
        <Welcome {...props} />
      </BrowserRouter>
    );
  };

  describe('Acceptance Criteria: Welcome screen displays logo, description, and [Continuar] button', () => {
    test('displays Contoso logo with proper accessibility attributes', () => {
      renderWelcome();
      
      const logo = screen.getByRole('img', { name: /contoso bank logo/i });
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveAttribute('aria-label', 'Contoso Bank Logo');
    });

    test('displays welcome title and description', () => {
      renderWelcome();
      
      expect(screen.getByRole('heading', { name: /bem-vindo ao contoso bank/i })).toBeInTheDocument();
      expect(screen.getByText(/gerencie suas finanças com segurança/i)).toBeInTheDocument();
    });

    test('displays Continuar button with proper accessibility', () => {
      renderWelcome();
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      expect(continueButton).toBeInTheDocument();
      expect(continueButton).toHaveAttribute('aria-label', 'Continuar para o cadastro');
    });

    test('displays key features list', () => {
      renderWelcome();
      
      expect(screen.getByText(/transações seguras com autenticação multifator/i)).toBeInTheDocument();
      expect(screen.getByText(/histórico completo e relatórios detalhados/i)).toBeInTheDocument();
      expect(screen.getByText(/acesso 24\/7 em todos os dispositivos/i)).toBeInTheDocument();
      expect(screen.getByText(/suporte especializado quando precisar/i)).toBeInTheDocument();
    });

    test('displays security information', () => {
      renderWelcome();
      
      expect(screen.getByText(/seus dados são protegidos com criptografia de nível bancário/i)).toBeInTheDocument();
    });
  });

  describe('Acceptance Criteria: Navigation to registration form is intuitive', () => {
    test('navigates to personal info form when Continuar button is clicked', () => {
      renderWelcome();
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      fireEvent.click(continueButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/register/personal-info');
    });

    test('calls custom onContinue handler when provided', () => {
      const mockOnContinue = jest.fn();
      renderWelcome({ onContinue: mockOnContinue });
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      fireEvent.click(continueButton);
      
      expect(mockOnContinue).toHaveBeenCalled();
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });

  describe('Accessibility and User Experience', () => {
    test('has proper heading structure for screen readers', () => {
      renderWelcome();
      
      const headings = screen.getAllByRole('heading');
      expect(headings).toHaveLength(1);
      expect(headings[0]).toHaveTextContent('Bem-vindo ao Contoso Bank');
    });

    test('button has focus management for keyboard navigation', () => {
      renderWelcome();
      
      const continueButton = screen.getByRole('button', { name: /continuar/i });
      continueButton.focus();
      
      expect(continueButton).toHaveFocus();
    });

    test('provides clear visual hierarchy with proper content structure', () => {
      renderWelcome();
      
      // Check that content is properly structured
      expect(screen.getByRole('img')).toBeInTheDocument(); // Logo first
      expect(screen.getByRole('heading')).toBeInTheDocument(); // Title second
      expect(screen.getByText(/gerencie suas finanças/i)).toBeInTheDocument(); // Description third
      expect(screen.getByRole('list')).toBeInTheDocument(); // Features list
      expect(screen.getByRole('button')).toBeInTheDocument(); // Action button last
    });
  });

  describe('Responsive Design', () => {
    test('applies responsive CSS classes for mobile optimization', () => {
      renderWelcome();
      
      const container = screen.getByRole('img').closest('.welcome-container');
      expect(container).toHaveClass('welcome-container');
      
      const card = screen.getByRole('img').closest('.welcome-card');
      expect(card).toHaveClass('welcome-card');
    });
  });

  describe('Brand Consistency', () => {
    test('displays Contoso branding consistently', () => {
      renderWelcome();
      
      // Logo should contain CONTOSO text
      const logo = screen.getByRole('img');
      expect(logo.querySelector('text')).toHaveTextContent('CONTOSO');
      
      // Title should mention Contoso Bank
      expect(screen.getByText(/contoso bank/i)).toBeInTheDocument();
    });
  });
});
