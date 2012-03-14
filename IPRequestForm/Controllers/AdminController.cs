using System.Linq;
using System.Web.Mvc;
using IPRequestForm.Models;
using System;
using AttributeRouting;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;

namespace IPRequestForm.Controllers
{
    public class AdminController : Controller
    {
        private RequestRepository repo = new RequestRepository();

        [GET("/admin/parseSegments")]
        public ActionResult ParseSegments()
        {
            StreamReader sr = new StreamReader("C:\\segments and vlans.txt");

            string output = "<table border='1' cellpadding='10'>";

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();

                var commentIndex = line.IndexOf('#');

                if (commentIndex >= 0)
                {
                    line = line.Remove(commentIndex).Trim();
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',');
                string[] ipParts = null;

                var vlanName = parts[1];

                int vlanNumber = -1;

                if (parts.Length > 2)
                {
                    vlanNumber = int.Parse(parts[2].Substring(4));
                }

                try
                {
                    ipParts = parts[0].Trim().Split('/');

                    //# 1 Giza
                    string[] gizaIPs = new string[] { "10.12", "172.17", "172.21", "10.11", "192.168.11", "200.200", "10.51", "10.14.1", "10.51", "192.168.13", "192.168.15", "192.168.90" };
                    //# 2 Mobtadayan
                    string[] mobtadayanIPs = new string[] { "10.13", "172.16", "172.19", "172.122", "10.56", "10.14.200", "192.168.20", "192.168.21", "172.30.1", "10.56" };
                    //# 3 Smart Village
                    string[] smartIPs = new string[] { "10.16", "10.100.100", "172.22", "10.14.155", "200.0.16", "10.53", "192.168.17.18", "192.168.16.136", "192.168.16.140", "10.53" };
                    //# 4 Tiba
                    string[] tibaIPS = new string[] { "10.100.101", "10.100.102", "10.17", "172.23", "10.59" };
                    //# 5 Portsaid
                    string[] portsaidIPS = new string[] { "10.14.21", "172.18.21", "10.15.21", "10.15.221", "10.14.221", "10.114.21" };
                    //# 6 Sulatn
                    string[] sultanIPS = new string[] { "10.14.127", "172.18.127", "10.15.127", "10.14.27", "172.18.27", "10.114.27" };
                    //# 7 Merryland
                    string[] merrylandIPS = new string[] { "10.14.43", "10.14.143", "172.18.43", "10.15.43", "10.114.43", "10.15.143", "10.15.243", "10.14.164", "10.15.252", "10.14.168", "10.14.145", "10.14.243", "10.14.180" };

                    string locationName = "";

                    if(gizaIPs.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Giza";
                    }

                    if (mobtadayanIPs.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Mobtadayan";
                    }

                    if (smartIPs.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Smart Village";
                    }

                    if (tibaIPS.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Tiba";
                    }

                    if (portsaidIPS.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Portsaid";
                    }

                    if (sultanIPS.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Sultan";
                    }

                    if (merrylandIPS.Any(x => ipParts[0].StartsWith(x)))
                    {
                        locationName = "Merryland";
                    }

                    var ip = IPAddress.Parse(ipParts[0]);
                    var subnetMask = CommonFunctions.SubnetMaskFromMaskBits(int.Parse(ipParts[1]));
                    var gateway = CommonFunctions.IPAddressFromInt((subnetMask.ToInt() & ip.ToInt()) + 1);

                    var vlan = repo.CreateVlan(vlanName, vlanNumber, subnetMask);
                    var location = repo.CreateLocation(locationName);
                    repo.CreateSegment(vlanName, subnetMask, gateway, vlan, location);
                    //repo.SaveChanges();

                    output += "<tr><td>" + ip + "</td><td>" + subnetMask + "</td><td>" + gateway + "</td><td>" + vlanName + "</td><td>" + vlanNumber + "</td><td>" + locationName + "</td></tr>";

                }
                catch (Exception ex)
                {
                    output += "<br/><br/>" + ipParts[0] + "<br/>" + ex.ToString() + "<br/><br/><br/>";
                }
            }

            sr.Close();

            return Content(output + "</table>");
        }

        [GET("/admin/parseServers")]
        public ActionResult ParseServers()
        {
            StreamReader sr = new StreamReader("C:\\servers.txt");

            string output = "<table border='1' cellpadding='10'>";
            string ipFormat = "10.11.{0}.0";
            int defaultSubnetMask = 24;
            string gatewayFormat = "10.11.{0}.{1}";

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();

                var commentIndex = line.IndexOf('#');

                if(commentIndex >= 0)
                {
                    line = line.Remove(commentIndex).Trim();
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',');
                var ipParts = parts[0].Split('.');


                var ip = string.Format(ipFormat, ipParts[0]);

                var gateway = IPAddress.Parse(string.Format(gatewayFormat, ipParts[0], 1));

                var subnetMask = CommonFunctions.SubnetMaskFromMaskBits(defaultSubnetMask);

                if (ipParts.Length > 1)
                {
                    var gatewaySubnetArr = ipParts[1].Split('/');
                    subnetMask = CommonFunctions.SubnetMaskFromMaskBits(int.Parse(gatewaySubnetArr[1]));
                    gateway = IPAddress.Parse(string.Format(gatewayFormat, ipParts[0], gatewaySubnetArr[0]));
                }

                ip = CommonFunctions.IPAddressFromInt(subnetMask.ToInt() & gateway.ToInt()).ToString();

                var vlanName = parts[1];
                var vlanNumber = int.Parse(parts[2].Substring(4));

                var vlan = repo.CreateVlan(vlanName, vlanNumber, subnetMask);
                var location = repo.CreateLocation("Giza");
                repo.CreateSegment(vlanName, subnetMask, gateway, vlan, location);
                //repo.SaveChanges();

                output += "<tr><td>" + ip + "</td><td>" + subnetMask + "</td><td>" + gateway + "</td><td>" + vlanName + "</td><td>" + vlanNumber + "</td><td>Giza</td></tr>";

            }


            output += "</table>";

            sr.Close();

            return Content(output);
        }

        [GET("/admin/parseBranches")]
        public ActionResult ParseBranches()
        {
            StreamReader sr = new StreamReader("C:\\branches.txt");

            string output = "<table border='1' cellpadding='10'>";
            List<string> ipFormats = new List<string>();
            List<int> subnetMasks = new List<int>();
            List<string> vlanNames = new List<string>();
            List<int> vlanNumbers = new List<int>();

            bool beginBranches = false;

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();

                var commentIndex = line.IndexOf('#');

                if (commentIndex >= 0)
                {
                    line = line.Remove(commentIndex).Trim();
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',');

                if (!beginBranches)
                {
                    if (line.StartsWith("---"))
                    {
                        beginBranches = true;
                        continue;
                    }

                    var ipParts = parts[0].Split('/');

                    ipFormats.Add(ipParts[0].Replace("X", "{0}"));
                    subnetMasks.Add(CommonFunctions.MaskBitsToInt(int.Parse(ipParts[1])));
                    vlanNames.Add(parts[1]);
                    vlanNumbers.Add(int.Parse(parts[2].Substring(4)));

                    for (int i = 0; i < ipFormats.Count; i++)
                    {
                        var ip = IPAddress.Parse(string.Format(ipFormats[i], 255));
                        var subnetMask = CommonFunctions.IPAddressFromInt(subnetMasks[i]);
                        var gateway = CommonFunctions.IPAddressFromInt((subnetMasks[i] & ip.ToInt()) + 1);
                        var vlanName = vlanNames[i];
                        var vlanNumber = vlanNumbers[i];
                        var locationName = "Giza";

                        var vlan = repo.CreateVlan(vlanName, vlanNumber, subnetMask);
                        var location = repo.CreateLocation(locationName);
                        repo.CreateSegment(vlanName, subnetMask, gateway, vlan, location);
                    }
                }
                else
                {
                    for (int i = 0; i < ipFormats.Count; i++)
                    {
                        var ip = IPAddress.Parse(string.Format(ipFormats[i], parts[0]));
                        var subnetMask = CommonFunctions.IPAddressFromInt(subnetMasks[i]);
                        var gateway = CommonFunctions.IPAddressFromInt((subnetMasks[i] & ip.ToInt()) + 1);
                        var vlanName = vlanNames[i];
                        var vlanNumber = vlanNumbers[i];
                        var locationName = parts[1];

                        var vlan = repo.CreateVlan(vlanName, vlanNumber, subnetMask);
                        var location = repo.CreateLocation(locationName);
                        repo.CreateSegment(vlanName, subnetMask, gateway, vlan, location);
                        //repo.SaveChanges();

                        output += "<tr><td>" + ip + "</td><td>" + subnetMask + "</td><td>" + gateway + "</td><td>" + vlanName + "</td><td>" + vlanNumber + "</td><td>" + locationName + "</td></tr>";
                    }
                }
            }

            output += "</table>";

            sr.Close();

            return Content(output);
        }

        [GET("/admin/ParsePools")]
        public ActionResult ParsePools()
        {
            StreamReader sr = new StreamReader("C:\\VlanPools.txt");

            string output = "<table border='1' cellpadding='10'>";

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();

                var commentIndex = line.IndexOf('#');

                if (commentIndex >= 0)
                {
                    line = line.Remove(commentIndex).Trim();
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',');
                var ipParts = parts[0].Split('/');

                var ipAddress = IPAddress.Parse(ipParts[0]);
                var subnetMask = CommonFunctions.SubnetMaskFromMaskBits(int.Parse(ipParts[1]));
                var name = parts[1];

                output += "<tr><td>" + ipAddress + "</td><td>" + subnetMask + "</td><td>" + name + "</td></tr>";

                repo.CreateIPGroup(name, ipAddress, subnetMask);
                //repo.SaveChanges();
            }

            output += "</table>";

            sr.Close();

            return Content(output);
        }


        /*[GET("/admin/AddIP")]
        public ActionResult AddIP(string ip)
        {
            IPRequestFormEntities db = new IPRequestFormEntities();

            var ipDB = new IP();
            ipDB.Address = CommonFunctions.IPDotted2(ip);

            db.IPs.AddObject(ipDB);

            //db.SaveChanges();

            return Content(ipDB.Id.ToString());
        }*/

        [GET("/test/SubnetMaskBits")]
        public ActionResult GetSubnetMaskBits(string ip)
        {
            string ipFormat = "255.255.255.x";

            Stopwatch sw = new Stopwatch();

            sw.Start();

            uint min = uint.MaxValue << 16;
            uint max = uint.MaxValue;

            List<string> subnetMasks = new List<string>();

            for (uint i = min; i < max; i++)
            {
                //i.BitsSetCountNaive();
                //CommonFunctions.SubnetMaskToInt(i);

                var subnet = CommonFunctions.IPAddressFromInt((int)i);
                subnetMasks.Add(string.Format("{0}/{1}", subnet, i.BitsSetCountNaive()));
            }

            sw.Stop();

            //return Content((max - min).ToString());

            var output = string.Join("<br/>", subnetMasks);

            return Content(sw.ElapsedMilliseconds.ToString() + "<br/>" + output);

            //List<string> subnetMasks = new List<string>();

            for (int i = 0; i < 255; i++)
            {
                var subnet = IPAddress.Parse(ipFormat.Replace("x", i.ToString()));
                subnetMasks.Add(string.Format("{0}/{1}", subnet, CommonFunctions.SubnetMaskToInt(subnet)));
            }

            //var output = string.Join("<br/>", subnetMasks);

            //var output = CommonFunctions.SubnetMaskToInt(IPAddress.Parse(ip)).ToString();

            return Content(output);
        }

        [GET("/test/GetIPBytes")]
        public ActionResult GetIPBytes(string ip)
        {
            return Content(string.Join("<br/>", IPAddress.Parse(ip).GetAddressBytes()));
        }

        [GET("/test/GetIPRange")]
        public ActionResult GetIPRange(string ip1, string ip2)
        {
            var ipAdd1 = IPAddress.Parse(ip1).ToInt();
            var ipAdd2 = IPAddress.Parse(ip2).ToInt();

            List<string> ips = new List<string>();

            for (int i = ipAdd1; i <= ipAdd2; i++)
			{
                ips.Add(CommonFunctions.IPAddressFromInt(i).ToString());
			}

            return Content(string.Format("[{0} - {1}]<br/>", ipAdd1, ipAdd2) + string.Join("<br/>", ips.ToArray()));
        }

        [GET("/admin/RenameRoles")]
        public ActionResult RenameRole(string oldName, string newName)
        {
            //repo.RenameRoleAndUsers(oldName, newName);

            return Content(oldName + " => " + newName);
        }

        [GET("/admin/ReassignSegment")]
        public ActionResult ReassignSegment()
        {
            repo.FixIPs();

            repo.SaveChanges();

            return Content("Fixed");
        }
    }

    public static class Extensions
    {
        public static int ToInt(this IPAddress ip)
        {
            return BitConverter.ToInt32(ip.GetAddressBytes().Reverse().ToArray(), 0);
        }

        public static uint BitsSetCountNaive(this uint input)
        {
            uint count = 0;

            while (input != 0 && (input & 0x80000000) == 0x80000000)
            {
                count++;
                input <<= 1;            // do a bitwise shift to the left to create a new MSB
            }

            return count;
        }
    }
}
