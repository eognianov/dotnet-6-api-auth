namespace ApiAuth.Models.RequestModels.Auth;

public class RefreshTokenRequestModel
{
    public string Token { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;
}