using NUnit.Framework;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.Generic;
using CosmedBleLib;
using System;
using System.Threading;

namespace CosmedBle.NUnit.Test
{
    public class Tests
    {
        private CosmedBleDevice device;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private IReadOnlyCollection<CosmedBleDevice> collection;
        [SetUp]
        public void Setup()
        {
            device = new CosmedBleDevice(0,
                        DateTimeOffset.UtcNow,
                        true,
                        new BluetoothLEAdvertisement(),
                        new BluetoothLEAdvertisementType()
                    );

            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            collection = watcher.AutoUpdatedDevices.allDiscoveredDevices;
        }

        [Test]
        public void Test1()
        {
            watcher.startPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.getDiscoveredDevicesUpdated(1000);

            Assert.AreSame(watcher.AutoUpdatedDevices.allDiscoveredDevices, collection);
        }

    }
}