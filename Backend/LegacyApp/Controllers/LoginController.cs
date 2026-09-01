using DAL.Common.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LegacyApp.Controllers
{
    /// <summary>
    /// Handles user authentication, credential validation, and JWT token issuance.
    /// Provides public entry point for logging into the application and establishing session context.
    /// </summary>
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

        /// <summary>
        /// Authenticates user credentials (username and password) and generates authorization token.
        /// Returns user identity, assigned roles, and menu permissions upon successful login.
        /// </summary>
        [HttpPost("login")]
        public async Task<ApiResposne> Login(LoginBO login)
        {
            return await _loginService.Login(login);
        }
    }
}
