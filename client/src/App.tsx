
import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Welcome from './components/Welcome';
import PersonalInfoForm from './components/PersonalInfoForm';
import SecuritySetupForm from './components/SecuritySetupForm';
import ConfirmationTutorial from './components/ConfirmationTutorial';
import LoginForm from './components/LoginForm';
import Dashboard from './components/Dashboard';
import MfaPage from './pages/MfaPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';
import TransactionPage from './pages/TransactionPage';
import TransactionHistoryPage from './pages/TransactionHistoryPage';
import ProfilePage from './pages/ProfilePage';
import InactivityWarning from './components/InactivityWarning';
import { RegistrationProvider } from './contexts/RegistrationContext';
import { AuthProvider } from './contexts/AuthContext';
import { initializeAccessibilityAndPerformance } from './utils/story5-1-1';
import './styles/global.css';

const App: React.FC = () => {
  // Initialize accessibility and performance features - User Story 5.1.1
  useEffect(() => {
    initializeAccessibilityAndPerformance();
  }, []);

  return (
    <AuthProvider>
      <RegistrationProvider>
        <Router>
          <div className="App" id="app-root">
            {/* Skip link for accessibility */}
            <a href="#main-content" className="skip-link sr-only-focusable">
              Pular para o conteúdo principal
            </a>
            
            {/* Inactivity Warning - Feature 2.3.1 */}
            <InactivityWarning />
            
            <main id="main-content" role="main">
              <Routes>
                {/* Default route redirects to welcome */}
              <Route path="/" element={<Navigate to="/welcome" replace />} />
              {/* Welcome screen - User Story 1.1.1 */}
              <Route path="/welcome" element={<Welcome />} />
              {/* Registration flow routes */}
              <Route path="/register/personal-info" element={<PersonalInfoForm />} />
              <Route path="/register/verification" element={<div>Identity Verification (Coming Soon)</div>} />
              <Route path="/register/security" element={<SecuritySetupForm />} />
              <Route path="/register/confirmation" element={<ConfirmationTutorial />} />
              {/* Login flow routes - User Story 2.1.1 & 2.1.3 */}
              <Route path="/login" element={<LoginForm />} />
              <Route path="/login/mfa" element={<MfaPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />
              {/* Transaction routes - User Story 3.1.1 */}
              <Route path="/transaction" element={<TransactionPage />} />
              {/* Transaction History routes - User Story 3.2.1 */}
              <Route path="/transaction/history" element={<TransactionHistoryPage />} />
              {/* Dashboard - User Story 4.1.1 */}
              <Route path="/dashboard" element={<Dashboard />} />
              {/* Profile & Settings - User Story 4.2.1 */}
              <Route path="/profile" element={<ProfilePage />} />
              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/welcome" replace />} />
            </Routes>
            </main>
          </div>
        </Router>
      </RegistrationProvider>
    </AuthProvider>
  );
};

export default App;
