using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.CodeDom;
using System.Security.Claims;
using TikTokClone.Application.Constants;
using TikTokClone.Application.DTOs;
using TikTokClone.Application.Interfaces.Repositories;
using TikTokClone.Application.Interfaces.Services;

namespace TikTokClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IUserRepository userRepository)
        {
            _authService = authService;
            _logger = logger;
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request?.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for registration");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    ErrorCode = ErrorCodes.VALIDATION_ERROR
                });
            }

            var result = await _authService.RegisterAsync(request!);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Registration failed for {Email}: {Message}",
                    request!.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("User registered successfully: {Email}", request!.Email);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            _logger.LogInformation("Reset password attempt for email: {Email}", request?.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for password reset");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    ErrorCode = ErrorCodes.VALIDATION_ERROR
                });
            }

            var result = await _authService.ResetPasswordAsync(request!);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Reset password failed for {Email}: {Message}",
                    request!.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Password reset successfully: {Email}", request!.Email);
            return Ok(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Login attempt for: {UsernameOrEmail}", request?.UsernameOrEmail);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    ErrorCode = ErrorCodes.VALIDATION_ERROR
                });
            }

            var result = await _authService.LoginAsync(request!);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for {UsernameOrEmail}: {Message}",
                    request!.UsernameOrEmail, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("User logged in successfully: {UsernameOrEmail}",
                request!.UsernameOrEmail);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            _logger.LogInformation("Token refresh attempt");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for token refresh");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    ErrorCode = ErrorCodes.VALIDATION_ERROR
                });
            }

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Token refresh failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Tokens refreshed successfully");
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Logout attempt with invalid token - no user ID found");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid token - user ID not found",
                    ErrorCode = ErrorCodes.INVALID_TOKEN
                });
            }

            _logger.LogInformation("Logout attempt for user: {UserId}", userId);

            var result = await _authService.LogoutAsync(userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Logout failed for user: {UserId}", userId);
                return BadRequest(result);
            }

            _logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(result);
        }

        [HttpPost("send-verification-code")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeDto request)
        {
            _logger.LogInformation("Send confirmation attempt for email: {Email}", request?.Email);

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.Email))
            {
                _logger.LogWarning("Invalid email for resend confirmation");
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Valid email address is required",
                    ErrorCode = ErrorCodes.VALIDATION_ERROR
                });
            }

            var result = await _authService.SendEmailCodeAsync(request.Email, request.Type);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Could not send confirmation email to: {Email}", request.Email);
                return BadRequest(result);
            }

            _logger.LogInformation("Confirmation email sent successfully to: {Email}", request.Email);
            return Ok(result);
        }
    }
}
