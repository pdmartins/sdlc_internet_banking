import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { transactionApi, TransactionResponse } from '../services/transactionApi';

const Dashboard: React.FC = () => {
  const { session, isLoading: authLoading } = useAuth();
  const [balance, setBalance] = useState<number>(0);
  const [recentTransactions, setRecentTransactions] = useState<TransactionResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Load recent transactions (last 5)
      const transactions = await transactionApi.getTransactionHistory(1, 5);
      setRecentTransactions(Array.isArray(transactions) ? transactions : []);

      // Calculate current balance from the latest transaction
      if (Array.isArray(transactions) && transactions.length > 0) {
        setBalance(transactions[0].balanceAfter);
      } else {
        // Default balance for new accounts
        setBalance(1000.00); // Demo balance
      }
    } catch (err) {
      console.error('Error loading dashboard data:', err);
      setError('Erro ao carregar informações da conta');
      // Set demo data on error
      setBalance(1000.00);
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
            <div className="action-card" onClick={() => window.location.href = '/transaction'}>
              <div className="action-icon">💸</div>
              <h4>Nova Transação</h4>
              <p>Transferir ou depositar dinheiro</p>
            </div>
            <div className="action-card" onClick={() => window.location.href = '/transaction/history'}>
              <div className="action-icon">📊</div>
              <h4>Histórico</h4>
              <p>Ver todas as transações</p>
            </div>
            <div className="action-card" onClick={() => window.location.href = '/profile'}>
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
              onClick={() => window.location.href = '/transaction/history'}
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
              {recentTransactions.map((transaction) => (
                <div key={transaction.id} className="table-row">
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