using System.ComponentModel.DataAnnotations;

namespace ApiAuth.Models.RequestModels.Auth;

public class UserLoginRequestModel
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}