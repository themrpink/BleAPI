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

        public string HexDeviceAddress { get { return string.Format("0x{0:X}", DeviceAddress); } }

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

        public CosmedBleAdvertisedDevice(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv, BluetoothLEAdvertisementType advType) : this()
        {
            if (adv == null)
                throw new ArgumentNullException();
            DeviceName = adv.LocalName;
            DeviceAddress = address;
            this.Timestamp = timestamp;
            this.IsConnectable = isConnectable;
        }



        public CosmedBleAdvertisedDevice(BluetoothLEAdvertisementReceivedEventArgs args) : this()
        {
            _ = SetAdvertisement(args);
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
            IsScannable = args.IsScannable;
            TransmitPowerLevelInDBm = args.TransmitPowerLevelInDBm;

            if (args.AdvertisementType == BluetoothLEAdvertisementType.ScanResponse)
            {
                if (DeviceName == null || !args.Advertisement.LocalName.Equals(""))
                {
                    DeviceName = args.Advertisement.LocalName;
                }

                scanResponseAdvertisementContent.Advertisement = args.Advertisement;
                scanResponseAdvertisementContent.AdvertisementType = args.AdvertisementType;
                HasScanResponse = true;
                ScanResponseReceived?.Invoke(this);
            }
            else
            {
                DeviceName = args.Advertisement.LocalName;
                advertisementContent.Advertisement = args.Advertisement;
                advertisementContent.AdvertisementType = args.AdvertisementType;
            }
            return this;
        }



        public AdvertisementContent GetAdvertisementContent => advertisementContent;
        public AdvertisementContent GetScanResponseAdvertisementContent => scanResponseAdvertisementContent;



        public IReadOnlyCollection<Guid> ServiceUuids
        {
            get
            {
                return advertisementContent.Advertisement == null ? new List<Guid>().AsReadOnly() :
                    new List<Guid>(advertisementContent.Advertisement.ServiceUuids).AsReadOnly();
            }
        }


        public IReadOnlyCollection<Guid> ServiceUuidsFromScanResponse
        {
            get
            {
                return HasScanResponse ? new List<Guid>().AsReadOnly() :
                    new List<Guid>(scanResponseAdvertisementContent.Advertisement.ServiceUuids).AsReadOnly();
            }
        }



        public BluetoothLEAdvertisementFlags? Flags
        {
            get
            {
                return advertisementContent.Advertisement.Flags;
            }
        }


        public ManufacturerDataCollection ManufacturerData
        {
            get
            {
                return advertisementContent.Advertisement != null ? new ManufacturerDataCollection(advertisementContent.Advertisement.ManufacturerData) :
                    new ManufacturerDataCollection(new List<BluetoothLEManufacturerData>());
            }
        }


        public ManufacturerDataCollection ManufacturerDataFromScanResponse
        {
            get
            {
                return scanResponseAdvertisementContent.Advertisement != null ? new ManufacturerDataCollection(scanResponseAdvertisementContent.Advertisement.ManufacturerData) :
                    new ManufacturerDataCollection(new List<BluetoothLEManufacturerData>());
            }
        }


        public DataSectionCollection DataSections
        {
            get
            {
                return advertisementContent.Advertisement != null ? new DataSectionCollection(advertisementContent.Advertisement.DataSections) :
                    new DataSectionCollection(new List<BluetoothLEAdvertisementDataSection>());
            }
        }



        public DataSectionCollection DataSectionsFromScanResponse
        {
            get
            {
                return scanResponseAdvertisementContent.Advertisement != null ? new DataSectionCollection(scanResponseAdvertisementContent.Advertisement.DataSections) :
                    new DataSectionCollection(new List<BluetoothLEAdvertisementDataSection>());
            }
        }






        public void PrintAdvertisement()
        {
            Console.WriteLine();
            Console.WriteLine("################################### new ####################################");
            Console.WriteLine();

            Console.WriteLine("found: " + DeviceAddress);
            Console.WriteLine("mac: " + HexDeviceAddress);
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

            Console.WriteLine(" localname: " + DeviceName);
            Console.WriteLine(" flags: " + Flags);
            ///Console.WriteLine(" guid numb: " + devAdv.Advertisement.ServiceUuids.Count);
            foreach (Guid g in ServiceUuids)
            {
                Console.WriteLine(" guid: " + g.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("----------------- normal advertisement ----------------------------");
            Console.WriteLine();

            string advType = "";
            foreach (AdvertisementManufacturerData m in ManufacturerData)
            {
                Console.WriteLine(advType + " company id: " + m.CompanyId);
                Console.WriteLine(advType + " company id HEX: " + m.CompanyIdHex);
                Console.WriteLine(advType + " manufacturer data capacity: " + m.RawData.Capacity);
                Console.WriteLine(advType + " manufacturer data length: " + m.RawData.Length);

                Console.WriteLine(advType + " manufacturer buffer: " + m.HexData);
                Console.WriteLine(advType + " manufacturer buffer UTF8: " + m.UTF8Data);
                Console.WriteLine(advType + " manufacturer buffer ASCII: " + m.ASCIIData);
                Console.WriteLine(advType + " manufacturer buffer UTF16: " + m.UTF16Data);
            }
            Console.WriteLine();
            foreach (AdvertisementDataSection m in DataSections)
            {
                Console.WriteLine(advType + " data type: " + m.DataType);

                Console.WriteLine(advType + " buffer: " + m.HexData);
                Console.WriteLine(advType + " buffer UTF8: " + m.UTF8Data);
                Console.WriteLine(advType + " buffer ASCII: " + m.ASCIIData);
                Console.WriteLine(advType + " buffer UTF16: " + m.UTF16Data);
            }
            Console.WriteLine();
            Console.WriteLine("++++++++++++++++++++++++ scan response +++++++++++++++++++++++");
            Console.WriteLine();
            advType = "sr:";
            foreach (AdvertisementManufacturerData m in ManufacturerDataFromScanResponse)
            {
                Console.WriteLine(advType + " company id: " + m.CompanyId);
                Console.WriteLine(advType + " company id HEX: " + m.CompanyIdHex);
                Console.WriteLine(advType + " manufacturer data capacity: " + m.RawData.Capacity);
                Console.WriteLine(advType + " manufacturer data length: " + m.RawData.Length);

                Console.WriteLine(advType + " manufacturer buffer: " + m.HexData);
                Console.WriteLine(advType + " manufacturer buffer UTF8: " + m.UTF8Data);
                Console.WriteLine(advType + " manufacturer buffer ASCII: " + m.ASCIIData);
                Console.WriteLine(advType + " manufacturer buffer UTF16: " + m.UTF16Data);
            }
            Console.WriteLine();
            foreach (AdvertisementDataSection m in DataSectionsFromScanResponse)
            {
                Console.WriteLine(advType + " data type: " + m.DataType);

                Console.WriteLine(advType + " buffer: " + m.HexData);
                Console.WriteLine(advType + " buffer UTF8: " + m.UTF8Data);
                Console.WriteLine(advType + " buffer ASCII: " + m.ASCIIData);
                Console.WriteLine(advType + " buffer UTF16: " + m.UTF16Data);
            }
            Console.WriteLine();
        }
    }


    //meglio una classe??
    public struct AdvertisementContent
    {
        public BluetoothLEAdvertisement Advertisement { get; set; }
        public BluetoothLEAdvertisementType AdvertisementType { get; set; }
    }


}

