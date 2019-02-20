using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AzureWebUIapp.Models
{
    public class SetAPIAccessModel
    {
        public SetAPIAccessModel()
        {
            AccessModes = new List<SelectListItem>();
        }

        [Required]
        [Display(Name = "Set Graph API Access Permissions")]
        public string accessMode { get; set; }

        public List<SelectListItem> AccessModes;
    }

    public class GuestUserModel
    {
        [Display(Name = "Guest user name")]
        public string UserDisplayName { get; set; }

        [Required]
        [Display(Name = "Guest user email address")]
        public string UserEmailAddress { get; set; }

        [Display(Name = "Invite message")]
        public string WelcomeMessage { get; set; }

        public bool sendInvitationMessage = true;
        public List<ResultsItem> results { get; set; }

        [Display(Name = "Phone Number")]
        public string phone { get; set; }

        [Display(Name = "Department")]
        public string department { get; set; }

        public string userID { get; set; }

        public string resultantMessage { get; set; }

        public bool status { get; set; }
    }

    public class UpdateUserModel
    {

        [Required]
        [Display(Name = "User email address")]
        public string UserEmailAddress { get; set; }

        [Display(Name = "Job title")]
        public string Jobtitle { get; set; }

        [Display(Name = "Department")]
        public string department { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Phone Number")]
        public string phone { get; set; }

        public string userID { get; set; }

        public string resultantMessage { get; set; }
        public bool status { get; set; }
    }

    public class AccessReviews
    {
        public bool status { get; set; }
        public string resultantMessage { get; set; }
        public List<Tuple<string, string, string, string>> lstResult = new List<Tuple<string, string, string, string>>();
    }
}