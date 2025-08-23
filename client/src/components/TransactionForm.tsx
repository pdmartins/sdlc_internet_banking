import React, { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { transactionApi, TransactionRequest } from '../services/transactionApi';

interface TransactionFormProps {}

interface TransactionFormData {
  type: 'CREDIT' | 'DEBIT';
  amount: string;
  recipientAccount?: string;
  description: string;
}

interface TransactionFormErrors {
  amount?: string;
  recipientAccount?: string;
  description?: string;
}

const TransactionForm: React.FC<TransactionFormProps> = () => {
  const { session } = useAuth();
  const [formData, setFormData] = useState<TransactionFormData>({
    type: 'CREDIT',
    amount: '',
    recipientAccount: '',
    description: ''
  });
  const [errors, setErrors] = useState<TransactionFormErrors>({});
  const [isLoading, setIsLoading] = useState(false);

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
    
    // Clear error when user starts typing
    if (errors[name as keyof TransactionFormErrors]) {
      setErrors(prev => ({ ...prev, [name]: undefined }));
    }

    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleTypeChange = (type: 'CREDIT' | 'DEBIT') => {
    setFormData(prev => ({ 
      ...prev, 
      type,
      // Clear recipient account when switching to credit
      recipientAccount: type === 'CREDIT' ? '' : prev.recipientAccount
    }));
    
    // Clear recipient account error when switching to credit
    if (type === 'CREDIT' && errors.recipientAccount) {
      setErrors(prev => ({ ...prev, recipientAccount: undefined }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setIsLoading(true);
    
    try {
      const transactionData: TransactionRequest = {
        type: formData.type,
        amount: parseFloat(formData.amount),
        recipientAccount: formData.type === 'DEBIT' ? formData.recipientAccount : undefined,
        description: formData.description
      };
      
      console.log('Sending transaction data:', transactionData);
      
      // Call the real API
      const response = await transactionApi.createTransaction(transactionData);
      
      console.log('Transaction response:', response);
      
      alert(`✅ Transação processada com sucesso! ID: ${response.id}`);
      
      // Reset form
      setFormData({
        type: 'CREDIT',
        amount: '',
        recipientAccount: '',
        description: ''
      });
      
    } catch (error) {
      console.error('Transaction error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido';
      alert(`❌ Erro ao processar transação: ${errorMessage}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <div className="form-header">
          <h1 className="form-title">Nova Transação</h1>
        </div>
        
        <form onSubmit={handleSubmit}>
          {/* Transaction Type Selection */}
          <div className="form-section">
            <label className="form-label">
              Tipo de Transação
            </label>
            <div className="radio-group">
              <label className="radio-option">
                <input
                  type="radio"
                  name="type"
                  value="CREDIT"
                  checked={formData.type === 'CREDIT'}
                  onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                />
                <span>Depósito / Crédito</span>
              </label>
              <label className="radio-option">
                <input
                  type="radio"
                  name="type"
                  value="DEBIT"
                  checked={formData.type === 'DEBIT'}
                  onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                />
                <span>Transferência / Débito</span>
              </label>
            </div>
          </div>

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
          )}

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
              {isLoading ? 'Processando...' : 'Continuar'}
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

        {/* Information Card */}
        <div className="info-card">
          <h3 className="info-title">Informações Importantes:</h3>
          <ul className="info-list">
            <li>Valor máximo por transação: R$ 10.000,00</li>
            <li>Transações são processadas em tempo real</li>
            <li>Mantenha sua descrição clara e detalhada</li>
            <li>Verifique os dados antes de confirmar</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default TransactionForm;
