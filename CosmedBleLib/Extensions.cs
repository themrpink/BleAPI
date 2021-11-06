using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace CosmedBleLib
{
    public enum CosmedGattCommunicationStatus : ushort
    {
        Success = 0,
        Unreachable = 1,
        ProtocolError = 2,
        AccessDenied = 3,
        WriteNotPermitted = 4,
        ReadNotPermitted = 5,
        NotifyNotPermitted = 6,
        IndicateNotPermitted = 7,
        Unknown = 8
    }


    public static class GattCharacteristicExtensions

    {
        public static async Task<CosmedGattCommunicationStatus> Write(this GattCharacteristic characteristic, byte value, ushort? maxPduSize = null)
        {

            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                if(maxPduSize != null)
                {
                    //check byte size
                }
                //check MaxPduSize from GattSession before use
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                writer.WriteByte(value);
                try
                {
                    var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask().ConfigureAwait(false);
                    return ConvertStatus(statusResultValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
           return CosmedGattCommunicationStatus.WriteNotPermitted;
        }


        public static CosmedGattCommunicationStatus ConvertStatus(GattCommunicationStatus status)
        {
            switch (status)
            {
                case GattCommunicationStatus.Success: return CosmedGattCommunicationStatus.Success;
                case GattCommunicationStatus.AccessDenied: return CosmedGattCommunicationStatus.AccessDenied;
                case GattCommunicationStatus.ProtocolError: return CosmedGattCommunicationStatus.ProtocolError;
                case GattCommunicationStatus.Unreachable: return CosmedGattCommunicationStatus.Unreachable;
                default:  return CosmedGattCommunicationStatus.Unknown;
            }
        }


        public static async Task<GattReadResultReader> Read(this GattCharacteristic characteristic)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            GattReadResultReader grr;

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                try 
                { 
                    GattReadResult value = await characteristic.ReadValueAsync().AsTask().ConfigureAwait(false);

                    if (value.Status == GattCommunicationStatus.Success)
                    {
                        CosmedGattCommunicationStatus newStatus = ConvertStatus(value.Status);
                        grr = new GattReadResultReader(value.Value, newStatus, value.ProtocolError);
                        return grr;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return new GattReadResultReader(null, CosmedGattCommunicationStatus.ReadNotPermitted, null);       
        }


        public static async Task<CosmedGattCommunicationStatus> SubscribeToNotifications(this GattCharacteristic characteristic, Action<GattCharacteristic, GattValueChangedEventArgs> response)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            
            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                try
                {
                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
                    if (status == GattCommunicationStatus.Success)
                    {
                        characteristic.ValueChanged += (sender, args) => response(sender, args);
                        return ConvertStatus(status);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return CosmedGattCommunicationStatus.NotifyNotPermitted;
        }


        public static async Task<CosmedGattCommunicationStatus> SubscribeToIndications(this GattCharacteristic characteristic, Action<GattCharacteristic, GattValueChangedEventArgs> response)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

            if (properties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                try
                {
                    var conf = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();
                    if(conf.Status == GattCommunicationStatus.Success)
                    {
                        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(conf.ClientCharacteristicConfigurationDescriptor).AsTask().ConfigureAwait(false);
                        if (status == GattCommunicationStatus.Success)
                        {
                            characteristic.ValueChanged += (sender, args) => response(sender, args);
                            return ConvertStatus(status);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return CosmedGattCommunicationStatus.IndicateNotPermitted;
        }
    
    }


    public static class AsycOperationExtensions
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

        public static void AddDevice(this CosmedBluetoothLEAdvertisementWatcher watcher, CosmedBleAdvertisedDevice device)
        {

        }
    }


}
