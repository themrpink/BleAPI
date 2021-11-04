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

    public class CosmedBluetoothLEAdapter
    {
        private static BluetoothAdapter adapter;
        private static bool isLowEnergySupported;
        private static ulong decimalAddress;
        private static string hexAddress;
        private static bool areLowEnergySecureConnectionsSupported;
        private static uint maxAdvertisementDataLength;
        private static bool isExtendedAdvertisingSupported;
        private static bool isCentralRoleSupported;
        private static CosmedBluetoothLEAdapter thisAdapter;

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
 
       
        public static async Task<CosmedBluetoothLEAdapter> GetAdapterAsync()
        {
            if(thisAdapter != null)
            {
                return thisAdapter;
            }
            else
            {
                thisAdapter = new CosmedBluetoothLEAdapter();
                adapter = await BluetoothAdapter.GetDefaultAsync().AsTask();
                isLowEnergySupported = adapter.IsLowEnergySupported;
                decimalAddress = adapter.BluetoothAddress;
                hexAddress = string.Format("{0:X}", decimalAddress);
                areLowEnergySecureConnectionsSupported = adapter.AreLowEnergySecureConnectionsSupported;
                maxAdvertisementDataLength = adapter.MaxAdvertisementDataLength;
                isExtendedAdvertisingSupported = adapter.IsExtendedAdvertisingSupported;
                isCentralRoleSupported = adapter.IsCentralRoleSupported;
                return thisAdapter;
            }
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



        //public static async Task<RadioState> GetRadioState()
        //{
        //    Task.WaitAll();
        //    if (adapter == null)
        //    {
        //        await SetAdapterAsync();
        //    }
        //    var temp = adapter.GetRadioAsync().AsTask();
        //    Task.WaitAll(temp);
        //    Radio radio = temp.Result;
        //    return radio.State;
        //}

        //public static async Task<bool> IsRadioStateOn()
        //{

        //    RadioState rs = await GetRadioState();
        //    Task.WaitAll();
        //    return rs == RadioState.On;
        //}

    }
}
