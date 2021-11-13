using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib.Test
{
    [TestClass]
    public class CosmedBluetoothLEAdvertisementFilterTest
    {

        private CosmedBleAdvertisedDevice device;
        public CosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher Watcher;
        public CosmedBluetoothLEAdvertisementFilter Filter;



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

            Watcher = new CosmedBluetoothLEAdvertisementWatcher();

            Filter = new CosmedBluetoothLEAdvertisementFilter();

        }



        [TestMethod]
        [TestCategory("scanning.filter.state")]
        public void NewWatcher_PassAFilter_FilteringIsActive()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher(Filter);

            Assert.IsTrue(Watcher.IsFilteringActive);
        }

        [TestMethod]
        [TestCategory("scanning.filter.state")]
        public void NewWatcher_WithoutFilter_FilteringIsNotActive()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher();

            Assert.IsFalse(Watcher.IsFilteringActive);
        }


        [TestMethod]
        [TestCategory("scanning.filter.state")]
        public void NewWatcherWithoutFilter_AddAFilter_FilteringIsActive()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher();
            Watcher.SetFilter(Filter);

            Assert.IsTrue(Watcher.IsFilteringActive);
        }


        [TestMethod]
        [TestCategory("scanning.filter.state")]
        public void NewWatcherWithFilter_RemoveFilter_FilteringIsNotActive()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher(Filter);

            Watcher.StartActiveScanning();
            Watcher.RemoveFilter();

            Assert.IsFalse(Watcher.IsFilteringActive);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("scanning.filter.state")]
        public void NewWatcher_WithNullFilter_throwsException()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher(null);
            //Watcher.SetFilter(Filter);
     
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("scanning.filter.state")]
        public void Watcher_SetNullFilter_throwsException()
        {
            Watcher.SetFilter(null);
        }



        [TestMethod]
        [TestCategory("scanning.filter.state")]
        public void NewWatcher_CheckUwpWatcherFilters_filtersAreNotNull()
        {
            Watcher = new CosmedBluetoothLEAdvertisementWatcher();

            Watcher.StartActiveScanning();

            Assert.IsNotNull(Watcher.GetWatcher().AdvertisementFilter);
            Assert.IsNotNull(Watcher.GetWatcher().SignalStrengthFilter);
        }




        [TestCleanup]
        public void Cleanup()
        {
            Watcher.StopScanning();
            Watcher = null;
            Filter = null;
        }
    }
}
