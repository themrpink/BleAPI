using CosmedBleLib.Helpers;
using CosmedBleLib.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Storage.Streams;

namespace CosmedBleLib.Test
{
    [TestClass]
    public class CosmedDaraReaderTest
    {

        IBuffer bufferData;
        DateTime dt;

        [TestInitialize]
        public void Setup()
        {
            DateTime dt = DateTime.Now;

        }
        //BufferWriter
        //ToIBuffer(string data)
        //    ToIBufferFromHexString(string data)
        //    ConvertValueToBuffer(DateTime time)
        
            
        [TestMethod]
        public void ManufacturerDataReader_GivenDataBuffer_DataTranslatedCorrectly()
        {
            string testData = "test data";
            var testBuffer = BufferWriter.ToIBuffer(testData);
            ushort companyId = 0x092C;
            string companyIdString = string.Format("{0:X}", companyId);

            var manufacturerReader = new ManufacturerDataReader(testBuffer, companyId);

            Assert.AreEqual(testData, manufacturerReader.UTF8Value);
            Assert.AreEqual(companyIdString, manufacturerReader.CompanyIdHex);
        }


        [TestMethod]
        public void DataSectionReader_GivenDataBuffer_DataTranslatedCorrectly()
        {
            string testData = "test data";
            var testBuffer = BufferWriter.ToIBuffer(testData);
            var dataType = AdvertisementSectionType.ShortenedLocalName;
            byte dataTypeByte = ((byte)dataType);

            var dataSectionReader = new DataSectionReader(testBuffer, dataTypeByte);

            Assert.AreEqual(testData, dataSectionReader.UTF8Value);
            Assert.AreEqual(AdvertisementSectionType.ShortenedLocalName.ToString(), AdvertisementDataTypeHelper.ConvertAdvertisementDataTypeToString(dataSectionReader.RawDataType));
        }


        [TestMethod]
        public void ClientBufferReader_BufferToUTF8String_DataConversionIsCorrect()
        {
            string testData = "test data";
            var testBuffer = BufferWriter.ToIBuffer(testData);
            
            var result = ClientBufferReader.ToUTF8String(testBuffer);

            Assert.AreEqual(testData, result);
            Assert.AreNotEqual("false test", result);
        }
    }
}
