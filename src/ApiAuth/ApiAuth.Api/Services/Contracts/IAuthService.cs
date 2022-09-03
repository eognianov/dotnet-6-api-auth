using ApiAuth.Models.Auth;
using ApiAuth.Models.RequestModels.Auth;

namespace ApiAuth.Api.Services.Contracts;

public interface IAuthService
{
    Task<AuthenticationResult> CreateUserAsync(CreateUserRequestModel createUserRequest);
}