using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Alias.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Alias.Pages {
    [AllowAnonymous]
    public class LoginModel : PageModel {
        private readonly IServiceProvider _provider;

        public LoginModel(IServiceProvider provider) {
            _provider = provider;
        }

        public async Task<IActionResult> OnGetAsync(string username) {
            var returnUrl = Url.Content("~/");
            
            try {
                // Clear the existing external cookie
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            } catch {
                // ignored
            }

            // *** !!! This is where you would validate the user !!! ***
            // In this example we just log the user in
            // (Always log the user in for this demo)
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Player"),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties {
                IsPersistent = true,
                RedirectUri = this.Request.Host.Value
            };

            try {
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                using (var context = _provider.GetService<EFContext>()) {
                    var user = context.Users.FirstOrDefault(x => x.Name == username);

                    if (user == null) {
                        context.Users.Add(new User { Name = username });
                        await context.SaveChangesAsync();
                    }
                }

            } catch (Exception ex) {
                string error = ex.Message;
            }

            return LocalRedirect(returnUrl);
        }
    }
}
