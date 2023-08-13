using Microsoft.AspNetCore.Mvc;
using ApiAuth.Api.Services.Contracts;
using ApiAuth.Models.RequestModels.Auth;
using ApiAuth.Models.ResultModels.Auth;

namespace ApiAuth.Api.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(Common.Routes.V1.Users.Register)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequestModel registerUserRequest)
    {
        if (!ModelState.IsValid)
        {
            
            return BadRequest(new AuthFailedResultModel
            {
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(err => err.ErrorMessage))
            });
        }
        
        var authResponse = await _authService.RegisterUserAsync(registerUserRequest);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResultModel
            {
                Errors = authResponse.Errors!
            });
        }

        return Ok($"Successful registration. Username {registerUserRequest.Username}");
    }

    [HttpPost(Common.Routes.V1.Users.Login)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestModel userLoginRequest)
    {
        if (!ModelState.IsValid)
        {
            
            return BadRequest(new AuthFailedResultModel
            {
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(err => err.ErrorMessage))
            });
        }

        var authResponse = await _authService.UserLoginAsync(userLoginRequest);
        
        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResultModel
            {
                Errors = authResponse.Errors!
            });
        }

        return Ok(new AuthSuccessResultModel
        {
            Token = authResponse.Token!,
            ExpirationDate = authResponse.ExpirationDate,
            RefreshToken = authResponse.RefreshToken!
        });
    }

    [HttpPost(Common.Routes.V1.Users.Refresh)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestModel refreshTokenRequestModel)
    {
        var authResponse = await _authService.RefreshTokenAsync(refreshTokenRequestModel);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResultModel
            {
                Errors = authResponse.Errors!
            });
        }

        return Ok(new AuthSuccessResultModel
        {
            Token = authResponse.Token!,
            ExpirationDate = authResponse.ExpirationDate,
            RefreshToken = authResponse.RefreshToken!
        });
    }
}