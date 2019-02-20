using AzureWebUIapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebAppGroupClaimsDotNet.Utils;

namespace AzureWebUIapp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            SetAPIAccessModel m = new SetAPIAccessModel();

            m.AccessModes.Add(new SelectListItem() { Value = "A", Text = "Use Application Permissions" });
            m.AccessModes.Add(new SelectListItem() { Value = "D", Text = "Use Delegated Permissions" });

            m.accessMode = (ConfigHelper.UseApplicationPermissions ? "A" : "D");

            return View(m);
        }


        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}