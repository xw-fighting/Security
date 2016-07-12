using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OpenIdConnect.AzureAdSample
{
    public class Startup
    {
        private const string GraphResourceID = "https://graph.windows.net";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
                sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Information);

            // Simple error page
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(ex.ToString());
                    }
                    else
                    {
                        throw;
                    }
                }
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            var clientId = Configuration["oidc:clientid"];
            var clientSecret = Configuration["oidc:clientsecret"];
            var authority = Configuration["oidc:authority"];
            var resource = "https://graph.windows.net";
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret, // for code flow
                Authority = authority,
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                // GetClaimsFromUserInfoEndpoint = true,
                Events = new OpenIdConnectEvents()
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        var request = context.HttpContext.Request;
                        var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
                        var credential = new ClientCredential(clientId, clientSecret);
                        var authContext = new AuthenticationContext(authority, AuthPropertiesTokenCache.ForCodeRedemption(context.Properties));

                        var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                            context.ProtocolMessage.Code, new Uri(currentUri), credential, resource);

                        context.HandleCodeRedemption();
                    }
                }
            });

            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/signout"))
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>Signing out {context.User.Identity.Name}<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/\">Sign In</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }

                if (context.Request.Path.Equals("/signout-remote"))
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                    {
                        RedirectUri = "/signedout"
                    });

                    return;
                }

                if (context.Request.Path.Equals("/signedout"))
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>You have been signed out.<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/\">Sign In</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }

                if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                {
                    await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
                    return;
                }

                // Summarize the authentication information
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync($"<html><body><h1>Users:</h1>{context.User.Identity.Name}<br>{Environment.NewLine}");

                await context.Response.WriteAsync("<h1>Claims:</h1>");
                await context.Response.WriteAsync("<table><tr><th>Type</th><th>Value</th></tr>");
                foreach (var claim in context.User.Claims)
                {
                    await context.Response.WriteAsync($"<tr><td>{claim.Type}</td><td>{claim.Value}</td></tr>");
                }
                await context.Response.WriteAsync("</table>");

                await context.Response.WriteAsync("<h1>Tokens:</h1>");
                try
                {
                    // Use ADAL to get the right token
                    var authContext = new AuthenticationContext(authority, AuthPropertiesTokenCache.ForApiCalls(context, CookieAuthenticationDefaults.AuthenticationScheme));
                    var credential = new ClientCredential(clientId, clientSecret);
                    string userObjectID = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                    var result = await authContext.AcquireTokenSilentAsync(resource, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                    await context.Response.WriteAsync($"access_token: {result.AccessToken}<br>{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync($"AquireToken error: {ex.Message}<br>{Environment.NewLine}");
                }

                await context.Response.WriteAsync("<h1>Actions:</h1>");
                await WriteHtmlLink(context, "Sign Out", "/signout");
                await WriteHtmlLink(context, "Sign Out from STS", "/signout-remote");
                await context.Response.WriteAsync($"</body></html>");
            });
        }

        private static Task WriteHtmlLink(HttpContext context, string name, string path)
        {
            return context.Response.WriteAsync($"<a href=\"{path}\">{name}</a><br>");
        }
    }
}

