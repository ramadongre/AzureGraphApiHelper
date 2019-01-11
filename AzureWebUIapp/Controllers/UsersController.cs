using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AzureWebUIapp.Models;
using AzureWebUIapp.Utils;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Graph;
using WebAppGroupClaimsDotNet.Utils;

namespace AzureWebUIapp.Controllers
{

    [Authorize]
    public class UsersController : Controller
    {
        UsersGraphServices usersService = new UsersGraphServices();
        public ViewResult GuestUsers()
        {
            Tuple<bool, string, List<Microsoft.Graph.User>> results = new Tuple<bool, string, List<Microsoft.Graph.User>>(false, string.Empty, null);

            try
            {
                var graphServiceClient = GraphAuthService.CreateGraphServiceClient();
                results = usersService.GetGuestUsers(graphServiceClient).Result;
            }
            catch (Exception ex)
            {
                results = new Tuple<bool, string, List<Microsoft.Graph.User>>(false, ex.Message, null);

            }

            return View("GuestUsers", results);
        }
        public ViewResult AddGuestUser()
        {
            return View("CreateGuestUsers");
        }

        public ViewResult GetUser()
        {
            return View();
        }

        [HttpPost]
        public ViewResult GetUser(GuestUserModel model)
        {
            List<Tuple<string, List<ResultsItem>>> tupAppRoles = new List<Tuple<string, List<ResultsItem>>>();
            List<ResultsItem> lstUserAppRoles = new List<ResultsItem>();
            List<ResultsItem> exts = new List<ResultsItem>();

            if (ModelState.IsValid)
            {
                try
                {
                    //get application roles
                    var azureClient = GraphAuthService.GetActiveDirectoryClient();

                    var user = azureClient.Users.Where(a => a.Mail == model.UserEmailAddress).Expand(p => p.AppRoleAssignments).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                    if (user != null)
                    {
                        var cc = user.AppRoleAssignments;
                        var approlesassigns = AzureADExtensions.EnumerateAllAsync(cc).Result;
                        var filtered = approlesassigns.Where(a => a.PrincipalType == "User");

                        //now get role names for those
                        var fapplications = azureClient.Applications.ExecuteAsync().Result;
                        if (fapplications != null)
                        {
                            IEnumerable<IApplication> allapps = AzureADExtensions.EnumerateAllAsync(fapplications).Result;

                            foreach (IApplication app in allapps)
                            {
                                string applicationname = app.DisplayName;

                                var fapplication = azureClient.Applications.Where(a => a.DisplayName == applicationname).ExecuteAsync().Result;
                                if (fapplication != null)
                                {
                                    lstUserAppRoles = new List<ResultsItem>();

                                    var myroles = fapplication.CurrentPage.FirstOrDefault().AppRoles.Where(a => filtered.Select(b => b.Id).Contains(a.Id));

                                    foreach (AppRole r in myroles)
                                        lstUserAppRoles.Add(new ResultsItem() { Id = r.DisplayName, Display = r.Description });

                                    tupAppRoles.Add(new Tuple<string, List<ResultsItem>>(app.DisplayName, lstUserAppRoles));
                                }
                            }
                        }
                    }

                    //get extension attributes                

                    Microsoft.Azure.ActiveDirectory.GraphClient.User myuser = (Microsoft.Azure.ActiveDirectory.GraphClient.User)azureClient.Users.Where(u => u.Mail.Equals(
                                        model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase)).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                    if (myuser != null)
                    {
                        foreach (var s in myuser.GetExtendedProperties())
                        {
                            exts.Add(new ResultsItem() { Id = s.Key, Display = (s.Value == null ? "" : s.Value.ToString()) });
                        }
                    }


                    return View("GetUserDetails", new UserDetails() { isOk = true, message = "", exts = exts, tupAppRoles = tupAppRoles, user = myuser });

                }
                catch (Exception ex)
                {
                    model.status = false;
                    model.resultantMessage = ex.Message;
                }
            }

            return View("GetUser", model);
        }

        public ViewResult ListTenantApps()
        {
            List<ResultsItem> lst = new List<ResultsItem>();
            bool isok = false;
            string message = "";

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps.OrderBy(a => a.DisplayName))
                    lst.Add(new ResultsItem() { Id = app.DisplayName, Display = app.DisplayName });

                isok = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return View(new Tuple<bool, string, List<ResultsItem>>(isok, message, lst));
        }

        public ViewResult UpdateUser()
        {
            return View("UpdateUser");
        }

        public ViewResult ShowUser(Tuple<bool, string, string, Microsoft.Azure.ActiveDirectory.GraphClient.User> tup)
        {
            return View("ShowUser", tup);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateUser(UpdateUserModel model)
        {
            List<ResultsItem> items = new List<ResultsItem>();

            if (ModelState.IsValid)
            {
                try
                {
                    var azureClient = GraphAuthService.GetActiveDirectoryClient();

                    Tuple<bool, string, string> tup = await usersService.UpdateUser(azureClient, model.UserEmailAddress, model.Jobtitle,
                        model.department, model.City, model.phone);


                    Microsoft.Azure.ActiveDirectory.GraphClient.User user = (Microsoft.Azure.ActiveDirectory.GraphClient.User)azureClient.Users.Where(u => u.Mail.Equals(
                                        model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase)).ExecuteAsync().Result.CurrentPage.FirstOrDefault();



                    return View("ShowUser", new Tuple<bool, string, string, Microsoft.Azure.ActiveDirectory.GraphClient.User>(
                        tup.Item1, tup.Item2, model.UserEmailAddress, user));
                }
                catch (Exception ex)
                {
                    model.status = false;
                    model.resultantMessage = ex.Message;
                }
            }

            return View("UpdateUser", model);
        }

        public ViewResult AssociateUserWithGroup()
        {
            return View("AssociateUserWithGroup");
        }

        public ViewResult DeAssociateUserWithGroup()
        {
            return View("DeAssociateUserWithGroup");
        }

        public ViewResult ListGroupMembers(Tuple<bool, string, string, List<ResultsItem>> tup)
        {
            return View("ListGroupMembers", tup);
        }

        public ActionResult GetApplicationRoles(string appName)
        {
            List<ResultsItem> lst = new List<ResultsItem>();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();
                var application = azureClient.Applications.Where(a => a.DisplayName == appName).ExecuteAsync().Result;
                if (application != null)
                {
                    var approles = application.CurrentPage.FirstOrDefault().AppRoles;

                    foreach (AppRole r in approles)
                        lst.Add(new ResultsItem() { Id = r.DisplayName, Display = r.Description });
                }
            }
            catch (Exception ex)
            {

            }

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetUserApplicationRoles(string appName, string useremail)
        {
            List<ResultsItem> lst = new List<ResultsItem>();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();

                var user = azureClient.Users.Where(a => a.Mail == useremail).Expand(p => p.AppRoleAssignments).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                if (user != null)
                {
                    var cc = user.AppRoleAssignments;
                    var approlesassigns = AzureADExtensions.EnumerateAllAsync(cc).Result;
                    var filtered = approlesassigns.Where(a => a.PrincipalType == "User");

                    //now get role names for those
                    var fapplication = azureClient.Applications.Where(a => a.DisplayName == appName).ExecuteAsync().Result;
                    if (fapplication != null)
                    {
                        var myroles = fapplication.CurrentPage.FirstOrDefault().AppRoles.Where(a => filtered.Select(b => b.Id).Contains(a.Id));

                        foreach (AppRole r in myroles)
                            lst.Add(new ResultsItem() { Id = r.DisplayName, Display = r.Description });
                    }
                }
            }
            catch (Exception ex)
            { }


            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetApplicationExtAttribs(string appName)
        {
            List<ResultsItem> lst = new List<ResultsItem>();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();
                var application = azureClient.Applications.Where(a => a.DisplayName == appName).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
                if (application != null)
                {

                    var exts = azureClient.Applications[application.ObjectId].ExtensionProperties;

                    foreach (IExtensionProperty r in exts.ExecuteAsync().Result.EnumerateAllAsync().Result)
                        lst.Add(new ResultsItem() { Id = r.Name, Display = r.Name });
                }
            }
            catch (Exception ex)
            { }

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        public ViewResult AssignExtAttribToUser()
        {
            ManageApplicationExtensionsAssignment model = new ManageApplicationExtensionsAssignment();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps.OrderBy(a => a.DisplayName))
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.message = ex.Message;
            }

            return View("AssignExtAttribToUser", model);
        }

        public ViewResult ListUserClaims()
        {
            List<ResultsItem> lst = new List<ResultsItem>();

            if (HttpContext.User.Identity.IsAuthenticated)
            {
                var userClaimsIdentity = (ClaimsIdentity)HttpContext.User.Identity;
                foreach (Claim c in userClaimsIdentity.Claims)
                {
                    lst.Add(new ResultsItem() { Id = c.Type, Display = c.Value });
                }
            }

            return View("ListUserClaims", new Tuple<string, List<ResultsItem>>(HttpContext.User.Identity.Name, lst));
        }

        [HttpPost]
        public async Task<ActionResult> AssignExtAttribToUser(ManageApplicationExtensionsAssignment model)
        {
            List<ResultsItem> items = new List<ResultsItem>();

            if (ModelState.IsValid)
            {
                try
                {
                    var azureClient = GraphAuthService.GetActiveDirectoryClient();

                    Tuple<bool, string, string, string, string> tup = await usersService.AssignExtensionAttributeToUser(azureClient, model.UserEmailAddress, model.AppName,
                        model.AppExtAttribName, model.ExtAttribValue);


                    Microsoft.Azure.ActiveDirectory.GraphClient.User user = (Microsoft.Azure.ActiveDirectory.GraphClient.User)azureClient.Users.Where(u => u.Mail.Equals(
                                        model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase)).ExecuteAsync().Result.CurrentPage.FirstOrDefault();

                    if (user != null)
                    {
                        foreach (var s in user.GetExtendedProperties())
                        {
                            items.Add(new ResultsItem() { Id = s.Key, Display = (s.Value == null ? "" : s.Value.ToString()) });
                        }
                    }

                    return View("ListuserExtAttributes", new Tuple<bool, string, string, List<ResultsItem>>(tup.Item1, tup.Item2, model.UserEmailAddress, items));
                }
                catch (Exception ex)
                {
                    model.isOk = false;
                    model.message = ex.Message;
                }
            }

            return View("AssignExtAttribToUser", model);
        }

        public ViewResult AddAppRoleToUser()
        {
            AddAppRoleToUserModel model = new AddAppRoleToUserModel();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps)
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.isOk = false;
                model.message = ex.Message;
            }

            return View("AddAppRoleToUser", model);
        }

        public async Task<ViewResult> ListRoleMembers(Tuple<bool, string, string, List<ResultsItem>> tup)
        {
            return View("ListRoleMembers", tup);
        }

        public async Task<ViewResult> ListuserExtAttributes(Tuple<bool, string, string, List<ResultsItem>> tup)
        {
            return View("ListExtAttributeMembers", tup);
        }

        public ViewResult RemoveAppRoleFromUser()
        {
            AddAppRoleToUserModel model = new AddAppRoleToUserModel();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient();

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps)
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.message = ex.Message;
            }

            return View("RemoveAppRoleFromUser", model);
        }

        [HttpPost]
        public async Task<ActionResult> RemoveAppRoleFromUser(AddAppRoleToUserModel model)
        {
            List<ResultsItem> items = new List<ResultsItem>();

            if (ModelState.IsValid)
            {
                try
                {
                    var azureClient = GraphAuthService.GetActiveDirectoryClient();

                    string token = await GraphAuthService.GetTokenForApplication();

                    Tuple<bool, string, string, string, string> tup = await usersService.RemoveApplicationRoleFromUser(azureClient, token,
                        ConfigHelper.AzureADGraphUrl, ConfigHelper.Tenant, model.AppName, model.UserEmailAddress, model.AppRoleName);

                    // Get group members.                     
                    var appRoleAssignmentsPaged = await azureClient.ServicePrincipals
                        .GetByObjectId(tup.Item5)
                        .AppRoleAssignedTo
                        .ExecuteAsync();
                    var appRoleAssignments = await AzureADExtensions.EnumerateAllAsync(appRoleAssignmentsPaged);

                    Guid approleid = Guid.Parse(tup.Item4);

                    var users = appRoleAssignments
                        .Where(a => a.Id == approleid && a.PrincipalType == "User")
                        .Select(a => new { Id = a.PrincipalId.ToString(), Name = a.PrincipalDisplayName })
                        .ToList();

                    if (users != null)
                    {
                        foreach (var s in users)
                        {
                            items.Add(new ResultsItem() { Id = s.Id, Display = s.Name });
                        }
                    }

                    return View("ListRoleMembers", new Tuple<bool, string, string, List<ResultsItem>>(tup.Item1, tup.Item2, model.AppRoleName, items));
                }
                catch (Exception ex)
                {
                    model.isOk = false;
                    model.message = ex.Message;
                }

            }

            return View("RemoveAppRoleFromUser", model);
        }


        [HttpPost]
        public async Task<ActionResult> AddAppRoleToUser(AddAppRoleToUserModel model)
        {
            List<ResultsItem> items = new List<ResultsItem>();

            if (ModelState.IsValid)
            {
                try
                {
                    var azureClient = GraphAuthService.GetActiveDirectoryClient();

                    Tuple<bool, string, string, string, string> tup = await usersService.AddApplicationRoleToUser(azureClient, model.AppName, model.UserEmailAddress, model.AppRoleName);

                    // Get group members.                     
                    var appRoleAssignmentsPaged = await azureClient.ServicePrincipals
                        .GetByObjectId(tup.Item5)
                        .AppRoleAssignedTo
                        .ExecuteAsync();
                    var appRoleAssignments = await AzureADExtensions.EnumerateAllAsync(appRoleAssignmentsPaged);

                    Guid approleid = Guid.Parse(tup.Item4);

                    var users = appRoleAssignments
                        .Where(a => a.Id == approleid && a.PrincipalType == "User")
                        .Select(a => new { Id = a.PrincipalId.ToString(), Name = a.PrincipalDisplayName })
                        .ToList();

                    if (users != null)
                    {
                        foreach (var s in users)
                        {
                            items.Add(new ResultsItem() { Id = s.Id, Display = s.Name });
                        }
                    }

                    return View("ListRoleMembers", new Tuple<bool, string, string, List<ResultsItem>>(tup.Item1, tup.Item2, model.AppRoleName, items));
                }
                catch (Exception ex)
                {
                    model.isOk = false;
                    model.message = ex.Message;
                }
            }

            return View("AddAppRoleToUser", model);
        }

        [HttpPost]
        public async Task<ActionResult> DeAssociateUserWithGroup(UserGroupAssociation model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient();

                    Tuple<bool, string, string, string> tup = await usersService.DeAssociateUserWithAGroup(graphServiceClient, model.UserEmailAddress, model.GroupName);

                    //get member of this group, which must include the new addition
                    List<ResultsItem> items = new List<ResultsItem>();

                    // Get group members. 
                    IGroupMembersCollectionWithReferencesPage members = await graphServiceClient.Groups[tup.Item4].Members.Request().GetAsync();

                    if (members?.Count > 0)
                    {
                        foreach (Microsoft.Graph.User user in members)
                        {
                            // Get member properties.
                            items.Add(new ResultsItem() { Display = user.DisplayName, Id = user.Id });
                        }
                    }

                    return View("ListGroupMembers", new Tuple<bool, string, string, List<ResultsItem>>(tup.Item1, tup.Item2, model.GroupName, items));
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            return View("AssociateUserWithGroup", model);
        }

        [HttpPost]
        public async Task<ActionResult> AssociateUserWithGroup(UserGroupAssociation model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient();

                    Tuple<bool, string, string, string> tup = await usersService.AssociateUserWithAGroup(graphServiceClient, model.UserEmailAddress, model.GroupName);

                    //get member of this group, which must include the new addition
                    List<ResultsItem> items = new List<ResultsItem>();

                    // Get group members. 
                    IGroupMembersCollectionWithReferencesPage members = await graphServiceClient.Groups[tup.Item4].Members.Request().GetAsync();

                    if (members?.Count > 0)
                    {
                        foreach (Microsoft.Graph.User user in members)
                        {
                            // Get member properties.
                            items.Add(new ResultsItem() { Display = user.DisplayName, Id = user.Id });
                        }
                    }

                    return View("ListGroupMembers", new Tuple<bool, string, string, List<ResultsItem>>(tup.Item1, tup.Item2, model.GroupName, items));
                }
                catch (Exception ex)
                {
                    model.isOk = false;
                    model.message = ex.Message;
                }

            }

            return View("AssociateUserWithGroup", model);
        }

        public ActionResult GuestUsersCreateResult(GuestUserModel _objGstUsrMdl)
        {
            return View(_objGstUsrMdl);
        }

        public async Task<ActionResult> CreateGuestUser(GuestUserModel _objGstUsrMdl)
        {
            ResultsViewModel results = new ResultsViewModel(false);
            if (ModelState.IsValid)
            {
                try
                {
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient();

                    string inviteURL = ConfigHelper.inviteRedirectURLBase;// + ConfigHelper.Tenant;

                    Tuple<bool, string, string> tup = await usersService.CreateGuestUser(graphServiceClient, _objGstUsrMdl.UserDisplayName, _objGstUsrMdl.UserEmailAddress,
                           inviteURL, _objGstUsrMdl.WelcomeMessage);

                    _objGstUsrMdl.userID = tup.Item2;
                    _objGstUsrMdl.resultantMessage = tup.Item3;
                    _objGstUsrMdl.status = tup.Item1;

                    return View("GuestUsersCreateResult", _objGstUsrMdl);
                }

                catch (Exception ex)
                {
                    _objGstUsrMdl.status = false;
                    _objGstUsrMdl.resultantMessage = ex.Message;
                }

            }
            return View("GuestUsers", _objGstUsrMdl);
        }
    }
}