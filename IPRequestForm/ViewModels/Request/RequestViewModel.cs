using System.Collections.Generic;
using System.Linq;
using IPRequestForm.Controllers;
using IPRequestForm.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace IPRequestForm.ViewModels
{
    public class RequestViewModel : ViewModelBase
    {
        public RequestFormViewModel Request { get; set; }

        public Request OlderRequest { get; set; }

        public Request NewerRequest { get; set; }

        public IEnumerable<Vlan> Vlans { get; set; }

        public SecurityAction LastSecurityAction { get; set; }

        public CommunicationAction LastCommunicationAction { get; set; }

        public string ServerIPAddressError { get; set; }

        public string IPAddress { get; set; }

        public Vlan Vlan { get; set; }

        public IEnumerable<Switch> Switches { get; set; }

        public IEnumerable<SwitchModule> SwitchModules { get; set; }

        public IEnumerable<SwitchPort> SwitchPorts { get; set; }


        public string SwitchIPAddress { get; set; }

        public string SwitchName { get; set; }

        public int? SwitchNumber { get; set; }

        public int SwitchModuleNumber { get; set; }

        public int SwitchPortNumber { get; set; }
        
        public string Notes { get; set; }

        public List<Log> Logs { get; set; }

        public RequestViewModel(User user, Request request)
            : this(user, request, null, null, null, null, RequestFilters.All)
        {
        }

        public RequestViewModel(User user, Request request, IEnumerable<Vlan> vlans, IEnumerable<Switch> switches, 
            IEnumerable<SwitchModule> switchModules, IEnumerable<SwitchPort> switchPorts, RequestFilters requestFilter)
        {
            RequestFilter = requestFilter;
            User = user;
            Switches = switches;
            SwitchModules = switchModules;
            SwitchPorts = switchPorts;

            var repo = new RequestRepository();

            OlderRequest = repo.GetOlderRequest(request);

            NewerRequest = repo.GetNewerRequest(request);

            Request = new RequestFormViewModel(request, user, RequestFormViews.View);

            var lastApprovedAction = repo.GetLastApprovedSecurityAction(request);

            if (lastApprovedAction != null)
            {
                Vlan = lastApprovedAction.Vlan;
            }

            // Get the last security action of this request version.
            if (request.SecurityActions.Count > 0)
            {
                LastSecurityAction = request.SecurityActions.OrderByDescending(x => x.Id).First();
            }

            // Get the last communication action of this request version.
            if (request.CommunicationActions.Count > 0)
            {
                LastCommunicationAction = request.CommunicationActions.OrderByDescending(x => x.Id).First();

                Notes = LastCommunicationAction.Notes;
            }

            var lastCompletedAction = repo.GetLastCompletedCommunicationAction(request);

            if (lastCompletedAction != null)
            {
                IPAddress = CommonFunctions.IPDotted(lastCompletedAction.ServerIP.IP.Address);

                if (lastCompletedAction.Completed == true)
                {
                    if (Request.ServerType.Id == (int)ServerTypes.Standalone)
                    {
                        SwitchIPAddress = CommonFunctions.IPDotted(lastCompletedAction.ServerIP.SwitchPort.SwitchModule.Switch.IP.Address);
                        SwitchName = lastCompletedAction.ServerIP.SwitchPort.SwitchModule.Switch.Name;
                        SwitchNumber = lastCompletedAction.ServerIP.SwitchPort.SwitchModule.Switch.StackableNumber;

                        SwitchModuleNumber = lastCompletedAction.ServerIP.SwitchPort.SwitchModule.Number;
                        SwitchPortNumber = lastCompletedAction.ServerIP.SwitchPort.Port;
                    }
                }
            }

            Vlans = vlans;

            Logs = new List<Log>();

            var reqs = repo.GetRequestUpdates(request.OriginalRequest).ToList();

            Logs.AddRange(reqs.Select(x => new Log
            {
                User = x.User,
                Date = x.SubmissionDate,
                Notes = x.Notes,
                LogAction = (x.Id == x.OriginalId) ? LogActions.RequestCreated : LogActions.RequestUpdated
            }));

            Logs.AddRange(reqs.SelectMany(x => x.SecurityActions.Select(action => new Log
            {
                User = action.User,
                Date = action.Date,
                Notes = action.Notes,
                LogAction = action.Approved == true ? LogActions.SecurityApproved :
                            (action.Approved == false ? LogActions.SecurityRejected :
                            (action.Assigned ? LogActions.SecurityAssigned :
                            LogActions.SecurityReleased))
            })));

            Logs.AddRange(reqs.SelectMany(x => x.CommunicationActions.Select(action => new Log
            {
                User = action.User,
                Date = action.Date,
                Notes = action.Notes,
                LogAction = action.Completed == true ? LogActions.CommunicationCompleted :
                            (action.Completed == false ? LogActions.CommunicationProcessing :
                            (action.Assigned ? LogActions.CommunicationAssigned :
                            LogActions.CommunicationReleased))
            })));

            Logs = Logs.OrderByDescending(x => x.Date).ToList();
        }
    }

    public class Log
    {
        public User User { get; set; }

        public DateTime Date { get; set; }

        public string Notes { get; set; }

        public LogActions LogAction { get; set; }
    }

    public enum LogActions
    {
        [Display(Name = "Created")]
        RequestCreated,
        [Display(Name = "Updated")]
        RequestUpdated,
        [Display(Name = "Deleted")]
        RequestDeleted,
        [Display(Name = "Undeleted")]
        RequestUndeleted,

        [Display(Name = "Security Assigned")]
        SecurityAssigned,
        [Display(Name = "Security Released")]
        SecurityReleased,
        [Display(Name = "Approved")]
        SecurityApproved,
        [Display(Name = "Rejected")]
        SecurityRejected,

        [Display(Name = "Communication Assigned")]
        CommunicationAssigned,
        [Display(Name = "Communication Released")]
        CommunicationReleased,
        [Display(Name = "Processing")]
        CommunicationProcessing,
        [Display(Name = "Completed")]
        CommunicationCompleted
    }
}