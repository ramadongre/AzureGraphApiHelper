using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace AzureWebUIapp.Models
{
    public class UserDetails
    {
        public Microsoft.Azure.ActiveDirectory.GraphClient.User user { get; set; }
        public List<ResultsItem> exts { get; set; }

        public List<Tuple<string, List<ResultsItem>>> tupAppRoles { get; set; }

        public List<string> Groups;

        public bool isOk { get; set; }
        public string message { get; set; }

    }

    public class UserGroupAssociation
    {
        [Required]
        [Display(Name = "Guest user email address")]
        public string UserEmailAddress { get; set; }

        [Required]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; }

        public bool isOk { get; set; }
        public string message { get; set; }
    }

    public class AddAppRoleToUserModel
    {
        public AddAppRoleToUserModel()
        {
            TenantApplications = new List<SelectListItem>();
            ApplicationRoles = new List<SelectListItem>();
        }

        [Required]
        [Display(Name = "Application Name")]
        public string AppName { get; set; }

        public List<SelectListItem> TenantApplications;

        [Required]
        [Display(Name = "Guest user email address")]
        public string UserEmailAddress { get; set; }

        [Required]
        [Display(Name = "Application Role Name")]
        public string AppRoleName { get; set; }

        public List<SelectListItem> ApplicationRoles;

        public bool isOk { get; set; }
        public string message { get; set; }
    }

    public class ManageApplicationExtensionsAssignment
    {
        public ManageApplicationExtensionsAssignment()
        {
            TenantApplications = new List<SelectListItem>();
            AppExtAttributes = new List<SelectListItem>();
        }

        [Required]
        [Display(Name = "Application Name")]
        public string AppName { get; set; }

        public List<SelectListItem> TenantApplications;

        [Required]
        [Display(Name = "Guest user email address")]
        public string UserEmailAddress { get; set; }

        [Required]
        [Display(Name = "Extension Attribute Value")]
        public string ExtAttribValue { get; set; }

        [Required]
        [Display(Name = "Application Extension Attribute")]
        public string AppExtAttribName { get; set; }

        public List<SelectListItem> AppExtAttributes;

        public bool isOk { get; set; }
        public string message { get; set; }
    }
}