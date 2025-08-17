import React, { useState } from 'react';
import axios from 'axios';

const IdentityVerificationForm = ({ onNext }) => {
    const [idFile, setIdFile] = useState(null);
    const [selfieFile, setSelfieFile] = useState(null);
    const [userId, setUserId] = useState('');
    const [statusMessage, setStatusMessage] = useState('');

    const handleFileChange = (e, setFile) => {
        setFile(e.target.files[0]);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!idFile || !selfieFile || !userId) {
            setStatusMessage('Please upload both ID and selfie, and provide a user ID.');
            return;
        }

        const formData = new FormData();
        formData.append('idFile', idFile);
        formData.append('selfieFile', selfieFile);
        formData.append('userId', userId);

        try {
            const response = await axios.post('http://localhost:5000/api/IdentityVerification', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });

            setStatusMessage('Upload successful! Verification is pending.');
            onNext(); // Move to the next step
        } catch (error) {
            setStatusMessage('Upload failed. Please try again.');
        }
    };

    return (
        <div className="form-container">
            <h2>Identity Verification</h2>
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
                    <label htmlFor="idFile">Upload ID:</label>
                    <input
                        type="file"
                        id="idFile"
                        onChange={(e) => handleFileChange(e, setIdFile)}
                        className="input-field"
                    />
                </div>
                <div>
                    <label htmlFor="selfieFile">Upload Selfie:</label>
                    <input
                        type="file"
                        id="selfieFile"
                        onChange={(e) => handleFileChange(e, setSelfieFile)}
                        className="input-field"
                    />
                </div>
                <button type="submit" className="form-button">Submit</button>
            </form>
            {statusMessage && <p className="feedback-message">{statusMessage}</p>}
        </div>
    );
};

export default IdentityVerificationForm;
