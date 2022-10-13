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

    public async Task<AuthenticationResult> RegisterUserAsync(RegisterUserRequestModel registerUserRequest)
    {
        var existingUserWithThisEmail = await _userManager.FindByEmailAsync(registerUserRequest.Email);
        var existingUSerWithThisUsername = await _userManager.FindByNameAsync(registerUserRequest.Username);
        if (existingUserWithThisEmail != null || existingUSerWithThisUsername is not null)
        {
            return new AuthenticationResult
            {
                Errors = new[]
                {
                    $"User with {(existingUserWithThisEmail?.Email is not null ? registerUserRequest.Email : registerUserRequest.Username)} already exists!"
                }
            };
        }

        var newUser = new ApplicationUser
        {
            Email = registerUserRequest.Email,
            UserName = registerUserRequest.Username
        };

        var createdUser = await _userManager.CreateAsync(newUser, registerUserRequest.Password);

        if (!createdUser.Succeeded)
        {
            return new AuthenticationResult
            {
                Errors = createdUser.Errors.Select(x => x.Description).ToArray()
            };
        }

        return GenerateAuthenticationResultForUser(newUser);
    }

    public async Task<AuthenticationResult> UserLoginAsync(UserLoginRequestModel userLoginRequest)
    {
        var user = await _userManager.FindByNameAsync(userLoginRequest.Username);
        if (user == null)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"User does not exist!"}
            };
        }

        var userHasValidPassword = await _userManager.CheckPasswordAsync(user, userLoginRequest.Password);
        
        if (!userHasValidPassword)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"Wrong password!"}
            };
        }

        return GenerateAuthenticationResultForUser(user, userLoginRequest.RememberMe);
    }

    private AuthenticationResult GenerateAuthenticationResultForUser(IdentityUser user, bool rememberMe = false)
    {
        var expirationDate = DateTime.UtcNow.AddHours(2);
        
        if (rememberMe)
        {
            expirationDate = DateTime.UtcNow.AddYears(1);
        }
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("id", user.Id),
            }),
            Expires = expirationDate,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthenticationResult
        {
            Success = true,
            Token = tokenHandler.WriteToken(token),
            ExpirationDate = expirationDate
        };
    }
}