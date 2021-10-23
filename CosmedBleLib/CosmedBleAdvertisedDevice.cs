using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Storage.Streams;
//using static Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataTypes;


namespace CosmedBleLib
{
    public class CosmedBleAdvertisedDevice
    {

        public AdvertisementContent scanResponseAdvertisementContent { get; private set; }
        public AdvertisementContent advertisementContent { get; private set; }
        public ulong DeviceAddress { get; private set; }
        public DateTimeOffset timestamp { get; private set; }
        public bool isConnectable { get; private set; }
        public bool hasAScanResponse { get; private set; }
        public BluetoothLEDevice device { get; set; } 

        public CosmedBleAdvertisedDevice(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv,  BluetoothLEAdvertisementType advType)
        {
            if (adv == null)
                throw new ArgumentNullException();

            DeviceAddress = address;
            this.timestamp = timestamp;
            this.isConnectable = isConnectable;
            this.hasAScanResponse = false;
            this.advertisementContent = new AdvertisementContent();
            this.scanResponseAdvertisementContent = new AdvertisementContent();
            setAdvertisement(adv, advType, timestamp);           
        }

        public async void SetBleDevice()
        {
            if(isConnectable)
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(DeviceAddress);
         }
        
     
        public string getDeviceAddress()
        {            
            return string.Format("0x{0:X}", DeviceAddress);
        }


        public override string ToString()
        {
            return DeviceAddress.ToString();
        }

        public void setAdvertisement(BluetoothLEAdvertisement adv, BluetoothLEAdvertisementType advType, DateTimeOffset timestamp)
        {
            if(advType == BluetoothLEAdvertisementType.ScanResponse)
            {
                scanResponseAdvertisementContent.Advertisement = adv;
                scanResponseAdvertisementContent.AdvertisementType = advType;
                hasAScanResponse = true;
                this.timestamp = timestamp;   
            }
            else
            {
                advertisementContent.Advertisement = adv;
                advertisementContent.AdvertisementType = advType;
                this.timestamp = timestamp;
            }
        }

        public void updateAll(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv, BluetoothLEAdvertisementType advType)
        {
            DeviceAddress = address;
            this.timestamp = timestamp;
            this.isConnectable = isConnectable;

            if (advType == BluetoothLEAdvertisementType.ScanResponse)
            {
                scanResponseAdvertisementContent.Advertisement = adv;
                scanResponseAdvertisementContent.AdvertisementType = advType;
                hasAScanResponse = true;
            }
            else
            {
                advertisementContent.Advertisement = adv;
                advertisementContent.AdvertisementType = advType;
            }
        }

        public void printAdvertisement()
        {
            Console.WriteLine("found: " + DeviceAddress);
            Console.WriteLine("mac: " + getDeviceAddress());
            Console.WriteLine("scan type: " + advertisementContent.AdvertisementType.ToString());
            Console.WriteLine("local name: " + advertisementContent.Advertisement.LocalName);
            
            printNameFlagsGuid(advertisementContent);
            printManufacturerData(advertisementContent);
            printDataSections(advertisementContent);
        }


        public void printScanResponses()
        {
            if (scanResponseAdvertisementContent != null)
            {
                Console.WriteLine("found: " + DeviceAddress);
                Console.WriteLine("mac: " + getDeviceAddress());

                if (scanResponseAdvertisementContent.Advertisement != null)
                {
                    Console.WriteLine("sr local name: " + scanResponseAdvertisementContent.Advertisement.LocalName);
                    Console.WriteLine("sr scan type: " + scanResponseAdvertisementContent.AdvertisementType.ToString());

                    printNameFlagsGuid(scanResponseAdvertisementContent, "sr");

                    if (scanResponseAdvertisementContent.Advertisement.DataSections != null)
                    {
                        Console.WriteLine("sr company numb: " + scanResponseAdvertisementContent.Advertisement.ManufacturerData.Count);
                        printManufacturerData(scanResponseAdvertisementContent, "sr");
                    }
                    else
                    {
                        Console.WriteLine("sr: manufacturer data is null");
                    }

                    if (scanResponseAdvertisementContent.Advertisement.DataSections != null)
                    {
                        printDataSections(scanResponseAdvertisementContent, "sr");
                    }
                    else
                    {
                        Console.WriteLine("sr: data section is null");
                    }
                }
                else
                {
                    Console.WriteLine("sr: advertisement is null");
                }                  
            }
            else
            {
                Console.WriteLine("sr: qualcosa era null");
            }
        }

        private void printNameFlagsGuid(AdvertisementContent devAdv, string advType = "")
        {
            if (devAdv.Advertisement != null)
            {
                Console.WriteLine(advType + " localname: " + devAdv.Advertisement.LocalName);
                Console.WriteLine(advType + " flags: " + devAdv.Advertisement.Flags.ToString());
                Console.WriteLine(advType + " guid numb: " + devAdv.Advertisement.ServiceUuids.Count);
                foreach (Guid g in devAdv.Advertisement.ServiceUuids)
                    Console.WriteLine(advType + " guid: " + g.ToString());
            }
        }

        private void printManufacturerData(AdvertisementContent adv, string advType = "")
        {
            if(adv.Advertisement != null)
            {
                IList<BluetoothLEManufacturerData> manData = adv.Advertisement.ManufacturerData;
                Console.WriteLine(advType + " Manufacturer data count: " + manData.Count);

                foreach (BluetoothLEManufacturerData m in manData)
                {
                    Console.WriteLine(advType + " company id: " + m.CompanyId);
                    Console.WriteLine(advType + " company id HEX: " + m.CompanyId.ToString("X"));
                    Console.WriteLine(advType + " manufacturer data capacity: " + m.Data.Capacity);
                    Console.WriteLine(advType + " manufacturer data length: " + m.Data.Length);

                    var data = new byte[m.Data.Length];
                    using (var reader = DataReader.FromBuffer(m.Data))
                    {
                        reader.ReadBytes(data);
                    }
                    string dataContent = BitConverter.ToString(data); ;
                    Console.WriteLine(advType + " manufacturer buffer: " + dataContent);

                    string utfString = Encoding.UTF8.GetString(data, 0, data.Length);
                    Console.WriteLine(advType + "utfString: " + utfString);

                    // ASCII conversion - string from bytes  
                    string asciiString = Encoding.ASCII.GetString(data, 0, data.Length);
                    Console.WriteLine(advType + "asciiString: " + asciiString);
                }
            }
            
        }
        private void printDataSections(AdvertisementContent adv, string advType = "")
        {
            if(adv.Advertisement != null)
            {
                IList<BluetoothLEAdvertisementDataSection> dataSections = adv.Advertisement.DataSections;
                Console.WriteLine(advType + " data numb: " + dataSections.Count);
                foreach (BluetoothLEAdvertisementDataSection ds in dataSections)
                {
                    Console.WriteLine(advType + "data type (data section): " + ds.DataType);
                    Console.WriteLine(advType + "data type in hex (data section): " + ds.DataType.ToString("X"));
                    Console.WriteLine(advType + "data length: " + ds.Data.Length);
                    Console.WriteLine(advType + "data capacity: " + ds.Data.Capacity);

                    var data = new byte[ds.Data.Length];
                    using (var reader = DataReader.FromBuffer(ds.Data))
                    {
                        reader.ReadBytes(data);
                    }
                    string dataContent = BitConverter.ToString(data);
                    Console.WriteLine(advType + "data buffer with bit converter: " + string.Format("0x: {0}", dataContent));
                }
            }
            
        }
    }

    public class AdvertisementContent
    {
        public BluetoothLEAdvertisement Advertisement { get; set; }
        public BluetoothLEAdvertisementType AdvertisementType { get; set; }
    }

}

