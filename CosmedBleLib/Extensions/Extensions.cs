using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
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


    public static class GattCharacteristicExtensions

    {
        #region gatt operations


        //public static async Task<GattDescriptorsResult> GetDescriptorValue(this GattCharacteristic characteristic)
        //{
        //    var result = await characteristic.GetDescriptorsAsync().AsTask();
        //    return result;

        //}

        //allows from each characteristic to add a value to a reliable write instance
        public static void AddCharacteristicToReliableWrite(this GattCharacteristic characteristic, GattReliableWriteTransaction reliableWriteTransaction, IBuffer value)
        {
            reliableWriteTransaction.WriteValue(characteristic, value);
        }



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
            //what about reliable writes ???

            
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
                    if (errorAction != null)
                    {
                        ErrorFoundClass.ErrorFound += errorAction;
                    }
                    var args = new CosmedGattErrorFoundEventArgs(e, properties);

                    //it fires the event 
                    ErrorFoundClass.Call(characteristic, args);

                    ErrorFoundClass.ErrorFound -= errorAction;
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                    
                    return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.Unreachable);
                }
            }
            return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.OperationNotSupported);
        }

        //public static async Task<CosmedGattCommunicationStatus> Write(this GattCharacteristic characteristic, byte[] value)
        //{
        //    GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
        //    if (characteristic.IsWriteAllowed()) 
        //    {
        //        //check MaxPduSize from GattSession before use
        //        var writer = new DataWriter();
        //        // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle

        //        try
        //        {
        //            writer.WriteBytes(value);

        //            if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
        //            {
        //                var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse).AsTask().ConfigureAwait(false);
        //                return statusResultValue.ConvertStatus();
        //            }

        //            // write without response cannot write values larger than MTU as per spec. Any longer writes can only be handled with response.
        //            else if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
        //            {
        //                var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse).AsTask().ConfigureAwait(false);
        //                return statusResultValue.ConvertStatus();
        //            }
        //            writer.Dispose();
        //        }
        //        catch (Exception e)
        //        {
        //            if (errorAction != null)
        //            {
        //                ErrorFoundClass.ErrorFound += errorAction;
        //            }
        //            var args = new CosmedGattErrorFoundEventArgs(e, properties);

        //            //it fires the event 
        //            ErrorFoundClass.Call(characteristic, args);

        //            ErrorFoundClass.ErrorFound -= errorAction;
        //            Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
        //            Console.WriteLine(e.Message);
        //        }
        //    }
        //   return CosmedGattCommunicationStatus.OperationNotSupported;
        //}


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
                    if (errorAction != null)
                    {
                        ErrorFoundClass.ErrorFound += errorAction;
                    }
                    var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Read);

                    //it fires the event 
                    ErrorFoundClass.Call(characteristic, args);

                    ErrorFoundClass.ErrorFound -= errorAction;
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

                return CosmedGattCommunicationStatus.Unreachable;
            }
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
                   // return cccd.Status.ConvertStatus();
                }

                //if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Indicate)
                //{
                //    return CosmedGattCommunicationStatus.OperationAlreadyRegistered;
                //}


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

                return CosmedGattCommunicationStatus.Unreachable;
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



    //    public static class Inkoke
    //    {
    //        [DllImport("irprops.cpl", SetLastError = true)]
    //        static extern uint BluetoothAuthenticateDeviceEx(IntPtr hwndParentIn, IntPtr hRadioIn, ref BLUETOOTH_DEVICE_INFO pbtdiInout, BLUETOOTH_OOB_DATA pbtOobData, uint authenticationRequirement);
    //    }

    //    public enum AUTHENTICATION_REQUIREMENTS : uint 
    //    {
    //        MITMProtectionNotRequired = 0x00,
    //        MITMProtectionRequired = 0x01,
    //        MITMProtectionNotRequiredBonding = 0x02,
    //        MITMProtectionRequiredBonding = 0x03,
    //        MITMProtectionNotRequiredGeneralBonding = 0x04,
    //        MITMProtectionRequiredGeneralBonding = 0x05,
    //        MITMProtectionNotDefined = 0xff
    //    }


    //    typedef struct _BLUETOOTH_DEVICE_INFO
    //    {
    //        DWORD dwSize;
    //        BLUETOOTH_ADDRESS Address;
    //        ULONG ulClassofDevice;
    //        BOOL fConnected;
    //        BOOL fRemembered;
    //        BOOL fAuthenticated;
    //        SYSTEMTIME stLastSeen;
    //        SYSTEMTIME stLastUsed;
    //        WCHAR szName[BLUETOOTH_MAX_NAME_SIZE];
    //    }
    //    BLUETOOTH_DEVICE_INFO_STRUCT;

    //
}

namespace Wintellect.Interop.Sound { 
    using System; 
    using System.Runtime.InteropServices; 
    using System.ComponentModel; 
    
    sealed class Sound { 
        public static void MessageBeep(BeepTypes type) { 
            if (!MessageBeep((UInt32)type)) { 
                Int32 err = Marshal.GetLastWin32Error(); 
                throw new Win32Exception(err); 
            } 
        }
        
        [DllImport("User32.dll", SetLastError = true)] 
        static extern Boolean MessageBeep(UInt32 beepType); 
        private Sound() { } 
    } 
    
    enum BeepTypes { 
        Simple = -1, 
        Ok = 0x00000000, 
        IconHand = 0x00000010, 
        IconQuestion = 0x00000020, 
        IconExclamation = 0x00000030, 
        IconAsterisk = 0x00000040 
    } 
}
