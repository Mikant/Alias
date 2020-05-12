using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Alias.Services;
using Alias.ViewModels;
using Microsoft.AspNetCore.HttpOverrides;

namespace Alias {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSingleton<GameService>();
            services.AddScoped<IndexViewModel>();
            services.AddProtectedBrowserStorage();

            services.Configure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            );
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            app.UseForwardedHeaders();
            app.Use(async (context, next) => {
                if (context.Request.IsHttps || context.Request.Headers["X-Forwarded-Proto"] == Uri.UriSchemeHttps)
                    await next();

                else {
                    string queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
                    var https = "https://" + context.Request.Host + context.Request.Path + queryString;
                    context.Response.Redirect(https, true);
                }
            });

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();

            } else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
