using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace LilyMarket.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerHandler;
    private readonly LoginUserHandler _loginHandler;

    public AuthController(RegisterUserHandler registerHandler, LoginUserHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    //POST /api/auth/register — регистрация нового пользователя
    //принимает email (@sfedu.ru), пароль, имя
    //возвращает JWT-токен и данные пользователя
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _registerHandler.Handle(request, ct);
        //201 Created — пользователь создан, возвращаем токен
        return CreatedAtAction(nameof(Register), result);
    }

    //POST /api/auth/login — вход по email и паролю
    //возвращает JWT-токен для авторизации в других запросах
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _loginHandler.Handle(request, ct);
        //200 OK — возвращаем токен
        return Ok(result);
    }
}