using System.ComponentModel.DataAnnotations;

namespace ApiAuth.Models.RequestModels.Auth;

public class CreateUserRequestModel
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
    
    [Required] 
    [EmailAddress]
    public string Email { get; set; } = null!;
}