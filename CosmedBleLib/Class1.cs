using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace CosmedBleLib
{
    public static class AsynOperationExtensions
    {

        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> asyncOperation)
        {
            var tsc = new TaskCompletionSource<TResult>();

            asyncOperation.Completed += delegate
            {
                switch (asyncOperation.Status)
                {
                    case AsyncStatus.Completed:
                        tsc.TrySetResult(asyncOperation.GetResults());
                        break;
                    case AsyncStatus.Error:
                        tsc.TrySetException(asyncOperation.ErrorCode);
                        break;
                    case AsyncStatus.Canceled:
                        tsc.SetCanceled();
                        break;

                }
            };
            return tsc.Task;
        }
    }
    public class CosmedAdvertisementEventArgs : EventArgs
    {
        public BluetoothLEAdvertisementReceivedEventArgs args { get; private set; }
        public BluetoothLEAdvertisementWatcher sender { get; private set; }

        public CosmedAdvertisementEventArgs(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.args = args;
            this.sender = sender;
        }
    }
}
