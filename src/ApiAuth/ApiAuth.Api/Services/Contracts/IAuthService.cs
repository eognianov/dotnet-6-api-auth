using ApiAuth.Models.Auth;
using ApiAuth.Models.RequestModels.Auth;

namespace ApiAuth.Api.Services.Contracts;

public interface IAuthService
{
    Task<AuthenticationResult> RegisterUserAsync(RegisterUserRequestModel registerUserRequest);

    Task<AuthenticationResult> UserLoginAsync(UserLoginRequestModel userLoginRequest);

    Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenRequestModel refreshTokenInputModel);
}