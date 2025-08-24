using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;

namespace ContosoBank.Application.Services;

/// <summary>
/// Security service for encryption, hashing, and other security operations
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Encrypt sensitive data
    /// </summary>
    string EncryptData(string plainText);
    
    /// <summary>
    /// Decrypt sensitive data
    /// </summary>
    string DecryptData(string encryptedText);
    
    /// <summary>
    /// Hash password with salt
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify password against hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
    
    /// <summary>
    /// Generate secure random token
    /// </summary>
    string GenerateSecureToken(int length = 32);
    
    /// <summary>
    /// Sanitize input to prevent XSS
    /// </summary>
    string SanitizeInput(string input);
}

public class SecurityService : ISecurityService
{
    private readonly IDataProtector _dataProtector;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public SecurityService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("ContosoBank.Security");
    }

    public string EncryptData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        return _dataProtector.Protect(plainText);
    }

    public string DecryptData(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            return _dataProtector.Unprotect(encryptedText);
        }
        catch (CryptographicException)
        {
            // Log the error and return empty string
            return string.Empty;
        }
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        // Generate salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password with salt using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        byte[] hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            byte[] hashBytes = Convert.FromBase64String(hash);
            
            if (hashBytes.Length != SaltSize + HashSize)
                return false;

            // Extract salt
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Extract hash
            byte[] storedHash = new byte[HashSize];
            Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

            // Hash the provided password with the stored salt
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(HashSize);

            // Compare hashes
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateSecureToken(int length = 32)
    {
        byte[] tokenBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        return Convert.ToBase64String(tokenBytes);
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Basic XSS prevention - encode HTML characters
        return System.Net.WebUtility.HtmlEncode(input.Trim());
    }
}
