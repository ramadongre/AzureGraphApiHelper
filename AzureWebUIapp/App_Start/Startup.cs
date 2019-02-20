using System;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebAppGroupClaimsDotNet.Utils;
using System.Web;
using AzureWebUIapp.Utils;

[assembly: OwinStartup(typeof(AzureWebUIapp.App_Start.Startup))]

namespace AzureWebUIapp.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            string aut = ConfigHelper.Authority;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                CookieManager = new SystemWebChunkingCookieManager()
            });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ConfigHelper.ClientId,
                    Authority = ConfigHelper.Authority,
                    RedirectUri = ConfigHelper.PostLogoutRedirectUri,
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error/message=" + context.Exception.Message);
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = async (context) =>
                        {

                           // GraphAuthService.code = context.Code;
                            var code = context.Code;
                            ClientCredential credential =
                            new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
                            string userObjectID = context.AuthenticationTicket.Identity.FindFirst(
                                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                            AuthenticationContext authContext = new AuthenticationContext(ConfigHelper.Authority, new TokenDbCache(userObjectID));

                            AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(code,
                                new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, ConfigHelper.GraphUrl);

                          //  AuthenticationResult azureGraphresult = await authContext.AcquireTokenByAuthorizationCodeAsync(code,
                          //      new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, ConfigHelper.AzureADGraphUrl);

                         //   GraphAuthService.token = azureGraphresult.AccessToken;


                        }
                    }
                });
        }
    }
}
