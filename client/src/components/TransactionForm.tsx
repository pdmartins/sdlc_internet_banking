import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { transactionApi, TransactionRequest } from '../services/transactionApi';

interface TransactionFormProps {}

interface TransactionFormData {
  type: 'CREDIT' | 'DEBIT';
  amount: string;
  recipientAccount?: string;
  recipientName?: string;
  description: string;
}

interface TransactionFormErrors {
  amount?: string;
  recipientAccount?: string;
  recipientName?: string;
  description?: string;
}

const TransactionForm: React.FC<TransactionFormProps> = () => {
  const { session } = useAuth();
  const navigate = useNavigate();
  const [formData, setFormData] = useState<TransactionFormData>({
    type: 'CREDIT',
    amount: '',
    recipientAccount: '',
    recipientName: '',
    description: ''
  });
  const [errors, setErrors] = useState<TransactionFormErrors>({});
  const [isLoading, setIsLoading] = useState(false);
  const [transactionError, setTransactionError] = useState<string | null>(null);

  // Redirect if not authenticated
  if (!session?.isAuthenticated) {
    return (
      <div className="form-container">
        <div className="form-card">
          <h2 className="form-title">Acesso Negado</h2>
          <p className="form-description">Você precisa estar logado para acessar esta funcionalidade.</p>
          <button
            onClick={() => window.location.href = '/login'}
            className="btn btn-primary"
          >
            Fazer Login
          </button>
        </div>
      </div>
    );
  }

  const validateForm = (): boolean => {
    const newErrors: TransactionFormErrors = {};

    // Validate amount
    const amount = parseFloat(formData.amount);
    if (!formData.amount) {
      newErrors.amount = 'Valor é obrigatório';
    } else if (isNaN(amount) || amount <= 0) {
      newErrors.amount = 'Valor deve ser um número positivo';
    } else if (amount > 10000) {
      newErrors.amount = 'Valor máximo por transação é R$ 10.000,00';
    }

    // Validate recipient account for debit transactions
    if (formData.type === 'DEBIT') {
      if (!formData.recipientAccount?.trim()) {
        newErrors.recipientAccount = 'Conta de destino é obrigatória para transferências';
      } else if (!/^\d{4}-\d{6}-\d{1}$/.test(formData.recipientAccount)) {
        newErrors.recipientAccount = 'Formato inválido. Use: 1234-567890-1';
      }
    }

    // Validate description
    if (!formData.description.trim()) {
      newErrors.description = 'Descrição é obrigatória';
    } else if (formData.description.length < 3) {
      newErrors.description = 'Descrição deve ter pelo menos 3 caracteres';
    } else if (formData.description.length > 100) {
      newErrors.description = 'Descrição deve ter no máximo 100 caracteres';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    
    // Clear errors when user starts typing
    if (errors[name as keyof TransactionFormErrors]) {
      setErrors(prev => ({ ...prev, [name]: undefined }));
    }
    
    // Clear transaction error when user modifies the form
    if (transactionError) {
      setTransactionError(null);
    }

    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleTypeChange = (type: 'CREDIT' | 'DEBIT') => {
    setFormData(prev => ({ 
      ...prev, 
      type,
      // Clear recipient fields when switching to credit
      recipientAccount: type === 'CREDIT' ? '' : prev.recipientAccount,
      recipientName: type === 'CREDIT' ? '' : prev.recipientName
    }));
    
    // Clear recipient field errors when switching to credit
    if (type === 'CREDIT' && (errors.recipientAccount || errors.recipientName)) {
      setErrors(prev => ({ 
        ...prev, 
        recipientAccount: undefined,
        recipientName: undefined 
      }));
    }
    
    // Clear transaction error when changing type
    if (transactionError) {
      setTransactionError(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setIsLoading(true);
    setTransactionError(null); // Clear any previous errors
    
    try {
      // Map transaction type to category
      const category = formData.type === 'CREDIT' ? 'DEPOSIT' : 'TRANSFER';
      
      const transactionData: TransactionRequest = {
        type: formData.type,
        category: category,
        amount: parseFloat(formData.amount),
        description: formData.description,
        recipientAccount: formData.type === 'DEBIT' ? formData.recipientAccount : undefined,
        recipientName: formData.type === 'DEBIT' ? formData.recipientName : undefined,
      };
      
      console.log('Sending transaction data:', transactionData);
      
      // Call the real API
      const response = await transactionApi.createTransaction(transactionData);
      
      console.log('Transaction response:', response);
      
      // Navigate to dashboard with success state
      navigate('/dashboard', { 
        state: { 
          transactionSuccess: true,
          transactionId: response.transactionId,
          transactionType: formData.type,
          transactionAmount: parseFloat(formData.amount)
        }
      });
      
    } catch (error) {
      console.error('Transaction error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido';
      
      // Check for specific error types and provide user-friendly messages
      if (errorMessage.toLowerCase().includes('insufficient') || 
          errorMessage.toLowerCase().includes('saldo insuficiente') ||
          errorMessage.toLowerCase().includes('funds')) {
        setTransactionError('Saldo insuficiente para realizar esta transação. Verifique seu saldo atual e tente novamente com um valor menor.');
      } else if (errorMessage.toLowerCase().includes('limit') || 
                 errorMessage.toLowerCase().includes('limite')) {
        setTransactionError('Transação excede o limite permitido. Verifique os limites da sua conta ou entre em contato conosco.');
      } else if (errorMessage.toLowerCase().includes('invalid account') || 
                 errorMessage.toLowerCase().includes('conta inválida')) {
        setTransactionError('Conta de destino inválida. Verifique o número da conta e tente novamente.');
      } else {
        setTransactionError(`Erro ao processar transação: ${errorMessage}`);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="dashboard-wrapper">
        {/* Header */}
        <div className="dashboard-header">
          <h1 className="form-title">Nova Transação</h1>
          <p className="form-description">Realize depósitos, transferências e pagamentos de forma segura.</p>
        </div>

        {/* Transaction Form Card */}
        <div className="transaction-form-card">
          <div className="card-header">
            <h3>Detalhes da Transação</h3>
          </div>
          
          {/* Transaction Error Display */}
          {transactionError && (
            <div className="transaction-error-alert">
              <div className="error-icon">❌</div>
              <div className="error-content">
                <h4>Erro na Transação</h4>
                <p>{transactionError}</p>
                <button 
                  onClick={() => setTransactionError(null)}
                  className="btn btn-secondary btn-small"
                >
                  Tentar Novamente
                </button>
              </div>
            </div>
          )}
          
          <form onSubmit={handleSubmit}>
            {/* Transaction Type Selection */}
            <div className="form-section">
              <label className="form-label">
                Tipo de Transação
              </label>
              <div className="transaction-type-cards">
                <label className={`transaction-type-card ${formData.type === 'CREDIT' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="type"
                    value="CREDIT"
                    checked={formData.type === 'CREDIT'}
                    onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                  />
                  <div className="type-content">
                    <div className="type-icon">💰</div>
                    <div className="type-info">
                      <h4>Depósito / Crédito</h4>
                      <p>Adicionar dinheiro à conta</p>
                    </div>
                  </div>
                </label>
                <label className={`transaction-type-card ${formData.type === 'DEBIT' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="type"
                    value="DEBIT"
                    checked={formData.type === 'DEBIT'}
                    onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                  />
                  <div className="type-content">
                    <div className="type-icon">📤</div>
                    <div className="type-info">
                      <h4>Transferência / Débito</h4>
                      <p>Enviar dinheiro para outra conta</p>
                    </div>
                  </div>
                </label>
              </div>
            </div>

            <div className="form-grid">
              {/* Amount Input */}
              <div className="form-group">
                <label htmlFor="amount" className="form-label">
                  Valor (R$)
                </label>
                <input
                  type="number"
                  id="amount"
                  name="amount"
                  step="0.01"
                  min="0"
                  max="10000"
                  value={formData.amount}
                  onChange={handleInputChange}
                  placeholder="0,00"
                  className={`form-input ${errors.amount ? 'error' : ''}`}
                  required
                />
                {errors.amount && (
                  <p className="error-message">{errors.amount}</p>
                )}
              </div>

              {/* Recipient Account (only for debit transactions) */}
              {formData.type === 'DEBIT' && (
                <>
                  <div className="form-group">
                    <label htmlFor="recipientAccount" className="form-label">
                      Conta de Destino
                    </label>
                    <input
                      type="text"
                      id="recipientAccount"
                      name="recipientAccount"
                      value={formData.recipientAccount}
                      onChange={handleInputChange}
                      placeholder="1234-567890-1"
                      className={`form-input ${errors.recipientAccount ? 'error' : ''}`}
                      required
                    />
                    {errors.recipientAccount && (
                      <p className="error-message">{errors.recipientAccount}</p>
                    )}
                    <p className="input-hint">
                      Formato: agência-conta-dígito (ex: 1234-567890-1)
                    </p>
                  </div>
                  
                  <div className="form-group">
                    <label htmlFor="recipientName" className="form-label">
                      Nome do Destinatário
                    </label>
                    <input
                      type="text"
                      id="recipientName"
                      name="recipientName"
                      value={formData.recipientName}
                      onChange={handleInputChange}
                      placeholder="Nome completo do destinatário"
                      className={`form-input ${errors.recipientName ? 'error' : ''}`}
                    />
                    {errors.recipientName && (
                      <p className="error-message">{errors.recipientName}</p>
                    )}
                    <p className="input-hint">
                      Nome como registrado na conta bancária
                    </p>
                  </div>
                </>
              )}
            </div>

            {/* Description */}
            <div className="form-group">
              <label htmlFor="description" className="form-label">
                Descrição
              </label>
              <textarea
                id="description"
                name="description"
                rows={3}
                value={formData.description}
                onChange={handleInputChange}
                placeholder="Descreva o motivo da transação..."
                className={`form-input ${errors.description ? 'error' : ''}`}
                required
              />
              {errors.description && (
                <p className="error-message">{errors.description}</p>
              )}
              <p className="input-hint">
                {formData.description.length}/100 caracteres
              </p>
            </div>

            {/* Action Buttons */}
            <div className="form-actions">
              <button
                type="submit"
                disabled={isLoading}
                className={`btn btn-primary ${isLoading ? 'loading' : ''}`}
              >
                {isLoading ? 'Processando...' : 'Processar Transação'}
              </button>
              <button
                type="button"
                onClick={() => window.history.back()}
                className="btn btn-secondary"
              >
                Cancelar
              </button>
            </div>
          </form>
        </div>

        {/* Information Card */}
        <div className="transaction-info-card">
          <div className="card-header">
            <h3>💡 Informações Importantes</h3>
          </div>
          <div className="info-grid">
            <div className="info-item">
              <div className="info-icon">💰</div>
              <div className="info-content">
                <h4>Limite por Transação</h4>
                <p>Valor máximo: R$ 10.000,00</p>
              </div>
            </div>
            <div className="info-item">
              <div className="info-icon">⚡</div>
              <div className="info-content">
                <h4>Processamento</h4>
                <p>Transações em tempo real</p>
              </div>
            </div>
            <div className="info-item">
              <div className="info-icon">🔐</div>
              <div className="info-content">
                <h4>Segurança</h4>
                <p>Verifique os dados antes de confirmar</p>
              </div>
            </div>
            <div className="info-item">
              <div className="info-icon">📝</div>
              <div className="info-content">
                <h4>Descrição</h4>
                <p>Mantenha clara e detalhada</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionForm;
