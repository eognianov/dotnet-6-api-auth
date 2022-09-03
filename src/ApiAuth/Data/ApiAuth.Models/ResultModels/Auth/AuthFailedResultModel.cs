namespace ApiAuth.Models.ResultModels.Auth;

public class AuthFailedResultModel
{
    public IEnumerable<string> Errors { get; set; } = null!;
}