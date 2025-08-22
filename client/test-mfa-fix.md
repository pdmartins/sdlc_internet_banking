# MFA Duplicate Call Fix - Testing Guide

## Issue Fixed
The `MfaVerification` component was calling the `/api/mfa/send-code` endpoint twice when mounted, causing duplicate MFA codes to be sent to users.

## Root Cause
The original implementation used a `useEffect` with dependencies that caused the effect to run multiple times:
```tsx
// PROBLEMATIC CODE:
useEffect(() => {
  sendMfaCode();
}, []); // This would run twice in React StrictMode or when component re-mounted
```

## Solution Implemented
1. **Added useRef to track call state**: Used `hasInitialCodeSent.current` to prevent duplicate calls
2. **Used useCallback for sendMfaCode**: Memoized the function with proper dependencies
3. **Added ESLint disable comment**: Intentionally disabled exhaustive-deps for the initial effect

```tsx
// FIXED CODE:
const hasInitialCodeSent = useRef(false);

const sendMfaCode = useCallback(async () => {
  // ... implementation
}, [email, mfaMethod]);

useEffect(() => {
  if (!hasInitialCodeSent.current) {
    hasInitialCodeSent.current = true;
    sendMfaCode();
  }
  // eslint-disable-next-line react-hooks/exhaustive-deps
}, []); // Intentionally empty - we only want this to run once on mount
```

## How to Test
1. **Start the API**: `cd api && dotnet run --project src/ContosoBank.Web`
2. **Start the Client**: `cd client && npm start`
3. **Trigger MFA Flow**:
   - Register a new user with MFA enabled
   - Attempt to login
   - Check the API logs for MFA code generation
4. **Verify Single Call**: Look for only ONE occurrence of the MFA log entries:
   ```
   Sending MFA code to {email} via {method}
   🔐 MFA CODE GENERATED for {email}: {code}
   ```

## Expected Behavior
- ✅ MFA code should be sent only ONCE when the verification page loads
- ✅ User should receive only ONE MFA code via SMS/email
- ✅ Component should work normally for code verification and resend functionality

## Previous Behavior (Fixed)
- ❌ MFA code was sent TWICE when the verification page loaded
- ❌ Users received duplicate MFA codes
- ❌ Rate limiting could be triggered unnecessarily
