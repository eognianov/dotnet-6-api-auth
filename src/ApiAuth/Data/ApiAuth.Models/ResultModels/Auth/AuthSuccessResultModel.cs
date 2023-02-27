namespace ApiAuth.Models.ResultModels.Auth;

public class AuthSuccessResultModel
{
    public string Token { get; set; } = null!;
    
    public string RefreshToken { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }
}