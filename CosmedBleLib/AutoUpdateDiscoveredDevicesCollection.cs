using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmedBleLib
{
    class AutoUpdateDiscoveredDevicesCollection
    {
        private bool isCollectingDevices = false;
        private IReadOnlyCollection<CosmedBleDevice> allDiscoveredDevices = null;
        private static int UpdateTime;

        public AutoUpdateDiscoveredDevicesCollection(CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            watcher.devicesCollectionUpdated += (devices) => { allDiscoveredDevices =  devices; };
            //watcher.stoppedListening += () => { stopUpdating(); };
        }

        /*
        public IReadOnlyCollection<CosmedBleDevice> getDiscoveredDevicesUpdated(int ms)
        {
            if (isCollectingDevices)
                return allDiscoveredDevices;

            isCollectingDevices = true;
            UpdateTime = ms;
            Thread update = new Thread(this.sendUpdateDiscoveredDevices);
            update.Start();
            return allDiscoveredDevices;
        }

        private void sendUpdateDiscoveredDevices()
        {
            while (isCollectingDevices)
            {
                Thread.Sleep(UpdateTime);
                allDiscoveredDevices = watcher.allDiscoveredDevices;
            }

        }

        private void stopUpdating()
        {
            isCollectingDevices = false;
        }*/
    }
}
