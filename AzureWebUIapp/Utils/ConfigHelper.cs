using System;
using System.Configuration;
using System.Globalization;


namespace WebAppGroupClaimsDotNet.Utils
{
    public class ConfigHelper
    {
        public static string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];
        internal static string AppKey { get; } = ConfigurationManager.AppSettings["ida:AppKey"];
        internal static string Tenant { get; } = ConfigurationManager.AppSettings["ida:Tenant"];
        internal static string Authority { get; } = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["ida:AADInstance"] , Tenant);

        internal static string AadInstance { get; } = ConfigurationManager.AppSettings["ida:AADInstance"];
        internal static string PostLogoutRedirectUri { get; } = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        internal static string GraphUrl { get; } = ConfigurationManager.AppSettings["ida:GraphUrl"];
        internal static string GraphApiVersion { get; } = ConfigurationManager.AppSettings["ida:GraphApiVersion"];

        internal static string AzureADGraphUrl { get; } = ConfigurationManager.AppSettings["ida:AzureADGraphUrl"];
        internal static string inviteRedirectURLBase { get; } = ConfigurationManager.AppSettings["ida:inviteRedirectURLBase"];
    }
}