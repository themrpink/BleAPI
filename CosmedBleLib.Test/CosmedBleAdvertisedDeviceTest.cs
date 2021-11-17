using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CosmedBleLib.Test
{
    [TestClass]
    public class CosmedBleAdvertisedDeviceTest
    {
        [TestMethod]
        public void AdvertisedDevice_NewInstance_AdvContentNotNull()
        {
            var advDevice = new CosmedBleAdvertisedDevice();

            var adv1 = advDevice.GetAdvertisementContent;
            var adv2 = advDevice.GetScanResponseAdvertisementContent;

            Assert.IsNotNull(adv1);
            Assert.IsNotNull(adv2);
        }
    }
}
