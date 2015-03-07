﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace chainedlupine.UPnP
{
    class httpresponse
    {
        public int status = -1;
        public string statusText = "";

        public Dictionary<string, string> values;

        public bool decode (string raw)
        {
            values = new Dictionary<string, string>();

            List<string> lines = raw.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (lines.Count > 0)
            {

                // Check first line
                string codeRaw = lines[0];
                lines.RemoveAt(0);
                MatchCollection matches = Regex.Matches(codeRaw, @"^HTTP\/1.1\s+(\d+)\s+([\w,\d]+)", RegexOptions.IgnoreCase);

                if (matches.Count == 1 && matches[0].Groups.Count == 3)
                {
                    if (!int.TryParse (matches[0].Groups[1].Value, out status))
                        return false ;

                    statusText = matches[0].Groups[2].Value;

                    foreach (string line in lines)
                    {
                        foreach (Match match in Regex.Matches (line, @"^([\w-_]+):(.*$)"))
                        {
                            if (match.Groups.Count == 3)
                                values.Add(match.Groups[1].Value.ToLower(), match.Groups[2].Value.TrimStart(' '));
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }

    class ssdp
    {
        public const string DEVICETYPE_ROOTDEVICE = "upnp:rootdevice" ;
        public const string DEVICETYPE_ALL = "ssdp:all";

        const ushort SSDP_PORT = 1900;

        public List<Device> Discover(string deviceType, ushort port = SSDP_PORT)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            s.Blocking = false;
            
            string req = 
                "M-SEARCH * HTTP/1.1\r\n" +
                "HOST: 239.255.255.250:" + port.ToString() + "\r\n" +
                "ST:" + deviceType + "\r\n" +
                "MAN:\"ssdp:discover\"\r\n" +
                "MX:1\r\n\r\n";

            byte[] data = Encoding.ASCII.GetBytes(req);
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, 1900);
            byte[] buffer = new byte[0x1000];

            DateTime start = DateTime.Now;

            List<Device> devices = new List<Device>();

            do
            {
                s.SendTo(data, endpoint);

                int length = 0;
                while (s.Available > 0)
                {
                    length = s.Receive(buffer);

                    string resp = Encoding.ASCII.GetString(buffer, 0, length);

                    httpresponse response = new httpresponse();
                    if (response.decode(resp) && response.status == 200)
                    {
                        // We've got a valid status
                        Device device = new Device();
                        device.deviceUri = new Uri(response.values["location"]);
                        devices.Add(device);
                    }
                }

                Thread.Sleep(250);

            } while (DateTime.Now.Subtract (start).TotalSeconds < 3) ;

            return devices;
        }
    }
}