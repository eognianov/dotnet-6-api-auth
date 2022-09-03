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

    [HttpPost(Common.Routes.V1.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestModel createUserRequest)
    {
        if (!ModelState.IsValid)
        {
            
            return BadRequest(new AuthFailedResultModel
            {
                Errors = new [] {"Validation Errors"}
            });
        }
        
        var authResponse = await _authService.CreateUserAsync(createUserRequest);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResultModel
            {
                Errors = authResponse.Errors!
            });
        }

        return Ok(
            new AuthSuccessResultModel
            {
                Token = authResponse.Token!
            });
    }
}