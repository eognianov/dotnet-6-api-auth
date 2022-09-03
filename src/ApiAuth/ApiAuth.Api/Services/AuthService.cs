using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiAuth.Api.Options;
using ApiAuth.Api.Services.Contracts;
using ApiAuth.Models.Auth;
using ApiAuth.Models.RequestModels.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ApiAuth.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, JwtSettings jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
    }

    public async Task<AuthenticationResult> CreateUserAsync(CreateUserRequestModel createUserRequest)
    {
        var existingUserWithThisEmail = await _userManager.FindByEmailAsync(createUserRequest.Email);
        var existingUSerWithThisUsername = await _userManager.FindByNameAsync(createUserRequest.Username);
        if (existingUserWithThisEmail != null || existingUSerWithThisUsername is not null)
        {
            return new AuthenticationResult
            {
                Errors = new[]
                {
                    $"User with {(existingUserWithThisEmail?.Email is not null ? createUserRequest.Email : createUserRequest.Username)} already exists!"
                }
            };
        }

        var newUser = new ApplicationUser
        {
            Email = createUserRequest.Email,
            UserName = createUserRequest.Username
        };

        var createdUser = await _userManager.CreateAsync(newUser, createUserRequest.Password);

        if (!createdUser.Succeeded)
        {
            return new AuthenticationResult
            {
                Errors = createdUser.Errors.Select(x => x.Description).ToArray()
            };
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, newUser.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, newUser.Email),
                new Claim("id", newUser.Id),
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthenticationResult
        {
            Success = true,
            Token = tokenHandler.WriteToken(token)
        };
    }
}