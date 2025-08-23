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
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Acesso Negado</h2>
          <p className="text-gray-600 mb-4">Você precisa estar logado para acessar esta funcionalidade.</p>
          <button
            onClick={() => window.location.href = '/login'}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
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
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-2xl mx-auto px-4">
        <div className="bg-white rounded-lg shadow-md p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Nova Transação</h1>
          
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Transaction Type Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-3">
                Tipo de Transação
              </label>
              <div className="flex space-x-4">
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="type"
                    value="CREDIT"
                    checked={formData.type === 'CREDIT'}
                    onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                  />
                  <span className="ml-2 text-sm text-gray-700">Depósito / Crédito</span>
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="type"
                    value="DEBIT"
                    checked={formData.type === 'DEBIT'}
                    onChange={(e) => handleTypeChange(e.target.value as 'CREDIT' | 'DEBIT')}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                  />
                  <span className="ml-2 text-sm text-gray-700">Transferência / Débito</span>
                </label>
              </div>
            </div>

            {/* Amount Input */}
            <div>
              <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
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
                className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                  errors.amount ? 'border-red-500' : 'border-gray-300'
                }`}
                required
              />
              {errors.amount && (
                <p className="mt-1 text-sm text-red-600">{errors.amount}</p>
              )}
            </div>

            {/* Recipient Account (only for debit transactions) */}
            {formData.type === 'DEBIT' && (
              <div>
                <label htmlFor="recipientAccount" className="block text-sm font-medium text-gray-700 mb-1">
                  Conta de Destino
                </label>
                <input
                  type="text"
                  id="recipientAccount"
                  name="recipientAccount"
                  value={formData.recipientAccount}
                  onChange={handleInputChange}
                  placeholder="1234-567890-1"
                  className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.recipientAccount ? 'border-red-500' : 'border-gray-300'
                  }`}
                  required
                />
                {errors.recipientAccount && (
                  <p className="mt-1 text-sm text-red-600">{errors.recipientAccount}</p>
                )}
                <p className="mt-1 text-xs text-gray-500">
                  Formato: agência-conta-dígito (ex: 1234-567890-1)
                </p>
              </div>
            )}

            {/* Description */}
            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                Descrição
              </label>
              <textarea
                id="description"
                name="description"
                rows={3}
                value={formData.description}
                onChange={handleInputChange}
                placeholder="Descreva o motivo da transação..."
                className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                  errors.description ? 'border-red-500' : 'border-gray-300'
                }`}
                required
              />
              {errors.description && (
                <p className="mt-1 text-sm text-red-600">{errors.description}</p>
              )}
              <p className="mt-1 text-xs text-gray-500">
                {formData.description.length}/100 caracteres
              </p>
            </div>

            {/* Action Buttons */}
            <div className="flex space-x-4">
              <button
                type="submit"
                disabled={isLoading}
                className={`flex-1 py-2 px-4 rounded-md text-white font-medium focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                  isLoading 
                    ? 'bg-gray-400 cursor-not-allowed' 
                    : 'bg-blue-600 hover:bg-blue-700'
                }`}
              >
                {isLoading ? 'Processando...' : 'Continuar'}
              </button>
              <button
                type="button"
                onClick={() => window.history.back()}
                className="flex-1 py-2 px-4 rounded-md border border-gray-300 text-gray-700 font-medium hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                Cancelar
              </button>
            </div>
          </form>

          {/* Information Card */}
          <div className="mt-6 p-4 bg-blue-50 rounded-md">
            <h3 className="text-sm font-medium text-blue-900 mb-2">Informações Importantes:</h3>
            <ul className="text-xs text-blue-800 space-y-1">
              <li>• Valor máximo por transação: R$ 10.000,00</li>
              <li>• Transações são processadas em tempo real</li>
              <li>• Mantenha sua descrição clara e detalhada</li>
              <li>• Verifique os dados antes de confirmar</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionForm;
