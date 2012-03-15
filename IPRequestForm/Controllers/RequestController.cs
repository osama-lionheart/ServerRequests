using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;
using AttributeRouting;
using IPRequestForm.Models;
using IPRequestForm.ViewModels;

namespace IPRequestForm.Controllers
{
    public class RequestController : Controller
    {
        private RequestRepository repo = new RequestRepository();

        private Mailer mailer = new Mailer();

        private const int PageSize = 10;

        [Authorize]
        [GET("/Request/New")]
        public ActionResult CreateNewRequest()
        {
            var user = repo.GetCurrentUser();

            if (user != null)
            {
                return View(new NewRequestViewMode
                {
                    Request = new RequestFormViewModel(repo.GetAllApplicationTypes(), repo.GetAllLocations(), repo.GetAllServerTypes(), 
                        repo.GetAllPortTypes(), repo.GetAllOperatingSystems(), RequestFormViews.Create),
                    User = user,
                    RequestFilter = RequestFilters.New
                });
            }

            return new HttpUnauthorizedResult();
        }

        [Authorize]
        [POST("/Request/New")]
        public ActionResult SubmitRequest(int? oldRequestId, string businessService, string applicationName, int[] applicationTypeId,
                                            string netBIOSName, string dnsName, int locationId,
                                            int serverTypeId, int? bladeChassisId,
                                            string bladeSwitchLocation, bool? bladeTeaming,
                                            int operatingSystemId,
                                            int[] portId, string[] ipAddress, string[] portNumber, int[] portTypeId, int[] portDirectionId, string[] startDate, string[] endDate,
                                            string notes, string submit)
        {
            var request = repo.CreateRequest(businessService, applicationName, applicationTypeId, netBIOSName, dnsName,
                locationId, serverTypeId, bladeChassisId, bladeSwitchLocation, bladeTeaming, operatingSystemId, notes, oldRequestId);

            for (int i = 0; i < portTypeId.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(ipAddress[i]))
                {
                    continue;
                }

                DateTime? startDateTime = null;
                DateTime? endDateTime = null;

                if (!string.IsNullOrWhiteSpace(startDate[i]))
                {
                    startDateTime = DateTime.ParseExact(startDate[i], "d/M/yyyy", null);
                }

                if (!string.IsNullOrWhiteSpace(endDate[i]))
                {
                    endDateTime = DateTime.ParseExact(endDate[i], "d/M/yyyy", null);
                }

                // Create a list of ports to be opened for each IP.
                var finalPorts = new Dictionary<int, int?>();

                var ports = portNumber[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var port in ports)
                {
                    var rangePorts = port.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    var startPort = int.Parse(rangePorts[0].Trim());

                    if (rangePorts.Length > 1) // Range of Ports
                    {
                        var endPort = int.Parse(rangePorts[1].Trim());

                        finalPorts.Add(startPort, endPort);
                    }
                    else // Only one port
                    {
                        finalPorts.Add(startPort, null);
                    }
                }



                var ips = ipAddress[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var oneIP in ips)
                {
                    if (oneIP.Contains('/'))
                    {
                        var parts = oneIP.Split('/');

                        foreach (var port in finalPorts)
                        {
                            repo.CreatePort(request, portId[i], IPAddress.Parse(parts[0]), int.Parse(parts[1]), portTypeId[i], port.Key, port.Value, portDirectionId[i], startDateTime, endDateTime);
                        }
                    }
                    else
                    {
                        var rangeIPs = oneIP.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                        var firstIP = IPAddress.Parse(rangeIPs[0]);
                        IPAddress secondIP;

                        if (rangeIPs.Length > 1) // Range
                        {
                            secondIP = IPAddress.Parse(rangeIPs[1]);

                            var ipAddNum1 = firstIP.ToInt();
                            var ipAddNum2 = secondIP.ToInt();

                            for (var j = ipAddNum1; j <= ipAddNum2; j++)
                            {
                                var currentIP = CommonFunctions.IPAddressFromInt(j);

                                foreach (var port in finalPorts)
                                {
                                    repo.CreatePort(request, portId[i], currentIP, null, portTypeId[i], port.Key, port.Value, portDirectionId[i], startDateTime, endDateTime);
                                }
                            }
                        }
                        else // Only One IP
                        {
                            foreach (var port in finalPorts)
                            {
                                repo.CreatePort(request, portId[i], firstIP, null, portTypeId[i], port.Key, port.Value, portDirectionId[i], startDateTime, endDateTime);
                            }
                        }
                    }
                }
            }

            repo.SaveChanges();

            if (!request.OriginalId.HasValue)
            {
                request.OriginalId = request.Id;
                repo.SaveChanges();
            }

            if (request.Id == request.OriginalId)
            {
                mailer.SendNewRequestMail(request);
            }
            else
            {
                mailer.SendUpdateRequestMail(request);
            }

            return Redirect("/Request/Pending");
        }

        [Authorize]
        [GET("/Search")]
        public ActionResult Search(string q, int pageNo = 1)
        {
            User loggedInUser = repo.GetCurrentUser();

            var requests = repo.Search(repo.GetUserRequests(loggedInUser).Where(x => !x.Deleted), q);
            var model = new RequestsViewModel(requests, pageNo, PageSize, loggedInUser, RequestFilters.Search);
            model.Query = q;
            
            return View("UserRequests", model);
        }

        [Authorize]
        [GET("/")]
        [GET("/Request")]
        [GET("/Request/All/{pageNo=1}")]
        [GET("/{userId?}")]
        [GET("/{userId?}/Request")]
        [GET("/{userId?}/Request/All/{pageNo=1}")]
        public ActionResult UserRequests(int? userId, int? applicationTypeId, int? vlanId, int? pageNo)
        {
            User loggedInUser = repo.GetCurrentUser();
            User user;

            if (userId.HasValue)
            {
                user = repo.GetUserById(userId.Value);
            }
            else
            {
                user = repo.GetCurrentUser();
            }

            var requests = repo.GetUserRequests(user).Where(x => !x.Deleted);

            if (vlanId.HasValue)
            {
                requests = requests.Where(i => i.SecurityActions.Count > 0 && i.SecurityActions.OrderByDescending(x => x.Id).FirstOrDefault().VlanId == vlanId);
            }

            if (applicationTypeId.HasValue)
            {
                requests = requests.Where(i => i.RequestApplicationTypes.Any(x => x.ApplicationTypeId == applicationTypeId));
            }

            return View(new RequestsViewModel(requests, pageNo ?? 1, PageSize, loggedInUser, RequestFilters.All));
        }

        [Authorize]
        [GET("/Request/Pending/{pageNo=1}")]
        public ActionResult UserPendingRequests(int pageNo)
        {
            var user = repo.GetCurrentUser();
            var requests = repo.GetUserRequests(user);

            requests = requests.Where(x => !x.Deleted).Where(i => i.SecurityActions.Count() == 0 || i.SecurityActions.OrderByDescending(j => j.Id).FirstOrDefault().Approved == null);

            return View("UserRequests", new RequestsViewModel(requests, pageNo, PageSize, user, RequestFilters.Pending));
        }

        [Authorize]
        [GET("/Request/Approved/{pageNo=1}")]
        public ActionResult UserApprovedRequests(int pageNo)
        {
            var user = repo.GetCurrentUser();
            var requests = repo.GetUserRequests(user);

            requests = requests.Where(x => !x.Deleted).Where(i => i.SecurityActions.Count() > 0 && i.SecurityActions.OrderByDescending(j => j.Id).FirstOrDefault().Approved == true &&
                (i.CommunicationActions.Count() == 0 || !i.CommunicationActions.OrderByDescending(j => j.Id).FirstOrDefault().Completed.HasValue || !i.CommunicationActions.OrderByDescending(j => j.Id).FirstOrDefault().Completed.Value));

            return View("UserRequests", new RequestsViewModel(requests, pageNo, PageSize, user, RequestFilters.Approved));
        }

        [Authorize]
        [GET("/Request/Rejected/{pageNo=1}")]
        public ActionResult UserRejectedRequests(int pageNo)
        {
            var user = repo.GetCurrentUser();
            var requests = repo.GetUserRequests(user);

            requests = requests.Where(x => !x.Deleted).Where(i => i.SecurityActions.Count() > 0 && i.SecurityActions.OrderByDescending(j => j.Id).FirstOrDefault().Approved == false);

            return View("UserRequests", new RequestsViewModel(requests, pageNo, PageSize, user, RequestFilters.Rejected));
        }

        [Authorize]
        [GET("/Request/Completed/{pageNo=1}")]
        public ActionResult UserCompletedRequests(int pageNo)
        {
            var user = repo.GetCurrentUser();
            var requests = repo.GetUserRequests(user);

            requests = requests.Where(x => !x.Deleted).Where(i => i.CommunicationActions.Count() > 0 && i.CommunicationActions.OrderByDescending(j => j.Id).FirstOrDefault().Completed == true);

            return View("UserRequests", new RequestsViewModel(requests, pageNo, PageSize, user, RequestFilters.Completed));
        }

        [Authorize]
        [GET("/Request/Deleted/{pageNo=1}")]
        public ActionResult UserDeletedRequests(int pageNo)
        {
            var user = repo.GetCurrentUser();
            var requests = repo.GetUserRequests(user);

            requests = requests.Where(i => i.Deleted);

            return View("UserRequests", new RequestsViewModel(requests, pageNo, PageSize, user, RequestFilters.Deleted));
        }

        [Authorize]
        [GET("/Request/{requestId}")]
        public ActionResult UserRequest(int requestId)
        {
            var user = repo.GetCurrentUser();

            var isSecurity = Roles.IsUserInRole("Security");
            var isCommunication = Roles.IsUserInRole("Communication");

            var departmentUserIds = user.Department.Users.Select(x => x.Id);

            var request = repo.GetRequestById(requestId);

            if (request != null && request.UserId == user.Id || departmentUserIds.Contains(request.UserId) || isSecurity || isCommunication)
            {
                var viewModel = new RequestViewModel(user, request, repo.GetAllVlans(), 
                    repo.GetAllSwitches(), repo.GetAllSwitchModules(), repo.GetAllSwitchPorts(), RequestFilters.View);

                if(TempData.ContainsKey("ServerIPAddressError"))
                {
                    viewModel.ServerIPAddressError = TempData["ServerIPAddressError"] as string;
                }

                return View("UserRequest", viewModel);
            }
            else
            {
                return Content("Not Found");
            }
        }

        [Authorize]
        [GET("/Request/{requestId}/Edit")]
        public ActionResult GetRequestEditForm(int requestId)
        {
            var user = repo.GetCurrentUser();

            return View("CreateNewRequest", new NewRequestViewMode
            {
                Request = new RequestFormViewModel(repo.GetRequestById(requestId), user, repo.GetAllApplicationTypes(),
                    repo.GetAllLocations(), repo.GetAllServerTypes(), repo.GetAllPortTypes(), repo.GetAllOperatingSystems(), RequestFormViews.Update),
                User = user,
                RequestFilter = RequestFilters.Edit
            });
        }

        [Authorize]
        [POST("/Request/{requestId}/Delete")]
        public ActionResult DeleteRequest(int requestId)
        {
            repo.DeleteRequestById(requestId);

            repo.SaveChanges();

            return Redirect("/request/deleted");
        }

        [Authorize]
        [POST("/Request/{requestId}/Undelete")]
        public ActionResult UndeleteRequest(int requestId)
        {
            repo.UndeleteRequestById(requestId);

            repo.SaveChanges();

            return Redirect("/");
        }
    }

    public enum RequestFilters
    {
        All,
        Pending,
        Approved,
        Rejected,
        Completed,
        Deleted,
        SignIn,
        SignUp,
        ResetPassword,
        ChangePassword,
        Settings,
        New,
        View,
        Edit,
        Search
    }
}

