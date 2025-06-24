using Microsoft.AspNetCore.Mvc;
using ShakesGodmist.Services;
using ShakesGodmist.Models;

namespace ShakesGodmist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            Console.WriteLine($"[DEBUG] Register called - Email: {request.Email}, Username: {request.Username}");
            
            try
            {
                var response = await _authService.RegisterAsync(request);
                Console.WriteLine($"[DEBUG] Register successful - User ID: {response.User.Id}, Token generated");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[DEBUG] Register failed - {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            Console.WriteLine($"[DEBUG] Login called - Email: {request.Email}");
            
            try
            {
                var response = await _authService.LoginAsync(request);
                Console.WriteLine($"[DEBUG] Login successful - User ID: {response.User.Id}, Token generated, Characters: {response.User.Characters.Count}");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[DEBUG] Login failed - {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 