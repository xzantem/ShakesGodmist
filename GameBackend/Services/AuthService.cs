using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShakesGodmist.Models;

namespace ShakesGodmist.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public class AuthService : IAuthService
    {
        private readonly GameContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(GameContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Starting registration for email: {request.Email}, username: {request.Username}");
            
            // Check if user already exists
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Registration failed: Email {request.Email} already exists");
                throw new InvalidOperationException("User with this email already exists");
            }

            if (_context.Users.Any(u => u.Username == request.Username))
            {
                Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Registration failed: Username {request.Username} already taken");
                throw new InvalidOperationException("Username is already taken");
            }

            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Email and username validation passed");

            // Hash password
            var passwordHash = HashPassword(request.Password);
            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Password hashed successfully");

            // Create user
            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Creating user object");

            _context.Users.Add(user);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - User created successfully with ID: {user.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Error creating user: {ex.Message}");
                throw;
            }

            // Generate tokens
            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Generating JWT token");
            var token = GenerateJwtToken(user);
            
            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Generating refresh token");
            var refreshToken = GenerateRefreshToken();

            Console.WriteLine($"[DEBUG] AuthService.RegisterAsync - Registration completed successfully for user ID: {user.Id}");

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Characters = new List<PlayerDto>()
                }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Starting login for email: {request.Email}");
            
            var user = await _context.Users
                .Include(u => u.Characters)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Login failed: User not found for email: {request.Email}");
                throw new InvalidOperationException("Invalid email or password");
            }

            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - User found with ID: {user.Id}, verifying password");

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Login failed: Invalid password for user ID: {user.Id}");
                throw new InvalidOperationException("Invalid email or password");
            }

            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Password verified successfully for user ID: {user.Id}");

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Last login updated for user ID: {user.Id}");

            // Generate tokens
            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Generating JWT token");
            var token = GenerateJwtToken(user);
            
            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Generating refresh token");
            var refreshToken = GenerateRefreshToken();

            Console.WriteLine($"[DEBUG] AuthService.LoginAsync - Login completed successfully for user ID: {user.Id}, Characters: {user.Characters.Count}");

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Characters = user.Characters.Select(c => new PlayerDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Level = c.Level,
                        Class = c.Class,
                        LastActive = c.LastActive
                    }).ToList()
                }
            };
        }

        public string GenerateJwtToken(User user)
        {
            Console.WriteLine($"[DEBUG] AuthService.GenerateJwtToken - Generating JWT token for user ID: {user.Id}");
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            Console.WriteLine($"[DEBUG] AuthService.GenerateJwtToken - Claims created for user: {user.Username}");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"[DEBUG] AuthService.GenerateJwtToken - JWT token generated successfully for user ID: {user.Id}");
            
            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            Console.WriteLine($"[DEBUG] AuthService.GenerateRefreshToken - Generating refresh token");
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);
            Console.WriteLine($"[DEBUG] AuthService.GenerateRefreshToken - Refresh token generated successfully");
            return refreshToken;
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            Console.WriteLine($"[DEBUG] AuthService.GetPrincipalFromExpiredToken - Validating expired token");
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"[DEBUG] AuthService.GetPrincipalFromExpiredToken - Invalid token format or algorithm");
                throw new SecurityTokenException("Invalid token");
            }

            Console.WriteLine($"[DEBUG] AuthService.GetPrincipalFromExpiredToken - Token validated successfully");
            return principal;
        }

        private string HashPassword(string password)
        {
            Console.WriteLine($"[DEBUG] AuthService.HashPassword - Hashing password");
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = Convert.ToBase64String(hashedBytes);
            Console.WriteLine($"[DEBUG] AuthService.HashPassword - Password hashed successfully");
            return hash;
        }

        private bool VerifyPassword(string password, string hash)
        {
            Console.WriteLine($"[DEBUG] AuthService.VerifyPassword - Verifying password");
            var hashedPassword = HashPassword(password);
            var isValid = hashedPassword == hash;
            Console.WriteLine($"[DEBUG] AuthService.VerifyPassword - Password verification result: {isValid}");
            return isValid;
        }
    }
} 