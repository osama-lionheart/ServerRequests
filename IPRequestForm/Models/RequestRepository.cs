using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Security;
using IPRequestForm.Controllers;
using IPRequestForm.ViewModels;

namespace IPRequestForm.Models
{
    public class RequestRepository
    {
        #region Private Members

        private readonly IPRequestFormEntities db;

        private List<IP> addedIPs;

        #endregion

        #region Constructors

        public RequestRepository()
        {
            db = new IPRequestFormEntities();
            addedIPs = new List<IP>();
        }

        #endregion

        #region Requests

        public Request GetRequestById(int requestId)
        {
            return db.Requests.SingleOrDefault(x => x.Id == requestId);
        }

        public void DeleteRequestById(int requestId)
        {
            var request = GetRequestById(requestId);

            request.Deleted = true;

            request.DeleteDate = DateTime.Now;
        }

        public void UndeleteRequestById(int requestId)
        {
            var request = GetRequestById(requestId);

            request.Deleted = false;
        }

        public IQueryable<Request> GetUserRequests(User user)
        {
            var loggedInUser = GetCurrentUser();

            var requests = db.Requests.AsQueryable();
            
            if (Roles.IsUserInRole("Users"))
            {
                var departmentUserIds = loggedInUser.Department.Users.Select(k => k.Id);

                requests = requests.Where(i => departmentUserIds.Contains(i.UserId));
            }

            if (user.Id != loggedInUser.Id)
            {
                if (Roles.IsUserInRole(user.Username, "Users"))
                {
                    requests = requests.Where(i => i.UserId == user.Id || i.OwnerId == user.Id || i.OriginalRequest.Requests.Any(x => x.UserId == user.Id));
                }
                else if (Roles.IsUserInRole(user.Username, "Security"))
                {
                    requests = requests.Where(i => i.UserId == user.Id || i.SecurityActions.Count > 0 && i.SecurityActions.OrderByDescending(x => x.Id).FirstOrDefault().UserId == user.Id);
                }
                else if (Roles.IsUserInRole(user.Username, "Communication"))
                {
                    requests = requests.Where(i => i.UserId == user.Id || i.CommunicationActions.Count > 0 && i.CommunicationActions.OrderByDescending(x => x.Id).FirstOrDefault().UserId == user.Id);
                }
            }

            var reqs = db.Requests.AsQueryable();

            requests = requests.Where(x => !reqs.Any(y => y.OldRequestId == x.Id));

            return requests.OrderByDescending(x => x.SubmissionDate);
        }

        public IQueryable<Request> GetRequestUpdates(Request request)
        {
            return db.Requests.Where(x => x.OriginalId == request.OriginalId);
        }

        public IQueryable<Request> Search(IQueryable<Request> requests, string query)
        {
            query = query.ToLower();

            var result = requests.SelectMany(i => i.OriginalRequest.Requests.Where(x => x.ApplicationName.ToLower().Contains(query) ||
                x.BusinessService.ToLower().Contains(query) ||
                x.Notes.ToLower().Contains(query) ||
                x.User.FirstName.ToLower().Contains(query) ||
                x.User.LastName.ToLower().Contains(query) ||
                x.Owner.FirstName.ToLower().Contains(query) ||
                x.Owner.LastName.ToLower().Contains(query) ||
                x.RequestApplicationTypes.Any(y => y.ApplicationType.Name.ToLower().Contains(query)) ||
                x.Virtualization.DNSName.ToLower().Contains(query) ||
                x.Virtualization.NetBIOSName.ToLower().Contains(query) ||
                x.Virtualization.OperatingSystem.Name.ToLower().Contains(query) ||
                x.Virtualization.Server.Location.Name.ToLower().Contains(query) ||
                x.Virtualization.Server.ServerType.Name.ToLower().Contains(query) ||
                x.Virtualization.Server.BladeChassi.RackChassisPort.ToLower().Contains(query) ||
                x.SecurityActions.Any(sa => 
                    sa.Notes.ToLower().Contains(query) ||
                    sa.User.FirstName.ToLower().Contains(query) ||
                    sa.User.LastName.ToLower().Contains(query)) ||
                x.CommunicationActions.Any(ca => 
                    ca.Notes.ToLower().Contains(query) ||
                    ca.User.FirstName.ToLower().Contains(query) ||
                    ca.User.LastName.ToLower().Contains(query))
                ));

            DateTime dateTime;
            
            if (DateTime.TryParseExact(query, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime))
            {
                dateTime = dateTime.Date;

                result = result.Union(requests.SelectMany(i => 
                    i.OriginalRequest.Requests.Where(x => 
                        EntityFunctions.TruncateTime(x.SubmissionDate).Value.Equals(dateTime) ||
                        (x.DeleteDate.HasValue && EntityFunctions.TruncateTime(x.DeleteDate).Value.Equals(dateTime)) ||
                        x.RequestPorts.Any(p => 
                            (p.Port.StartDate.HasValue && EntityFunctions.TruncateTime(p.Port.StartDate.Value).Value.Equals(dateTime))  ||
                            (p.Port.EndDate.HasValue && EntityFunctions.TruncateTime(p.Port.EndDate.Value).Value.Equals(dateTime))
                        ) ||
                        x.SecurityActions.Any(sa => EntityFunctions.TruncateTime(sa.Date).Value.Equals(dateTime)) ||
                        x.CommunicationActions.Any(ca => EntityFunctions.TruncateTime(ca.Date).Value.Equals(dateTime))
                    )
                ));
            }

            IPAddress address;

            if (IPAddress.TryParse(query, out address))
            {
                var bytes = address.GetAddressBytes();

                result = result.Union(requests.SelectMany(i => 
                    i.OriginalRequest.Requests.Where(x =>
                        x.CommunicationActions.Any(ca => ca.ServerIP.IP.Address == bytes) ||
                        x.RequestPorts.Any(p => p.Port.IP.Address == bytes)
                    )
                ));
            }

            var reqs = db.Requests.AsQueryable();

            var tmp = result;

            result = result.GroupBy(x => x.OriginalRequest).Select(x => x.Key.Requests.OrderByDescending(o => o.Id).FirstOrDefault());

            //result = result.Where(x => !reqs.Any(y => y.OldRequestId == x.Id));

            return result.OrderByDescending(x => x.SubmissionDate);
        }
        
        public Request GetNewerRequest(Request request)
        {
            return db.Requests.SingleOrDefault(x => x.OldRequestId == request.Id);
        }

        public Request GetOlderRequest(Request request)
        {
            if (request.OldRequestId.HasValue)
            {
                return GetRequestById(request.OldRequestId.Value);

            }

            return null;
        }

        public Request CreateRequest(string businessService, string applicationName, int[] applicationTypeId,
            string netBIOSName, string dnsName, int locationId, int serverTypeId, int? bladeChassisId,
            string bladeSwitchLocation, bool? bladeTeaming, int operatingSystemId, string notes, int? oldRequestId)
        {
            BladeChassis bladeChassis = null;

            if (serverTypeId == (int)ServerTypes.Blades)
            {
                bladeChassis = CreateBladeChassis(bladeSwitchLocation, bladeTeaming.Value);
            }

            var server = new Server
            {
                ServerTypeId = serverTypeId,
                LocationId = locationId,
                BladeChassi = bladeChassis
            };

            var vir = new Virtualization
            {
                OperatingSystemId = operatingSystemId,
                NetBIOSName = netBIOSName,
                DNSName = dnsName,
                Server = server
            };

            var oldRequest = oldRequestId.HasValue ? GetRequestById(oldRequestId.Value) : null;
            var user = GetCurrentUser();
            var owner = (oldRequest != null) ? oldRequest.Owner : user;

            var request = new Request
            {
                Owner = owner,
                User = user,
                BusinessService = businessService,
                ApplicationName = applicationName,
                Virtualization = vir,
                SubmissionDate = System.DateTime.Now,
                OldRequestId = oldRequestId,
                Notes = notes
            };
            
            if(oldRequest != null)
            {
                request.OriginalRequest = oldRequest.OriginalRequest;
            }
            else
            {
                //request.OriginalRequest = request;
            }

            if (oldRequest != null && user.Id != owner.Id)
            {
                var inheritedPorts = oldRequest.RequestPorts.Where(x => x.Port.UserId != user.Id);

                foreach (var port in inheritedPorts)
                {
                    var requestPort = new RequestPort
                    {
                        Request = request,
                        Port = port.Port
                    };

                    db.RequestPorts.AddObject(requestPort);
                }
            }

            for (int i = 0; i < applicationTypeId.Length; i++)
            {
                var appType = new RequestApplicationType
                {
                    ApplicationTypeId = applicationTypeId[i],
                    Request = request
                };
            }

            db.Requests.AddObject(request);

            return request;
        }

        string[] serversIPs = new string[] { "10.11.", "192.168.", "200.200.200." };

        public Port CreatePort(Request request, int portId, IPAddress ipAddress, int? subnetMask, int portTypeId, int portNumber, int portDirectionId, DateTime? startDate, DateTime? endDate)
        {
            if (portDirectionId == (int)PortDirections.UserToRequestedServer ||
                portDirectionId == (int)PortDirections.RequestedServerToUser)
            {
                foreach (var serverIP in serversIPs)
                {
                    if (ipAddress.ToString().StartsWith(serverIP))
                    {
                        throw new IPAddressException("Either the IPAddress is invalid or the PortDirection");
                    }
                }
            }

            var port = db.Ports.SingleOrDefault(x => x.Id == portId);
            var changed = false;

            if (port != null)
            {
                if (!port.IP.Address.SequenceEqual(ipAddress.GetAddressBytes()) || portTypeId != port.PortTypeId ||
                    portNumber != port.PortNumber || portDirectionId != port.PortDirectionId)
                {
                    changed = true;
                }

                if (subnetMask.HasValue)
                {
                    if (port.SubnetMaskId.HasValue)
                    {
                        if (!port.SubnetMask.Address.SequenceEqual(CommonFunctions.SubnetMaskFromMaskBits(subnetMask.Value).GetAddressBytes()))
                        {
                            changed = true;
                        }
                    }
                    else
                    {
                        changed = true;
                    }
                }
                else if (port.SubnetMaskId.HasValue)
                {
                    changed = true;
                }

                if (startDate.HasValue)
                {
                    if (port.StartDate.HasValue)
                    {
                        if (!startDate.Value.Date.Equals(port.StartDate.Value))
                        {
                            changed = true;
                        }
                    }
                    else
                    {
                        changed = true;
                    }
                }
                else if (port.StartDate.HasValue)
                {
                    changed = true;
                }

                if (endDate.HasValue)
                {
                    if (port.EndDate.HasValue)
                    {
                        if (!endDate.Value.Date.Equals(port.EndDate.Value))
                        {
                            changed = true;
                        }
                    }
                    else
                    {
                        changed = true;
                    }
                }
                else if (port.EndDate.HasValue)
                {
                    changed = true;
                }
            }
            else
            {
                changed = true;
            }

            if (changed)
            {
                port = new Port
                {
                    PortTypeId = portTypeId,
                    PortNumber = portNumber,
                    PortDirectionId = portDirectionId,
                    IP = CreateIP(ipAddress),
                    StartDate = startDate,
                    EndDate = endDate,
                    User = GetCurrentUser()
                };

                if (port.IP.Segment == null)
                {
                    throw new IPAddressException("The IP address doesn't have a segment");
                }

                if (subnetMask.HasValue)
                {
                    port.SubnetMask = CreateSubnetMask(subnetMask.Value);
                }

                db.Ports.AddObject(port);
            }

            var requestPort = new RequestPort
            {
                Request = request,
                Port = port
            };

            db.RequestPorts.AddObject(requestPort);

            return port;
        }

        public BladeChassis CreateBladeChassis(string bladeSwitchLocation, bool bladeTeaming)
        {
            var bladeChassis = db.BladeChassis.SingleOrDefault(x => x.RackChassisPort == bladeSwitchLocation.Trim());

            if (bladeChassis == null)
            {
                bladeChassis = new BladeChassis
                {
                    RackChassisPort = bladeSwitchLocation.Trim()
                };

                db.BladeChassis.AddObject(bladeChassis);
            }

            bladeChassis.NICTeaming = bladeTeaming;

            return bladeChassis;
        }

        #endregion

        #region Users

        public User GetUserById(int userId)
        {
            return db.Users.SingleOrDefault(i => i.Id == userId);
        }

        public User GetCurrentUser()
        {
            return GetUserByUsername(Membership.GetUser().UserName);
        }

        public User GetUserByUsername(string username)
        {
            username = GetUsernameFromEmail(username);

            return db.Users.SingleOrDefault(x => x.Username == username);
        }

        public string GetUsernameFromEmail(string email)
        {
            var idx = email.IndexOf("@cibeg.com");

            if (idx >= 0)
            {
                email = email.Remove(idx);
            }

            return email;
        }

        public string GetEmailFromUsername(string username)
        {
            return GetUsernameFromEmail(username) + "@cibeg.com";
        }

        public void GenerateResetToken(User user)
        {
            user.ResetToken = Guid.NewGuid().ToString();
        }

        public void ClearResetToken(User user)
        {
            user.ResetToken = null;
        }

        public User GetUserByResetToken(string token)
        {
            return db.Users.SingleOrDefault(i => i.ResetToken == token);
        }

        public User CreateUser(string firstName, string lastName, string username, int departmentId, string[] roleNames)
        {
            var password = Membership.GeneratePassword(8, 3); //DateTime.Now.Ticks.ToString("x");

            var user = Membership.CreateUser(GetUsernameFromEmail(username), password, GetEmailFromUsername(username));

            Roles.AddUserToRoles(username, roleNames);

            var dbUser = new User
            {
                Username = GetUsernameFromEmail(username),
                FirstName = firstName,
                LastName = lastName,
                DepartmentId = departmentId,
                MembershipUserId = (Guid)user.ProviderUserKey
            };

            db.Users.AddObject(dbUser);

            GenerateResetToken(dbUser);

            return dbUser;
        }

        public User ChangeUserSettings(string firstName, string lastName, string password)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                var membershipUser = Membership.GetUser();
                membershipUser.ChangePassword(membershipUser.ResetPassword(), password);
            }

            var user = GetCurrentUser();

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                user.FirstName = firstName;
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                user.LastName = lastName;
            }

            return user;
        }

        public IQueryable<User> GetUsersInRole(string roleName)
        {
            return db.Users.Where(x => x.aspnet_Users.aspnet_Roles.Any(y => y.RoleName == roleName));
        }

        #endregion

        #region Get All Tables

        public IEnumerable<string> GetAllRoles()
        {
            return Roles.GetAllRoles();
        }

        public IQueryable<Department> GetAllDaprtments()
        {
            return db.Departments;
        }

        public IQueryable<ApplicationType> GetAllApplicationTypes()
        {
            return db.ApplicationTypes;
        }

        public IQueryable<Location> GetAllLocations()
        {
            return db.Locations;
        }

        public IQueryable<ServerType> GetAllServerTypes()
        {
            return db.ServerTypes;
        }

        public IQueryable<PortType> GetAllPortTypes()
        {
            return db.PortTypes;
        }

        public IQueryable<OperatingSystem> GetAllOperatingSystems()
        {
            return db.OperatingSystems;
        }

        public IQueryable<Vlan> GetAllVlans()
        {
            return db.Vlans;
        }

        public IQueryable<Switch> GetAllSwitches()
        {
            return db.Switches;
        }

        public IQueryable<SwitchModule> GetAllSwitchModules()
        {
            return db.SwitchModules;
        }

        public IQueryable<SwitchPort> GetAllSwitchPorts()
        {
            return db.SwitchPorts;
        }

        #endregion

        #region Admin

        public IP CreateSubnetMask(int subnetMask)
        {
            return CreateIP(CommonFunctions.SubnetMaskFromMaskBits(subnetMask));
        }

        public IP CreateIP(IPAddress address)
        {
            var arr = address.GetAddressBytes();
            var ip = db.IPs.SingleOrDefault(x => x.Address == arr);

            if (ip == null)
            {
                ip = addedIPs.SingleOrDefault(x => x.Address.SequenceEqual(arr));
            }

            if (ip == null)
            {
                ip = new IP
                {
                    Address = arr,
                    Segment = GetIPSegment(address)
                };

                addedIPs.Add(ip);

                db.IPs.AddObject(ip);
            }

            return ip;
        }

        public Vlan CreateVlan(string vlanName, int vlanNumber, IPAddress subnetmask)
        {
            var vlan = db.Vlans.SingleOrDefault(x => x.Number == vlanNumber);

            if (vlan == null)
            {
                vlan = new Vlan
                {
                    Name = vlanName,
                    Number = vlanNumber,
                    IP = CreateIP(subnetmask)
                };

                db.Vlans.AddObject(vlan);
            }

            return vlan;
        }

        public Location CreateLocation(string locationName)
        {
            var location = db.Locations.SingleOrDefault(x => x.Name.Equals(locationName, StringComparison.CurrentCultureIgnoreCase));

            if (location == null)
            {
                location = new Location
                {
                    Name = locationName
                };

                db.Locations.AddObject(location);
            }

            return location;
        }

        public Segment CreateSegment(string name, IPAddress subnetMask, IPAddress gateway, Vlan vlan, Location location)
        {
            var subnetMaskArray = subnetMask.GetAddressBytes();
            var gatewayArray = gateway.GetAddressBytes();

            var segment = db.Segments.SingleOrDefault(x => x.Gateway.Address == gatewayArray && x.SubnetMask.Address == subnetMaskArray);

            if (segment == null)
            {
                segment = new Segment
                {
                    Name = name,
                    SubnetMask = CreateIP(subnetMask),
                    Gateway = CreateIP(gateway),
                    Vlan = vlan,
                    IPGroup = GetIPGroup(gateway)
                };

                db.Segments.AddObject(segment);

                var segmentLocation = new SegmentLocation
                {
                    Segment = segment,
                    Location = location
                };

                db.SegmentLocations.AddObject(segmentLocation);
            }

            return segment;
        }

        public IPGroup CreateIPGroup(string name, IPAddress ipAddress, IPAddress subnetMask)
        {
            var group = db.IPGroups.SingleOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if(group == null)
            {
                group = new IPGroup
                {
                    Name = name,
                    IP = CreateIP(ipAddress),
                    SubnetMask = CreateIP(subnetMask)
                };

                db.IPGroups.AddObject(group);
            }

            return group;
        }

        public IPGroup GetIPGroup(IPAddress ip)
        {
            var ipGroup = db.IPGroups.AsEnumerable().SingleOrDefault(x => 
                (new IPAddress(x.SubnetMask.Address).ToInt() & ip.ToInt()) ==
                (new IPAddress(x.IP.Address).ToInt() & new IPAddress(x.SubnetMask.Address).ToInt()));
            
            return ipGroup;
        }

        public Segment GetIPSegment(IPAddress address)
        {
            return GetIPSegment(address.GetAddressBytes());
        }

        public Segment GetIPSegment(byte[] arr)
        {
            var query = db.CreateQuery<int?>(
                "SELECT VALUE IPRequestFormModel.Store.GetIPSegmentId(@someParameter) FROM {1}",
                new ObjectParameter("someParameter", arr));

            var segmentId = query.FirstOrDefault();

            Segment segment = null;

            if (segmentId != null)
            {
                segment = db.Segments.SingleOrDefault(x => x.Id == segmentId);
            }

            return segment;
        }

        public void FixIPs()
        {
            foreach (var ip in db.IPs)
            {
                ip.Segment = GetIPSegment(ip.Address);
            }
        }

        #endregion

        #region Security Engineer Actions

        public SecurityAction GetSecurityAction(int requestId)
        {
            var request = GetRequestById(requestId);

            return request.SecurityActions.LastOrDefault();
        }

        public SecurityAction ApproveRequest(int requestId, int? vlanId, string notes)
        {
            var request = GetRequestById(requestId);

            if (request == null)
            {
                throw new ArgumentException("The specified request Id doesn't exist.");
            }

            var action = new SecurityAction
            {
                Date = DateTime.Now,
                User = GetCurrentUser(),
                Assigned = true,
                RequestId = requestId,
                Notes = notes,
                Approved = true,
                VlanId = vlanId
            };

            db.SecurityActions.AddObject(action);

            return action;
        }

        public SecurityAction RejectRequest(int requestId, string notes)
        {
            var request = GetRequestById(requestId);

            if (request == null)
            {
                throw new ArgumentException("The specified request Id doesn't exist.");
            }

            var action = new SecurityAction
            {
                Date = DateTime.Now,
                User = GetCurrentUser(),
                Assigned = true,
                RequestId = requestId,
                Notes = notes,
                Approved = false
            };

            db.SecurityActions.AddObject(action);

            return action;
        }

        public SecurityAction AssignSecurityEngineer(int requestId, string notes)
        {
            var request = GetRequestById(requestId);

            var action = new SecurityAction
            {
                User = GetCurrentUser(),
                Assigned = true,
                RequestId = requestId,
                Notes = notes,
                Date = DateTime.Now
            };

            db.SecurityActions.AddObject(action);

            return action;
        }

        public SecurityAction ReleaseSecurityEngineer(int requestId, string notes)
        {
            var request = GetRequestById(requestId);

            var lastAction = request.SecurityActions.LastOrDefault();

            if (lastAction != null && lastAction.Assigned)
            {
                var action = new SecurityAction
                {
                    User = GetCurrentUser(),
                    Assigned = false,
                    RequestId = requestId,
                    Notes = notes,
                    Date = DateTime.Now
                };

                db.SecurityActions.AddObject(action);

                return action;
            }

            return null;
        }

        #endregion

        #region Communication Engineer Actions

        public CommunicationAction GetCommunicationAction(int requestId)
        {
            var request = GetRequestById(requestId);

            return request.CommunicationActions.LastOrDefault();
        }

        public CommunicationAction AssignCommunicationEngineer(int requestId, string notes)
        {
            var request = GetRequestById(requestId);

            var action = new CommunicationAction
            {
                User = GetCurrentUser(),
                Assigned = true,
                RequestId = requestId,
                Notes = notes,
                Date = DateTime.Now
            };

            db.CommunicationActions.AddObject(action);

            return action;
        }

        public CommunicationAction ReleaseCommunicationEngineer(int requestId, string notes)
        {
            var request = GetRequestById(requestId);

            var lastAction = request.CommunicationActions.LastOrDefault();

            if (lastAction != null && lastAction.Assigned)
            {
                var action = new CommunicationAction
                {
                    User = GetCurrentUser(),
                    Assigned = false,
                    RequestId = requestId,
                    Notes = notes,
                    Date = DateTime.Now
                };

                db.CommunicationActions.AddObject(action);

                return action;
            }

            return null;
        }

        public CommunicationAction ResolveRequest(int requestId, int? switchId, string switchIPAddress, string switchName, int? switchNumber,
            int? switchModuleNumber, int? switchPortNumber, string serverIPAddress, string notes)
        {
            var action = AssignCommunicationEngineer(requestId, notes);

            var securityAction = action.Request.SecurityActions.OrderByDescending(x => x.Id).FirstOrDefault();
            
            if(securityAction == null || !securityAction.Assigned || securityAction.Approved != true || securityAction.Vlan == null)
            {
                throw new VlanException("The request is not approved by the security team.");
            }

            var serverIP = action.ServerIP = new ServerIP();
            serverIP.Virtualization = action.Request.Virtualization;
            serverIP.IP = CreateIP(IPAddress.Parse(serverIPAddress));

            if (serverIP.IP.Segment == null || serverIP.IP.Segment.Vlan == null || serverIP.IP.Segment.Vlan.Id == securityAction.Vlan.Id)
            {
                throw new VlanException("The assigned IP address is not within the Vlan approved by the security team.");
            }

            if (action.Request.Virtualization.Server.ServerType.Id == (int)ServerTypes.Standalone)
            {
                if ((switchName != null && !string.IsNullOrEmpty(switchName.Trim()) && switchIPAddress != null && 
                    !string.IsNullOrEmpty(switchIPAddress.Trim()) || switchId.HasValue)
                    && switchModuleNumber.HasValue && switchPortNumber.HasValue)
                {
                    var switchPort = serverIP.SwitchPort = new SwitchPort();
                    switchPort.Port = switchPortNumber.Value;

                    var switchModule = switchPort.SwitchModule = new SwitchModule();
                    switchModule.Number = switchModuleNumber.Value;

                    var switchIP = IPAddress.Parse(switchIPAddress);
                    var switchIPAddressArr = switchIP.GetAddressBytes();

                    var switches = db.Switches.Where(i => i.IP.Address == switchIPAddressArr);
                    var switchesCount = switches.Count();

                    Switch newSwitch = null;

                    if (switchesCount > 0)
                    {
                        newSwitch = switches.SingleOrDefault(i => i.StackableNumber == switchNumber);
                    }

                    if (newSwitch == null)
                    {
                        newSwitch = new Switch();
                        newSwitch.Name = switchName;
                        newSwitch.StackableNumber = switchNumber;
                        newSwitch.IP = CreateIP(switchIP);
                    }

                    switchModule.Switch = newSwitch;

                    action.Completed = true;
                }
                else
                {
                    action.Completed = false;
                }
            }
            else
            {
                action.Completed = true;
            }

            db.CommunicationActions.AddObject(action);

            return action;
        }

        #endregion

        #region Commit Changes

        public int SaveChanges()
        {
            var status = db.SaveChanges();

            addedIPs.Clear();

            return status;
        }

        #endregion

        public OperatingSystem GetOperatingSystemById(int operatingSystemId)
        {
            return db.OperatingSystems.SingleOrDefault(x => x.Id == operatingSystemId);
        }

        public Location GetLocationById(int locationId)
        {
            return db.Locations.SingleOrDefault(x => x.Id == locationId);
        }

        public ServerType GetServerTypeById(int serverTypeId)
        {
            return db.ServerTypes.SingleOrDefault(x => x.Id == serverTypeId);
        }

        public ApplicationType GetApplicationTypeById(int applicationTypeId)
        {
            return db.ApplicationTypes.SingleOrDefault(x => x.Id == applicationTypeId);
        }
    }

    [Serializable]
    public class VlanException : Exception
    {
        public VlanException() { }
        public VlanException(string message) : base(message) { }
        public VlanException(string message, Exception inner) : base(message, inner) { }
        protected VlanException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class IPAddressException : Exception
    {
        public IPAddressException() { }
        public IPAddressException(string message) : base(message) { }
        public IPAddressException(string message, Exception inner) : base(message, inner) { }
        protected IPAddressException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}