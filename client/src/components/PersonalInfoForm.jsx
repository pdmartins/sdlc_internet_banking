import React, { useState } from 'react';
import './PersonalInfoForm.css'; // Import the CSS file for styling

const PersonalInfoForm = ({ onNext }) => {
  const [name, setName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [address, setAddress] = useState('');
  const [feedback, setFeedback] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const response = await fetch('http://localhost:5000/api/personalinfo/submit-info', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, dateOfBirth, address }),
      });
      const data = await response.json();
      if (response.ok) {
        setFeedback('Personal information submitted successfully!');
        onNext(); // Move to the next step
      } else {
        setFeedback(data.message || 'Failed to submit personal information.');
      }
    } catch (error) {
      setFeedback('An error occurred while submitting personal information.');
    }
  };

  return (
    <div className="form-container">
      <h2>Personal Information</h2>
      <form onSubmit={handleSubmit}>
        <div>
          <label>
            Name:
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              className="input-field"
            />
          </label>
        </div>
        <div>
          <label>
            Date of Birth:
            <input
              type="date"
              value={dateOfBirth}
              onChange={(e) => setDateOfBirth(e.target.value)}
              required
              className="input-field"
            />
          </label>
        </div>
        <div>
          <label>
            Address:
            <input
              type="text"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              required
              className="input-field"
            />
          </label>
        </div>
        <button type="submit" className="form-button">Submit</button>
      </form>
      {feedback && <p className="feedback-message">{feedback}</p>}
    </div>
  );
};

export default PersonalInfoForm;
