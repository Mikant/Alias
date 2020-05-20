using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Alias {
    public class Program {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseEnvironment(Environments.Development)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
                });
    }
}
