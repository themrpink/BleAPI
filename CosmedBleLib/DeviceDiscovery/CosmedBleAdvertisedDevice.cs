using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using CosmedBleLib.Helpers;
using CosmedBleLib.Collections;


namespace CosmedBleLib.DeviceDiscovery
{

    /// <summary>
    /// Represents a remote devices with its received advertisement and, if available, scan response
    /// </summary>
    public class CosmedBleAdvertisedDevice : ICosmedBleAdvertisedDevice
    {
        private AdvertisementContent scanResponseAdvertisementContent;
        private AdvertisementContent advertisementContent;


        #region Device Properties

        /// <value>
        /// Gets the the name of the device
        /// </value>
        public string DeviceName { get; private set; }


        /// <value>
        /// Gets the boolean indicating if a scan response from the device has been received
        /// </value>
        public bool HasScanResponse { get; private set; }


        /// <value>
        /// Gets and sets the device address value
        /// </value>
        public ulong DeviceAddress { get; set; }


        /// <value>
        /// Gets and sets the device adress value expressed as string representation of the hexadecimal value
        /// </value>
        public string HexDeviceAddress { get { return string.Format("0x{0:X2}", DeviceAddress); } }


        /// <value>
        /// Gets the type of address (public - random)
        /// </value>
        public BluetoothAddressType BluetoothAddressType { get; private set; }


        /// <value>
        /// Gets and sets the Timestamp of the last received advertising
        /// </value>
        public DateTimeOffset Timestamp { get; set; }


        /// <value>
        /// Gets the boolean indicating whether the Bluetooth LE device is currently advertising a connectable advertisement.
        /// </value>
        public bool IsConnectable { get; private set; }


        /// <value>
        /// Gets the Signal Strength in dBm
        /// </value>
        public short RawSignalStrengthInDBm { get; private set; }


        /// <value>
        /// Gets the boolean indicating whether a Bluetooth Address was omitted from the received advertisement.
        /// </value>
        public bool IsAnonymous { get; private set; }


        /// <value>
        /// Indicates whether the received advertisement is directed.
        /// </value>
        public bool IsDirected { get; private set; }


        /// <value>
        /// Indicates whether the received advertisement is scannable.
        /// </value>
        public bool IsScannable { get; private set; }


        /// <value>
        /// Represents the received transmit power of the advertisement.
        /// </value>
        public short? TransmitPowerLevelInDBm { get; private set; }


        /// <value>
        /// Gets the AdvertisementContect object
        /// </value>
        public AdvertisementContent GetAdvertisementContent => advertisementContent;


        /// <value>
        /// Gets the tScanResponseAdvertisementContect object 
        /// </value>
        public AdvertisementContent GetScanResponseAdvertisementContent => scanResponseAdvertisementContent;


        /// <value>
        /// Gets the colletion of services uuid if they exists, otherwise returns an empty collection
        /// </value>
        public IReadOnlyCollection<Guid> ServiceUuids
        {
            get
            {
                return advertisementContent.Advertisement == null ? new List<Guid>().AsReadOnly() :
                    new List<Guid>(advertisementContent.Advertisement.ServiceUuids).AsReadOnly();
            }
        }


        /// <value>
        /// Gets the collection of services uuid from the scan response if they exists, otherwise returns an empty collection
        /// </value>
        public IReadOnlyCollection<Guid> ServiceUuidsFromScanResponse
        {
            get
            {
                return HasScanResponse ? new List<Guid>().AsReadOnly() :
                    new List<Guid>(scanResponseAdvertisementContent.Advertisement.ServiceUuids).AsReadOnly();
            }
        }


        /// <value>
        /// Gets the flags of the advertisement if it exists, otherwise returns null
        /// </value>
        public BluetoothLEAdvertisementFlags? Flags
        {
            get
            {
                return advertisementContent.Advertisement == null ? null : advertisementContent.Advertisement.Flags;
            }
        }


        /// <value>
        /// Gets the collection of ManufacturerData if exists, otherwise returns an empty collection

        /// </value>
        public ManufacturerDataCollection ManufacturerData
        {
            get
            {
                return advertisementContent.Advertisement != null ? new ManufacturerDataCollection(advertisementContent.Advertisement.ManufacturerData) :
                    new ManufacturerDataCollection(new List<BluetoothLEManufacturerData>());
            }
        }


        /// <value>
        /// Gets the collection of ManufacturerData from the scan response if exists, otherwise returns an empty collection
        /// </value>
        public ManufacturerDataCollection ManufacturerDataFromScanResponse
        {
            get
            {
                return scanResponseAdvertisementContent.Advertisement != null ? new ManufacturerDataCollection(scanResponseAdvertisementContent.Advertisement.ManufacturerData) :
                    new ManufacturerDataCollection(new List<BluetoothLEManufacturerData>());
            }
        }


        /// <value>
        /// Gets the collection of DataSections if they exists, otherwise returns an empty collection

        /// </value>
        public DataSectionCollection DataSections
        {
            get
            {
                return advertisementContent.Advertisement != null ? new DataSectionCollection(advertisementContent.Advertisement.DataSections) :
                    new DataSectionCollection(new List<BluetoothLEAdvertisementDataSection>());
            }
        }


        /// <value>
        /// Gets the collection of DataSections from the scan response if they exists, otherwise returns an empty collection
        /// </value>
        public DataSectionCollection DataSectionsFromScanResponse
        {
            get
            {
                return scanResponseAdvertisementContent.Advertisement != null ? new DataSectionCollection(scanResponseAdvertisementContent.Advertisement.DataSections) :
                    new DataSectionCollection(new List<BluetoothLEAdvertisementDataSection>());
            }
        }

        #endregion


        #region Events

        /// <summary>
        /// Fired when a scan response is received
        /// </summary>
        public event Action<CosmedBleAdvertisedDevice> ScanResponseReceived;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructor of the class
        /// </summary>
        public CosmedBleAdvertisedDevice()
        {
            this.advertisementContent = new AdvertisementContent();
            this.scanResponseAdvertisementContent = new AdvertisementContent();
            this.HasScanResponse = false;
        }

        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="address">The device address</param>
        /// <param name="timestamp">Timestamp of the advertisement</param>
        /// <param name="isConnectable">Boolean indicating if the device is connectable</param>
        /// <param name="adv">The received advertisement</param>
        /// <param name="advType">Type of the received advertisement</param>
        public CosmedBleAdvertisedDevice(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv, BluetoothLEAdvertisementType advType) : this()
        {
            if (adv == null)
                throw new ArgumentNullException();
            DeviceName = adv.LocalName;
            DeviceAddress = address;
            this.Timestamp = timestamp;
            this.IsConnectable = isConnectable;
        }

        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="args"></param>
        public CosmedBleAdvertisedDevice(BluetoothLEAdvertisementReceivedEventArgs args) : this()
        {
            _ = SetAdvertisement(args);
        }


        #endregion


        #region methods


        /// <summary>
        /// Sets an advertisement received from the device
        /// </summary>
        /// <param name="args">The arguments containing all the data about a received advertisement</param>
        /// <returns>An instance of the class</returns>
        public CosmedBleAdvertisedDevice SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException();

            DeviceAddress = args.BluetoothAddress;
            Timestamp = args.Timestamp;
            RawSignalStrengthInDBm = args.RawSignalStrengthInDBm;
            BluetoothAddressType = args.BluetoothAddressType;
            IsAnonymous = args.IsAnonymous;
            IsDirected = args.IsDirected;
            IsScannable = args.IsScannable;
            TransmitPowerLevelInDBm = args.TransmitPowerLevelInDBm;

            //if the advertisement is a scan response saves it as such
            if (args.AdvertisementType == BluetoothLEAdvertisementType.ScanResponse)
            {
                if (DeviceName == null || !args.Advertisement.LocalName.Equals(""))
                {
                    DeviceName = args.Advertisement.LocalName;
                }

                scanResponseAdvertisementContent.Advertisement = args.Advertisement;
                scanResponseAdvertisementContent.AdvertisementType = args.AdvertisementType;
                HasScanResponse = true;

                //rise the event
                ScanResponseReceived?.Invoke(this);
            }
            //if the advertisement is normal advertisement saves it as such
            else
            {
                IsConnectable = args.IsConnectable;
                DeviceName = args.Advertisement.LocalName;
                advertisementContent.Advertisement = args.Advertisement;
                advertisementContent.AdvertisementType = args.AdvertisementType;
            }
            return this;
        }


        /// <summary>
        /// Prints the advertisement and scan response data
        /// </summary>
        public void PrintAdvertisement()
        {
            Console.WriteLine();
            Console.WriteLine("############################## new advertisement #############################");
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
            Console.WriteLine(" flags: " + Flags.ToString());

            foreach (Guid g in ServiceUuids)
            {
                Console.WriteLine(" guid: " + g.ToString() + " " + GattServiceUuidHelper.ConvertUuidToName(g));
            }

            Console.WriteLine();
            string advType = "";

            foreach (ManufacturerDataReader m in ManufacturerData)
            {
                Console.WriteLine(advType + " company id: " + m.CompanyId);
                Console.WriteLine(advType + " company id HEX: " + m.CompanyIdHex);
                Console.WriteLine(advType + " manufacturer data capacity: " + m.RawData.Capacity);
                Console.WriteLine(advType + " manufacturer data length: " + m.RawData.Length);

                Console.WriteLine(advType + " manufacturer buffer: " + m.HexValue);
                Console.WriteLine(advType + " manufacturer buffer UTF8: " + m.UTF8Value);
            }

            Console.WriteLine();

            foreach (DataSectionReader m in DataSections)
            {
                Console.WriteLine(advType + " data type: " + m.DataType);
                Console.WriteLine(advType + " data type --> " + AdvertisementDataTypeHelper.ConvertAdvertisementDataTypeToString(m.RawDataType));
                Console.WriteLine(advType + " buffer: " + m.HexValue);
                Console.WriteLine(advType + " buffer UTF8: " + m.UTF8Value);
                //Console.WriteLine(advType + " buffer UTF8 --> " + ClientGattBufferReaderWriter.ToUTF8String(m.RawData));
            }

            Console.WriteLine();
            Console.WriteLine("++++++++++++++++++++++++ scan response +++++++++++++++++++++++");
            Console.WriteLine();

            int count = ManufacturerDataFromScanResponse.AdvertisedManufacturerData.Count;
            advType = "sr:";

            foreach (ManufacturerDataReader m in ManufacturerDataFromScanResponse)
            {
                Console.WriteLine(advType + " company id: " + m.CompanyId);
                Console.WriteLine(advType + " company id HEX: " + m.CompanyIdHex);
                Console.WriteLine(advType + " manufacturer data capacity: " + m.RawData.Capacity);
                Console.WriteLine(advType + " manufacturer data length: " + m.RawData.Length);

                Console.WriteLine(advType + " manufacturer buffer: " + m.HexValue);
                Console.WriteLine(advType + " manufacturer buffer UTF8: " + m.UTF8Value);
                //Console.WriteLine(advType + " manufacturer buffer UTF8 -->" + ClientGattBufferReaderWriter.ToUTF8String(m.RawData));
            }


            count += DataSectionsFromScanResponse.AdvertisedDataSection.Count;
            if (count == 0)
                Console.WriteLine("Scan response not available");
            else
                Console.WriteLine();

            foreach (DataSectionReader m in DataSectionsFromScanResponse)
            {
                Console.WriteLine(advType + " data type: " + m.DataType);
                Console.WriteLine(advType + " data type --> " + AdvertisementDataTypeHelper.ConvertAdvertisementDataTypeToString(m.RawDataType));

                Console.WriteLine(advType + " buffer: " + m.HexValue);
                Console.WriteLine(advType + " buffer UTF8: " + m.UTF8Value);
                //Console.WriteLine(advType + " buffer UTF8 --> " + ClientGattBufferReaderWriter.ToUTF8String(m.RawData));
            }

            Console.WriteLine("--------------------- end advertisement -----------------------");
        }

        #endregion


    }


    /// <summary>
    /// Contains an istance of BluetoothLEAdvertisement and BluetoothLEAdvertisementType
    /// </summary>
    public struct AdvertisementContent
    {
        /// <value>
        /// Gets and sets the BluetoothLEAdvertisement 
        /// </value>
        public BluetoothLEAdvertisement Advertisement { get; set; }

        /// <value>
        /// Gets and sets the BluetoothLEAdvertisementType
        /// </value>
        public BluetoothLEAdvertisementType AdvertisementType { get; set; }
    }


}

