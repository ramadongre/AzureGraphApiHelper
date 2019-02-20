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


        public static GraphServiceClient CreateGraphServiceClient(bool useApplicationContext = false)
        {
            var clientCredential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);

            string userObjectID = ClaimsPrincipal.Current.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            AuthenticationContext authenticationContext = null;

            if (!useApplicationContext)
            {
                authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant, new TokenDbCache(userObjectID));
            }
            else
                authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant);


            if (authenticationContext.TokenCache.Count == 0 && !useApplicationContext)
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
                    if (!useApplicationContext)
                        res = authenticationContext.AcquireTokenSilentAsync(ConfigHelper.GraphUrl, clientCredential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId)).Result;
                    else
                        res = authenticationContext.AcquireTokenAsync(ConfigHelper.GraphUrl, clientCredential).Result;


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

        public static ActiveDirectoryClient GetActiveDirectoryClient(bool useApplicationContext = false)
        {
            Uri baseServiceUri = new Uri(ConfigHelper.AzureADGraphUrl);
            ActiveDirectoryClient activeDirectoryClient =
                new ActiveDirectoryClient(new Uri(baseServiceUri, ConfigHelper.Tenant),
                    async () => await AcquireTokenAsyncForApplication(useApplicationContext));
            return activeDirectoryClient;
        }

        public static async Task<string> AcquireTokenAsyncForApplication(bool useApplicationContext = false)
        {
            return await GetTokenForApplication(useApplicationContext).ConfigureAwait(false);
        }

        public static async Task<string> GetTokenForApplication(bool useApplicationContext = false, bool useAzureADGraph=true)
        {
            var clientCredential = new ClientCredential(ConfigHelper.ClientId, ConfigHelper.AppKey);
            string userObjectID = ClaimsPrincipal.Current.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            AuthenticationContext authenticationContext = null;

            if (!useApplicationContext)
                authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant, new TokenDbCache(userObjectID));
            else
                authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant);

            if (authenticationContext.TokenCache.Count == 0 && !useApplicationContext)
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
                    if(!useApplicationContext)
                        res = authenticationContext.AcquireTokenSilentAsync((useAzureADGraph? ConfigHelper.AzureADGraphUrl:ConfigHelper.GraphUrl) , 
                            clientCredential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId)).Result;
                    else
                        res = authenticationContext.AcquireTokenAsync((useAzureADGraph ? ConfigHelper.AzureADGraphUrl : ConfigHelper.GraphUrl), clientCredential).Result;
                }
                catch (Exception ex)
                {
                }

                var token = res.AccessToken;

                return token;
            }

            return null;
        }
    }
}