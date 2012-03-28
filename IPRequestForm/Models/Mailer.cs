using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using IPRequestForm.Controllers;
using RazorEngine;

namespace IPRequestForm.Models
{
    public class Mailer
    {
        private RequestRepository repo = new RequestRepository();

        private string domainName = "http://10.12.0.54";

        public string LoadMailTemplate(string name, object model)
        {
            var templatePath = string.Format("IPRequestForm.Views.Mails.{0}.cshtml", name);

            var assembly = Assembly.GetExecutingAssembly();

            var stream = assembly.GetManifestResourceStream(templatePath);

            var template = new StreamReader(stream).ReadToEnd();

            return Razor.Parse(template, model.ToExpando());
        }

        public void SendMail(IEnumerable<MailAddress> toAddress, string subject, string body)
        {
            SendMail(toAddress, null, subject, body);
        }

        public void SendMail(IEnumerable<MailAddress> toAddress, IEnumerable<MailAddress> ccAddress, string subject, string body)
        {
            MailMessage msg = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var email in toAddress)
	        {
                msg.To.Add(email);
            }

            if (ccAddress != null)
            {
                foreach (var email in ccAddress)
                {
                    msg.CC.Add(email);
                }
            }

            try
            {
                var smtp = new SmtpClient();
                smtp.Send(msg);
            }
            catch { }
        }

        public void SendChangePasswordMail(User user)
        {
            var link = string.Format("{0}/ChangePassword?token={1}", domainName, user.ResetToken);

            var body = LoadMailTemplate("ChangePassword", new { User = user, Url = link });

            SendMail(new MailAddress[] { new MailAddress(repo.GetEmailFromUsername(user.Username), user.FirstName + " " + user.LastName) }, "Server Request Reset Password", body);
        }

        public void SendNewRequestMail(Request request)
        {
            var subject = string.Format("{0} ({1}) Created by {2} {3}", request.ApplicationName, request.BusinessService, request.User.FirstName, request.User.LastName);
            var body = LoadMailTemplate("NewRequestMail", new {
                User = request.User,
                Url = string.Format("{0}/request/{1}", domainName, request.Id)
            });
            
            var to = GetUsersMailAddress(repo.GetUsersInRole("SecurityMails"));
            var cc = GetUsersMailAddress(repo.GetUsersInRole("CommunicationMails"));
            
            SendMail(to, cc, subject, body);
        }

        public void SendUpdateRequestMail(Request request)
        {
            var subject = string.Format("{0} ({1}) Updated by {2} {3}", request.ApplicationName, request.BusinessService, request.User.FirstName, request.User.LastName);
            var body = LoadMailTemplate("UpdateRequestMail", new {
                User = request.User,
                Url = string.Format("{0}/request/{1}", domainName, request.Id)
            });
            
            var to = GetUsersMailAddress(repo.GetUsersInRole("SecurityMails"));
            var cc = GetUsersMailAddress(repo.GetUsersInRole("CommunicationMails")).Union(
                GetUsersMailAddress(new User[] { request.Owner }));

            SendMail(to, cc, subject, body);
        }

        public void SendRequestApprovedMail(Request request)
        {
            var action = repo.GetLastSecurityAction(request);

            var subject = string.Format("{0} ({1}) Approved by {2} {3}", request.ApplicationName, request.BusinessService, action.User.FirstName, action.User.LastName);
            var body = LoadMailTemplate("ApprovedRequestMail", new {
                User = action.User, 
                Url = string.Format("{0}/request/{1}", domainName, request.Id),
                Vlan = action.Vlan
            });

            var to = GetUsersMailAddress(new User[] { request.User });
            var cc = GetUsersMailAddress(repo.GetUsersInRole("SecurityMails")).Union(
                GetUsersMailAddress(repo.GetUsersInRole("CommunicationMails"))).Union(
                GetUsersMailAddress(new User[] { request.Owner }));
            
            SendMail(to, cc, subject, body);
        }

        public void SendRequestRejectedMail(Request request)
        {
            var action = repo.GetLastSecurityAction(request);

            var subject = string.Format("{0} ({1}) Rejected by {2} {3}", request.ApplicationName, request.BusinessService, action.User.FirstName, action.User.LastName);
            var body = LoadMailTemplate("RejectedRequestMail", new {
                User = action.User,
                Url = string.Format("{0}/request/{1}", domainName, request.Id)
            });

            var to = GetUsersMailAddress(new User[] { request.User });
            var cc = GetUsersMailAddress(repo.GetUsersInRole("SecurityMails")).Union(
                GetUsersMailAddress(repo.GetUsersInRole("CommunicationMails"))).Union(
                GetUsersMailAddress(new User[] { request.Owner }));

            SendMail(to, cc, subject, body);
        }

        public void SendRequestCompletedMail(Request request)
        {
            var action = repo.GetLastCommunicationAction(request);
            
            var subject = string.Format("{0} ({1}) Completed by {2} {3}", request.ApplicationName, request.BusinessService, action.User.FirstName, action.User.LastName);
            var body = LoadMailTemplate("CompletedRequestMail", new {
                User = action.User,
                Url = string.Format("{0}/request/{1}", domainName, request.Id),
                Vlan = repo.GetLastSecurityAction(request).Vlan,
                IPAddress = CommonFunctions.IPDotted(action.ServerIP.IP.Address)
            });

            var to = GetUsersMailAddress(new User[] { request.User });
            var cc = GetUsersMailAddress(repo.GetUsersInRole("SecurityMails")).Union(
                GetUsersMailAddress(repo.GetUsersInRole("CommunicationMails"))).Union(
                GetUsersMailAddress(new User[] { request.Owner }));

            SendMail(to, cc, subject, body);
        }

        private IEnumerable<MailAddress> GetUsersMailAddress(IEnumerable<User> users)
        {
            return users.Select(x => new MailAddress(
                repo.GetEmailFromUsername(x.Username),
                string.Format("{0} {1}", x.FirstName, x.LastName))
            );
        }
    }
}