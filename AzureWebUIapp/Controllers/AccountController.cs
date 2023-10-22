
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Security.Claims;
using WebAppGroupClaimsDotNet.Utils;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

namespace AzureWebUIapp.Controllers
{
    public class AccountController : Controller
    { //test comment
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // Signal OWIN to send an authorization request to Azure.
               HttpContext.GetOwinContext().Authentication.Challenge(
                  new AuthenticationProperties { RedirectUri = "/" },
                  OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        // Here we just clear the token cache, sign out the GraphServiceClient, and end the session with the web app.  
        public void SignOut()
        {
            if (Request.IsAuthenticated)
            {
                // Get the user's token cache and clear it.
                string userObjectId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

                TokenDbCache tokenCache = new TokenDbCache(userObjectId);
                tokenCache.Clear();
                HttpContext.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
            }


            //// Send an OpenID Connect sign-out request. 
            //HttpContext.GetOwinContext().Authentication.SignOut(
            //  CookieAuthenticationDefaults.AuthenticationType);
            //Response.Redirect("/");
        }
    }
}
