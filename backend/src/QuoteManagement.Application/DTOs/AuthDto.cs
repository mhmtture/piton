namespace QuoteManagement.Application.DTOs;

public record LoginDto(string Email, string Password);
public record RegisterDto(string Name, string Email, string Password);
public record AuthResponseDto(string Token, string Name, string Email, string Role);
