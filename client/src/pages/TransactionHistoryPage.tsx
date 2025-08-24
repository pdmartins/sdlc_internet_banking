import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import TransactionHistory from '../components/TransactionHistory';

const TransactionHistoryPage: React.FC = () => {
    const { session, isLoading: authLoading } = useAuth();
    const navigate = useNavigate();

    // Show loading while checking authentication
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

    // Redirect to login if not authenticated
    if (!session?.isAuthenticated) {
        return (
            <div className="form-container">
                <div className="form-card">
                    <h2 className="form-title">Acesso Negado</h2>
                    <p className="form-description">
                        Você precisa estar logado para acessar o histórico de transações.
                    </p>
                    <button
                        onClick={() => navigate('/login')}
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
                {/* Dashboard Header */}
                <div className="dashboard-header">
                    <h1 className="form-title">Histórico de Transações</h1>
                    <p className="form-description">
                        Olá, {session.fullName || session.email}! Visualize e exporte o histórico completo das suas transações.
                    </p>
                    <div className="dashboard-nav">
                        <button
                            onClick={() => navigate('/dashboard')}
                            className="btn btn-secondary btn-small"
                        >
                            ← Voltar ao Dashboard
                        </button>
                    </div>
                </div>
                
                <TransactionHistory />
            </div>
        </div>
    );
};

export default TransactionHistoryPage;
