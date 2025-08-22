
import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Welcome from './components/Welcome';
import PersonalInfoForm from './components/PersonalInfoForm';
import SecuritySetupForm from './components/SecuritySetupForm';
import ConfirmationTutorial from './components/ConfirmationTutorial';
import LoginForm from './components/LoginForm';
import MfaPage from './pages/MfaPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';
import InactivityWarning from './components/InactivityWarning';
import { RegistrationProvider } from './contexts/RegistrationContext';
import { AuthProvider } from './contexts/AuthContext';
import './styles/global.css';

const App: React.FC = () => {
  return (
    <AuthProvider>
      <RegistrationProvider>
        <Router>
          <div className="App">
            {/* Inactivity Warning - Feature 2.3.1 */}
            <InactivityWarning />
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
              {/* Dashboard (to be implemented) */}
              <Route path="/dashboard" element={<div>Dashboard (Coming Soon)</div>} />
              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/welcome" replace />} />
            </Routes>
          </div>
        </Router>
      </RegistrationProvider>
    </AuthProvider>
  );
};

export default App;
