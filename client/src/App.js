import React, { useState } from 'react';
import OtpVerificationForm from './components/OtpVerificationForm';
import PersonalInfoForm from './components/PersonalInfoForm';
import IdentityVerificationForm from './components/IdentityVerificationForm';

function App() {
    const [currentScreen, setCurrentScreen] = useState('otp');

    const handleNextStep = () => {
        switch (currentScreen) {
            case 'otp':
                setCurrentScreen('personalInfo');
                break;
            case 'personalInfo':
                setCurrentScreen('identityVerification');
                break;
            case 'identityVerification':
                setCurrentScreen('otp'); // Optionally reset to the first step or handle differently
                break;
            default:
                setCurrentScreen('otp');
        }
    };

    const renderScreen = () => {
        switch (currentScreen) {
            case 'otp':
                return <OtpVerificationForm onNext={handleNextStep} />;
            case 'personalInfo':
                return <PersonalInfoForm onNext={handleNextStep} />;
            case 'identityVerification':
                return <IdentityVerificationForm onNext={handleNextStep} />;
            default:
                return <OtpVerificationForm onNext={handleNextStep} />;
        }
    };

    return (
        <div className="App">
            <nav>
                <button onClick={() => setCurrentScreen('otp')}>OTP Verification</button>
                <button onClick={() => setCurrentScreen('personalInfo')}>Personal Info</button>
                <button onClick={() => setCurrentScreen('identityVerification')}>Identity Verification</button>
            </nav>
            {renderScreen()}
        </div>
    );
}

export default App;