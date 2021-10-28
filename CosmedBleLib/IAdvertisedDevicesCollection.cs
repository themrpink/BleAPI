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

        void AllDevicesUpdatedHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> allDevices);
        void NewDiscoveredDeviceHandler(CosmedBleAdvertisedDevice newDevice);
        void RecentlyUpdatedDevicesHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> recentlyUpdatedDevices);

    }

    public interface IAdvertisedDevicesCollection2
    {
        IReadOnlyCollection<CosmedBleAdvertisedDevice> getLastDiscoveredDevices();

        void NewDiscoveredDeviceHandler(CosmedBleAdvertisedDevice newDevice);

    }

}
