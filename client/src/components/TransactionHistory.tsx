import React, { useState, useEffect } from 'react';
import { transactionApi, TransactionResponse } from '../services/transactionApi';
import { API_BASE_URL } from '../config/api';
import '../styles/global.css';

export interface Transaction {
    id: string;
    amount: number;
    type: 'CREDIT' | 'DEBIT';
    description: string;
    recipientAccountNumber?: string;
    recipientName?: string;
    timestamp: string;
    status: 'COMPLETED' | 'PENDING' | 'FAILED' | 'CANCELLED';
}

export interface TransactionHistoryFilters {
    startDate?: string;
    endDate?: string;
    transactionType?: 'CREDIT' | 'DEBIT';
    minAmount?: number;
    maxAmount?: number;
    status?: 'COMPLETED' | 'PENDING' | 'FAILED' | 'CANCELLED';
}

export interface ExportRequest {
    format: 'csv' | 'pdf';
    filters: TransactionHistoryFilters;
}

interface TransactionHistoryProps {
    className?: string;
}

const TransactionHistory: React.FC<TransactionHistoryProps> = ({ className = '' }) => {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [pageSize] = useState(10);
    const [exporting, setExporting] = useState(false);

    // Filter states
    const [filters, setFilters] = useState<TransactionHistoryFilters>({
        startDate: '',
        endDate: '',
        transactionType: undefined,
        minAmount: undefined,
        maxAmount: undefined,
        status: undefined
    });

    // UI states
    const [showFilters, setShowFilters] = useState(false);

    useEffect(() => {
        loadTransactions();
    }, [currentPage, filters]);

    const loadTransactions = async () => {
        setLoading(true);
        setError(null);
        
        try {
            const params = {
                page: currentPage,
                pageSize,
                ...filters
            };

            const response = await transactionApi.getTransactionHistoryWithFilters(params);
            
            if (response.success && response.data) {
                // Backend returns transactions array directly, not wrapped in a 'transactions' property
                const transactionArray = Array.isArray(response.data) ? response.data : [];
                
                // Convert TransactionResponse to Transaction format
                const convertedTransactions: Transaction[] = transactionArray.map((tr: TransactionResponse) => ({
                    id: tr.transactionId,
                    amount: tr.amount,
                    type: tr.type as 'CREDIT' | 'DEBIT',
                    description: tr.description,
                    recipientAccountNumber: tr.recipientAccount || '',
                    recipientName: tr.recipientName || '',
                    timestamp: tr.createdAt,
                    status: tr.status.toUpperCase() as 'COMPLETED' | 'PENDING' | 'FAILED' | 'CANCELLED'
                }));
                
                setTransactions(convertedTransactions);
                // Backend doesn't return pagination info in the simple history endpoint
                // For now, calculate based on response size
                const hasMoreData = transactionArray.length === params.pageSize;
                setTotalPages(hasMoreData ? currentPage + 1 : currentPage);
            } else {
                setError('Failed to load transaction history');
            }
        } catch (err) {
            setError('An error occurred while loading transactions');
            console.error('Transaction history error:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleFilterChange = (field: keyof TransactionHistoryFilters, value: any) => {
        setFilters(prev => ({
            ...prev,
            [field]: value
        }));
        setCurrentPage(1); // Reset to first page when filters change
    };

    const clearFilters = () => {
        setFilters({
            startDate: '',
            endDate: '',
            transactionType: undefined,
            minAmount: undefined,
            maxAmount: undefined,
            status: undefined
        });
        setCurrentPage(1);
    };

    const handleExport = async (format: 'csv' | 'pdf') => {
        setExporting(true);
        setError(null);

        try {
            let url: string;
            let filename: string;

            // Build query parameters (only send parameters supported by export endpoints)
            const params = new URLSearchParams();
            if (filters.startDate) params.append('startDate', filters.startDate);
            if (filters.endDate) params.append('endDate', filters.endDate);
            if (filters.transactionType) params.append('type', filters.transactionType);
            // Note: Export endpoints don't support minAmount, maxAmount, and status filters yet

            if (format === 'csv') {
                url = `${API_BASE_URL}/api/transactions/export/csv?${params.toString()}`;
                filename = `transactions_${new Date().toISOString().split('T')[0]}.csv`;
            } else {
                url = `${API_BASE_URL}/api/transactions/export/pdf?${params.toString()}`;
                filename = `transactions_${new Date().toISOString().split('T')[0]}.pdf`;
            }

            // Get authentication token the same way as transactionApi
            const userData = localStorage.getItem('user');
            const token = userData ? JSON.parse(userData)?.token : null;

            // Create download link
            const response = await fetch(url, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': token ? `Bearer ${token}` : ''
                }
            });

            if (!response.ok) {
                throw new Error(`Export failed: ${response.statusText}`);
            }

            const blob = await response.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);

        } catch (err) {
            setError(`Export failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
            console.error('Export error:', err);
        } finally {
            setExporting(false);
        }
    };

    const formatCurrency = (amount: number): string => {
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL'
        }).format(amount);
    };

    const formatDate = (dateString: string): string => {
        const date = new Date(dateString);
        return date.toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    return (
        <div className="form-container">
            <div className="dashboard-wrapper">
                {/* Error Display */}
                {error && (
                    <div className="error-alert">
                        <div className="error-content">
                            <div className="error-icon">❌</div>
                            <div className="error-text">
                                <strong>Erro:</strong> {error}
                            </div>
                        </div>
                    </div>
                )}

                {/* Filters Section */}
                <div className="recent-transactions">
                    <div className="section-header">
                        <h3 className="section-title">Filtros de Pesquisa</h3>
                        <div className="export-buttons">
                            <button
                                onClick={() => setShowFilters(!showFilters)}
                                className="btn btn-secondary btn-small"
                            >
                                {showFilters ? 'Ocultar Filtros' : 'Mostrar Filtros'}
                            </button>
                        </div>
                    </div>

                    {showFilters && (
                        <div className="form-section">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label htmlFor="startDate" className="form-label">Data Inicial</label>
                                    <input
                                        type="date"
                                        id="startDate"
                                        value={filters.startDate}
                                        onChange={(e) => handleFilterChange('startDate', e.target.value)}
                                        className="form-input"
                                    />
                                </div>
                                <div className="form-group">
                                    <label htmlFor="endDate" className="form-label">Data Final</label>
                                    <input
                                        type="date"
                                        id="endDate"
                                        value={filters.endDate}
                                        onChange={(e) => handleFilterChange('endDate', e.target.value)}
                                        className="form-input"
                                    />
                                </div>
                                <div className="form-group">
                                    <label htmlFor="transactionType" className="form-label">Tipo de Transação</label>
                                    <select
                                        id="transactionType"
                                        value={filters.transactionType || ''}
                                        onChange={(e) => handleFilterChange('transactionType', e.target.value || undefined)}
                                        className="form-input"
                                    >
                                        <option value="">Todos os Tipos</option>
                                        <option value="CREDIT">Crédito</option>
                                        <option value="DEBIT">Débito</option>
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label htmlFor="minAmount" className="form-label">Valor Mínimo</label>
                                    <input
                                        type="number"
                                        id="minAmount"
                                        step="0.01"
                                        value={filters.minAmount || ''}
                                        onChange={(e) => handleFilterChange('minAmount', e.target.value ? parseFloat(e.target.value) : undefined)}
                                        className="form-input"
                                        placeholder="R$ 0,00"
                                    />
                                </div>
                                <div className="form-group">
                                    <label htmlFor="maxAmount" className="form-label">Valor Máximo</label>
                                    <input
                                        type="number"
                                        id="maxAmount"
                                        step="0.01"
                                        value={filters.maxAmount || ''}
                                        onChange={(e) => handleFilterChange('maxAmount', e.target.value ? parseFloat(e.target.value) : undefined)}
                                        className="form-input"
                                        placeholder="R$ 0,00"
                                    />
                                </div>
                                <div className="form-group">
                                    <label htmlFor="status" className="form-label">Status da Transação</label>
                                    <select
                                        id="status"
                                        value={filters.status || ''}
                                        onChange={(e) => handleFilterChange('status', e.target.value || undefined)}
                                        className="form-input"
                                    >
                                        <option value="">Todos os Status</option>
                                        <option value="COMPLETED">Concluída</option>
                                        <option value="PENDING">Pendente</option>
                                        <option value="FAILED">Falhou</option>
                                        <option value="CANCELLED">Cancelada</option>
                                    </select>
                                </div>
                            </div>
                            <div className="form-actions">
                                <button onClick={clearFilters} className="btn btn-secondary">
                                    Limpar Filtros
                                </button>
                            </div>
                        </div>
                    )}
                </div>

                {/* Export Section */}
                <div className="quick-actions">
                    <h3 className="section-title">Exportar Dados</h3>
                    <div className="action-cards">
                        <div 
                            className={`action-card ${exporting || loading ? 'disabled' : ''}`}
                            onClick={() => !exporting && !loading && handleExport('csv')}
                        >
                            <div className="action-icon">📊</div>
                            <h4>{exporting ? 'Exportando...' : 'Exportar CSV'}</h4>
                            <p>Baixar histórico em formato CSV</p>
                        </div>
                        <div 
                            className={`action-card ${exporting || loading ? 'disabled' : ''}`}
                            onClick={() => !exporting && !loading && handleExport('pdf')}
                        >
                            <div className="action-icon">📄</div>
                            <h4>{exporting ? 'Exportando...' : 'Exportar PDF'}</h4>
                            <p>Baixar relatório em formato PDF</p>
                        </div>
                    </div>
                </div>

                {/* Loading State */}
                {loading && (
                    <div className="recent-transactions">
                        <div className="loading-transactions">
                            <p>Carregando transações...</p>
                        </div>
                    </div>
                )}

                {/* Transactions Table */}
                {!loading && (
                    <div className="recent-transactions">
                        <div className="section-header">
                            <h3 className="section-title">
                                Histórico Completo ({transactions.length} transações)
                            </h3>
                        </div>
                        
                        {transactions.length === 0 ? (
                            <div className="empty-state">
                                <div className="empty-icon">📋</div>
                                <h4>Nenhuma transação encontrada</h4>
                                <p>Ajuste os filtros ou tente novamente mais tarde</p>
                            </div>
                        ) : (
                            <div className="transactions-table">
                                <div className="table-header">
                                    <div>Data</div>
                                    <div>Tipo</div>
                                    <div>Descrição</div>
                                    <div>Destinatário</div>
                                    <div>Valor</div>
                                    <div>Status</div>
                                </div>
                                {transactions.map((transaction) => (
                                    <div key={transaction.id} className="table-row">
                                        <div className="col-date">
                                            {formatDate(transaction.timestamp)}
                                        </div>
                                        <div className="col-type">
                                            <span className={`transaction-type ${transaction.type.toLowerCase()}`}>
                                                {transaction.type === 'CREDIT' ? 'Crédito' : 'Débito'}
                                            </span>
                                        </div>
                                        <div className="col-description" title={transaction.description}>
                                            {transaction.description}
                                        </div>
                                        <div className="col-recipient">
                                            {transaction.recipientName || transaction.recipientAccountNumber || '-'}
                                        </div>
                                        <div className={`col-amount ${transaction.type.toLowerCase()}`}>
                                            {transaction.type === 'CREDIT' ? '+' : '-'} {formatCurrency(Math.abs(transaction.amount))}
                                        </div>
                                        <div className="col-status">
                                            <span className={`status-badge ${transaction.status.toLowerCase()}`}>
                                                {transaction.status === 'COMPLETED' ? 'Concluída' : 
                                                 transaction.status === 'PENDING' ? 'Pendente' : 
                                                 transaction.status === 'FAILED' ? 'Falhou' :
                                                 transaction.status === 'CANCELLED' ? 'Cancelada' : transaction.status}
                                            </span>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}

                        {/* Pagination */}
                        {totalPages > 1 && (
                            <div className="pagination-section">
                                <button
                                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                                    disabled={currentPage === 1}
                                    className="btn btn-secondary btn-small"
                                >
                                    ← Anterior
                                </button>
                                
                                <span className="pagination-info">
                                    Página {currentPage} de {totalPages}
                                </span>
                                
                                <button
                                    onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                                    disabled={currentPage === totalPages}
                                    className="btn btn-secondary btn-small"
                                >
                                    Próxima →
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
};

export default TransactionHistory;
