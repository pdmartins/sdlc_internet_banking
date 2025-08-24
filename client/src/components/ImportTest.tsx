// Test import file to validate security components
import { API_BASE_URL } from '../config/api';
import { SecurityUtils } from '../services/secureHttpClient';
import { validationPatterns } from '../config/security';

// Simple test to verify imports work
console.log('API_BASE_URL:', API_BASE_URL);
console.log('SecurityUtils available:', typeof SecurityUtils);
console.log('Validation patterns available:', typeof validationPatterns);

export default function ImportTest() {
  return (
    <div>
      <h1>Import Test Successful</h1>
      <p>API Base URL: {API_BASE_URL}</p>
      <p>Security Utils: {typeof SecurityUtils}</p>
    </div>
  );
}
