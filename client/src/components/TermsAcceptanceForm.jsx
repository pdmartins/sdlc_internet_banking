import React, { useState } from 'react';

const TermsAcceptanceForm = ({ onNext }) => {
  const [isAccepted, setIsAccepted] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();

    if (!isAccepted) {
      setError('You must accept the terms to proceed.');
      return;
    }

    setError('');
    onNext(); // Proceed to account creation
  };

  return (
    <div className="form-container">
      <h2>Terms and Conditions</h2>
      <div className="terms-content">
        <p>
          Please read and accept the terms and conditions to proceed with your registration.
        </p>
        <p>
          [Insert detailed terms and conditions here.]
        </p>
      </div>
      <form onSubmit={handleSubmit}>
        <div className="checkbox-container">
          <input
            type="checkbox"
            id="acceptTerms"
            checked={isAccepted}
            onChange={(e) => setIsAccepted(e.target.checked)}
          />
          <label htmlFor="acceptTerms">I accept the terms and conditions</label>
        </div>
        {error && <p className="error-message">{error}</p>}
        <button type="submit" className="form-button">Proceed</button>
      </form>
    </div>
  );
};

export default TermsAcceptanceForm;
