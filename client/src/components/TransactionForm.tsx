import React, { useState, useEffect } from 'react';
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
  const [currentStep, setCurrentStep] = useState<'input' | 'review' | 'processing' | 'success'>('input');
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
  const [transactionResult, setTransactionResult] = useState<any>(null);
  const [currentBalance, setCurrentBalance] = useState<number | null>(null);

  // Load current balance when component mounts
  useEffect(() => {
    const loadCurrentBalance = async () => {
      if (session?.isAuthenticated) {
        try {
          const transactions = await transactionApi.getTransactionHistory(1, 1);
          if (transactions && transactions.length > 0) {
            setCurrentBalance(transactions[0].balanceAfter);
          } else {
            setCurrentBalance(0.00); // Default balance for new accounts
          }
        } catch (error) {
          console.error('Error loading current balance:', error);
          setCurrentBalance(0.00);
        }
      }
    };
    
    loadCurrentBalance();
  }, [session?.isAuthenticated]);

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

    // Move to review step instead of processing immediately
    setCurrentStep('review');
  };

  const handleConfirmTransaction = async () => {
    setCurrentStep('processing');
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
      
      // Set transaction result for success display
      setTransactionResult({
        ...response,
        transactionType: formData.type,
        transactionAmount: parseFloat(formData.amount),
        previousBalance: currentBalance,
        newBalance: response.balanceAfter
      });
      
      // Update current balance
      setCurrentBalance(response.balanceAfter);
      
      // Show success step
      setCurrentStep('success');
      
    } catch (error) {
      console.error('Transaction error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido';
      
      // Check for specific error types and provide user-friendly messages with enhanced feedback
      if (errorMessage.toLowerCase().includes('insufficient') || 
          errorMessage.toLowerCase().includes('saldo insuficiente') ||
          errorMessage.toLowerCase().includes('funds')) {
        setTransactionError(`Saldo insuficiente para realizar esta transação. Saldo atual: R$ ${currentBalance?.toFixed(2) || '0,00'}. Valor solicitado: R$ ${parseFloat(formData.amount).toFixed(2)}.`);
      } else if (errorMessage.toLowerCase().includes('limit') || 
                 errorMessage.toLowerCase().includes('limite')) {
        setTransactionError('Transação excede o limite permitido. Verifique os limites da sua conta ou entre em contato conosco.');
      } else if (errorMessage.toLowerCase().includes('invalid account') || 
                 errorMessage.toLowerCase().includes('conta inválida')) {
        setTransactionError('Conta de destino inválida. Verifique o número da conta e tente novamente.');
      } else {
        setTransactionError(`Erro ao processar transação: ${errorMessage}`);
      }
      
      // Go back to review step to show error
      setCurrentStep('review');
    } finally {
      setIsLoading(false);
    }
  };

  const handleEditTransaction = () => {
    setCurrentStep('input');
    setTransactionError(null);
  };

  const handleCancel = () => {
    if (currentStep === 'review') {
      setCurrentStep('input');
    } else {
      window.history.back();
    }
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(amount);
  };

  const getTransactionTypeDisplay = (type: string): string => {
    return type === 'CREDIT' ? 'Depósito / Crédito' : 'Transferência / Débito';
  };

  const getTransactionIcon = (type: string): string => {
    return type === 'CREDIT' ? '💰' : '📤';
  };

  // Calculate estimated fee (currently 0 as per requirement)
  const estimatedFee = 0.00;
  const totalAmount = parseFloat(formData.amount) || 0;
  const finalAmount = formData.type === 'DEBIT' ? totalAmount + estimatedFee : totalAmount;

  return (
    <div className="form-container">
      <div className="dashboard-wrapper">
        {/* Header */}
        <div className="dashboard-header">
          <h1 className="form-title">
            {currentStep === 'input' ? 'Nova Transação' : 
             currentStep === 'review' ? 'Revisar Transação' :
             currentStep === 'processing' ? 'Processando Transação' :
             'Transação Concluída'}
          </h1>
          <p className="form-description">
            {currentStep === 'input' 
              ? 'Realize depósitos, transferências e pagamentos de forma segura.'
              : currentStep === 'review' 
              ? 'Revise os detalhes da sua transação antes de confirmar.'
              : currentStep === 'processing'
              ? 'Sua transação está sendo processada, aguarde...'
              : 'Sua transação foi processada com sucesso!'
            }
          </p>
        </div>

        {/* Progress Steps */}
        <div className="progress-steps">
          <div className={`progress-step ${currentStep === 'input' ? 'active' : 'completed'}`}>
            <div className="step-number">1</div>
            <div className="step-label">Detalhes</div>
          </div>
          <div className="progress-line"></div>
          <div className={`progress-step ${currentStep === 'review' ? 'active' : (currentStep === 'processing' || currentStep === 'success') ? 'completed' : ''}`}>
            <div className="step-number">2</div>
            <div className="step-label">Revisão</div>
          </div>
          <div className="progress-line"></div>
          <div className={`progress-step ${currentStep === 'processing' ? 'active' : currentStep === 'success' ? 'completed' : ''}`}>
            <div className="step-number">3</div>
            <div className="step-label">Processando</div>
          </div>
          <div className="progress-line"></div>
          <div className={`progress-step ${currentStep === 'success' ? 'active' : ''}`}>
            <div className="step-number">4</div>
            <div className="step-label">Concluído</div>
          </div>
        </div>

        {/* Transaction Form, Review, Processing, or Success Card */}
        {currentStep === 'input' && (
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
                  className="btn btn-primary"
                >
                  Revisar Transação
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  className="btn btn-secondary"
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Review Step */}
        {currentStep === 'review' && (
          <div className="transaction-review-card">
            <div className="card-header">
              <h3>🔍 Revisão da Transação</h3>
              <p className="review-subtitle">Confira todos os detalhes antes de confirmar</p>
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

            {/* Transaction Summary Card */}
            <div className="transaction-summary">
              <div className="summary-header">
                <div className="transaction-type-display">
                  <div className="type-icon-large">{getTransactionIcon(formData.type)}</div>
                  <div className="type-details">
                    <h4>{getTransactionTypeDisplay(formData.type)}</h4>
                    <p className="transaction-category">
                      {formData.type === 'CREDIT' ? 'Depósito' : 'Transferência'}
                    </p>
                  </div>
                </div>
                <div className="amount-display">
                  <div className={`amount-value ${formData.type.toLowerCase()}`}>
                    {formData.type === 'CREDIT' ? '+' : '-'}{formatCurrency(totalAmount)}
                  </div>
                </div>
              </div>

              <div className="summary-details">
                <div className="detail-row">
                  <span className="detail-label">Valor da Transação:</span>
                  <span className="detail-value">{formatCurrency(totalAmount)}</span>
                </div>
                
                <div className="detail-row">
                  <span className="detail-label">Taxa de Transação:</span>
                  <span className="detail-value">{formatCurrency(estimatedFee)}</span>
                </div>
                
                <div className="detail-row total-row">
                  <span className="detail-label">
                    {formData.type === 'CREDIT' ? 'Valor a Creditar:' : 'Valor Total a Debitar:'}
                  </span>
                  <span className="detail-value total-amount">{formatCurrency(finalAmount)}</span>
                </div>

                {formData.type === 'DEBIT' && formData.recipientAccount && (
                  <>
                    <div className="detail-divider"></div>
                    <div className="detail-row">
                      <span className="detail-label">Conta de Destino:</span>
                      <span className="detail-value">{formData.recipientAccount}</span>
                    </div>
                    {formData.recipientName && (
                      <div className="detail-row">
                        <span className="detail-label">Nome do Destinatário:</span>
                        <span className="detail-value">{formData.recipientName}</span>
                      </div>
                    )}
                  </>
                )}

                <div className="detail-divider"></div>
                <div className="detail-row description-row">
                  <span className="detail-label">Descrição:</span>
                  <span className="detail-value description-text">{formData.description}</span>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="form-actions review-actions">
              <button
                onClick={handleConfirmTransaction}
                disabled={isLoading}
                className={`btn btn-primary btn-large ${isLoading ? 'loading' : ''}`}
              >
                {isLoading ? (
                  <>
                    <span className="loading-spinner"></span>
                    Processando...
                  </>
                ) : (
                  <>
                    ✅ Confirmar Transação
                  </>
                )}
              </button>
              <button
                onClick={handleEditTransaction}
                disabled={isLoading}
                className="btn btn-secondary"
              >
                ✏️ Editar Dados
              </button>
              <button
                onClick={handleCancel}
                disabled={isLoading}
                className="btn btn-tertiary"
              >
                ❌ Cancelar
              </button>
            </div>
          </div>
        )}

        {/* Processing Step */}
        {currentStep === 'processing' && (
          <div className="processing-card">
            <div className="card-header">
              <h3>⏳ Processando Transação</h3>
            </div>
            <div className="processing-content">
              <div className="processing-animation">
                <div className="spinner-large"></div>
              </div>
              <h4>Sua transação está sendo processada</h4>
              <p>Por favor, aguarde enquanto validamos e processamos sua transação. Isso pode levar alguns segundos.</p>
              <div className="processing-details">
                <div className="detail-row">
                  <span>Tipo:</span>
                  <span>{formData.type === 'CREDIT' ? '📈 Crédito' : '📉 Débito'}</span>
                </div>
                <div className="detail-row">
                  <span>Valor:</span>
                  <span>R$ {parseFloat(formData.amount).toFixed(2)}</span>
                </div>
                <div className="detail-row">
                  <span>Status:</span>
                  <span className="status-processing">🔄 Processando...</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Success Step */}
        {currentStep === 'success' && (
          <div className="success-card">
            <div className="success-main-content">
              {/* Success message */}
              <div className="success-message">
                <div className="success-checkmark">
                  ✅
                </div>
                <div className="success-text">
                  <h3>Sua transação foi processada com sucesso!</h3>
                  <p>Todos os dados foram atualizados em tempo real. Você pode visualizar a transação no seu histórico.</p>
                </div>
              </div>

              {/* Transaction Summary */}
              <div className="transaction-summary">
                <h4>📋 Resumo da Transação</h4>
                <div className="summary-details">
                  <div className="detail-row">
                    <span>ID da Transação:</span>
                    <span className="transaction-id">{transactionResult?.transactionId || 'N/A'}</span>
                  </div>
                  <div className="detail-row">
                    <span>Tipo:</span>
                    <span>{formData.type === 'CREDIT' ? 'Crédito' : 'Débito'}</span>
                  </div>
                  <div className="detail-row">
                    <span>Valor:</span>
                    <span className={formData.type === 'CREDIT' ? 'amount-positive' : 'amount-negative'}>
                      {formData.type === 'CREDIT' ? '+' : '-'}R$ {parseFloat(formData.amount).toFixed(2)}
                    </span>
                  </div>
                  <div className="detail-row">
                    <span>Descrição:</span>
                    <span>{formData.description}</span>
                  </div>
                  {formData.type === 'DEBIT' && formData.recipientAccount && (
                    <>
                      <div className="detail-row">
                        <span>Conta Destino:</span>
                        <span>{formData.recipientAccount}</span>
                      </div>
                      <div className="detail-row">
                        <span>Destinatário:</span>
                        <span>{formData.recipientName}</span>
                      </div>
                    </>
                  )}
                </div>
              </div>

              {/* Balance Update */}
              <div className="balance-update">
                <h4>💰 Atualização do Saldo</h4>
                <div className="balance-comparison">
                  <div className="balance-item">
                    <span className="balance-label">Saldo Anterior:</span>
                    <span className="balance-value">
                      R$ {currentBalance ? (currentBalance - parseFloat(formData.amount) * (formData.type === 'CREDIT' ? -1 : 1)).toFixed(2) : '0,00'}
                    </span>
                  </div>
                  <div className="balance-arrow">→</div>
                  <div className="balance-item">
                    <span className="balance-label">Saldo Atual:</span>
                    <span className="balance-value balance-current">
                      R$ {currentBalance?.toFixed(2) || '0,00'}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="success-actions">
              <button
                onClick={() => navigate('/dashboard')}
                className="btn btn-primary btn-large"
              >
                📊 Ver Dashboard
              </button>
              <button
                onClick={() => {
                  setCurrentStep('input');
                  setFormData({
                    type: 'CREDIT',
                    amount: '',
                    recipientAccount: '',
                    recipientName: '',
                    description: ''
                  });
                  setTransactionResult(null);
                  setErrors({});
                  setTransactionError(null);
                }}
                className="btn btn-secondary"
              >
                💸 Nova Transação
              </button>
              <button
                onClick={() => navigate('/transaction/history')}
                className="btn btn-tertiary"
              >
                📋 Ver Histórico
              </button>
            </div>
          </div>
        )}

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
