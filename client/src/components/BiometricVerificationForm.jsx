import React, { useState } from 'react';
import axios from 'axios';

const BiometricVerificationForm = ({ onNext }) => {
  const [biometricFile, setBiometricFile] = useState(null);
  const [userId, setUserId] = useState('');
  const [statusMessage, setStatusMessage] = useState('');

  const handleFileChange = (e) => {
    setBiometricFile(e.target.files[0]);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!biometricFile || !userId) {
      setStatusMessage('Please upload biometric data and provide a user ID.');
      return;
    }

    console.log('Submitting biometric verification with userId:', userId);
    console.log('Biometric file details:', biometricFile);

    const formData = new FormData();
    formData.append('biometricData', biometricFile);
    formData.append('userId', userId);

    try {
      const response = await axios.post('http://localhost:5000/api/biometricverification/match', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      setStatusMessage(response.data.message);
      if (response.data.status === 'Matched') {
        onNext(); // Proceed to the next step
      }
    } catch (error) {
      setStatusMessage('Biometric verification failed. Please try again.');
    }
  };

  const handleRetry = async () => {
    try {
      const response = await axios.post('http://localhost:5000/api/biometricverification/retry', { userId });
      setStatusMessage(response.data.message);
    } catch (error) {
      setStatusMessage('Retry failed. Please try again.');
    }
  };

  return (
    <div className="form-container">
      <h2>Biometric Verification</h2>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="userId">User ID:</label>
          <input
            type="text"
            id="userId"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            className="input-field"
          />
        </div>
        <div>
          <label htmlFor="biometricFile">Upload Biometric Data:</label>
          <input
            type="file"
            id="biometricFile"
            onChange={handleFileChange}
            className="input-field"
          />
        </div>
        <button type="submit" className="form-button">Submit</button>
      </form>
      <button onClick={handleRetry} className="form-button retry-button">Retry</button>
      {statusMessage && <p className="feedback-message">{statusMessage}</p>}
    </div>
  );
};

export default BiometricVerificationForm;
