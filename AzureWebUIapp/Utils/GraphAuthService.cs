using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using WebAppGroupClaimsDotNet.Utils;
using System.Web.WebPages;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
using Microsoft.Owin.Host.SystemWeb;

namespace AzureWebUIapp.Utils
{
    public class GraphAuthService
    {
        public static String token;


        public static GraphServiceClient CreateGraphServiceClient()
        {
            var clientCredential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            string userObjectID = ClaimsPrincipal.Current.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant, new TokenDbCache(userObjectID));

            if (authenticationContext.TokenCache.Count == 0)
            {
                authenticationContext.TokenCache.Clear();
                TokenDbCache tokenCache = new TokenDbCache(userObjectID);
                tokenCache.Clear();
                HttpContext.Current.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
                string signOutUrl = ConfigHelper.PostLogoutRedirectUri;
                if (signOutUrl.Length == 0) throw new Exception("Configuration missing key - ida:SignOutUrl");

                signOutUrl = String.Format(signOutUrl, ConfigHelper.Tenant, ConfigHelper.PostLogoutRedirectUri);
                HttpContext.Current.Response.Redirect(signOutUrl);
            }
            else
            {

                AuthenticationResult res = null;

                try
                {
                    res = authenticationContext.AcquireTokenSilentAsync(ConfigHelper.GraphUrl, clientCredential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId)).Result;
                }
                catch (Exception ex)
                {
                    //res = authenticationContext.AcquireTokenAsync(ConfigHelper.GraphUrl, clientCredential).Result;
                }

                var delegateAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", res.AccessToken);

                    return Task.FromResult(0);
                });

                return new GraphServiceClient(delegateAuthProvider);
            }

            return null;
        }

        public static ActiveDirectoryClient GetActiveDirectoryClient()
        {
            Uri baseServiceUri = new Uri(ConfigHelper.AzureADGraphUrl);
            ActiveDirectoryClient activeDirectoryClient =
                new ActiveDirectoryClient(new Uri(baseServiceUri, ConfigHelper.Tenant),
                    async () => await AcquireTokenAsyncForApplication());
            return activeDirectoryClient;
        }

        public static async Task<string> AcquireTokenAsyncForApplication()
        {
            return await GetTokenForApplication().ConfigureAwait(false);
        }

        public static async Task<string> GetTokenForApplication()
        {
            var clientCredential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            string userObjectID = ClaimsPrincipal.Current.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant, new TokenDbCache(userObjectID));

            if (authenticationContext.TokenCache.Count == 0)
            {
                authenticationContext.TokenCache.Clear();
                TokenDbCache tokenCache = new TokenDbCache(userObjectID);
                tokenCache.Clear();
                HttpContext.Current.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
                string signOutUrl = ConfigHelper.PostLogoutRedirectUri;
                if (signOutUrl.Length == 0) throw new Exception("Configuration missing key - ida:SignOutUrl");

                signOutUrl = String.Format(signOutUrl, ConfigHelper.Tenant, ConfigHelper.PostLogoutRedirectUri);
                HttpContext.Current.Response.Redirect(signOutUrl);
            }
            else
            {
                AuthenticationResult res = null;

                try
                {
                    res = authenticationContext.AcquireTokenSilentAsync(ConfigHelper.AzureADGraphUrl, clientCredential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId)).Result;
                }
                catch (Exception ex)
                {
                    //res = authenticationContext.AcquireTokenAsync(ConfigHelper.AzureADGraphUrl, clientCredential).Result;
                }

                var token = res.AccessToken;

                return token;
            }

            return null;
        }


        //public static async Task<string> AcquireTokenAsync()
        //{
        //    if (token == null || token.IsEmpty())
        //    {
        //        throw new Exception("Authorization Required.");
        //    }
        //    return token;
        //}


    }
}