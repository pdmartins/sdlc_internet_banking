import React from 'react';
import OtpVerificationForm from './components/OtpVerificationForm';
import PersonalInfoForm from './components/PersonalInfoForm';

const App = () => {
  return (
    <div>
      <h1>Internet Banking Application</h1>
      <OtpVerificationForm />
      <PersonalInfoForm />
    </div>
  );
};

export default App;
