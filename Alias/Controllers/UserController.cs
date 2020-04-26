using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alias.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller {
        private readonly IServiceProvider _provider;

        public UserController(IServiceProvider provider) {
            _provider = provider;
        }

        [HttpGet("[action]")]
        public string[] GetWords() {
            if (!User.Identity.IsAuthenticated)
                return Array.Empty<string>();

            using var context = _provider.GetService<EFContext>();

            var user = context.Users
                .AsNoTracking()
                .First(x => x.Name == User.Identity.Name);

            var words = user
                .Words;

            if (string.IsNullOrWhiteSpace(words))
                return Array.Empty<string>();

            return JsonSerializer.Deserialize<string[]>(words);
        }

        [HttpPost("[action]")]
        public void SetWords(string[] words) {
            if (!User.Identity.IsAuthenticated)
                return;

            using var context = _provider.GetService<EFContext>();

            var user = context.Users
                .AsNoTracking()
                .First(x => x.Name == User.Identity.Name);

            user.Words = JsonSerializer.Serialize(words);

            context.Update(user);
            context.SaveChanges();
        }
    }
}
