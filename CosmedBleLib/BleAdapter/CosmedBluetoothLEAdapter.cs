using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Foundation;

namespace CosmedBleLib
{

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

        public ulong DecimalAddress { get {return decimalAddress; } }
        public string HexAddress { get { return hexAddress; }  }
        public bool AreLowEnergySecureConnectionsSupported { get { return areLowEnergySecureConnectionsSupported; } }
        public uint MaxAdvertisementDataLength { get { return maxAdvertisementDataLength; }  }
        public bool IsExtendedAdvertisingSupported { get { return isExtendedAdvertisingSupported; } }
        public bool IsCentralRoleSupported { get { return isCentralRoleSupported; }  }
        public bool IsLowEnergySupported { get { return isLowEnergySupported; } }
        public bool IsAdapterOn => IsBluetoothLEOn();
        

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


        public static async Task<CosmedBluetoothLEAdapter> CreateAsync()
        {
            var adapter = new CosmedBluetoothLEAdapter();
            await adapter.InitializeAsync();
            return adapter;
        }


        //controlla la compatibilità di questa soluzione
        public static bool IsBluetoothLEOn()
        {
            SelectQuery sq = new SelectQuery("SELECT DeviceId FROM Win32_PnPEntity WHERE service='BthLEEnum'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(sq);
            return searcher.Get().Count > 0;
        }


        public static async Task<BluetoothLEDevice> GetRemoteDeviceAsync(ulong deviceAddress)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress).AsTask().ConfigureAwait(false);
            return device;
        }


        public static async Task<BluetoothLEDevice> GetRemoteDeviceAsync(string deviceId)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask().ConfigureAwait(false);
            return device;
        }


    }
}
