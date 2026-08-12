using DAL.Common.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LegacyApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        private readonly ILogin _loginService;
        public LoginController(ILogin login)
        {
            _loginService = login;
            
        }



        [HttpPost("login")]
        public async Task<ApiResposne> Login(LoginBO login)
        {
            return await _loginService.Login(login);
        }

    }
}
