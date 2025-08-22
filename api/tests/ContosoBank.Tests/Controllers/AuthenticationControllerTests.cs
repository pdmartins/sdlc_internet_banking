using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ContosoBank.Web.Controllers;
using ContosoBank.Application.DTOs;
using ContosoBank.Application.Interfaces;
using System;
using System.Threading.Tasks;
using System.Net;

namespace ContosoBank.Tests.Controllers;

public class AuthenticationControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly Mock<ILogger<AuthenticationController>> _mockLogger;
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTests()
    {
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthenticationController>>();
        _controller = new AuthenticationController(_mockAuthenticationService.Object, _mockLogger.Object);

        // Setup HttpContext for IP address
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Login_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        var loginResponse = new LoginResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            Token = "jwt-token",
            RequiresMfa = false,
            Message = "Login realizado com sucesso"
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<LoginResponseDto>(okResult.Value);
        Assert.Equal(loginResponse.UserId, returnValue.UserId);
        Assert.Equal(loginResponse.Email, returnValue.Email);
    }

    [Fact]
    public async Task Login_UnauthorizedAccess_ReturnsUnauthorizedResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrongpassword",
            RememberDevice = false
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ThrowsAsync(new UnauthorizedAccessException("Email ou senha inválidos."));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = unauthorizedResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Login_InvalidOperation_ReturnsBadRequestResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ThrowsAsync(new InvalidOperationException("Conta bloqueada."));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Login_ArgumentException_ReturnsBadRequestResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ThrowsAsync(new ArgumentException("Dados inválidos."));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Login_UnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123",
            RememberDevice = false
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ValidateCredentials_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _mockAuthenticationService.Setup(s => s.ValidateCredentialsAsync(loginRequest.Email, loginRequest.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateCredentials(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task ValidateCredentials_Exception_ReturnsInternalServerError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _mockAuthenticationService.Setup(s => s.ValidateCredentialsAsync(loginRequest.Email, loginRequest.Password))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ValidateCredentials(loginRequest);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CheckLockStatus_ValidEmail_ReturnsOkResult()
    {
        // Arrange
        var email = "test@example.com";

        _mockAuthenticationService.Setup(s => s.IsAccountLockedAsync(email))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckLockStatus(email);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CheckLockStatus_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = "";

        // Act
        var result = await _controller.CheckLockStatus(email);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CheckLockStatus_Exception_ReturnsInternalServerError()
    {
        // Arrange
        var email = "test@example.com";

        _mockAuthenticationService.Setup(s => s.IsAccountLockedAsync(email))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CheckLockStatus(email);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Theory]
    [InlineData("test@example.com", "password123", false)]
    [InlineData("user@test.com", "mypassword", true)]
    public async Task Login_DifferentInputs_ProcessesCorrectly(string email, string password, bool rememberDevice)
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password,
            RememberDevice = rememberDevice
        };

        var loginResponse = new LoginResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = email,
            FullName = "Test User",
            RequiresMfa = false
        };

        _mockAuthenticationService.Setup(s => s.LoginAsync(loginRequest, "127.0.0.1"))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<LoginResponseDto>(okResult.Value);
        Assert.Equal(email, returnValue.Email);
    }
}
