import React, { useState } from 'react';

const OtpVerificationForm = ({ onNext }) => {
  const [emailOrPhone, setEmailOrPhone] = useState('');
  const [otp, setOtp] = useState('');
  const [step, setStep] = useState(1); // Step 1: Input email/phone, Step 2: Input OTP
  const [feedback, setFeedback] = useState('');

  const handleSendOtp = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/otp/send-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emailOrPhone }),
      });
      const data = await response.json();
      if (response.ok) {
        setStep(2);
        setFeedback('OTP sent successfully!');
      } else {
        setFeedback(data.message || 'Failed to send OTP.');
      }
    } catch (error) {
      setFeedback('An error occurred while sending OTP.');
    }
  };

  const handleVerifyOtp = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/otp/verify-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emailOrPhone, otp }),
      });
      const data = await response.json();
      if (response.ok) {
        setFeedback('OTP verified successfully!');
        onNext(); // Move to the next step
      } else {
        setFeedback(data.message || 'Failed to verify OTP.');
      }
    } catch (error) {
      setFeedback('An error occurred while verifying OTP.');
    }
  };

  return (
    <div>
      <h2>OTP Verification</h2>
      {step === 1 && (
        <div>
          <label>
            Email or Phone:
            <input
              type="text"
              value={emailOrPhone}
              onChange={(e) => setEmailOrPhone(e.target.value)}
            />
          </label>
          <button onClick={handleSendOtp}>Send OTP</button>
        </div>
      )}
      {step === 2 && (
        <div>
          <label>
            Enter OTP:
            <input
              type="text"
              value={otp}
              onChange={(e) => setOtp(e.target.value)}
            />
          </label>
          <button onClick={handleVerifyOtp}>Verify OTP</button>
        </div>
      )}
      {feedback && <p>{feedback}</p>}
    </div>
  );
};

export default OtpVerificationForm;
