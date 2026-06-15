namespace LilyMarket.Application.DTO.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}