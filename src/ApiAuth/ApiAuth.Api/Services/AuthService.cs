using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiAuth.Api.Options;
using ApiAuth.Api.Services.Contracts;
using ApiAuth.Data;
using ApiAuth.Models.Auth;
using ApiAuth.Models.RequestModels.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ApiAuth.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly ApplicationDbContext _dbContext;

    public AuthService(UserManager<ApplicationUser> userManager, JwtSettings jwtSettings, TokenValidationParameters tokenValidationParameters, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
        _tokenValidationParameters = tokenValidationParameters;
        _dbContext = dbContext;
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

        return await GenerateAuthenticationResultForUserAsync(newUser);
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

        return await GenerateAuthenticationResultForUserAsync(user);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenRequestModel refreshTokenInputModel)
    {
        var validateToken = GetPrincipalFromToken(refreshTokenInputModel.Token, true);

        if (validateToken == null)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"Invalid Token!"}
            };
        }
        
        var expiryDateUnix =
            long.Parse(validateToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expiryDateUnix);
        
        if (expiryDateTimeUtc > DateTime.UtcNow)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Token hasn't expired yet!"}
            };
        }

        var jti = validateToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
        var storedRefreshToken =
            await _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshTokenInputModel.RefreshToken);

        if (storedRefreshToken == null)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Refresh Token does not exist!"}
            };
        }
        
        if (DateTime.UtcNow > storedRefreshToken.ExpirationDate)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Refresh Token has expired!"}
            };
        }

        if (storedRefreshToken.Invalidated)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Refresh Token has been invalidated!"}
            };
        }

        if (storedRefreshToken.Used)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Refresh Token has been used!"}
            };
        }

        if (storedRefreshToken.JwtId != jti)
        {
            return new AuthenticationResult
            {
                Errors = new[] {"This Refresh Token does not match this JWT!"}
            };
        }
        
        storedRefreshToken.Used = true;
        _dbContext.RefreshTokens.Update(storedRefreshToken);
        await _dbContext.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(validateToken.Claims.Single(x => x.Type == "id").Value);
        return await GenerateAuthenticationResultForUserAsync(user);
    }

    private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser user)
    {
        var expirationDate = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime);
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
        var refreshToken = new RefreshToken
        {
            JwtId = token.Id,
            UserId = user.Id,
            CreatedOn = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddHours(1)
        };
        await _dbContext.RefreshTokens.AddRangeAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
        return new AuthenticationResult
        {
            Success = true,
            Token = tokenHandler.WriteToken(token),
            ExpirationDate = expirationDate,
            RefreshToken = refreshToken.Token
        };
    }
    
    private ClaimsPrincipal? GetPrincipalFromToken(string token, bool refresh = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var tokenValidationParameters = _tokenValidationParameters;
            if (refresh)
            {
                tokenValidationParameters.ClockSkew = TimeSpan.FromHours(1);
            }
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
            {
                return null;
            }

            return principal;
        }
        catch (Exception e)
        {
            // TODO Add logging
            Console.WriteLine(e.Message);
            return null;
        }
    }

    private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
    {
        return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
               jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                   StringComparison.InvariantCultureIgnoreCase);
    }
}