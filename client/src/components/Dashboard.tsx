import React from 'react';

const Dashboard: React.FC = () => {
  return (
    <div className="container">
      <div className="dashboard-header">
        <h1 className="form-title">Dashboard</h1>
        <p className="form-description">Bem-vindo ao seu painel de controle</p>
      </div>
      
      <div className="dashboard-content">
        <div className="info-card">
          <h2 className="info-title">Em Desenvolvimento</h2>
          <p>O dashboard estará disponível em breve com todas as funcionalidades bancárias.</p>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;