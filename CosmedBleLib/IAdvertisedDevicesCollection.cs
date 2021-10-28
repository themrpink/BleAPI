using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmedBleLib
{
    public interface IAdvertisedDevicesCollection
    {
        IReadOnlyCollection<CosmedBleAdvertisedDevice> allDiscoveredDevices { get; }
        IReadOnlyCollection<CosmedBleAdvertisedDevice> recentDiscoveredDevices { get; }

        IReadOnlyCollection<CosmedBleAdvertisedDevice> getLastDiscoveredDevices();
        void AllDevicesUpdatedHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> updatedDevices);
        void NewDiscoveredDeviceHandler(CosmedBleAdvertisedDevice newDevice);
        void RecentlyUpdatedDevicesHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> updatedDevices);

    }
}
