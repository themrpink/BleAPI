using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmedBleLib
{
    public class AutoUpdateDiscoveredDevicesCollection : IAdvertisedDevicesCollection
    {
        // private bool isCollectingDevices = false;
        private readonly object ThreadLock = new object();

        private IReadOnlyCollection<CosmedBleAdvertisedDevice> lastDiscoveredDevices;

        public AutoUpdateDiscoveredDevicesCollection()
        {
            this.lastDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        }


        public IReadOnlyCollection<CosmedBleAdvertisedDevice> allDiscoveredDevices { get; private set; } = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> recentDiscoveredDevices { get; private set; } = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> getLastDiscoveredDevices()
        {
            lock (ThreadLock)
            {
                IReadOnlyCollection<CosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.ToList().AsReadOnly();
                lastDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
                return lastDiscoveredDevicesTemp;
            }
        }
        
        public void onRecentDevicesUpdated(IReadOnlyCollection<CosmedBleAdvertisedDevice> devices)
        {
            recentDiscoveredDevices = devices;
        }

        public void onNewDeviceDiscovered(CosmedBleAdvertisedDevice device)
        {
            lock (ThreadLock)
            {
                //lastDiscoveredDevices.Where(dev => dev.DeviceAddress == device.DeviceAddress).ToList().ForEach(dev =>
                //dev.setAdvertisement(device.advertisementContent.Advertisement, device.advertisementContent.AdvertisementType, device.timestamp));
                
                List<CosmedBleAdvertisedDevice> temp = lastDiscoveredDevices.Where(dev => dev.DeviceAddress != device.DeviceAddress).ToList();
                temp.Add(device);
                lastDiscoveredDevices = temp.AsReadOnly();
            }
        }

        public void onAllDevicesUpdated(IReadOnlyCollection<CosmedBleAdvertisedDevice> devices)
        {
            allDiscoveredDevices = devices;
        }

        public void ClearCollections()
        {
            allDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
            recentDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
            lastDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        }

        public void Dispose()
        {      
            allDiscoveredDevices = null;
            recentDiscoveredDevices = null;
            lastDiscoveredDevices = null;
        }
    }
}
