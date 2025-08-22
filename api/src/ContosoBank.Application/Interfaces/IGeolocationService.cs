namespace ContosoBank.Application.Interfaces;

/// <summary>
/// Service interface for geolocation operations
/// </summary>
public interface IGeolocationService
{
    /// <summary>
    /// Gets location information from IP address
    /// </summary>
    Task<GeolocationResult> GetLocationFromIpAsync(string ipAddress);

    /// <summary>
    /// Calculates distance between two geographical points in kilometers
    /// </summary>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);

    /// <summary>
    /// Determines if a location is considered unusual for the user
    /// </summary>
    Task<bool> IsUnusualLocationAsync(Guid userId, string country, string region, string city);
}

/// <summary>
/// Result of geolocation lookup
/// </summary>
public class GeolocationResult
{
    public bool IsSuccess { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? TimeZone { get; set; }
    public string? ErrorMessage { get; set; }
}
