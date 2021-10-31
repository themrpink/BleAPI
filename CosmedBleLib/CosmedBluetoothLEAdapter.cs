using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Foundation;

namespace CosmedBleLib
{

    public static class CosmedBluetoothLEAdapter
    {
        private static BluetoothAdapter adapter;
        public static bool IsLowEnergySupported { get; private set; }
        public static ulong DecimalAddress { get; private set; }
        public static string HexAddress { get; private set; }
        public static bool AreLowEnergySecureConnectionsSupported { get; private set; }
        public static uint MaxAdvertisementDataLength { get; private set; }
        public static bool IsExtendedAdvertisingSupported { get; private set; }
        public static bool IsCentralRoleSupported { get; private set; }


        static CosmedBluetoothLEAdapter()
        {
            _ = SetAdapterAsync();
        }

        private static async Task SetAdapterAsync()
        {
            adapter = await BluetoothAdapter.GetDefaultAsync();

            IsLowEnergySupported = adapter.IsLowEnergySupported;
            DecimalAddress = adapter.BluetoothAddress;
            HexAddress = string.Format("{0:X}", DecimalAddress);
            AreLowEnergySecureConnectionsSupported = adapter.AreLowEnergySecureConnectionsSupported;
            MaxAdvertisementDataLength = adapter.MaxAdvertisementDataLength;
            IsExtendedAdvertisingSupported = adapter.IsExtendedAdvertisingSupported;
            IsCentralRoleSupported = adapter.IsCentralRoleSupported;
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
