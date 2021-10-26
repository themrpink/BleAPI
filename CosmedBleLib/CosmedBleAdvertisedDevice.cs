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
    public class CosmedBleAdvertisedDevice : IAdvertisedDevice<CosmedBleAdvertisedDevice>
    {
        private AdvertisementContent scanResponseAdvertisementContent;// { get; private set; }
        private AdvertisementContent advertisementContent;// { get; private set; }

        public bool HasScanResponse { get; private set; }
        public BluetoothLEDevice device { get; private set; }
        public ulong DeviceAddress { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool IsConnectable { get; private set; }
        public short RawSignalStrengthInDBm { get; private set; }
        public BluetoothAddressType BluetoothAddressType { get; private set; }
        public bool IsAnonymous { get; private set; }        
        public bool IsDirected { get; private set; }
        public bool IsScanResponse { get; private set; }
        public bool IsScannable { get; private set; }
        public short? TransmitPowerLevelInDBm { get; private set; }


        public CosmedBleAdvertisedDevice()
        {
            this.advertisementContent = new AdvertisementContent();
            this.scanResponseAdvertisementContent = new AdvertisementContent();
            this.HasScanResponse = false;
        }

        public CosmedBleAdvertisedDevice(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv,  BluetoothLEAdvertisementType advType) : this()
        {
            if (adv == null)
                throw new ArgumentNullException();
            DeviceAddress = address;
            this.Timestamp = timestamp;
            this.IsConnectable = isConnectable;          
        }


        public async void SetBleDevice()
        {
            if (IsConnectable)
            {
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(DeviceAddress);
            }
                
         }
        
     
        public string getDeviceAddress()
        {            
            return string.Format("0x{0:X}", DeviceAddress);
        }


        public override string ToString()
        {
            return DeviceAddress.ToString();
        }


        public CosmedBleAdvertisedDevice SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            DeviceAddress = args.BluetoothAddress;
            Timestamp = args.Timestamp;
            IsConnectable = args.IsConnectable;
            RawSignalStrengthInDBm = args.RawSignalStrengthInDBm;
            BluetoothAddressType = args.BluetoothAddressType;
            IsAnonymous = args.IsAnonymous;
            IsDirected = args.IsDirected;
            IsScanResponse = args.IsScanResponse;
            IsScannable = args.IsScannable;
            TransmitPowerLevelInDBm = args.TransmitPowerLevelInDBm;

            if (args.AdvertisementType == BluetoothLEAdvertisementType.ScanResponse)
            {
                scanResponseAdvertisementContent.Advertisement = args.Advertisement;
                scanResponseAdvertisementContent.AdvertisementType = args.AdvertisementType;
                HasScanResponse = true;  
            }
            else
            {
                advertisementContent.Advertisement = args.Advertisement;
                advertisementContent.AdvertisementType = args.AdvertisementType;
            }
            return this;
        }


        public void updateAll(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv, BluetoothLEAdvertisementType advType)
        {
            DeviceAddress = address;
            this.Timestamp = timestamp;
            this.IsConnectable = isConnectable;

            if (advType == BluetoothLEAdvertisementType.ScanResponse)
            {
                scanResponseAdvertisementContent.Advertisement = adv;
                scanResponseAdvertisementContent.AdvertisementType = advType;
                HasScanResponse = true;
            }
            else
            {
                advertisementContent.Advertisement = adv;
                advertisementContent.AdvertisementType = advType;
            }
        }


        public void PrintAdvertisement()
        {
            Console.WriteLine("found: " + DeviceAddress);
            Console.WriteLine("mac: " + getDeviceAddress());
            Console.WriteLine("scan type: " + advertisementContent.AdvertisementType.ToString());
            Console.WriteLine("found: " + DeviceAddress);
            Console.WriteLine("mac: " + getDeviceAddress());
            Console.WriteLine("scan type: " + advertisementContent.AdvertisementType.ToString());

            Console.WriteLine("has scan response: " + HasScanResponse);
            Console.WriteLine("device address: " + DeviceAddress);
            Console.WriteLine("timestamp: " + Timestamp.ToString());
            Console.WriteLine("is connectable: " + IsConnectable);
            Console.WriteLine("raw signal strenght: " + RawSignalStrengthInDBm);
            Console.WriteLine("ble address type: " + this.BluetoothAddressType.ToString());
            Console.WriteLine("is anonymous: " + IsAnonymous); 
            Console.WriteLine("is directed: " + IsDirected); 
            Console.WriteLine("is scan response: " + IsScanResponse); 
            Console.WriteLine("is scannable: " + IsScannable); 
            Console.WriteLine("transmit powr level: " + TransmitPowerLevelInDBm);

            printNameFlagsGuid(advertisementContent);
            printManufacturerData(advertisementContent);
            printDataSections(advertisementContent);
        }


        public void PrintScanResponses()
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
                {
                    Console.WriteLine(advType + " guid: " + g.ToString());
                }
                    
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

                    string result = System.Text.Encoding.UTF8.GetString(data);
                    Console.WriteLine(advType + " manufacturer buffer UTF8: " + result);

                    string result2 = System.Text.Encoding.ASCII.GetString(data);
                    Console.WriteLine(advType + " manufacturer buffer ASCII: " + result2);

                    System.Text.UnicodeEncoding unicode = new System.Text.UnicodeEncoding();
                    String decodedString = unicode.GetString(data);
                    Console.WriteLine(advType + " manufacturer buffer UTF16: " + decodedString);
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
                    Console.WriteLine(advType + " data type (data section): " + ds.DataType);
                    Console.WriteLine(advType + " data type in hex (data section): " + ds.DataType.ToString("X"));
                    Console.WriteLine(advType + " data length: " + ds.Data.Length);
                    Console.WriteLine(advType + " data capacity: " + ds.Data.Capacity);

                    var data = new byte[ds.Data.Length];
                    using (var reader = DataReader.FromBuffer(ds.Data))
                    {
                        reader.ReadBytes(data);
                    }
                    string dataContent = BitConverter.ToString(data);
                    Console.WriteLine(advType + " data buffer with bit converter: " + string.Format("0x: {0}", dataContent));
                    string result = System.Text.Encoding.UTF8.GetString(data);
                    Console.WriteLine(advType + " data buffer UTF8: " + result);

                    string result2 = System.Text.Encoding.ASCII.GetString(data);
                    Console.WriteLine(advType + " data buffer ASCII: " + result2);

                    System.Text.UnicodeEncoding unicode = new System.Text.UnicodeEncoding();
                    String decodedString = unicode.GetString(data);
                    Console.WriteLine(advType + " data buffer UTF16: " + decodedString);
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

