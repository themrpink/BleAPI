using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using CosmedBleLib.GattCommunication;
using CosmedBleLib.CustomExceptions;
using CosmedBleLib.Helpers;
using CosmedBleLib.Values;
using CosmedBleLib.DeviceDiscovery;
using CosmedBleLib.Collections;

namespace CosmedBleLib.Extensions
{

    /// <summary>
    /// Types of Gatt Communication status
    /// </summary>
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


    /// <summary>
    /// Event to be fired when an exception is raised during a gatt operation
    /// </summary>
    public static class ErrorFoundClass
    {
        /// <summary>
        /// The event to be fired in case of error
        /// </summary>
        public static event Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> ErrorFound;

        /// <summary>
        /// Fires the ErrorFound event
        /// </summary>
        /// <param name="sender">The characteristic on which the error occurred</param>
        /// <param name="args">The error data</param>
        public static void Call(GattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        {
            ErrorFound?.Invoke(sender, args);
        }
    }


    /// <summary>
    /// Extension methods for the GattService
    /// </summary>
    public static class GattServiceExtensions
    {
        /// <summary>
        /// Utility methods, prints the Service data
        /// </summary>
        /// <param name="service">the extended service</param>
        public static void Print(this GattDeviceService service)
        {
            Console.WriteLine("printing a service:");
            Console.WriteLine("service handle: " + service.AttributeHandle.ToString("X2"));
            Console.WriteLine("service uuid: " + service.Uuid.ToString() + " " + GattServiceUuidHelper.ConvertUuidToName(service.Uuid));
            Console.WriteLine("service device access information (current status): " + service.DeviceAccessInformation.CurrentStatus.ToString());
            Console.WriteLine("service Gatt CanMaintainConnection: " + service.Session.CanMaintainConnection);
            Console.WriteLine("service Gatt Device Id: " + service.Session.DeviceId.Id);
            Console.WriteLine("service Gatt Is classic device: " + service.Session.DeviceId.IsClassicDevice);
            Console.WriteLine("service Gatt IsLowEnergyDevice: " + service.Session.DeviceId.IsLowEnergyDevice);
            Console.WriteLine("service Gatt MaintainConnection: " + service.Session.MaintainConnection);
            Console.WriteLine("service Gatt MaxPduSize: " + service.Session.MaxPduSize);
            Console.WriteLine("service Gatt SessionStatus: " + service.Session.SessionStatus.ToString());
            Console.WriteLine("service Gatt SharingMode: " + service.SharingMode.ToString());
        }
    }


    /// <summary>
    /// Extension methods for the GattCharacteristic
    /// </summary>
    public static class GattCharacteristicExtensions

    {
        #region gatt operations

        private static event Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> ErrorFoundCustom;

        //public static async Task<GattDescriptorsResult> GetDescriptorValue(this GattCharacteristic characteristic)
        //{
        //    var result = await characteristic.GetDescriptorsAsync().AsTask();
        //    return result;

        //}

        //allows from each characteristic to add a value to a reliable write instance
        /// <summary>
        /// Allows each characteristic to add a value to a reliable write instance
        /// </summary>
        /// <param name="characteristic">The extended characteristic</param>
        /// <param name="reliableWriteTransaction">the reliable write instance to which append the value</param>
        /// <param name="value">Value to be written</param>
        public static void AddCharacteristicToReliableWrite(this GattCharacteristic characteristic, GattReliableWriteTransaction reliableWriteTransaction, IBuffer value)
        {
            if (characteristic.IsWriteAllowed())
            {
                reliableWriteTransaction.WriteValue(characteristic, value);
            }         
        }


        /// <summary>
        /// Writes a characteristic with result
        /// </summary>
        /// <param name="characteristic">the extended characteristic</param>
        /// <param name="value">the value to be written</param>
        /// <param name="writeOption">Write with or without response</param>
        /// <param name="errorAction">Optional action to manage communication errors</param>
        /// <returns>The write result</returns>
        public static async Task<CosmedCharacteristicWriteResult> WriteWithResult(this GattCharacteristic characteristic, byte[] value, GattWriteOption writeOption, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null) 
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

            if(writeOption == GattWriteOption.WriteWithoutResponse)
            {
                properties = GattCharacteristicProperties.WriteWithoutResponse;
            }
            else if(writeOption == GattWriteOption.WriteWithResponse)
            {
                properties = GattCharacteristicProperties.Write;
            }
            
            if (characteristic.IsWriteAllowed())
            {
                //check MaxPduSize from GattSession before use
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle

                try
                {
                    writer.WriteBytes(value);

                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                    {
                        var statusResultValue = await characteristic.WriteValueWithResultAsync(writer.DetachBuffer(), writeOption).AsTask().ConfigureAwait(false);
                        return new CosmedCharacteristicWriteResult(statusResultValue.ProtocolError, statusResultValue.Status.ConvertStatus());
                    }

                    // write without response cannot write values larger than MTU as per spec. Any longer writes can only be handled with response.
                    else if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                    {
                        var statusResultValue = await characteristic.WriteValueWithResultAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse).AsTask().ConfigureAwait(false);
                        return new CosmedCharacteristicWriteResult(statusResultValue.ProtocolError, statusResultValue.Status.ConvertStatus());
                    }

                    writer.Dispose();
                }
                catch (Exception e)
                {
                    var args = new CosmedGattErrorFoundEventArgs(e, properties);
                    if (errorAction != null)
                    {
                        GattCharacteristicExtensions.ErrorFoundCustom += errorAction;
                        GattCharacteristicExtensions.ErrorFoundCustom.Invoke(characteristic, args);
                        GattCharacteristicExtensions.ErrorFoundCustom -= errorAction;
                    }
                    else
                    {
                        ErrorFoundClass.Call(characteristic, args);
                    }

                    //Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    //Console.WriteLine(e.Message);
                    
                    return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.Unreachable);
                }
            }
            return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.OperationNotSupported);
        }

        /// <summary>
        /// Reads the characteristic values.
        /// </summary>
        /// <param name="characteristic">The extended characteristic</param>
        /// <param name="errorAction">Optional action to manage communication errors</param>
        /// <returns>The read result</returns>
        public static async Task<CosmedCharacteristicReadResult> Read(this GattCharacteristic characteristic, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
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
                    var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Read);
                    if (errorAction != null)
                    {
                        GattCharacteristicExtensions.ErrorFoundCustom += errorAction;
                        GattCharacteristicExtensions.ErrorFoundCustom.Invoke(characteristic, args);
                        GattCharacteristicExtensions.ErrorFoundCustom -= errorAction;
                    }
                    else
                    {
                        ErrorFoundClass.Call(characteristic, args);
                    }

                    //it fires the event 

                    //Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    //Console.WriteLine(e.Message);
                    return new CosmedCharacteristicReadResult(null, CosmedGattCommunicationStatus.Unreachable, null);
                }
            }
            return new CosmedCharacteristicReadResult(null, CosmedGattCommunicationStatus.OperationNotSupported, null);       
        }

        /// <summary>
        /// Subscribes to notifications.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <param name="valueChangedAction">Optional action to manage the incoming notifications</param>
        /// <param name="errorAction">Optional action to manage communication errors</param>
        /// <returns>The subscription result</returns>
        public static async Task<CosmedGattCommunicationStatus> SubscribeToNotification(this GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsNotificationAllowed() && !characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported;
            }

            //GattReadClientCharacteristicConfigurationDescriptorResult cccd;

            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                //cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();


                //if (cccd.Status != GattCommunicationStatus.Success)
                //{
                //    CosmedCharacteristicSubscriptionResult test = new CosmedCharacteristicSubscriptionResult(characteristic.CharacteristicProperties,
                //                                                                                                cccd.Status.ConvertStatus(),
                //                                                                                                cccd.ClientCharacteristicConfigurationDescriptor,
                //                                                                                                cccd.ProtocolError
                //                                                                                                );
                //    Console.WriteLine("impossibile leggere cccd");
                //    return CosmedGattCommunicationStatus.Unreachable;
                //}

                //if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.None)
                //{
                //    return CosmedGattCommunicationStatus.OperationNotRegistered;
                //}

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
                    else
                    {
                        throw new ArgumentNullException("valueChangedAction parameter cannot be null");
                    }
                }

                //gives the last status
                return status.ConvertStatus();

            }
            catch (Exception e)
            {
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Notify);
                if (errorAction != null)
                {
                    GattCharacteristicExtensions.ErrorFoundCustom += errorAction;
                    GattCharacteristicExtensions.ErrorFoundCustom.Invoke(characteristic, args);
                    GattCharacteristicExtensions.ErrorFoundCustom -= errorAction;
                }
                else
                {
                    ErrorFoundClass.Call(characteristic, args);
                }

                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);

                return CosmedGattCommunicationStatus.Unreachable;
            }
        }



        /// <summary>
        /// Subscribes to indications.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <param name="valueChangedAction">Optional action to manage the incoming notifications.</param>
        /// <param name="errorAction">Optional action to manage communication errors.</param>
        /// <returns>The subscription result.</returns>
        public static async Task<CosmedGattCommunicationStatus> SubscribeToIndication(this GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported; 
            }

            //GattReadClientCharacteristicConfigurationDescriptorResult cccd;
            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());

                //subscribes
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask().ConfigureAwait(false);
                if (status == GattCommunicationStatus.Success)
                {
                    if (valueChangedAction != null)
                    {
                        characteristic.ValueChanged += valueChangedAction;

                        //if another event handler is present for the characteristic, it unsubscribes it
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
                    else
                    {
                        throw new ArgumentNullException("valueChangedAction parameter cannot be null");
                    }
                }
                //gives the last status
                return status.ConvertStatus();

            }
            catch (Exception e)
            {
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate);
                if (errorAction != null)
                {
                    //uses the given error handler and then unsubscribes
                    GattCharacteristicExtensions.ErrorFoundCustom += errorAction;
                    GattCharacteristicExtensions.ErrorFoundCustom.Invoke(characteristic, args);
                    GattCharacteristicExtensions.ErrorFoundCustom -= errorAction;
                }
                else
                {
                    ErrorFoundClass.Call(characteristic, args);
                }

                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString() + " name: " + characteristic.Service.Device.Name);
                Console.WriteLine(e.Message);

                return CosmedGattCommunicationStatus.Unreachable;
            }
        }

        /// <summary>
        /// Unsubscribes from the characteristic.
        /// </summary>
        /// <param name="characteristic">the extended characteristic.</param>
        /// <param name="errorAction">Optional action to manage communication errors.</param>
        /// <returns>The unsubscription result.</returns>
        public static async Task<CosmedGattCommunicationStatus> UnSubscribe(this GattCharacteristic characteristic, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {

            if (!characteristic.IsNotificationAllowed() && !characteristic.IsIndicationAllowed())
            {
                return CosmedGattCommunicationStatus.OperationNotSupported;
            }

            try
            {
                Console.WriteLine(" unsubscribe from characteristec UUID: " + characteristic.Uuid.ToString());

                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).AsTask().ConfigureAwait(false);
                
                TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> oldEventHandler;
                var success = GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions.TryGetValue(characteristic, out oldEventHandler);
                if (success)
                {
                    characteristic.ValueChanged -= oldEventHandler;
                }

                return status.ConvertStatus();

                }
            catch (Exception e)
            {
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.None);
                if (errorAction != null)
                {
                    GattCharacteristicExtensions.ErrorFoundCustom += errorAction;
                    GattCharacteristicExtensions.ErrorFoundCustom.Invoke(characteristic, args);
                    GattCharacteristicExtensions.ErrorFoundCustom -= errorAction;
                }
                else
                {
                    ErrorFoundClass.Call(characteristic, args);
                }

                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);
           
                return CosmedGattCommunicationStatus.Unreachable;
            }
            
        }
        #endregion

        #region helper methods

        /// <summary>
        /// Utility method to print the characteristic values.
        /// </summary>
        /// <param name="c">The extended characteristic.</param>
        public static async void Print(this GattCharacteristic c)
        {

            if (c.IsAppearanceValue())
            {
                var appearance = await c.GetAppearanceValue();
                Console.WriteLine("appearance: " + appearance.ToString());
            }

            Console.WriteLine("Characteristic  Uuid: " + c.Uuid.ToString() + " " + GattCharacteristicUuidHelper.ConvertUuidToName(c.Uuid)); 
            Console.WriteLine("ProtectionLevel :" + c.ProtectionLevel.ToString());
            Console.WriteLine("Attribute Handle :" + c.AttributeHandle);
            Console.WriteLine("Attribute Handle hex:" + String.Format("{0:X}", c.AttributeHandle));
            Console.WriteLine("CharacteristicProperties :" + c.CharacteristicProperties.ToString());
            Console.WriteLine("user description:" + c.UserDescription);
            Console.WriteLine("_________Gatt presentation format:________");
            try
            {
                foreach (var pres in c.PresentationFormats)
                {
                    Console.WriteLine("Description: " + pres.Description);
                    Console.WriteLine("Exponent: " + pres.Exponent);
                    Console.WriteLine("FormatType: " +    pres.FormatType.ToString("X2") + " " + PresentationFormatTypeHelper.ConvertFormatTypeToString(pres.FormatType));
                    Console.WriteLine("namepsace: " + pres.Namespace.ToString("X2") + " " + NamespaceTypeHelper.ConvertNamespaceTypeToString(pres.Namespace));
                    Console.WriteLine("unit: " + pres.Unit + " " +  PresentationFormatUnitsHelper.ConvertUnitTypeToString(pres.Unit));
                    Console.WriteLine("BluetoothSigAssignedNumbers: " + GattPresentationFormat.BluetoothSigAssignedNumbers);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("PresentationFormat error: " + e.Message);
            }
            Console.WriteLine("______________________");
        }


        /// <summary>
        /// Searches for appearance value and translates it.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>The Appearance type.</returns>
        public static async Task<BluetoothAppearanceType> GetAppearanceValue(this GattCharacteristic characteristic)
        {
            var result = await characteristic.Read();

            var data = new byte[result.RawData.Length];
            using (var reader = DataReader.FromBuffer(result.RawData))
            {
                reader.ReadBytes(data);
            }
            if(data.Length > 0)
            {
                ushort dataType = BitConverter.ToUInt16(data, 0);

                BluetoothAppearanceType appearenceType;
                if (Enum.TryParse(dataType.ToString(), out appearenceType))
                {
                    return appearenceType;
                }
            }


            return BluetoothAppearanceType.Unknown;
        }

        /// <summary>
        /// Checks if the Characteristic is an Appearance attribute.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>Boolean result.</returns>
        public static bool IsAppearanceValue(this GattCharacteristic characteristic)
        {
            var shortId = BluetoothUuidHelper.TryGetShortId(characteristic.Uuid);
            
            if(!shortId.HasValue)
            {
                return false;
            }
            
            if(shortId.Value == 0x2A01)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Converts the communication status.
        /// </summary>
        /// <param name="status">the extended communication status.</param>
        /// <returns>the converted status.</returns>
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


        /// <summary>
        /// Checks if notification is allowed.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>Boolean result.</returns>
        public static bool IsNotificationAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
        }

        /// <summary>
        /// Checks if write is allowed.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>Boolean result.</returns>
        private static bool IsWriteAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse);
        }

        /// <summary>
        /// Checks if read is allowed.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>Boolean result.</returns>
        private static bool IsReadAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read);
        }

        /// <summary>
        /// Checks if indication is allowed.
        /// </summary>
        /// <param name="characteristic">The extended characteristic.</param>
        /// <returns>Boolean result.</returns>
        public static bool IsIndicationAllowed(this GattCharacteristic characteristic)
        {
            return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);
        }
        #endregion

    }


    /// <summary>
    /// GattDeviceServiceResults extesion methods.
    /// </summary>
    public static class GattDeviceServiceResultsExtesions
    {
        /// <summary>
        /// Discover all services and characteristic of the remote device.
        /// </summary>
        /// <param name="gattResult">Contains the requested Services and Characteristic if communication succeeds.</param>
        /// <returns>The discovery result.</returns>
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


    /// <summary>
    /// Extension class for byte
    /// </summary>
    public static class HelperExtensions
    {
        /// <summary>
        /// Converts byte array to string
        /// </summary>
        /// <param name="array">Byte array to covert</param>
        /// <returns>string equivalent of the byte array</returns>
        public static string BytesToString(this byte[] array)
        {
            var result = new StringBuilder();

            for (int i = 0; i < array.Length; i++)
            {
                result.Append($"{array[i]:X2}");
                if (i < array.Length - 1)
                {
                    result.Append(" ");
                }
            }

            return result.ToString();
        }
    }



    /// <summary>
    /// Extension class for Async Operations
    /// </summary>
    public static class AsyncOperationExtensions
    {

        /// <summary>
        /// Extension method for Task
        /// </summary>
        /// <typeparam name="TResult">Async operation result</typeparam>
        /// <param name="asyncOperation">the extended operation</param>
        /// <returns>the task result</returns>
        public static Task<TResult> ToTask<TResult>(this IAsyncOperation<TResult> asyncOperation)
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
