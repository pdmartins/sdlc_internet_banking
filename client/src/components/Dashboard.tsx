import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { transactionApi, TransactionResponse } from '../services/transactionApi';

const Dashboard: React.FC = () => {
  const { session, isLoading: authLoading } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [balance, setBalance] = useState<number>(0);
  const [recentTransactions, setRecentTransactions] = useState<TransactionResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showSuccessMessage, setShowSuccessMessage] = useState(false);

  useEffect(() => {
    console.log('Dashboard - Auth state:', { 
      authLoading, 
      sessionExists: !!session, 
      isAuthenticated: session?.isAuthenticated,
      userEmail: session?.email 
    });
    
    if (session?.isAuthenticated) {
      loadDashboardData();
    }
  }, [session, authLoading]);

  // Handle transaction success state from navigation
  useEffect(() => {
    const state = location.state as any;
    if (state?.transactionSuccess) {
      setShowSuccessMessage(true);
      // Clear the state from history to prevent showing message on refresh
      window.history.replaceState({}, document.title);
      
      // Auto-hide success message after 5 seconds
      const timer = setTimeout(() => {
        setShowSuccessMessage(false);
      }, 5000);
      
      return () => clearTimeout(timer);
    }
  }, [location.state]);

  // Helper function to validate balance calculation (for debugging)
  const validateBalance = (transactions: TransactionResponse[]): number => {
    if (!transactions || transactions.length === 0) {
      return 0.00; // Default starting balance
    }

    // Sort transactions by date (oldest first for calculation)
    const sortedTransactions = [...transactions].sort((a, b) => 
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    );

    // Calculate balance by applying each transaction sequentially
    let calculatedBalance = 0.00; // Starting balance
    
    for (const transaction of sortedTransactions) {
      if (transaction.type.toLowerCase() === 'credit') {
        calculatedBalance += transaction.amount;
      } else if (transaction.type.toLowerCase() === 'debit') {
        calculatedBalance -= transaction.amount;
      }
      
      console.log(`Transaction ${transaction.transactionId}: ${transaction.type} ${transaction.amount}, Balance: ${calculatedBalance}`);
    }

    return calculatedBalance;
  };

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Load recent transactions (last 5)
      const transactions = await transactionApi.getTransactionHistory(1, 5);
      console.log('Dashboard - Loaded transactions:', transactions);
      
      // Check for duplicate IDs
      const ids = transactions.map(t => t.transactionId);
      const uniqueIds = new Set(ids);
      if (ids.length !== uniqueIds.size) {
        console.warn('Dashboard - Duplicate transaction IDs found:', ids);
      }
      
      setRecentTransactions(Array.isArray(transactions) ? transactions : []);

      // Calculate current balance from the latest transaction
      if (Array.isArray(transactions) && transactions.length > 0) {
        // Use the balance from the most recent transaction (should be first due to ordering)
        const currentBalance = transactions[0].balanceAfter;
        setBalance(currentBalance);
        
        // Validate balance calculation (for debugging)
        console.log('Dashboard - Current balance from latest transaction:', currentBalance);
        console.log('Dashboard - Latest transaction:', transactions[0]);
        
        // Optional: Validate by calculating balance from all transactions
        // This is just for debugging - in production you'd trust the balanceAfter field
        const calculatedBalance = validateBalance(transactions);
        if (Math.abs(calculatedBalance - currentBalance) > 0.01) {
          console.warn('Dashboard - Balance mismatch detected!', {
            reportedBalance: currentBalance,
            calculatedBalance: calculatedBalance,
            difference: Math.abs(calculatedBalance - currentBalance)
          });
        }
      } else {
        // Default balance for new accounts
        setBalance(0.00); // Starting balance with no transactions
        console.log('Dashboard - Using default balance (no transactions)');
      }
    } catch (err) {
      console.error('Error loading dashboard data:', err);
      setError('Erro ao carregar informações da conta');
      // Set default balance on error (should be 0 for new accounts)
      setBalance(0.00);
      setRecentTransactions([]);
    } finally {
      setIsLoading(false);
    }
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(amount);
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getTransactionTypeDisplay = (type: string): string => {
    switch (type.toLowerCase()) {
      case 'credit':
        return 'Crédito';
      case 'debit':
        return 'Débito';
      default:
        return type;
    }
  };

  const getTransactionIcon = (type: string): string => {
    switch (type.toLowerCase()) {
      case 'credit':
        return '↗️';
      case 'debit':
        return '↙️';
      default:
        return '💰';
    }
  };

  if (authLoading) {
    return (
      <div className="form-container">
        <div className="form-card">
          <h2 className="form-title">Carregando...</h2>
          <p className="form-description">Verificando suas credenciais...</p>
        </div>
      </div>
    );
  }

  if (!session?.isAuthenticated) {
    return (
      <div className="form-container">
        <div className="form-card">
          <h2 className="form-title">Acesso Negado</h2>
          <p className="form-description">Você precisa estar logado para acessar o dashboard.</p>
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

  return (
    <div className="form-container">
      <div className="dashboard-wrapper">
        {/* Header */}
        <div className="dashboard-header">
          <h1 className="form-title">Dashboard</h1>
          <p className="form-description">Olá, {session.fullName || session.email}! Bem-vindo ao seu painel de controle.</p>
        </div>

        {/* Transaction Success Message */}
        {showSuccessMessage && (
          <div className="success-alert">
            <div className="success-content">
              <div className="success-icon">✅</div>
              <div className="success-text">
                <h4>Transação Realizada com Sucesso!</h4>
                <p>Sua transação foi processada e os dados foram atualizados.</p>
              </div>
              <button 
                onClick={() => setShowSuccessMessage(false)}
                className="alert-close-btn"
                aria-label="Fechar mensagem"
              >
                ×
              </button>
            </div>
          </div>
        )}

        {error && (
          <div className="error-alert">
            <p>{error}</p>
            <button onClick={loadDashboardData} className="btn btn-secondary btn-small">
              Tentar Novamente
            </button>
          </div>
        )}

        {/* Account Balance Card */}
        <div className="balance-card">
          <div className="card-header">
            <h3>Saldo da Conta</h3>
            <button onClick={loadDashboardData} className="refresh-button" title="Atualizar">
              🔄
            </button>
          </div>
          <div className="balance-content">
            {isLoading ? (
              <div className="loading-balance">Carregando...</div>
            ) : (
              <div className="balance-amount">{formatCurrency(balance)}</div>
            )}
            <p className="balance-subtitle">Saldo disponível</p>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="quick-actions">
          <h3 className="section-title">Ações Rápidas</h3>
          <div className="action-cards">
            <div className="action-card" onClick={() => navigate('/transaction')}>
              <div className="action-icon">💸</div>
              <h4>Nova Transação</h4>
              <p>Transferir ou depositar dinheiro</p>
            </div>
            <div className="action-card" onClick={() => navigate('/transaction/history')}>
              <div className="action-icon">📊</div>
              <h4>Histórico</h4>
              <p>Ver todas as transações</p>
            </div>
            <div className="action-card" onClick={() => navigate('/profile')}>
              <div className="action-icon">⚙️</div>
              <h4>Configurações</h4>
              <p>Gerenciar sua conta</p>
            </div>
          </div>
        </div>

        {/* Recent Transactions */}
        <div className="recent-transactions">
          <div className="section-header">
            <h3 className="section-title">Transações Recentes</h3>
            <button 
              onClick={() => navigate('/transaction/history')}
              className="btn btn-secondary btn-small"
            >
              Ver Todas
            </button>
          </div>
          
          {isLoading ? (
            <div className="loading-transactions">
              <p>Carregando transações...</p>
            </div>
          ) : recentTransactions.length > 0 ? (
            <div className="transactions-table">
              <div className="table-header">
                <div className="col-date">Data</div>
                <div className="col-type">Tipo</div>
                <div className="col-description">Descrição</div>
                <div className="col-amount">Valor</div>
                <div className="col-status">Status</div>
              </div>
              {recentTransactions.map((transaction, index) => (
                <div key={transaction.transactionId || `transaction-${index}`} className="table-row">
                  <div className="col-date">
                    <span className="transaction-icon">{getTransactionIcon(transaction.type)}</span>
                    {formatDate(transaction.createdAt)}
                  </div>
                  <div className="col-type">
                    {getTransactionTypeDisplay(transaction.type)}
                  </div>
                  <div className="col-description">
                    {transaction.description}
                    {transaction.recipientAccount && (
                      <div className="recipient-info">Para: {transaction.recipientAccount}</div>
                    )}
                  </div>
                  <div className={`col-amount ${transaction.type.toLowerCase() === 'credit' ? 'credit' : 'debit'}`}>
                    {transaction.type.toLowerCase() === 'credit' ? '+' : '-'}{formatCurrency(Math.abs(transaction.amount))}
                  </div>
                  <div className="col-status">
                    <span className={`status-badge ${transaction.status.toLowerCase()}`}>
                      {transaction.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="empty-transactions">
              <div className="empty-icon">📭</div>
              <h4>Nenhuma transação encontrada</h4>
              <p>Você ainda não realizou nenhuma transação.</p>
              <button 
                onClick={() => window.location.href = '/transaction'}
                className="btn btn-primary"
              >
                Fazer Primeira Transação
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;