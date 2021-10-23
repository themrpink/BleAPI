using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CosmedBleLib.MSTest.UnitTest
{
    [TestClass]
    public class CosmedBluetoothLEAdvertisementWatcherTest
    {

        private CosmedBleAdvertisedDevice device;
        public CosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionAll;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionRecent;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionLast;

        [TestInitialize]
        public void Setup()
        {
            device = new CosmedBleAdvertisedDevice(0,
                                    DateTimeOffset.UtcNow,
                                    true,
                                    new BluetoothLEAdvertisement(),
                                    new BluetoothLEAdvertisementType()
                                );

            device2 = new CosmedBleAdvertisedDevice(2,
                        DateTimeOffset.UtcNow,
                        true,
                        new BluetoothLEAdvertisement(),
                        new BluetoothLEAdvertisementType()
                    );

            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            collectionAll = watcher.getUpdatedDiscoveredDevices().allDiscoveredDevices;
            collectionRecent = watcher.getUpdatedDiscoveredDevices().recentDiscoveredDevices;
            collectionLast = watcher.getUpdatedDiscoveredDevices().getLastDiscoveredDevices();
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void allDeviceDiscovered_NewDevice_DeviceAdded()
        {
            watcher.addDiscoveredDevices(device);

            CollectionAssert.Contains(watcher.allDiscoveredDevices.ToList(),  device);
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void recentlyDiscoveredDevices_AddDeviceAndWaitTimeout_DeviceRemoved()
        {         
            double timeout = watcher.timeout;
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            watcher.addDiscoveredDevices(device);
            Thread.Sleep((int)timeout * 1000);
            Thread.Sleep(2000);

            CollectionAssert.DoesNotContain(watcher.recentlyDiscoveredDevices.ToList(), device);
            CollectionAssert.DoesNotContain(collections.recentDiscoveredDevices.ToList(), device);
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void DiscoveredDeviceCollections_DeviceNotAdded_ListIsEmpty()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            CollectionAssert.AreEquivalent(collections.recentDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            CollectionAssert.AreEquivalent(collections.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            CollectionAssert.AreEquivalent(collections.getLastDiscoveredDevices().ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_UpdateTimeElapsed_CollectionChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices();

            watcher.startPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.getUpdatedDiscoveredDevices(1000);
            Thread.Sleep(1000);

            Assert.AreNotSame(collections.allDiscoveredDevices, collectionAll);
            Assert.IsTrue(collections.allDiscoveredDevices != collectionAll);
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_CollectionsUpdateTimeNotElapsedYet_CollectionNotChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);
            
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);

            //with sleep it fails
            //

            CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.allDiscoveredDevices.ToList());
            CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.recentDiscoveredDevices.ToList());
            //this one gets immediately updated:
            CollectionAssert.AreNotEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.getLastDiscoveredDevices().ToList());
        }

        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_DelayAddingElementToAllAndRecentCollections_CollectionsNotChangedYet()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            watcher.addDiscoveredDevices(device);

            CollectionAssert.AreEquivalent(collectionAll.ToList(), collections.allDiscoveredDevices.ToList());
            CollectionAssert.AreEquivalent(collectionRecent.ToList(), collections.recentDiscoveredDevices.ToList());
            //this one gets immediately updated:
           // CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.getLastDiscoveredDevices().ToList());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection2_NoDelayInAddingElementToLastCollection_CollectionChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            watcher.addDiscoveredDevices(device);

            CollectionAssert.AreNotEquivalent(collectionLast.ToList(), collections.getLastDiscoveredDevices().ToList());
        }

        [TestCleanup]
        public void Cleanup()
        {
            //watcher.stopScanning();
            //watcher = null;
        }
    }
}
