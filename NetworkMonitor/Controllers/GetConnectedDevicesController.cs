
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetConnectedDevicesController : ControllerBase
    {
        [HttpGet(Name = "GetConnectedDevices")]
        public void Get()
        {
            Console.WriteLine("hit");
            // Find the first IPv4 gateway address on any network interface
            IPAddress gateway = NetworkInterface
                .GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().GatewayAddresses)
                .Select(g => g?.Address)
                .FirstOrDefault(a => a != null && a.AddressFamily == AddressFamily.InterNetwork);

            Console.WriteLine($"Gateway IP: {gateway}");
            if (gateway == null)
            {
                Console.WriteLine("No IPv4 gateway found.");
                return;
            }
            string gatewayString = gateway.ToString();
            int lastdot = gatewayString.LastIndexOf('.');
            Console.WriteLine($"Last dot position: {lastdot}");
            string gatewayPrefix;
            if (lastdot >= 0)
            {
                gatewayPrefix = gatewayString.Substring(0, lastdot + 1);
            }else
            {
                gatewayPrefix = gatewayString;
            }
            Console.WriteLine($"DefaultGateway: {gatewayString}\n GatewayPrefix: {gatewayPrefix}");
            for (int i = 2; i < 245; i++)
            {
                Console.WriteLine(gatewayPrefix + i);
                PingReply ping = new Ping().Send(gatewayPrefix + i);
                Console.WriteLine($"{ping.Status}");

            }
        }
    }
}