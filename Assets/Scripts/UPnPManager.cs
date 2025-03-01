using System;
using System.Threading.Tasks;
using Open.Nat;  // NuGet Package

public class UPnPManager
{
    /*
    public static async Task<bool> OpenPort(int port)
    {
        /
        try
        {
            var discoverer = new NatDiscoverer();
            var device = await discoverer.DiscoverDeviceAsync();

            // Port Forwarding aktivieren
            await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, "Mirror Game"));

            Console.WriteLine($"Port {port} wurde erfolgreich geöffnet.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UPnP fehlgeschlagen: {ex.Message}");
            return false;
        }
    }
    */
}