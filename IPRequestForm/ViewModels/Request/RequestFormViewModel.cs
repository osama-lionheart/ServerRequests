using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IPRequestForm.Models;
using IPRequestForm.Controllers;
using System.Web.Security;

namespace IPRequestForm.ViewModels
{
    public enum RequestFormViews
    {
        View,
        Create,
        Update
    }

    public class RequestFormViewModel
    {
        public RequestFormViews RequestFormView { get; private set; }

        public int RequestId { get; set; }

        public User RequestUser { get; set; }

        public User RequestOwner { get; set; }

        public DateTime Date { get; set; }

        public string BusinessService { get; set; }

        public string ApplicationName { get; set; }

        public string NetBIOSName { get; set; }

        public string DNSName { get; set; }

        public Location ServerLocation { get; set; }

        public IEnumerable<ApplicationType> RequestApplicationTypes { get; set; }

        public ServerType ServerType { get; set; }

        public string RackChassisPort { get; set; }

        public bool NICTeaming { get; set; }

        public ServerTypes ServerTypeName { get; set; }

        public IPRequestForm.Models.OperatingSystem OperatingSystem { get; set; }

        public bool ShowPolicies { get; set; }

        public IEnumerable<Policy> Policies { get; set; }

        public string Notes { get; set; }

        public bool Deleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public IEnumerable<IPRequestForm.Models.OperatingSystem> OperatingSystems { get; set; }

        public IEnumerable<PortType> PortTypes { get; set; }

        public IEnumerable<ServerType> ServerTypes { get; set; }

        public IEnumerable<Location> Locations { get; set; }

        public IEnumerable<ApplicationType> ApplicationTypes { get; set; }

        public RequestFormViewModel(IEnumerable<ApplicationType> applicationTypes, IEnumerable<Location> locations, IEnumerable<ServerType> serverTypes,
            IEnumerable<PortType> portTypes, IEnumerable<IPRequestForm.Models.OperatingSystem> operatingSystems, RequestFormViews requestFormView)
        {
            this.RequestFormView = requestFormView;
            this.OperatingSystems = operatingSystems;
            this.PortTypes = portTypes;
            this.ServerTypes = serverTypes;
            this.Locations = locations;
            this.ApplicationTypes = applicationTypes;

            this.ShowPolicies = true;
        }

        public RequestFormViewModel(Request request, User user, RequestFormViews requestFormView) : 
            this(request, user, null, null, null, null, null, requestFormView)
        {

        }

        public RequestFormViewModel(Request request, User user, IEnumerable<ApplicationType> applicationTypes, IEnumerable<Location> locations, 
            IEnumerable<ServerType> serverTypes, IEnumerable<PortType> portTypes, IEnumerable<IPRequestForm.Models.OperatingSystem> operatingSystems, 
            RequestFormViews requestFormView)
        {
            this.RequestFormView = requestFormView;
            this.OperatingSystems = operatingSystems;
            this.PortTypes = portTypes;
            this.ServerTypes = serverTypes;
            this.Locations = locations;
            this.ApplicationTypes = applicationTypes;

            RequestFormView = requestFormView;

            RequestUser = request.User;

            this.RequestOwner = request.Owner;
            
            RequestId = request.Id;

            Date = request.SubmissionDate;
            BusinessService = request.BusinessService;
            ApplicationName = request.ApplicationName;

            RequestApplicationTypes = request.RequestApplicationTypes.Select(i => i.ApplicationType);

            NetBIOSName = request.Virtualization.NetBIOSName;
            DNSName = request.Virtualization.DNSName;

            Notes = request.Notes;

            Deleted = request.Deleted;
            DeletedDate = request.DeleteDate;

            ServerLocation = request.Virtualization.Server.Location;
            ServerType = request.Virtualization.Server.ServerType;

            ServerTypeName = (ServerTypes)request.Virtualization.Server.ServerType.Id;

            if (ServerTypeName == IPRequestForm.ViewModels.ServerTypes.Blades)
            {
                RackChassisPort = request.Virtualization.Server.BladeChassi.RackChassisPort;
                NICTeaming = request.Virtualization.Server.BladeChassi.NICTeaming;
            }

            this.OperatingSystem = request.Virtualization.OperatingSystem;

            //ShowPolicies = (!Roles.IsUserInRole("Users") || request.UserId == user.Id);

            ShowPolicies = true;
            
            var reqs = request.RequestPorts.AsQueryable();

            if (user.Id != request.OwnerId & !Roles.IsUserInRole("Security") & !Roles.IsUserInRole("Communication"))
            {
                reqs = reqs.Where(x => x.Port.UserId == user.Id);
            }

            Policies = reqs.Select(i => new Policy
            {
                Id = i.Port.Id,
                IPAddress = CommonFunctions.IPDotted(i.Port.IP.Address),
                SubnetMaskBits = i.Port.SubnetMaskId != null ? (int?)BitConverter.ToUInt32(i.Port.SubnetMask.Address.Reverse().ToArray(), 0).BitsSetCountNaive() : null,
                PortType = i.Port.PortType,
                PortNumber = i.Port.PortNumber,
                PortForwardDirection = i.Port.ServerInbound ? PortForwardDirections.In : PortForwardDirections.Out,
                StartDate = i.Port.StartDate,
                EndDate = i.Port.EndDate
            });
        }
    }

    public class Policy
    {
        public int Id { get; set; }

        public string IPAddress { get; set; }

        public int? SubnetMaskBits { get; set; }

        public PortType PortType { get; set; }

        public int PortNumber { get; set; }

        public PortForwardDirections PortForwardDirection { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public enum ServerTypes
    {
        Blades = 1,
        Standalone = 2
    }

    public enum PortForwardDirections
    {
        In,
        Out
    }
}