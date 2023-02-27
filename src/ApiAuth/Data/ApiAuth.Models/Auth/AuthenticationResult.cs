namespace ApiAuth.Models.Auth;

public class AuthenticationResult
{
    public string? Token { get; init; }

    public bool Success { get; init; }

    public IEnumerable<string>? Errors { get; init; }

    public DateTime ExpirationDate { get; init; }


    public string? RefreshToken { get; set; }
}