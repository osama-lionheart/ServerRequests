using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IPRequestForm.Models;
using System.Net;
using System.Collections;

namespace IPRequestForm.Controllers
{
    public class CommonFunctions
    {
        //Convert from bye[] to string 
        public static string IPDotted(byte[] ip)
        {
            return string.Join(".", ip.Select(x => x.ToString()));
        }

        //Convert from string to bye[]
        public static byte[] IPDotted2(string str)
        {
            var ips = str.Split('.');
            return ips.Select(x => byte.Parse(x)).ToArray();
        }


        public static int MaskBitsToInt(int mask)
        {
            return (int)(0xFFFFFFFF << (32 - mask));
        }

        public static int SubnetMaskToInt(IPAddress ip)
        {
            return SubnetMaskToInt((uint)ip.ToInt());
        }

        public static int SubnetMaskToInt(uint ip)
        {
            var arr = Convert.ToString(ip, 2);

            //BitArray arr = new BitArray(ip.GetAddressBytes());

            int bits = 0;

            foreach (var bit in arr)
            {
                if (bit == '0')
                {
                    break;
                }

                bits++;
            }

            return bits;
        }

        public static IPAddress IPAddressFromInt(int subnetMask)
        {
            var arr = BitConverter.GetBytes(subnetMask).Reverse().ToArray();
            return new IPAddress(arr);
        }

        public static IPAddress SubnetMaskFromMaskBits(int bits)
        {
            return IPAddressFromInt(MaskBitsToInt(bits));
        }
    }
}



