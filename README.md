# AzureGraphApiHelper
Uses Microsoft Graph API and Azure Graph API to perform basic Azure Active Directory actions.

This example uses VS 2017, .NET 4.6.1 and ASP.NET MVC. This also assumes that the contextual signed-in user has right amount of privileges to perform underlying actions in the form of delegated API permissions and not application permissions.

The class to look for is AzureWebUIapp.Utils.UsersGraphServices.

I do acknowledge that i have used tips from several forums including StackOverflow.com and other repositories here.

This is just an effort to share the consolidated functions for Azure AD that i had to develop.

Prior to running the project, do register the app in Azure AD and input correct keys as described in web.config.

When this code was written many actions related to Application, roles etc were still at beta or not available in Microsoft Graph so have been written against Microsoft Azure Graph API.

#Test line1
#Test line2
