using EduPlatform.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AdminSettings _adminSettings;

    public AdminAuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IOptions<AdminSettings> adminOptions)
    {
        _userManager = userManager;
        _configuration = configuration;
        _adminSettings = adminOptions.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // التحقق من بيانات الإدمن من الـ appsettings.json
        if (model.Email == _adminSettings.Email && model.Password == _adminSettings.Password)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                // التحقق من أن المستخدم له دور Admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                if (!isAdmin)
                {
                    // إضافة دور Admin للمستخدم إذا لم يكن لديه
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                // إنشاء التوكن
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.FullName
                    },
                    Roles = new[] { "Admin" },
                    Message = "Login successful"
                });
            }
        }

        return Unauthorized(new { Message = "Invalid email or password" });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            //issuer: _configuration["Jwt:Issuer"],
            //audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}