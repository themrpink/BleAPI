using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace CosmedBleLib
{
    //public class RadioIsNotONException : Exception
    //{
    //    public RadioIsNotONException(string message) : base(message)
    //    {

    //    }
    //}
    public class ScanAbortedException : Exception
    {
        public ScanAbortedException(string message) : base(message)
        {

        }
    }

    public class BluetoothLeNotSupportedException : Exception
    {
        public BluetoothLeNotSupportedException(string message) : base(message)
        {

        }
    }

    public class CentralRoleNotSupportedException : Exception
    {
        public CentralRoleNotSupportedException(string message) : base(message)
        {

        }
    }


    public class BluetoothConnectionInterruptedEventArgs
    {
        public BluetoothError error { get; set; }
        public Exception exception { get; set; }

    }



    public static class BluetoothConnectionInterruptedEventArgsBuilder
    {
        public async static Task<BluetoothConnectionInterruptedEventArgs> checkBLEAdapterError()
        {
            string str = CosmedBluetoothLEAdapter.HexAddress;
            BluetoothConnectionInterruptedEventArgs eventArgs;

            //adesso manda tutte le eccezioni trovate. mettere un else if per mandarne solo una. Sono ordinate in ordine logico:
            // 1) verifica che il bluetooth sia acceso 2) verifica che sia compatibile BLE 3)verifica che possa avere ruolo di central
            //if (!await CosmedBluetoothLEAdapter.IsRadioStateOn())
            //{
            //    var ex = new RadioIsNotONException("Your adapter " + str + " radio signal is not ON");
            //    eventArgs = new BluetoothConnectionInterruptedEventArgs
            //    {
            //        error = BluetoothError.RadioNotAvailable,
            //        exception = ex
            //    };
            //    return eventArgs;
            //}
            if (!CosmedBluetoothLEAdapter.IsLowEnergySupported)
            {
                var ex = new BluetoothLeNotSupportedException("Your adapter " + str + " does not support Bluetooth Low Energy");
                var arg = new BluetoothConnectionInterruptedEventArgs
                {
                    error = BluetoothError.NotSupported,
                    exception = ex
                };
                return arg;
            }
            if (!CosmedBluetoothLEAdapter.IsCentralRoleSupported)
            {
                var ex = new CentralRoleNotSupportedException("Your adapter does not support Central Role");
                eventArgs = new BluetoothConnectionInterruptedEventArgs
                {
                    error = BluetoothError.DisabledByPolicy,
                    exception = ex
                };
                return eventArgs;
            }
            eventArgs = new BluetoothConnectionInterruptedEventArgs
            {
                error = BluetoothError.OtherError,
                exception = new InvalidOperationException()
            };
            return eventArgs;

        }

    }

}
