﻿using System.Web.Mvc;
using AttributeRouting;
using IPRequestForm.Models;
using IPRequestForm.ViewModels;

namespace IPRequestForm.Controllers
{
    [Authorize(Roles = "Communication")]
    public class CommunicationController : Controller
    {
        private RequestRepository repo = new RequestRepository();
        private Mailer mailer = new Mailer();

        [Authorize(Roles = "Communication")]
        [POST("/Request/{requestId}/AssignCommunicationEngineer")]
        public ActionResult AssignCommunicationEngineer(int requestId, string notes)
        {
            repo.AssignCommunicationEngineer(requestId, notes);
            
            repo.SaveChanges();

            return Redirect("/request/" + requestId);
        }

        [Authorize(Roles = "Communication")]
        [POST("/Request/{requestId}/ReleaseCommunicationEngineer")]
        public ActionResult ReleaseCommunicationEngineer(int requestId, string notes)
        {
            repo.ReleaseCommunicationEngineer(requestId, notes);

            repo.SaveChanges();

            return Redirect("/request/" + requestId);
        }

        [Authorize(Roles = "Communication")]
        [POST("/Request/{requestId}/ResolveRequest")]
        public ActionResult ResolveRequest(int requestId, int? switchId, string switchIPAddress, string switchName, int? switchNumber,
            int? switchModuleNumber, int? switchPortNumber, string serverIPAddress, string notes)
        {
            try
            {
                var action = repo.ResolveRequest(requestId, switchId, switchIPAddress, switchName, switchNumber, switchModuleNumber, switchPortNumber, serverIPAddress, notes);

                repo.SaveChanges();

                if (action.Completed == false)
                {
                    mailer.SendRequestCompletedMail(repo.GetRequestById(requestId));
                }

                if (action.Completed == true)
                {
                    return Redirect("/request/completed");
                }
            }
            catch (VlanException vlanEx)
            {
                TempData.Add("ServerIPAddressError", vlanEx.Message);
                
                return Redirect("/Request/" + requestId);
            }

            return Redirect("/request/approved");
        }
    }
}
