using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AzureWebUIapp.Models;
using AzureWebUIapp.Utils;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebAppGroupClaimsDotNet.Utils;

namespace AzureWebUIapp.Controllers
{

    [Authorize]
    public class UsersController : Controller
    {
        UsersGraphServices usersService = new UsersGraphServices();

        public string InvocationMethod
        {
            set
            {
                HttpContext.Session["InvocationMethod"] = value;
            }
        }

        public ViewResult GuestUsers()
        {
            Tuple<bool, string, List<Microsoft.Graph.User>> results = new Tuple<bool, string, List<Microsoft.Graph.User>>(false, string.Empty, null);

            try
            {
                var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);
                results = usersService.GetGuestUsers(graphServiceClient).Result;
            }
            catch (Exception ex)
            {
                results = new Tuple<bool, string, List<Microsoft.Graph.User>>(false, ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : ""), null);

            }

            //bool test = IsUserInDirectory("sesha.kavuri@dhcs.ca.gov");

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
        public async Task<ActionResult> ApplyAccessReviews(string id)
        {
            string formattedJson = "";
            string status = "ERROR";
            string message = "Could not complete the request";
            Tuple<bool, string, string> res = new Tuple<bool, string, string>(false, "", "");
            List<string> lstInstances = new List<string>();

            try
            {
                var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

                string token = GraphAuthService.GetTokenForApplication(ConfigHelper.UseApplicationPermissions, false).Result;

                res = await usersService.ApplyAccessReviews(graphServiceClient, token, ConfigHelper.GraphUrl, id);

                if (res.Item1)
                {
                    formattedJson = res.Item3;// JsonConvert.SerializeObject(res.Item3, Formatting.Indented);
                    status = "OK";
                }
                else
                    message = res.Item2;

            }
            catch (Exception ex)
            {
                message = ex.Message + " " + (ex.InnerException != null ? ex.InnerException.Message : "");
            }

            var jsonData = new
            {
                status = status,
                message = message,
                jresult = formattedJson
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> GetAccessReviewDetails(string typeid, string id)
        {
            string formattedJson = "";
            string status = "ERROR";
            string message = "Could not complete the request";
            Tuple<bool, string, string> res = new Tuple<bool, string, string>(false, "", "");
            List<string> lstInstances = new List<string>();

            try
            {
                var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

                string token = GraphAuthService.GetTokenForApplication(ConfigHelper.UseApplicationPermissions, false).Result;

                if (typeid == "1")
                    res = await usersService.GetAccessReviewDetails(graphServiceClient, token, ConfigHelper.GraphUrl, id);
                else if (typeid == "2")
                    res = await usersService.GetAccessReviewReviewers(graphServiceClient, token, ConfigHelper.GraphUrl, id);
                else if (typeid == "3")
                    res = await usersService.GetAccessReviewDecisions(graphServiceClient, token, ConfigHelper.GraphUrl, id);

                if (res.Item1)
                {
                    formattedJson = res.Item3;// JsonConvert.SerializeObject(res.Item3, Formatting.Indented);
                    status = "OK";
                }
                else
                    message = res.Item2;

            }
            catch (Exception ex)
            {
                message = ex.Message + " " + (ex.InnerException != null ? ex.InnerException.Message : "");
            }

            var jsonData = new
            {
                status = status,
                message = message,
                jresult = formattedJson
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        public async Task<ViewResult> ListAccessReviews()
        {
            string formattedJson = "";
            string status = "ERROR";
            string message = "Could not complete the request";
            Tuple<bool, string, string> res = new Tuple<bool, string, string>(false, "", "");
            List<Tuple<string, string, string, string>> AccessReviewIDs = new List<Tuple<string, string, string, string>>();
            AccessReviews ar = new AccessReviews();
            string masterRecurrenceType = "", childRecurrenceType = "";

            try
            {
                var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

                string token = GraphAuthService.GetTokenForApplication(ConfigHelper.UseApplicationPermissions, false).Result;

                res = await usersService.GetAccessReviewProgramControlList(graphServiceClient, token, ConfigHelper.GraphUrl);

                if (res.Item1)
                {
                    formattedJson = res.Item3;

                    JObject jObject = JObject.Parse(formattedJson);

                    JArray values = (JArray)jObject.SelectToken("value");//this is a list of current action reviews either recurring or not

                    foreach (JToken v in values)
                    {
                        string controlId = (string)v.SelectToken("controlId");//refers to action review
                        string displayName = (string)v.SelectToken("displayName");
                        string startDateTime = (string)v.SelectToken("startDateTime");
                        string endDateTime = (string)v.SelectToken("endDateTime");
                        string arstatus = (string)v.SelectToken("status");

                        //check if this is recurring
                        res = await usersService.GetAccessReviewDetails(graphServiceClient, token, ConfigHelper.GraphUrl, controlId);

                        if (res.Item1)
                        {
                            jObject = JObject.Parse(res.Item3);

                            masterRecurrenceType = (string)jObject.SelectToken("settings").SelectToken("recurrenceSettings").SelectToken("recurrenceType");

                            string det = "Name: " + displayName + " | Start Date: " + startDateTime + " | End Date: " + endDateTime + " | Status: " + arstatus;

                            AccessReviewIDs.Add(new Tuple<string, string, string, string>(controlId, "", masterRecurrenceType, det));

                            if (masterRecurrenceType != "onetime")
                            {
                                //get instances
                                res = await usersService.GetAccessReviewInstances(graphServiceClient, token, ConfigHelper.GraphUrl, controlId);//this would not inlcude current one

                                if (res.Item1)
                                {
                                    formattedJson = res.Item3;//this contains instance details

                                    jObject = JObject.Parse(formattedJson);

                                    JArray instances = (JArray)jObject.SelectToken("value");//instance array
                                    foreach (JToken v2 in instances)
                                    {
                                        string acid = (string)v2.SelectToken("id");
                                        startDateTime = (string)v2.SelectToken("startDateTime");
                                        endDateTime = (string)v2.SelectToken("endDateTime");
                                        arstatus = (string)v2.SelectToken("status");
                                        string dname = (string)v2.SelectToken("displayName");

                                        det = "Name: " + dname + " | Start Date: " + startDateTime + " | End Date: " + endDateTime + " | Status: " + arstatus;

                                        AccessReviewIDs.Add(new Tuple<string, string, string, string>(acid, controlId, "", det));
                                    }
                                }
                                else
                                {
                                    message = res.Item2;
                                }
                            }
                        }
                        else
                        {
                            message = res.Item2;
                        }
                    }

                    status = "OK";
                }
                else
                {
                    message = res.Item2;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message + " " + (ex.InnerException != null ? ex.InnerException.Message : "");
            }


            return View(new AccessReviews() { status = (status == "OK"), resultantMessage = message, lstResult = AccessReviewIDs });
        }

        public ViewResult AccessReview()
        {
            return View();
        }

        public static bool IsUserInDirectory(string Email)//this method was added to test piece of code from others
        {
            try
            {
                string graphResourceId = "https://graph.microsoft.com";
                string clientId = ConfigHelper.ClientId;
                string secret = ConfigHelper.AppKey;
                string Tenant = ConfigHelper.Tenant;
                string userObjectId = ClaimsPrincipal.Current.FindFirst(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                AuthenticationContext authContext = new AuthenticationContext($"https://login.microsoftonline.com/" + ConfigHelper.Tenant, new TokenDbCache(userObjectId));
                ClientCredential credential = new ClientCredential(clientId, secret);

                var accessToken = authContext.AcquireTokenSilentAsync(graphResourceId, new ClientCredential(clientId, secret), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId)).Result.AccessToken;

                var graphserviceClient = new GraphServiceClient(
                                   new DelegateAuthenticationProvider(
                                       requestMessage =>
                                       {
                                           requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken.ToString());

                                           return Task.FromResult(0);
                                       }));

                IGraphServiceUsersCollectionPage usersFilteredId = graphserviceClient.Users.Request().Filter($"Mail eq '" + Email + "'").GetAsync().Result;
                if (usersFilteredId.CurrentPage.Count() > 0)
                {
                    return true;
                }
                return false;

            }
            catch (Exception e)
            {
                throw e;

            }
        }

        [HttpPost]
        public ViewResult GetUser(GuestUserModel model)
        {
            List<Tuple<string, List<ResultsItem>>> tupAppRoles = new List<Tuple<string, List<ResultsItem>>>();
            List<ResultsItem> lstUserAppRoles = new List<ResultsItem>();
            List<ResultsItem> exts = new List<ResultsItem>();
            List<string> groups = new List<string>();
            Microsoft.Azure.ActiveDirectory.GraphClient.User myuser = null;

            if (ModelState.IsValid)
            {
                try
                {
                    var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                    var user = azureClient.Users.Where(a => a.UserPrincipalName.Equals(model.UserEmailAddress, StringComparison.InvariantCultureIgnoreCase) ||
                    a.Mail.Equals(model.UserEmailAddress, StringComparison.InvariantCultureIgnoreCase)
                        ).Expand(p => p.AppRoleAssignments).ExecuteAsync().Result.CurrentPage.FirstOrDefault();

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


                        //get extension attributes
                        myuser = (Microsoft.Azure.ActiveDirectory.GraphClient.User)user;

                        foreach (var s in myuser.GetExtendedProperties())
                        {
                            exts.Add(new ResultsItem() { Id = s.Key, Display = (s.Value == null ? "" : s.Value.ToString()) });
                        }

                        IUserFetcher retrievedUserFetcher = myuser;
                        Microsoft.Azure.ActiveDirectory.GraphClient.Extensions.IPagedCollection<IDirectoryObject> pagedCollection =
                            retrievedUserFetcher.MemberOf.ExecuteAsync().Result;

                        List<IDirectoryObject> directoryObjects = pagedCollection.EnumerateAllAsync().Result.ToList();

                        foreach (IDirectoryObject directoryObject in directoryObjects)
                        {
                            if (directoryObject is Microsoft.Azure.ActiveDirectory.GraphClient.Group)
                            {
                                Microsoft.Azure.ActiveDirectory.GraphClient.Group group = directoryObject as Microsoft.Azure.ActiveDirectory.GraphClient.Group;
                                groups.Add(group.DisplayName);
                            }
                        }
                    }

                    return View("GetUserDetails", new UserDetails() { isOk = true, message = "", exts = exts, tupAppRoles = tupAppRoles, user = myuser, Groups = groups });


                }
                catch (Exception ex)
                {
                    model.status = false;
                    model.resultantMessage = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps.OrderBy(a => a.DisplayName))
                    lst.Add(new ResultsItem() { Id = app.DisplayName, Display = app.DisplayName });

                isok = true;
            }
            catch (Exception ex)
            {
                message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                    Tuple<bool, string, string> tup = await usersService.UpdateUser(azureClient, model.UserEmailAddress, model.Jobtitle,
                        model.department, model.City, model.phone);


                    Microsoft.Azure.ActiveDirectory.GraphClient.User user = (Microsoft.Azure.ActiveDirectory.GraphClient.User)azureClient.Users.Where(u => u.Mail.Equals(
                                        model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase) ||
                                        u.UserPrincipalName.Equals(model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase)).ExecuteAsync().Result.CurrentPage.FirstOrDefault();


                    return View("ShowUser", new Tuple<bool, string, string, Microsoft.Azure.ActiveDirectory.GraphClient.User>(
                        tup.Item1, tup.Item2, model.UserEmailAddress, user));
                }
                catch (Exception ex)
                {
                    model.status = false;
                    model.resultantMessage = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);
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

        [HttpPost]
        public ActionResult SetAPIInvokeMethod(string invoketypeValue)
        {
            try
            {
                this.InvocationMethod = invoketypeValue;
            }
            catch (Exception ex)
            {

            }

            return Content("");
        }

        public ActionResult GetUserApplicationRoles(string appName, string useremail)
        {
            List<ResultsItem> lst = new List<ResultsItem>();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                var user = azureClient.Users.Where(a => a.Mail.Equals(useremail, StringComparison.InvariantCultureIgnoreCase) || a.UserPrincipalName.Equals(useremail, StringComparison.InvariantCultureIgnoreCase)
                        ).Expand(p => p.AppRoleAssignments).ExecuteAsync().Result.CurrentPage.FirstOrDefault();
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
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);
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
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps.OrderBy(a => a.DisplayName))
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                    Tuple<bool, string, string, string, string> tup = await usersService.AssignExtensionAttributeToUser(azureClient, model.UserEmailAddress, model.AppName,
                        model.AppExtAttribName, model.ExtAttribValue);


                    Microsoft.Azure.ActiveDirectory.GraphClient.User user = (Microsoft.Azure.ActiveDirectory.GraphClient.User)azureClient.Users.Where(u => u.Mail.Equals(
                                        model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase) ||
                                        u.UserPrincipalName.Equals(model.UserEmailAddress, StringComparison.CurrentCultureIgnoreCase)
                                        ).ExecuteAsync().Result.CurrentPage.FirstOrDefault();

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
                    model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
                }
            }

            return View("AssignExtAttribToUser", model);
        }

        public ViewResult AddAppRoleToUser()
        {
            AddAppRoleToUserModel model = new AddAppRoleToUserModel();

            try
            {
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps)
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.isOk = false;
                model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                var apps = azureClient.Applications.ExecuteAsync().Result;
                var enumeratedApps = AzureADExtensions.EnumerateAllAsync(apps).Result;

                foreach (var app in enumeratedApps)
                    model.TenantApplications.Add(new SelectListItem() { Text = app.DisplayName, Value = app.DisplayName });

                model.isOk = true;
            }
            catch (Exception ex)
            {
                model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

                    string token = await GraphAuthService.GetTokenForApplication(ConfigHelper.UseApplicationPermissions);

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
                    model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var azureClient = GraphAuthService.GetActiveDirectoryClient(ConfigHelper.UseApplicationPermissions);

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
                    model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

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
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

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
                    model.message = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
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
                    var graphServiceClient = GraphAuthService.CreateGraphServiceClient(ConfigHelper.UseApplicationPermissions);

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
                    _objGstUsrMdl.resultantMessage = ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
                }

            }
            return View("GuestUsers", _objGstUsrMdl);
        }
    }
}