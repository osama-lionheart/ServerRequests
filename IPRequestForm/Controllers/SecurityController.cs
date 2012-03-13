using System.Web.Mvc;
using AttributeRouting;
using IPRequestForm.Models;

namespace IPRequestForm.Controllers
{
    [Authorize(Roles = "Security")]
    public class SecurityController : Controller
    {
        private RequestRepository repo = new RequestRepository();
        private Mailer mailer = new Mailer();

        [Authorize(Roles = "Security")]
        [POST("/Request/{requestId}/SecurityApproval")]
        public ActionResult ApproveRequest(int requestId, string Approval, int? vlanId, string notes)
        {
            if (Approval == "Approve")
            {
                repo.ApproveRequest(requestId, vlanId, notes);

                var action = repo.GetCommunicationAction(requestId);

                if (action != null && action.Assigned == true)
                {
                    repo.ReleaseCommunicationEngineer(requestId, "Automatic releasing after security changes.");
                }

                repo.SaveChanges();

                mailer.SendRequestApprovedMail(repo.GetRequestById(requestId));

                return Redirect("/request/approved");
            }
            else
            {
                repo.RejectRequest(requestId, notes);

                var action = repo.GetCommunicationAction(requestId);

                if (action != null && action.Assigned == true)
                {
                    repo.ReleaseCommunicationEngineer(requestId, "Automatic releasing after security changes.");
                }

                repo.SaveChanges();

                mailer.SendRequestRejectedMail(repo.GetRequestById(requestId));

                return Redirect("/request/rejected");
            }
        }

        [Authorize(Roles = "Security")]
        [POST("/Request/{requestId}/AssignSecurityEngineer")]
        public ActionResult AssignSecurityEngineer(int requestId, string notes)
        {
            repo.AssignSecurityEngineer(requestId, notes);

            repo.SaveChanges();

            return Redirect("/request/" + requestId);
        }

        [Authorize(Roles = "Security")]
        [POST("/Request/{requestId}/ReleaseSecurityEngineer")]
        public ActionResult ReleaseSecurityEngineer(int requestId, string notes)
        {
            repo.ReleaseSecurityEngineer(requestId, notes);

            var action = repo.GetCommunicationAction(requestId);

            if (action != null && action.Assigned == true)
            {
                repo.ReleaseCommunicationEngineer(requestId, "Automatic releasing after security changes.");
            }

            repo.SaveChanges();


            return Redirect("/request/" + requestId);
        }
    }
}
