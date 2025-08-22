# MFA Code Logging Documentation

## Overview

The MFA (Multi-Factor Authentication) service now logs all generated MFA codes prominently in the application logs for development and testing purposes. This feature helps developers and testers verify the MFA flow without needing actual SMS or email services configured.

## Logging Features

### 1. Code Generation Logging
When a new MFA code is generated (during login), the system logs:
```
warn: 🔐 MFA CODE GENERATED for user@example.com: 123456 (Method: sms) - Session: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

### 2. Code Sending Logging
When the code is sent via SMS or email, the system logs:

**For SMS:**
```
warn: 📱 SMS MFA CODE for user 12345 (user@example.com): 123456 (would send to phone)
```

**For Email:**
```
warn: 📧 EMAIL MFA CODE for user 12345 (user@example.com): 123456
```

### 3. Code Resend Logging
When a user requests to resend the MFA code, the system logs:
```
warn: 🔄 MFA CODE RESENT for user@example.com: 654321 (Method: sms) - Session: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

## Log Level and Visibility

- **Log Level**: `Warning` - This ensures the MFA codes are highly visible in logs
- **Icons**: Each log entry includes an emoji icon for easy identification:
  - 🔐 = Code generation
  - 📱 = SMS sending
  - 📧 = Email sending  
  - 🔄 = Code resend

## Security Considerations

### Development vs Production

- **Development**: These detailed logs are essential for testing and debugging
- **Production**: Consider the security implications of logging sensitive MFA codes

### Recommendations for Production

1. **Log Level Configuration**: Configure production logging to suppress WARNING level logs from MFA services
2. **Secure Logging**: If MFA logging is needed in production, ensure logs are:
   - Stored securely
   - Access-controlled
   - Encrypted at rest
   - Have appropriate retention policies

3. **Alternative Approach**: Replace code logging with audit trails that don't expose actual codes:
   ```csharp
   _logger.LogInformation("MFA code generated for user {UserId} via {Method}", user.Id, method);
   ```

## Testing with MFA Codes

### Manual Testing Steps

1. **Start the API**: Run `dotnet run --project api/src/ContosoBank.Web`
2. **Register a User**: Create a new user account with MFA enabled
3. **Trigger MFA**: Attempt to login to trigger MFA code generation
4. **Check Logs**: Look for the 🔐 and 📱/📧 log entries with the actual code
5. **Use the Code**: Copy the 6-digit code from logs and use it in the MFA verification form
6. **Test Resend**: Try the resend functionality to see 🔄 logs

### Automated Testing

The logged codes can be captured in automated tests:
```csharp
// In test setup, configure a test logger to capture MFA codes
var testLogger = new TestLogger<MfaService>();
var mfaService = new MfaService(unitOfWork, testLogger, configuration);

// Trigger MFA code generation
await mfaService.SendMfaCodeAsync(request, clientIp, userAgent);

// Extract code from logs
var logEntry = testLogger.Logs.FirstOrDefault(l => l.Message.Contains("MFA CODE GENERATED"));
var code = ExtractCodeFromLogMessage(logEntry.Message);

// Use code in verification
await mfaService.VerifyMfaCodeAsync(new MfaVerificationDto { Code = code });
```

## Configuration

No additional configuration is required. The logging is implemented directly in the `MfaService` class:

- `SendMfaCodeAsync()`: Logs code generation and sending
- `ResendMfaCodeAsync()`: Logs code regeneration and resending
- `SendCodeAsync()`: Logs the actual sending simulation

## File Locations

- **Service Implementation**: `api/src/ContosoBank.Application/Services/MfaService.cs`
- **Logging Methods**: 
  - Line ~75: Code generation logging
  - Line ~408: SMS/Email sending logging  
  - Line ~295: Code resend logging

## Related Documentation

- [MFA Implementation Guide](USER_STORY_2.1.2_COMPLETE.md)
- [API Authentication Flow](docs/api-authentication.md)
- [Security Best Practices](docs/security.md)
