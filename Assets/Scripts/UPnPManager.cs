using System;
using System.Threading.Tasks;
using UnityEngine;
using Open.Nat;  // Open.NAT importieren

public class UPnPManager : MonoBehaviour
{
    private async void Start()
    {
        await OpenPort(7777);
    }

    private async Task OpenPort(int port)
    {
        try
        {
            var discoverer = new NatDiscoverer();
            var device = await discoverer.DiscoverDeviceAsync();
            await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, "Mirror Game"));

            Debug.Log($"✅ UPnP: Port {port} erfolgreich geöffnet!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ UPnP fehlgeschlagen: {ex.Message}");
        }
    }
}