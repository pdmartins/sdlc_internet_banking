import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ConfirmationTutorial } from '../components/ConfirmationTutorial';

// Mock useNavigate hook
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

// Helper function to render component with router
const renderWithRouter = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('ConfirmationTutorial Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.clearAllTimers();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('User Story 1.1.4 Acceptance Criteria', () => {
    test('displays success alert with confirmation message', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('Conta criada com sucesso!')).toBeInTheDocument();
      expect(screen.getByText(/Bem-vindo\(a\) ao Contoso Bank/)).toBeInTheDocument();
      expect(screen.getByText('✅')).toBeInTheDocument();
    });

    test('displays masked account summary with user data', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('Resumo da Conta')).toBeInTheDocument();
      expect(screen.getByText('João Silva')).toBeInTheDocument();
      expect(screen.getByText('j***@****.com')).toBeInTheDocument();
      expect(screen.getByText('(11) 9****-****')).toBeInTheDocument();
      expect(screen.getByText('****-1234')).toBeInTheDocument();
      expect(screen.getByText(/Seus dados estão protegidos/)).toBeInTheDocument();
    });

    test('displays tutorial card with navigation and security tips', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('Tutorial de Segurança')).toBeInTheDocument();
      expect(screen.getByText('1 de 5')).toBeInTheDocument();
      expect(screen.getByText('Navegação Segura')).toBeInTheDocument();
      expect(screen.getByText(/Use sempre o menu principal/)).toBeInTheDocument();
    });
  });

  describe('Account Summary Functionality', () => {
    test('allows closing account summary card', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const closeButton = screen.getByLabelText('Fechar resumo da conta');
      fireEvent.click(closeButton);
      
      expect(screen.queryByText('Resumo da Conta')).not.toBeInTheDocument();
    });

    test('displays security notice with encryption information', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('🔐')).toBeInTheDocument();
      expect(screen.getByText(/criptografia de nível bancário/)).toBeInTheDocument();
    });
  });

  describe('Tutorial Navigation', () => {
    test('displays tutorial progress correctly', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('1 de 5')).toBeInTheDocument();
      
      const progressBar = document.querySelector('.progress-fill');
      expect(progressBar).toHaveStyle('width: 20%');
    });

    test('allows manual navigation through tutorial steps', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      // Click next button
      const nextButton = screen.getByText('Próximo');
      fireEvent.click(nextButton);
      
      expect(screen.getByText('2 de 5')).toBeInTheDocument();
      expect(screen.getByText('Verificação de Sessão')).toBeInTheDocument();
      
      // Check previous button is enabled
      const prevButton = screen.getByText('Anterior');
      expect(prevButton).not.toBeDisabled();
    });

    test('previous button is disabled on first step', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const prevButton = screen.getByText('Anterior');
      expect(prevButton).toBeDisabled();
    });

    test('shows finish button on last step', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      // Navigate to last step (step 5)
      const nextButton = screen.getByText('Próximo');
      fireEvent.click(nextButton); // Step 2
      fireEvent.click(nextButton); // Step 3
      fireEvent.click(nextButton); // Step 4
      fireEvent.click(nextButton); // Step 5
      
      expect(screen.getByText('Finalizar Tutorial')).toBeInTheDocument();
      expect(screen.queryByText('Próximo')).not.toBeInTheDocument();
    });

    test('auto-advances tutorial steps every 5 seconds', async () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('1 de 5')).toBeInTheDocument();
      
      // Fast-forward 5 seconds
      jest.advanceTimersByTime(5000);
      
      await waitFor(() => {
        expect(screen.getByText('2 de 5')).toBeInTheDocument();
      });
    });

    test('allows clicking on step indicators to jump to specific steps', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const indicators = document.querySelectorAll('.indicator');
      expect(indicators).toHaveLength(5);
      
      // Click on third indicator (index 2)
      fireEvent.click(indicators[2]);
      
      expect(screen.getByText('3 de 5')).toBeInTheDocument();
      expect(screen.getByText('Monitoramento de Conta')).toBeInTheDocument();
    });
  });

  describe('Tutorial Content', () => {
    test('displays all 5 tutorial steps with correct content', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const steps = [
        { title: 'Navegação Segura', type: 'Navegação' },
        { title: 'Verificação de Sessão', type: 'Segurança' },
        { title: 'Monitoramento de Conta', type: 'Segurança' },
        { title: 'Central de Ajuda', type: 'Funcionalidade' },
        { title: 'Configurações de Segurança', type: 'Segurança' }
      ];

      steps.forEach((step, index) => {
        // Navigate to step
        const indicators = document.querySelectorAll('.indicator');
        fireEvent.click(indicators[index]);
        
        expect(screen.getByText(step.title)).toBeInTheDocument();
        expect(screen.getByText(step.type)).toBeInTheDocument();
      });
    });

    test('displays appropriate icons for each tutorial step', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const expectedIcons = ['🧭', '🔒', '👁️', '💬', '⚙️'];
      
      expectedIcons.forEach((icon, index) => {
        const indicators = document.querySelectorAll('.indicator');
        fireEvent.click(indicators[index]);
        
        expect(screen.getByText(icon)).toBeInTheDocument();
      });
    });
  });

  describe('Navigation Actions', () => {
    test('skip tutorial navigates to dashboard', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const skipButton = screen.getByText('Pular Tutorial');
      fireEvent.click(skipButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
    });

    test('finish tutorial navigates to dashboard', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      // Navigate to last step
      const nextButton = screen.getByText('Próximo');
      fireEvent.click(nextButton);
      fireEvent.click(nextButton);
      fireEvent.click(nextButton);
      fireEvent.click(nextButton);
      
      const finishButton = screen.getByText('Finalizar Tutorial');
      fireEvent.click(finishButton);
      
      expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
    });
  });

  describe('Security Tips Section', () => {
    test('displays security reminders', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('Lembre-se sempre:')).toBeInTheDocument();
      expect(screen.getByText(/Nunca compartilhe suas credenciais/)).toBeInTheDocument();
      expect(screen.getByText(/Use sempre o app oficial/)).toBeInTheDocument();
      expect(screen.getByText(/Reporte imediatamente/)).toBeInTheDocument();
      expect(screen.getByText(/Mantenha seu app sempre atualizado/)).toBeInTheDocument();
    });

    test('displays security tip icons', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByText('🔒')).toBeInTheDocument();
      expect(screen.getByText('📱')).toBeInTheDocument();
      expect(screen.getByText('🚨')).toBeInTheDocument();
      expect(screen.getByText('⚡')).toBeInTheDocument();
    });
  });

  describe('Responsive Design', () => {
    test('renders correctly on mobile viewport', () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      });

      renderWithRouter(<ConfirmationTutorial />);
      
      const container = document.querySelector('.confirmation-tutorial-container');
      expect(container).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    test('has proper ARIA labels and roles', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      expect(screen.getByLabelText('Fechar resumo da conta')).toBeInTheDocument();
      
      const indicators = document.querySelectorAll('.indicator');
      indicators.forEach((indicator, index) => {
        expect(indicator).toHaveAttribute('aria-label', `Ir para passo ${index + 1}`);
      });
    });

    test('supports keyboard navigation', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const skipButton = screen.getByText('Pular Tutorial');
      skipButton.focus();
      expect(document.activeElement).toBe(skipButton);
    });
  });

  describe('Animation and Styling', () => {
    test('applies correct CSS classes for animations', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      const successAlert = document.querySelector('.success-alert');
      const accountSummary = document.querySelector('.account-summary-card');
      const tutorialCard = document.querySelector('.tutorial-card');
      
      expect(successAlert).toHaveClass('success-alert');
      expect(accountSummary).toHaveClass('account-summary-card');
      expect(tutorialCard).toHaveClass('tutorial-card');
    });

    test('shows active and completed indicators correctly', () => {
      renderWithRouter(<ConfirmationTutorial />);
      
      // Navigate to step 3
      const indicators = document.querySelectorAll('.indicator');
      fireEvent.click(indicators[2]);
      
      expect(indicators[0]).toHaveClass('completed');
      expect(indicators[1]).toHaveClass('completed');
      expect(indicators[2]).toHaveClass('active');
      expect(indicators[3]).not.toHaveClass('active');
      expect(indicators[3]).not.toHaveClass('completed');
    });
  });
});
