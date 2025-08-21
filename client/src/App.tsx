import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Welcome from './components/Welcome';
import PersonalInfoForm from './components/PersonalInfoForm';
import SecuritySetupForm from './components/SecuritySetupForm';
import ConfirmationTutorial from './components/ConfirmationTutorial';
import { RegistrationProvider } from './contexts/RegistrationContext';
import './styles/global.css';

const App: React.FC = () => {
  return (
    <RegistrationProvider>
      <Router>
        <div className="App">
          <Routes>
          {/* Default route redirects to welcome */}
          <Route path="/" element={<Navigate to="/welcome" replace />} />
          
          {/* Welcome screen - User Story 1.1.1 */}
          <Route path="/welcome" element={<Welcome />} />
          
          {/* Registration flow routes */}
          {/* Personal Information Form - User Story 1.1.2 */}
          <Route path="/register/personal-info" element={<PersonalInfoForm />} />
          <Route path="/register/verification" element={<div>Identity Verification (Coming Soon)</div>} />
          {/* Security Setup Form - User Story 1.1.3 */}
          <Route path="/register/security" element={<SecuritySetupForm />} />
          <Route path="/register/confirmation" element={<ConfirmationTutorial />} />
          
          {/* Login flow routes (to be implemented) */}
          <Route path="/login" element={<div>Login (Coming Soon)</div>} />
          
          {/* Dashboard (to be implemented) */}
          <Route path="/dashboard" element={<div>Dashboard (Coming Soon)</div>} />
          
          {/* Catch all route */}
          <Route path="*" element={<Navigate to="/welcome" replace />} />
        </Routes>
      </div>
    </Router>
    </RegistrationProvider>
  );
};

export default App;
