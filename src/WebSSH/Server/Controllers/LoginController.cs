using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebSSH.Shared;

namespace WebSSH.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class LoginController : ControllerBase
    {
        public LoginController(ShellConfiguration shellConfiguration)
        {
            ShellConfiguration = shellConfiguration;
        }

        public ShellConfiguration ShellConfiguration { get; }

        public async Task<ClientLoginModel> Login(ClientLoginModel loginModel, [FromServices] ShellConfiguration shellConfiguration)
        {
            if (shellConfiguration.NeedAuthorization && (string.IsNullOrWhiteSpace(loginModel.Captcha) || HttpContext.Session.GetString(nameof(ClientLoginModel.Captcha)) != loginModel.Captcha.ToLowerInvariant()))
            {
                loginModel.Status = LoginStatus.Failed;
                loginModel.Message = "Wrong captcha";
            }
            else
            {
                var user = ShellConfiguration.Users.FirstOrDefault(u => !shellConfiguration.NeedAuthorization || u.UserName.ToLower() == loginModel.UserName.ToLower() && u.Password == loginModel.Password);

                if (user == null)
                {
                    loginModel.Status = LoginStatus.Failed;
                    loginModel.Message = "Wrong username or password";
                }
                else
                {
                    loginModel.Status = LoginStatus.Succesful;

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var properties = new AuthenticationProperties();

                    if (loginModel.Persist)
                    {
                        properties.IsPersistent = true;
                        properties.ExpiresUtc = DateTime.UtcNow.AddMonths(12);
                    }

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
                }
            }

            HttpContext.Session.Remove(nameof(ClientLoginModel.Captcha));
            return loginModel;
        }

        public async void Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public bool IsLogin()
        {
            return HttpContext.User.Identity.IsAuthenticated;
        }

        public IActionResult GenerateCaptcha()
        {
            HttpContext.Session.SetString(nameof(ClientLoginModel.Captcha), CaptchaUtils.GenerateCaptcha(200, 60, out var images).ToLowerInvariant());

            return File(images, "image/png");
        }
    }
}
