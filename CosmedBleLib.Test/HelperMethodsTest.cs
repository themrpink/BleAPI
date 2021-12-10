using CosmedBleLib.Helpers;
using CosmedBleLib.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Devices.Bluetooth;

namespace CosmedBleLib.Test
{
    [TestClass]
    public class HelperMethodsTest
    {
        [TestMethod]
        public void GattServiceUuidHelper_ConvertUuidToName_ConvertionIsCorrect()
        {
            var guid = BluetoothUuidHelper.FromShortId(0x1800); //genericAccess

            var convertedValue = GattServiceUuidHelper.ConvertUuidToName(guid);

            Assert.AreEqual(convertedValue, GattServiceUuid.GenericAccess.ToString());
        }


        [TestMethod]
        public void GattServiceUuidHelper_IsReadOnly_ResultIsTrue()
        {
            var guid = BluetoothUuidHelper.FromShortId(0x180A); //DeviceInformation

            var result = GattServiceUuidHelper.IsReadOnly(guid);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GattCharacteristicUuidHelper_ConvertUuidToName_ConvertionIsCorrect()
        {
            var guid = BluetoothUuidHelper.FromShortId(0x2A00); //DeviceName

            var convertedValue = GattCharacteristicUuidHelper.ConvertUuidToName(guid);

            Assert.AreEqual(convertedValue, GattCharacteristicUuid.DeviceName.ToString());
        }


        [TestMethod]
        public void GattDeclarationHelper_ConvertUuidToName_ConvertionIsCorrect()
        {
            var guid = BluetoothUuidHelper.FromShortId(0x2800); //PrimaryService

            var convertedValue = GattDeclarationHelper.ConvertUuidToName(guid);

            Assert.AreEqual(convertedValue, GattDeclarationUuid.PrimaryService.ToString());
        }


        [TestMethod]
        public void GattDescriptorUuidHelper_ConvertUuidToName_ConvertionIsCorrect()
        {
            var guid = BluetoothUuidHelper.FromShortId(0x2901); //CharacteristicUserDescription

            var convertedValue = GattDescriptorUuidHelper.ConvertUuidToName(guid);

            Assert.AreEqual(convertedValue, GattDescriptorUuid.CharacteristicUserDescription.ToString());
        }


        [TestMethod]
        public void AdvertisementDataTypeHelper_ConvertAdvertisementDataTypeToString_ConvertionIsCorrect()
        {
            var convertedValue = AdvertisementDataTypeHelper.ConvertAdvertisementDataTypeToString(0x08); //ShortenedLocalName
            
            Assert.AreEqual(convertedValue, AdvertisementSectionType.ShortenedLocalName.ToString());
        }

        [TestMethod]
        public void AdvertisementDataTypeHelper_FalseConvertAdvertisementDataTypeToString_ConvertionIsCorrect()
        {
            var test = ((byte)0xFA).ToString("X2");

            var convertedValue = AdvertisementDataTypeHelper.ConvertAdvertisementDataTypeToString(0xFA); //unknown value
            
            Assert.AreEqual(convertedValue, test);
            
           
        }


        [TestMethod]
        public void AppearenceDataTypeHelper_ConvertAppearenceTypeToString_ConvertionIsCorrect()
        {
            var convertedValue = AppearenceDataTypeHelper.ConvertAppearenceTypeToString(0x001); //GenericPhone

            Assert.AreEqual(convertedValue, BluetoothAppearanceType.GenericPhone.ToString());
        }

        [TestMethod]
        public void PresentationFormatUnitsHelper_ConvertUnitTypeToString_ConvertionIsCorrect()
        {
            var convertedValue = PresentationFormatUnitsHelper.ConvertUnitTypeToString(0x2700); //Unitless

            Assert.AreEqual(convertedValue, PresentationFormats.Units.Unitless.ToString());
        }


        [TestMethod]
        public void PresentationFormatTypeHelper_ConvertFormatTypeToString_ConvertionIsCorrect()
        {
            var convertedValue = PresentationFormatTypeHelper.ConvertFormatTypeToString(0x01); //Boolean 

            Assert.AreEqual(convertedValue, PresentationFormats.FormatTypes.Boolean.ToString());
        }


        [TestMethod]
        public void NamespaceTypeHelper_ConvertUnitTypeToString_ConvertionIsCorrect()
        {
            var convertedValue = NamespaceTypeHelper.ConvertNamespaceTypeToString(0x01); //BluetoothSigAssignedNumber 

            Assert.AreEqual(convertedValue, PresentationFormats.NamespaceId.BluetoothSigAssignedNumber.ToString());
        }


    }
}
