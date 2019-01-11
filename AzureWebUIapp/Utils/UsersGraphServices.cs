using AzureWebUIapp.Models;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebAppGroupClaimsDotNet.Utils;

namespace AzureWebUIapp.Utils
{
    /// <summary>
    /// This class provides access to basic Microsoft Graph or Azure AD Graph related functions
    /// </summary>
    public class UsersGraphServices
    {
        /// <summary>
        /// Note that this function when called end up in PendingAcceptance/Completed/InProgress/Error
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="userDisplayName"></param>
        /// <param name="userEmailAddress"></param>
        /// <param name="inviteRedirectURL"></param>
        /// <param name="welcomeMessage"></param>
        /// <returns>Tuple<bool, string, string> represents returnStatus, ID of transaction and message respectively</returns>
        public async Task<Tuple<bool, string, string>> CreateGuestUser(GraphServiceClient graphClient, string userDisplayName, string userEmailAddress,
            string inviteRedirectURL, string welcomeMessage)
        {
            bool ActionStatus = false;
            string message = string.Empty;
            string userID = string.Empty;

            if (graphClient == null || string.IsNullOrEmpty(userDisplayName) || string.IsNullOrEmpty(userEmailAddress) || string.IsNullOrEmpty(inviteRedirectURL))
                return new Tuple<bool, string, string>(ActionStatus, null, "Invalid input");
            else
            {

                try
                {
                    // send invite to guest user
                    Invitation guestUserInvite = await graphClient.Invitations.Request().AddAsync(new Invitation
                    {
                        InvitedUserDisplayName = userDisplayName,
                        InvitedUserEmailAddress = userEmailAddress,
                        InviteRedirectUrl = inviteRedirectURL,// "https://myapps.microsoft.com/tenant name from config",
                        InvitedUserMessageInfo = new InvitedUserMessageInfo
                        {
                            CustomizedMessageBody = welcomeMessage
                        },
                        SendInvitationMessage = true
                    });

                    if (guestUserInvite != null)
                    {
                        if (guestUserInvite.Status.Equals("Error", StringComparison.CurrentCultureIgnoreCase))
                        {
                            message = "An error occured";
                        }
                        else
                        {
                            ActionStatus = true;
                            message = "Guest user creation was successful";
                            userID = guestUserInvite.InvitedUser.Id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
                finally
                {
                }
            }

            return new Tuple<bool, string, string>(ActionStatus, userID, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="emailID"></param>
        /// <param name="groupName"></param>
        /// <returns>Return value represents returnStatus, message, userObjectID, groupID respectively</returns>
        public async Task<Tuple<bool, string, string, string>> AssociateUserWithAGroup(GraphServiceClient graphClient, string emailID, string groupName)
        {
            bool ActionStatus = false;
            string message = string.Empty;

            string groupid = "", userObjectID = "";

            if (graphClient == null || string.IsNullOrEmpty(emailID) || string.IsNullOrEmpty(groupName))
                return new Tuple<bool, string, string, string>(ActionStatus, "Invalid input", null, null);

            try
            {
                IGraphServiceUsersCollectionPage _usersFilteredId = await graphClient.Users.Request().Filter($"Mail eq '" + emailID + "'").GetAsync();
                userObjectID = _usersFilteredId.Select(a => a.Id).SingleOrDefault().ToString();

                if (!string.IsNullOrEmpty(userObjectID))
                {
                    //find group Id by groupname 
                    IGraphServiceGroupsCollectionPage groupsCollectionPage = graphClient.Groups.Request().Filter($"DisplayName eq '" + groupName + "'").GetAsync().Result;

                    if (groupsCollectionPage != null)
                    {
                        groupid = groupsCollectionPage.Select(a => a.Id).SingleOrDefault().ToString();

                        if (groupid != null)
                        {
                            //check if the user is already associated
                            var associatedmember = await graphClient.Groups[groupid].Members.Request().GetAsync();
                            if (associatedmember.Where(a => a.Id == userObjectID).FirstOrDefault() == null)
                            {
                                Microsoft.Graph.User userToAdd = await graphClient.Users[userObjectID].Request().GetAsync();
                                await graphClient.Groups[groupid].Members.References.Request().AddAsync(userToAdd);

                                ActionStatus = true;
                                message = "User association with group was successful";
                            }
                            else
                            {
                                message = "User already associated with the group";
                            }
                        }
                        else
                            message = "Invalid group";
                    }
                    else
                        message = "Invalid group";
                }
                else
                {
                    message = "Invalid user";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new Tuple<bool, string, string, string>(ActionStatus, message, userObjectID, groupid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="emailID"></param>
        /// <param name="groupName"></param>
        /// <returns>Return value represents returnStatus, message, userObjectID, groupID respectively</returns>
        public async Task<Tuple<bool, string, string, string>> DeAssociateUserWithAGroup(GraphServiceClient graphClient, string emailID, string groupName)
        {
            bool ActionStatus = false;
            string message = string.Empty;

            string groupid = "", userObjectID = "";

            if (graphClient == null || string.IsNullOrEmpty(emailID) || string.IsNullOrEmpty(groupName))
                return new Tuple<bool, string, string, string>(ActionStatus, "Invalid input", null, null);

            try
            {
                IGraphServiceUsersCollectionPage _usersFilteredId = await graphClient.Users.Request().Filter($"Mail eq '" + emailID + "'").GetAsync();
                userObjectID = _usersFilteredId.Select(a => a.Id).SingleOrDefault().ToString();

                if (!string.IsNullOrEmpty(userObjectID))
                {
                    //find group Id by groupname 
                    IGraphServiceGroupsCollectionPage groupsCollectionPage = graphClient.Groups.Request().Filter($"DisplayName eq '" + groupName + "'").GetAsync().Result;

                    if (groupsCollectionPage != null)
                    {
                        groupid = groupsCollectionPage.Select(a => a.Id).SingleOrDefault().ToString();

                        if (groupid != null)
                        {
                            //check if the user is already associated
                            var associatedmember = await graphClient.Groups[groupid].Members.Request().GetAsync();
                            if (associatedmember.Where(a => a.Id == userObjectID).FirstOrDefault() != null)
                            {
                                await graphClient.Groups[groupid].Members[userObjectID].Reference.Request().DeleteAsync();

                                ActionStatus = true;
                                message = "User de-association with group was successful";
                            }
                            else
                            {
                                message = "User not associated with the group";
                            }
                        }
                        else
                            message = "Invalid group";
                    }
                    else
                        message = "Invalid group";
                }
                else
                {
                    message = "Invalid user";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new Tuple<bool, string, string, string>(ActionStatus, message, userObjectID, groupid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="azureGraphclient"></param>
        /// <param name="model"></param>
        /// <returns>Return values are status,message, user-object-id, application-role-id, application-service-principal-id</returns>
        public async Task<Tuple<bool, string, string, string, string>> AddApplicationRoleToUser(ActiveDirectoryClient azureGraphclient, string AppName, string UserEmailAddress,
                string AppRoleName)
        {
            Guid userObjectID = Guid.Empty, appobjectid = Guid.Empty, approleid = Guid.Empty, srvpr = Guid.Empty;

            bool ActionStatus = false;
            string message = string.Empty;

            if (azureGraphclient == null || string.IsNullOrEmpty(AppName) || string.IsNullOrEmpty(UserEmailAddress) || string.IsNullOrEmpty(AppRoleName))
                return new Tuple<bool, string, string, string, string>(ActionStatus, "Invalid input", null, null, null);
            else
            {
                try
                {
                    AppRoleAssignment assignment = new AppRoleAssignment();
                    assignment.CreationTimestamp = System.DateTime.Now;

                    var _usersFiltered = azureGraphclient.Users.Where(a => a.Mail == UserEmailAddress).ExecuteAsync().Result;
                    if (_usersFiltered != null)
                    {
                        userObjectID = Guid.Parse(_usersFiltered.CurrentPage.Select(a => a.ObjectId).SingleOrDefault().ToString());

                        var application = azureGraphclient.Applications.Where(a => a.DisplayName == AppName).ExecuteAsync().Result;
                        if (application != null)
                        {
                            var approle = application.CurrentPage.FirstOrDefault().AppRoles.Where(a => a.DisplayName == AppRoleName).FirstOrDefault();

                            if (approle != null)
                            {
                                approleid = Guid.Parse(approle.Id.ToString());

                                srvpr = Guid.Parse(azureGraphclient.ServicePrincipals.Where(a => a.DisplayName == AppName).ExecuteAsync().Result.CurrentPage.FirstOrDefault().ObjectId);

                                //check if assignment is already made                
                                var cc = azureGraphclient.Users[userObjectID.ToString()].AppRoleAssignments.ExecuteAsync().Result;
                                var approlesassigns = await AzureADExtensions.EnumerateAllAsync(cc);
                                var filtered = approlesassigns.Where(a => a.Id == approleid && a.PrincipalType == "User").FirstOrDefault();

                                if (filtered == null)
                                {
                                    assignment.PrincipalId = userObjectID;
                                    assignment.PrincipalType = "User";
                                    assignment.ResourceId = srvpr;
                                    assignment.Id = approleid;

                                    await azureGraphclient.Users[userObjectID.ToString()].AppRoleAssignments.AddAppRoleAssignmentAsync(assignment);

                                    ActionStatus = true;
                                    message = "User successfully associated with application role";
                                }
                                else
                                {
                                    message = "user already associated with application role";
                                }
                            }
                            else
                            {
                                message = "Invalid application role";
                            }
                        }
                        else
                        {
                            message = "Invalid application";
                        }
                    }
                    else
                    {
                        message = "Invalid user";
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return new Tuple<bool, string, string, string, string>(ActionStatus, message, userObjectID.ToString(), approleid.ToString(), srvpr.ToString());
        }

        public async Task<Tuple<bool, string, string, string, string>> RemoveApplicationRoleFromUser(ActiveDirectoryClient azureGraphclient, string accessToken, string AzureADGraphUrl, string Tenant,
            string AppName, string UserEmailAddress, string AppRoleName)
        {
            List<ResultsItem> obj = new List<ResultsItem>();
            Guid userObjectID = Guid.Empty, approleid = Guid.Empty, srvpr = Guid.Empty;

            bool ActionStatus = false;
            string message = string.Empty;

            if (azureGraphclient == null || string.IsNullOrEmpty(AzureADGraphUrl) || string.IsNullOrEmpty(Tenant) ||
                string.IsNullOrEmpty(AppName) || string.IsNullOrEmpty(UserEmailAddress) || string.IsNullOrEmpty(AppRoleName))
                return new Tuple<bool, string, string, string, string>(ActionStatus, "Invalid input", null, null, null);
            else
            {

                try
                {
                    var _usersFiltered = azureGraphclient.Users.Where(a => a.Mail == UserEmailAddress).Expand(p => p.AppRoleAssignments).ExecuteAsync().Result;
                    if (_usersFiltered != null)
                    {
                        userObjectID = Guid.Parse(_usersFiltered.CurrentPage.Select(a => a.ObjectId).SingleOrDefault().ToString());

                        var application = azureGraphclient.Applications.Where(a => a.DisplayName == AppName).ExecuteAsync().Result;
                        if (application != null)
                        {
                            var approle = application.CurrentPage.FirstOrDefault().AppRoles.Where(a => a.DisplayName == AppRoleName).FirstOrDefault();

                            if (approle != null)
                            {
                                approleid = Guid.Parse(approle.Id.ToString());

                                srvpr = Guid.Parse(azureGraphclient.ServicePrincipals.Where(a => a.DisplayName == AppName).ExecuteAsync().Result.CurrentPage.FirstOrDefault().ObjectId);

                                //check if assignment is already made                
                                var cc = _usersFiltered.CurrentPage.FirstOrDefault();
                                var approlesassigns = AzureADExtensions.EnumerateAllAsync(cc.AppRoleAssignments).Result;
                                var filtered = approlesassigns.Where(a => a.Id == approleid && a.PrincipalType == "User").FirstOrDefault();

                                if (filtered != null)
                                {
                                    var roleassignObjectID = filtered.ObjectId;

                                    await RemoveRoleFromUser(accessToken, AzureADGraphUrl, Tenant, userObjectID, roleassignObjectID.ToString());

                                    ActionStatus = true;
                                    message = "Application role was succefully removed from user";
                                }
                                else
                                {
                                    message = "User not associated with application role";
                                }
                            }
                            else
                            {
                                message = "Invalid application role";
                            }
                        }
                        else
                        {
                            message = "Invalid application";
                        }
                    }
                    else
                    {
                        message = "Invalid user";
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return new Tuple<bool, string, string, string, string>(ActionStatus, message, userObjectID.ToString(), approleid.ToString(), srvpr.ToString());
        }

        private async Task RemoveRoleFromUser(string accessToken, string AzureADGraphUrl, string Tenant, Guid userId, string roleObjectId)
        {
            var uri = string.Format("{0}/users/{1}/appRoleAssignments/{2}?api-version=1.5", Tenant, userId, roleObjectId);
            await ExecuteRequest<object>(accessToken, AzureADGraphUrl, Tenant, uri, HttpMethod.Delete);
        }
        private async Task<string> ExecuteRequest<T>(string accessToken, string AzureADGraphUrl, string Tenant, string uri, HttpMethod method = null, Object body = null) where T : class
        {
            if (method == null) method = HttpMethod.Get;

            string response;

            using (var httpClient = new HttpClient { BaseAddress = getServicePointUri(AzureADGraphUrl, Tenant) })
            {
                var request = new HttpRequestMessage(method, uri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (body != null)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                }
                var responseMessage = await httpClient.SendAsync(request).ConfigureAwait(false);
                responseMessage.EnsureSuccessStatusCode();
                response = await responseMessage.Content.ReadAsStringAsync();
            }
            return response;
        }

        private Uri getServicePointUri(string AzureADGraphUrl, string Tenant)
        {
            Uri servicePointUri = new Uri(AzureADGraphUrl);
            Uri serviceRoot = new Uri(servicePointUri, Tenant);
            return serviceRoot;
        }

        public async Task<Tuple<bool, string, string, string, string>> AssignExtensionAttributeToUser(ActiveDirectoryClient azureGraphclient, string UserEmailAddress, string AppName,
            string AppExtAttribName, string ExtAttribValue)
        {
            Guid userObjectID = Guid.Empty, approleid = Guid.Empty, srvpr = Guid.Empty;

            bool ActionStatus = false;
            string message = string.Empty;

            if (azureGraphclient == null || string.IsNullOrEmpty(AppName) || string.IsNullOrEmpty(AppExtAttribName) || string.IsNullOrEmpty(ExtAttribValue))
                return new Tuple<bool, string, string, string, string>(ActionStatus, "Invalid input", null, null, null);
            else
            {
                try
                {
                    //get instance of extension attribute
                    var appi = azureGraphclient.Applications.Where(s => s.DisplayName == AppName).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                    if (appi != null)
                    {
                        var exts = azureGraphclient.Applications[appi.ObjectId].ExtensionProperties.ExecuteAsync().Result.EnumerateAllAsync().Result;
                        //var appexts = ((Application)appi).ExtensionProperties;

                        IExtensionProperty extAttrib = exts.Where(a => a.Name == AppExtAttribName).FirstOrDefault();

                        if (extAttrib != null)
                        {
                            Microsoft.Azure.ActiveDirectory.GraphClient.User userInstance = (Microsoft.Azure.ActiveDirectory.GraphClient.User)
                                    azureGraphclient.Users.Where(a => a.Mail == UserEmailAddress).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                            if (userInstance != null)
                            {
                                userInstance.SetExtendedProperty(AppExtAttribName, ExtAttribValue);
                                await userInstance.UpdateAsync();
                                userInstance.GetContext().SaveChanges();

                                ActionStatus = true;
                                message = "Successfully assigned application extension property to user";
                            }
                            else
                            {
                                message = "Invalid user";
                            }
                        }
                        else
                        {
                            message = "Attribute name not associated with application";
                        }
                    }
                    else
                    {
                        message = "Invalid application";
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return new Tuple<bool, string, string, string, string>(ActionStatus, message, userObjectID.ToString(), approleid.ToString(), srvpr.ToString());
        }
        public async Task<Tuple<bool, string, Microsoft.Graph.User>> GetUserByEmail(GraphServiceClient graphClient, string UserEmailAddress)
        {
            bool ActionStatus = false;
            string message = string.Empty;
            Microsoft.Graph.User returnUser = null;

            try
            {
                IGraphServiceUsersCollectionPage _usersFilteredId = await graphClient.Users.Request().Filter($"Mail eq '" + UserEmailAddress + "'").GetAsync();
                returnUser = _usersFilteredId.FirstOrDefault();

                ActionStatus = true;
                message = "Valid user found";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new Tuple<bool, string, Microsoft.Graph.User>(ActionStatus, message, returnUser);
        }

        public async Task<Tuple<bool,string,List<Microsoft.Graph.User>>> GetGuestUsers(GraphServiceClient graphClient)
        {
            bool isOk = false;
            string message = "";

            List<Microsoft.Graph.User> users = new List<Microsoft.Graph.User>();
            try
            {
                var userpage = graphClient.Users.Request().GetAsync().Result;

                if (userpage != null)
                    users = userpage.AsEnumerable().ToList();

                isOk = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new Tuple<bool, string, List<Microsoft.Graph.User>>(isOk,message,users);

        }

        public async Task<Tuple<bool, string>> CreateUserExtensionPropertyForAnApplication(ActiveDirectoryClient azureGraphclient, string AppName, string AppExtAttribName)
        {
            bool ActionStatus = false;
            string message = string.Empty;

            if (azureGraphclient == null || string.IsNullOrEmpty(AppName) || string.IsNullOrEmpty(AppExtAttribName))
                return new Tuple<bool, string>(ActionStatus, "Invalid input");
            else
            {
                try
                {
                    //get instance of extension attribute
                    var appi = azureGraphclient.Applications.Where(s => s.DisplayName == AppName).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                    if (appi != null)
                    {
                        var exts = azureGraphclient.Applications[appi.ObjectId].ExtensionProperties.ExecuteAsync().Result.EnumerateAllAsync().Result;
                        //var appexts = ((Application)appi).ExtensionProperties;

                        IExtensionProperty extAttrib = exts.Where(a => a.Name == AppExtAttribName).FirstOrDefault();

                        if (extAttrib == null)
                        {
                            ExtensionProperty newAttrib = new ExtensionProperty
                            {
                                Name = AppExtAttribName,
                                DataType = "String",
                                TargetObjects = { "User" }
                            };

                            ((Application)appi).ExtensionProperties.Add(newAttrib);
                            await ((Application)appi).UpdateAsync();

                            ActionStatus = true;
                            message = "Successfully created application extension property";

                        }
                        else
                        {
                            message = "Attribute already associated with application";
                        }
                    }
                    else
                    {
                        message = "Invalid application";
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return new Tuple<bool, string>(ActionStatus, message);
        }

        public async Task<Tuple<bool, string, string>> UpdateUser(ActiveDirectoryClient azureGraphclient, string UserEmailAddress, string title,
            string Department, string City, string Phone)
        {
            string userObjectID = string.Empty;

            bool ActionStatus = false;
            string message = string.Empty;

            if (azureGraphclient == null || string.IsNullOrEmpty(UserEmailAddress))
                return new Tuple<bool, string, string>(ActionStatus, "Invalid input", null);
            else
            {
                try
                {
                    Microsoft.Azure.ActiveDirectory.GraphClient.User userInstance = (Microsoft.Azure.ActiveDirectory.GraphClient.User)
                            azureGraphclient.Users.Where(a => a.Mail == UserEmailAddress).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                    if (userInstance != null)
                    {
                        userObjectID = userInstance.ObjectId;

                        userInstance.JobTitle = title;
                        userInstance.Department = Department;
                        userInstance.City = City;
                        userInstance.TelephoneNumber = Phone;

                        await userInstance.UpdateAsync();
                        userInstance.GetContext().SaveChanges();

                        ActionStatus = true;
                        message = "Successfully updated user";
                    }
                    else
                    {
                        message = "Invalid user";
                    }

                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            return new Tuple<bool, string, string>(ActionStatus, message, userObjectID.ToString());
        }
    }

    public static class AzureADExtensions
    {
        public static Task<IEnumerable<T>> EnumerateAllAsync<T>(
       this IPagedCollection<T> pagedCollection)
        {
            return EnumerateAllAsync(pagedCollection, Enumerable.Empty<T>());
        }

        private static async Task<IEnumerable<T>> EnumerateAllAsync<T>(
            this IPagedCollection<T> pagedCollection,
            IEnumerable<T> previousItems)
        {
            var newPreviousItems = previousItems.Concat(pagedCollection.CurrentPage);

            if (pagedCollection.MorePagesAvailable == false)
                return newPreviousItems;

            var newPagedCollection = await pagedCollection.GetNextPageAsync();
            return await EnumerateAllAsync(newPagedCollection, newPreviousItems);
        }
    }
}