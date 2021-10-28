using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Storage.Streams;
//using static Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataTypes;


namespace CosmedBleLib
{

    public static class DeviceFactory
    {
        public static CosmedBleAdvertisedDevice CreateAdvertisedDevice(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            return new CosmedBleAdvertisedDevice(args);
        }
    }

    public class CosmedBleAdvertisedDevice : IAdvertisedDevice<CosmedBleAdvertisedDevice>
    {
        private AdvertisementContent scanResponseAdvertisementContent;// { get; private set; }
        private AdvertisementContent advertisementContent;// { get; private set; }


        #region Device Properties

        //the name of the device
        public string DeviceName { get; private set; }

        //true is a scan response has been received
        public bool HasScanResponse { get; private set; }
       
        //the address value
        public ulong DeviceAddress { get; set; }

        //the type of address (public - random)
        public BluetoothAddressType BluetoothAddressType { get; private set; }

        //Timestamp of the last received advertising
        public DateTimeOffset Timestamp { get; set; }

        //Whether the Bluetooth LE device is currently advertising a connectable advertisement.
        public bool IsConnectable { get; private set; }

        //signal strength
        public short RawSignalStrengthInDBm { get; private set; }

        public bool IsAnonymous { get; private set; }        
        public bool IsDirected { get; private set; }
        public bool IsScannable { get; private set; }
        public short? TransmitPowerLevelInDBm { get; private set; }

        #endregion


        #region Delegate

        public event Action<CosmedBleAdvertisedDevice> ScanResponseReceived;

        #endregion


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
            DeviceName = adv.LocalName;
            DeviceAddress = address;
            this.Timestamp = timestamp;
            this.IsConnectable = isConnectable;          
        }



        /*
        private async Task SetBleDevice()
        {
            if (IsConnectable)
            {
                try
                {
                    device = await BluetoothLEDevice.FromBluetoothAddressAsync(DeviceAddress);
                }
                catch(Exception e)
                {
                    //throw new ArgumentNullException(nameof(DeviceAddress));
                }
            }


                
         }

        public async Task<CosmedBleConnection> ConnectDevice()
        {           
            await SetBleDevice();
            var connection = new CosmedBleConnection(device);
            CosmedBluetoothLEAdvertisementWatcher.StopScan();
            return connection;
        }*/

        public string getDeviceAddress()
        {            
            return string.Format("0x{0:X}", DeviceAddress);
        }


        public override string ToString()
        {
            return DeviceAddress.ToString();
        }

        public CosmedBleAdvertisedDevice (BluetoothLEAdvertisementReceivedEventArgs args) : this()
        {
            _ = SetAdvertisement(args);
        }


        public CosmedBleAdvertisedDevice SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            DeviceName = args.Advertisement.LocalName;
            DeviceAddress = args.BluetoothAddress;
            Timestamp = args.Timestamp;
            IsConnectable = args.IsConnectable;
            RawSignalStrengthInDBm = args.RawSignalStrengthInDBm;
            BluetoothAddressType = args.BluetoothAddressType;
            IsAnonymous = args.IsAnonymous;
            IsDirected = args.IsDirected;
            IsScannable = args.IsScannable;
            TransmitPowerLevelInDBm = args.TransmitPowerLevelInDBm;

            if (args.AdvertisementType == BluetoothLEAdvertisementType.ScanResponse)
            {
                scanResponseAdvertisementContent.Advertisement = args.Advertisement;
                scanResponseAdvertisementContent.AdvertisementType = args.AdvertisementType;
                HasScanResponse = true;
                ScanResponseReceived?.Invoke(this);
            }
            else
            {
                advertisementContent.Advertisement = args.Advertisement;
                advertisementContent.AdvertisementType = args.AdvertisementType;
            }
            return this;
        }


        public void PrintAdvertisement()
        {

            Console.WriteLine("found: " + DeviceAddress);
            Console.WriteLine("mac: " + getDeviceAddress());
            Console.WriteLine("device name: " + DeviceName);
            Console.WriteLine("scan type: " + advertisementContent.AdvertisementType.ToString());

            Console.WriteLine("has scan response: " + HasScanResponse);
            Console.WriteLine("timestamp: " + Timestamp.ToString());
            Console.WriteLine("is connectable: " + IsConnectable);
            Console.WriteLine("raw signal strenght: " + RawSignalStrengthInDBm);
            Console.WriteLine("ble address type: " + this.BluetoothAddressType.ToString());
            Console.WriteLine("is anonymous: " + IsAnonymous); 
            Console.WriteLine("is directed: " + IsDirected); 
            Console.WriteLine("is scannable: " + IsScannable); 
            Console.WriteLine("transmit powr level: " + TransmitPowerLevelInDBm);

            printNameFlagsGuid(advertisementContent);
            printManufacturerData(advertisementContent);
            printDataSections(advertisementContent);
        }


        public void PrintScanResponses()
        {
            if (scanResponseAdvertisementContent.Advertisement != null && scanResponseAdvertisementContent.Advertisement != null)
            {
                Console.WriteLine("found: " + DeviceAddress);
                Console.WriteLine("mac: " + getDeviceAddress());
           
                    Console.WriteLine("sr local name: " + scanResponseAdvertisementContent.Advertisement.LocalName);
                    Console.WriteLine("sr scan type: " + scanResponseAdvertisementContent.AdvertisementType.ToString());

                    printNameFlagsGuid(scanResponseAdvertisementContent, "sr");

          //          if (scanResponseAdvertisementContent.Advertisement.DataSections != null)
                    {
                        Console.WriteLine("sr company numb: " + scanResponseAdvertisementContent.Advertisement.ManufacturerData.Count);
                        printManufacturerData(scanResponseAdvertisementContent, "sr");
                    }
               //     else
                    {
                        Console.WriteLine("sr: manufacturer data is null");
                    }

                //    if (scanResponseAdvertisementContent.Advertisement.DataSections != null)
                    {
                        printDataSections(scanResponseAdvertisementContent, "sr");
                    }
                 //   else
                    {
                        Console.WriteLine("sr: data section is null");
                    }               
            }
            else
            {
                Console.WriteLine("sr: something is null");
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


    //meglio una classe??
    public struct AdvertisementContent
    {
        public BluetoothLEAdvertisement Advertisement { get; set; }
        public BluetoothLEAdvertisementType AdvertisementType { get; set; }
    }

}

