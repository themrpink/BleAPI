using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        OperationNotSupported = 4,
        OperationAlreadyRegistered = 5,
        OperationNotRegistered = 6
    }


    //offers an event to be raised when an exception is raised during a gatt operation
    public static class ErrorFoundClass
    {
        public static event Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> ErrorFound;
        public static void Call(GattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        {
            ErrorFound?.Invoke(sender, args);
        }
    }


    public static class GattServiceExtensions
    {
        public static void Print(this GattDeviceService service)
        {
            Console.WriteLine("printing a service:");
            Console.WriteLine("service handle: " + service.AttributeHandle.ToString("X2"));
            Console.WriteLine("service uuid: " + service.Uuid.ToString());
            Console.WriteLine("service device access information (current status): " + service.DeviceAccessInformation.CurrentStatus.ToString());
            Console.WriteLine("service Gatt Session: " + service.Session);
        }
    }


    public static class GattCharacteristicExtensions

    {
        #region gatt operations
        public static async Task<CosmedGattCommunicationStatus> Write(this GattCharacteristic characteristic, byte value, ushort? maxPduSize = null)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            if (characteristic.IsWriteAllowed()) 
            {
                if(maxPduSize != null)
                {
                    //check byte size
                }
                //check MaxPduSize from GattSession before use
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle

                try
                {
                    writer.WriteByte(value);

                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                    {
                        var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse).AsTask().ConfigureAwait(false);
                        return statusResultValue.ConvertStatus();
                    }

                    // write without response cannot write values larger than MTU as per spec. Any longer writes can only be handled with response.
                    else if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                    {
                        var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse).AsTask().ConfigureAwait(false);
                        return statusResultValue.ConvertStatus();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
           return CosmedGattCommunicationStatus.OperationNotSupported;
        }


        public static async Task<CosmedCharacteristicReadResult> Read(this GattCharacteristic characteristic)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                try 
                { 
                    GattReadResult value = await characteristic.ReadValueAsync().AsTask().ConfigureAwait(false);

                    if (value.Status == GattCommunicationStatus.Success)
                    {
                        return new CosmedCharacteristicReadResult(value.Value, value.Status.ConvertStatus(), value.ProtocolError);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return new CosmedCharacteristicReadResult(null, CosmedGattCommunicationStatus.OperationNotSupported, null);       
        }
        
        
        public static async Task<CosmedGattCommunicationStatus> Subscribe(this GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction = null, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsNotificationAllowed() && !characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported;
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd;

            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    return cccd.Status.ConvertStatus();
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.None)
                {
                    return CosmedGattCommunicationStatus.OperationNotRegistered;
                }

                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
                
                if (status == GattCommunicationStatus.Success)
                {
                    if (valueChangedAction != null)
                    {
                        characteristic.ValueChanged += valueChangedAction;
                        if (GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions.ContainsKey(characteristic))
                        {
                            TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> oldEventHandler;
                            var success = GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions.TryGetValue(characteristic, out oldEventHandler);
                            if (success)
                            {
                                characteristic.ValueChanged -= oldEventHandler;
                            }
                        }
                        GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions[characteristic] = valueChangedAction;
                    }                   
                }

                //gives the last status
                return status.ConvertStatus();

            }
            catch (Exception e)
            {
                if (errorAction != null)
                {
                    ErrorFoundClass.ErrorFound += errorAction;
                }
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Notify);
                ErrorFoundClass.Call(characteristic, args);
                //ErrorFoundClass.ErrorFound -= errorAction;
                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);

                return CosmedGattCommunicationStatus.Unreachable;
            }


        }


        public static async Task<CosmedGattCommunicationStatus> SubscribeToNotification(this GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction = null, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsNotificationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported;
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd;
            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                //if not notifying yet, then subscribe
                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    return cccd.Status.ConvertStatus();
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Notify)
                {
                    return CosmedGattCommunicationStatus.OperationAlreadyRegistered;
                }

                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
                if (status == GattCommunicationStatus.Success)
                {
                    if (valueChangedAction != null)
                    {
                        characteristic.ValueChanged += valueChangedAction;
                    }
                }
                //gives the last status
                return status.ConvertStatus();

            }
            catch (Exception e)
            {
                if (errorAction != null)
                {
                    ErrorFoundClass.ErrorFound += errorAction;
                }
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Notify);

                //it fires the event 
                ErrorFoundClass.Call(characteristic, args);
          
                ErrorFoundClass.ErrorFound -= errorAction;
                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);
            }


            return CosmedGattCommunicationStatus.OperationNotSupported;
        }


        public static async Task<CosmedGattCommunicationStatus> SubscribeToIndication(this GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction = null, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported; 
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd;
            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                //if not notifying yet, then subscribe
                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    /*
                     * TODO:
                     * utilizzare questo tipo di oggetto per il return.
                     * l´interfaccia serve per poterli restituire tutti dello stesso tipo, ma la property permette di
                     * fare un cast appropriato se necessario
                     */
                    CosmedCharacteristicSubscriptionResult test = new CosmedCharacteristicSubscriptionResult(   characteristic.CharacteristicProperties, 
                                                                                                                cccd.Status.ConvertStatus(), 
                                                                                                                cccd.ClientCharacteristicConfigurationDescriptor, 
                                                                                                                cccd.ProtocolError
                                                                                                                );
                    Console.WriteLine("impossibile leggere cccd");
                    return cccd.Status.ConvertStatus();
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Indicate)
                {
                    return CosmedGattCommunicationStatus.OperationAlreadyRegistered;
                }


                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask().ConfigureAwait(false);
                if (status == GattCommunicationStatus.Success)
                {
                    if (valueChangedAction != null)
                    {
                        characteristic.ValueChanged += valueChangedAction;
                    }
                }
                //gives the last status
                return status.ConvertStatus();

            }
            catch (Exception e)
            {
                if (errorAction != null)
                {
                    ErrorFoundClass.ErrorFound += errorAction;
                }
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate);
                ErrorFoundClass.Call(characteristic, args);
                ErrorFoundClass.ErrorFound -= errorAction;
                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString() + " name: " + characteristic.Service.Device.Name);
                Console.WriteLine(e.Message);
            }


            return CosmedGattCommunicationStatus.OperationNotSupported;
        }


        public static async Task<CosmedGattCommunicationStatus> UnSubscribe(this GattCharacteristic characteristic, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {

            if (!characteristic.IsNotificationAllowed() && !characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported;
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd = null;

            try
            {
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    return cccd.Status.ConvertStatus();
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.None)
                {
                    return CosmedGattCommunicationStatus.OperationNotRegistered;
                }


                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());

                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).AsTask().ConfigureAwait(false);
                
                //if (status == GattCommunicationStatus.Success)
                //{
                //    //adrebbe tolta la giusta action
                //    characteristic.ValueChanged -= (sender, args) => response(sender, args);

                //}
                //gives the last status
                return status.ConvertStatus();

                }
            catch (Exception e)
            {
                if (errorAction != null)
                {
                    ErrorFoundClass.ErrorFound += errorAction;
                }

                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Notify);
                ErrorFoundClass.Call(characteristic, args);
                ErrorFoundClass.ErrorFound -= errorAction;
                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);
           
                return CosmedGattCommunicationStatus.Unreachable;
            }
            
        }
        #endregion

        #region helper methods
        public static void Print(this GattCharacteristic c)
        {
            Console.WriteLine("Characteristic  Uuid: " + c.Uuid.ToString());
            Console.WriteLine("ProtectionLevel :" + c.ProtectionLevel.ToString());
            Console.WriteLine("Attribute Handle :" + c.AttributeHandle);
            Console.WriteLine("CharacteristicProperties :" + c.CharacteristicProperties.ToString());
            Console.WriteLine("user description: " + c.UserDescription);
            Console.WriteLine("_________Gatt presentation format:________");
            try
            {
                foreach (var pres in c.PresentationFormats)
                {

                    Console.WriteLine("Description: " + pres.Description);
                    Console.WriteLine("Exponent: " + pres.Exponent);
                    Console.WriteLine("FormatType: " + pres.FormatType.ToString("X2"));
                    Console.WriteLine("namepsace: " + pres.Namespace.ToString("X2"));
                    Console.WriteLine("unit: " + pres.Unit);
                    Console.WriteLine("BluetoothSigAssignedNumbers: " + GattPresentationFormat.BluetoothSigAssignedNumbers);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("______________________");
        }



        public static CosmedGattCommunicationStatus ConvertStatus(this GattCommunicationStatus status)
        {
            switch (status)
            {
                case GattCommunicationStatus.Success: return CosmedGattCommunicationStatus.Success;
                case GattCommunicationStatus.AccessDenied: return CosmedGattCommunicationStatus.AccessDenied;
                case GattCommunicationStatus.ProtocolError: return CosmedGattCommunicationStatus.ProtocolError;
                case GattCommunicationStatus.Unreachable: return CosmedGattCommunicationStatus.Unreachable;
                default: return CosmedGattCommunicationStatus.OperationNotSupported;
            }
        }

        public static bool IsNotificationAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
        }

        private static bool IsWriteAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse);
        }

        private static bool IsReadAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read);
        }

        public static bool IsIndicationAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);
        }
        #endregion

    }


    public static class GattDeviceServiceResultsExtesions
    {

        public static async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattCharacteristics(this GattDeviceServicesResult gattResult)
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(emptyDictionary);


            if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                var servicesDictionaryTemp = resultServices.ToDictionary(s => s, async (s) =>
                {
                    try
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        var temp = tempResult.Characteristics.ToList().AsEnumerable().ToList().AsReadOnly();
                        return temp;
                    }
                    catch (Exception e)
                    {
                        throw new GattCommunicationException("impossible to retrieve the characteristics from Gatt service", e);
                    }

                });
                var b = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(servicesDictionaryTemp);
                servicesDictionary = b;
            }

            return servicesDictionary;
        }

    }


    public static class AsyncOperationExtensions
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
