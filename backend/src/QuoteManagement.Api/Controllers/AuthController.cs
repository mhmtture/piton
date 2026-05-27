using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuoteManagement.Application.DTOs;
using QuoteManagement.Domain.Entities;
using QuoteManagement.Domain.Enums;
using QuoteManagement.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace QuoteManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var name = dto.Name?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var password = dto.Password ?? "";

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            return BadRequest(new { message = "Ad soyad en az 2 karakter olmalıdır." });

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return BadRequest(new { message = "Geçerli bir e-posta adresi girin." });

        if (password.Length < 6)
            return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

        if (await _context.Users.AnyAsync(u => u.Email == email, ct))
            return BadRequest(new { message = "Bu e-posta adresi zaten kullanılıyor." });

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = Role.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        return Ok(new { message = "Kayıt başarılı. Giriş yapabilirsiniz." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password ?? "", user.PasswordHash))
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        var token = CreateToken(user);
        return Ok(new AuthResponseDto(token, user.Name, user.Email, user.Role.ToString()));
    }

    private string CreateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "super-secret-key-for-jwt-token-auth");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
