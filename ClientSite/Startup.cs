using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClientSite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClientSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();

            // add this
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = "https://demo.identityserver.io/";
                options.ClientId = "interactive.confidential.short"; // 75 seconds
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.SaveTokens = true;

                // for API add offline_access scope to get refresh_token

                options.GetClaimsFromUserInfoEndpoint = true;

                options.Events = new OpenIdConnectEvents
                {
                    // called if user clicks Cancel during login
                    OnAccessDenied = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/");
                        return Task.CompletedTask;
                    }
                };
            });

            // add this
            // part 2: unnecessary, Blazor ignores it
            //services.AddMvcCore(options =>
            //{
            //    var policy = new AuthorizationPolicyBuilder()
            //        .RequireAuthenticatedUser() // site-wide auth
            //        .Build();
            //    options.Filters.Add(new AuthorizeFilter(policy));
            //});

            // part 2: add these
            services.AddSingleton<BlazorServerAuthStateCache>();
            services.AddScoped<AuthenticationStateProvider, BlazorServerAuthState>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthentication(); // add this
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
