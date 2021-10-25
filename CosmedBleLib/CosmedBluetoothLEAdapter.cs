using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Foundation;

namespace CosmedBleLib
{

    public static class CosmedBluetoothLEAdapter
    {
        private static BluetoothAdapter adapter;     
        public static bool IsLowEnergySupported { get; private set; }
        public static ulong DecimalAddress { get; private set; }
        public static string HexAddress { get; private set; }
        public static bool IsExtendedAdvertisingSupported { get; private set; }
        public static bool IsCentralRoleSupported { get; private set; }
        static CosmedBluetoothLEAdapter()
        {
            SetAdapter();
        }
        public static async void SetAdapter()
        {
            BluetoothAdapter adapter =  await BluetoothAdapter.GetDefaultAsync();
            IsLowEnergySupported = adapter.IsLowEnergySupported;
            DecimalAddress = adapter.BluetoothAddress;
            IsExtendedAdvertisingSupported = adapter.IsExtendedAdvertisingSupported;
            IsCentralRoleSupported = adapter.IsCentralRoleSupported;
            HexAddress = string.Format("{0:X}", DecimalAddress);
        }

        public static BluetoothAdapter GetAdapter()
        {
            if (adapter == null)
                SetAdapter();
            return adapter;
        }
    }


}
