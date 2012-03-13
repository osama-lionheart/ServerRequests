using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Security;
using AttributeRouting;
using IPRequestForm.Models;
using RazorEngine;
using IPRequestForm.ViewModels;

namespace IPRequestForm.Controllers
{
    public class AccountController : Controller
    {
        private RequestRepository repo = new RequestRepository();

        private Mailer mailer = new Mailer();

        [GET("/SignIn")]
        [GET("/login")]
        public ActionResult GetSignInForm(string returnUrl)
        {
            /*if (User.Identity.IsAuthenticated)
            {
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 0 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
            }*/

            return View("Login");
        }

        [POST("/SignIn")]
        [POST("/login")]
        public ActionResult SignIn(string email, string password, bool rememberMe, string returnUrl)
        {
            var username = repo.GetUsernameFromEmail(email);

            if (Membership.ValidateUser(username, password))
            {
                FormsAuthentication.SetAuthCookie(username, rememberMe);

                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 0 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return Redirect("/");
                }
            }
            else
            {
                ViewBag.ErrorMessage = "The user name or password provided is incorrect.";

                return View("Login");
            }
        }

        [GET("SignOut")]
        [GET("logout")]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();

            return Redirect("/");
        }

        [GET("/ResetPassword")]
        public ActionResult GetResetPasswordForm()
        {
            return View("ResetPassword");
        }

        [POST("/ResetPassword")]
        public ActionResult ResetPassword(string email)
        {
            var user = repo.GetUserByUsername(email);

            repo.GenerateResetToken(user);

            repo.SaveChanges();

            mailer.SendChangePasswordMail(user);

            return View("PasswordResetNotification");
        }

        [GET("/ChangePassword")]
        public ActionResult GetChangePasswordForm(string token)
        {
            var user = repo.GetUserByResetToken(token);

            if (user == null)
            {
                return Redirect("/");
            }

            ViewBag.Token = token;
            return View("ChangePassword");
        }

        [POST("/ChangePassword")]
        public ActionResult ChangePassword(string token, string password, string confirmPassword)
        {
            var user = repo.GetUserByResetToken(token);

            if (user == null)
            {
                ViewBag.ErrorMessage = "ResetToken is invalid";
                ViewBag.Token = token;
                return View("ChangePassword");
            }

            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Password and confirmation password doesn't match.";
                ViewBag.Token = token;
                return View("ChangePassword");
            }
            else if (!string.IsNullOrWhiteSpace(password) && password.Length < Membership.MinRequiredPasswordLength)
            {
                ViewBag.ErrorMessage = string.Format("Must have at least {0} characters.", Membership.MinRequiredPasswordLength);
                ViewBag.Token = token;
                return View("ChangePassword");
            }

            var membershipUser = Membership.GetUser(user.Username);

            try
            {

                if (!membershipUser.ChangePassword(membershipUser.ResetPassword(), password))
                {
                    ViewBag.ErrorMessage = "An unknown error has occured.";
                    ViewBag.Token = token;
                    return View("ChangePassword");
                }
            }
            catch
            {
                ViewBag.ErrorMessage = "An unknown error has occured.";
                ViewBag.Token = token;
                return View("ChangePassword");
            }

            repo.ClearResetToken(user);

            repo.SaveChanges();

            return View("PasswordChangeSuccess");
        }

        [GET("/SignUp")]
        [Authorize(Roles = "Administrators")]
        public ActionResult GetSignUpForm()
        {
            return View("SignUp", new SignUpViewModel
            {
                User = repo.GetCurrentUser(),
                RequestFilter = RequestFilters.SignUp,
                Departments = repo.GetAllDaprtments(),
                Roles = repo.GetAllRoles()
            });
        }

        [POST("/SignUp")]
        [Authorize(Roles = "Administrators")]
        public ActionResult SignUp(string firstName, string lastName, string username, int departmentId, string[] roleNames)
        {
            var user = repo.GetUserByUsername(username);

            if (user != null)
            {
                ViewBag.ErrorMessage = "This Email is already registered.";
                return View("SignUp");
            }

            user = repo.CreateUser(firstName, lastName, username, departmentId, roleNames);

            repo.SaveChanges();

            mailer.SendChangePasswordMail(user);

            return Redirect("/");
        }

        [GET("/Settings")]
        [Authorize]
        public ActionResult GetSettingsForm()
        {
            return View("Settings", new ViewModelBase
            {
                User = repo.GetCurrentUser(),
                RequestFilter = RequestFilters.Settings
            });
        }

        [POST("/Settings")]
        [Authorize]
        public ActionResult ChangeSettings(string firstName, string lastName, string password, string confirmPassword)
        {
            User user;

            if (password != confirmPassword)
            {
                user = repo.GetCurrentUser();
                ViewBag.ErrorMessage = "Password and confirmation password doesn't match.";
            }
            else if (!string.IsNullOrWhiteSpace(password) && password.Length < Membership.MinRequiredPasswordLength)
            {
                user = repo.GetCurrentUser();
                ViewBag.ErrorMessage = string.Format("Must have at least {0} characters.", Membership.MinRequiredPasswordLength);
            }
            else
            {
                user = repo.ChangeUserSettings(firstName, lastName, password);
                repo.SaveChanges();
            }

            return View("Settings", new { user.FirstName, user.LastName, user.Username }.ToExpando());
        }
    }
}
