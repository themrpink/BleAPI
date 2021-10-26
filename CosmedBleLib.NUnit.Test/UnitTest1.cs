using NUnit.Framework;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CosmedBleLib.NUnit.Test
{
    [TestFixture]
    public class Tests
    {
        private CosmedBleAdvertisedDevice device;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collection;


        [SetUp]
        public void Setup()
        {
            device = new CosmedBleAdvertisedDevice(0,
                        DateTimeOffset.UtcNow,
                        true,
                        new BluetoothLEAdvertisement(),
                        new BluetoothLEAdvertisementType()
                    );

            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            collection = watcher.getUpdatedDiscoveredDevices().allDiscoveredDevices;
        }

        [Test]
        public void Test1()
        {
            watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.getUpdatedDiscoveredDevices(1000);

            //Assert.AreSame(watcher.allDiscoveredDevices, collection);
        }
    }
}