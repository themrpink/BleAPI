using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Foundation;
using CosmedBleLib.CustomExceptions;

namespace CosmedBleLib.Adapter
{
    /// <summary>
    /// Represents the bluetooth adapter
    /// </summary>
    public sealed class CosmedBluetoothLEAdapter
    {
        private BluetoothAdapter adapter;
        private bool isLowEnergySupported;
        private ulong decimalAddress;
        private string hexAddress;
        private bool areLowEnergySecureConnectionsSupported;
        private uint maxAdvertisementDataLength;
        private bool isExtendedAdvertisingSupported;
        private bool isCentralRoleSupported;


        /// <summary>
        /// The address of the adapter expressed as decimal value
        /// </summary>
        public ulong DecimalAddress { get {return decimalAddress; } }


        /// <summary>
        /// The address of the adapter expressed as hexidecimal value
        /// </summary>
        public string HexAddress { get { return hexAddress; }  }


        /// <summary>
        /// Gets or sets a value indicating whether Secure Connections are supported for paired Bluetooth LE devices
        /// </summary>
        public bool AreLowEnergySecureConnectionsSupported { get { return areLowEnergySecureConnectionsSupported; } }


        /// <summary>
        /// Indicates the maximum length of an advertisement that can be published by this adapter.
        /// </summary>
        public uint MaxAdvertisementDataLength { get { return maxAdvertisementDataLength; }  }


        /// <summary>
        /// Indicates whether the adapter supports the 5.0 Extended Advertising format.
        /// </summary>
        public bool IsExtendedAdvertisingSupported { get { return isExtendedAdvertisingSupported; } }


        /// <summary>
        /// Gets a boolean indicating if the adapater supports LowEnergy central role.
        /// </summary>
        public bool IsCentralRoleSupported { get { return isCentralRoleSupported; }  }


        /// <summary>
        /// Gets a boolean indicating if the adapater supports LowEnergy Bluetooth Transport type.
        /// </summary>
        public bool IsLowEnergySupported { get { return isLowEnergySupported; } }


        /// <summary>
        /// Gets a boolean incating the the adapter is turned on 
        /// </summary>
        public bool IsAdapterOn => IsBluetoothLEOn;



        /// <summary>
        /// Checks if the bluetooth apapter is turned on
        /// </summary>
        public static bool IsBluetoothLEOn
        {
            get
            {
                SelectQuery sq = new SelectQuery("SELECT DeviceId FROM Win32_PnPEntity WHERE service='BthLEEnum'");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(sq);
                return searcher.Get().Count > 0;
            }
        }


        private CosmedBluetoothLEAdapter()
        {

        }


        private async Task InitializeAsync()
        {
            try
            {
                adapter = await BluetoothAdapter.GetDefaultAsync().AsTask();
            }
            catch(Exception e)
            {
                throw new BluetoothAdapterCommunicationFailureException("communication failure with the adapter", e);
            }

            isLowEnergySupported = adapter.IsLowEnergySupported;
            decimalAddress = adapter.BluetoothAddress;
            hexAddress = string.Format("{0:X}", decimalAddress);
            areLowEnergySecureConnectionsSupported = adapter.AreLowEnergySecureConnectionsSupported;
            maxAdvertisementDataLength = adapter.MaxAdvertisementDataLength;
            isExtendedAdvertisingSupported = adapter.IsExtendedAdvertisingSupported;
            isCentralRoleSupported = adapter.IsCentralRoleSupported;
        }


        /// <summary>
        /// Instatiates the Adapter
        /// </summary>
        /// <returns>an instance of the Bluetooth Adapter</returns>
        public static async Task<CosmedBluetoothLEAdapter> CreateAsync()
        {
            var adapter = new CosmedBluetoothLEAdapter();
            await adapter.InitializeAsync();
            return adapter;
        }


  
        /// <summary>
        /// Gets an instance of the required remote Ble device
        /// </summary>
        /// <param name="deviceAddress">The address of the remote device</param>
        /// <returns>An instance of the remote Ble device</returns>
        public static async Task<BluetoothLEDevice> GetRemoteDeviceAsync(ulong deviceAddress)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress).AsTask().ConfigureAwait(false);
            return device;
        }


        /// <summary>
        /// Gets an instance of the required remote Ble device
        /// </summary>
        /// <param name="deviceId">The ID of the remote device</param>
        /// <returns>An instance of the remote Ble device</returns>
        public static async Task<BluetoothLEDevice> GetRemoteDeviceAsync(string deviceId)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask().ConfigureAwait(false);
            return device;
        }


    }
}
