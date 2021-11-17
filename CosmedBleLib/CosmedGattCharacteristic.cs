using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;


namespace CosmedBleLib
{

    public class CosmedGattCharacteristic
    {

        private event Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> ErrorFound;
        private event Action<CosmedGattCharacteristic, GattValueChangedEventArgs> ValueChanged;

        //public events
        public Action<CosmedGattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged;
        public Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound;


        #region Properties

        public GattCharacteristic characteristic { get; private set; }
        public GattProtectionLevel ProtectionLevel { get; set; }
        public ushort AttributeHandle { get; private set; }
        public GattCharacteristicProperties CharacteristicProperties { get; private set; }
        public IReadOnlyList<GattPresentationFormat> PresentationFormats { get; private set; }
        public string UserDescription { get; private set; }
        public Guid Uuid { get; private set; }
        public bool IsWriteAllowed { get { return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write); } }
        public bool IsReadAllowed { get { return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read); } }
        public bool IsNotificationAllowed { get { return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify); } }
        public bool IsIndicationAllowed { get { return characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate); } }


        #endregion


        #region Constructor

        public CosmedGattCharacteristic(GattCharacteristic characteristic)
        {
            this.characteristic = characteristic;
            AttributeHandle = characteristic.AttributeHandle;
            CharacteristicProperties = characteristic.CharacteristicProperties;
            PresentationFormats = characteristic.PresentationFormats;
            UserDescription = characteristic.UserDescription;
            Uuid = characteristic.Uuid;
            ProtectionLevel = characteristic.ProtectionLevel;
        }

        public CosmedGattCharacteristic(GattCharacteristic characteristic, Action<CosmedGattCharacteristic, GattValueChangedEventArgs> characteristicValueChanged, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> characteristicErrorFound) : this(characteristic)
        {
            CharacteristicErrorFound = characteristicErrorFound;
            CharacteristicValueChanged = characteristicValueChanged;
            ErrorFound += CharacteristicErrorFound;
            ValueChanged += CharacteristicValueChanged;
            characteristic.ValueChanged += OnValueChanged;
        }

        #endregion


        #region Operation Methods

        public async Task<CosmedCharacteristicWriteResult> Write(byte value, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null, ushort? maxPduSize = null)
        {
            if (IsWriteAllowed)
            {
                //check MaxPduSize from GattSession before use
                if (maxPduSize != null)
                {
                    //check byte size
                }

                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                writer.WriteByte(value);

                try
                {
                    var statusResultValue = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask().ConfigureAwait(false);
                    return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.Success);
                }
                catch (Exception e)
                {
                    if (action != null)
                    {
                        CharacteristicErrorFound = action;
                    }
                    var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Write);
                    OnErrorFound(args);
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return new CosmedCharacteristicWriteResult(null, CosmedGattCommunicationStatus.OperationNotSupported);
        }


        public async Task<CosmedCharacteristicReadResult> Read(Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null)
        {
            if (IsReadAllowed)
            {
                try
                {
                    GattReadResult value = await characteristic.ReadValueAsync().AsTask().ConfigureAwait(false);

                    if (value.Status == GattCommunicationStatus.Success)
                    {
                        CosmedGattCommunicationStatus newStatus = ConvertStatus(value.Status);
                        return new CosmedCharacteristicReadResult(value.Value, newStatus, value.ProtocolError);
                    }
                }
                catch (Exception e)
                {
                    if (action != null)
                    {
                        CharacteristicErrorFound = action;
                    }
                    var arg = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Read);
                    OnErrorFound(arg);
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return new CosmedCharacteristicReadResult(null, CosmedGattCommunicationStatus.OperationNotSupported, null);
        }


        public async Task<CosmedCharacteristicSubscriptionResult> SubscribeToNotification(TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction = null, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsNotificationAllowed())
            {
                return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify, CosmedGattCommunicationStatus.OperationNotSupported);
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd;
            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                //if not notifying yet, then subscribe
                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify,
                                                                                        ConvertStatus(cccd.Status),
                                                                                        cccd.ClientCharacteristicConfigurationDescriptor,
                                                                                        cccd.ProtocolError
                                                                                        );
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Notify)
                {
                    return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify,
                                                                                        ConvertStatus(cccd.Status),
                                                                                        cccd.ClientCharacteristicConfigurationDescriptor,
                                                                                        cccd.ProtocolError
                                                                                        );
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
                return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify,
                                                                    ConvertStatus(status)
                                                                    );

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

            return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify,
                                                    CosmedGattCommunicationStatus.OperationNotSupported
                                                    );
        }


        public async Task<CosmedCharacteristicSubscriptionResult> SubscribeToIndication(TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> valueChangedAction = null, Action<GattCharacteristic, CosmedGattErrorFoundEventArgs> errorAction = null)
        {
            if (!characteristic.IsIndicationAllowed())
            {
                return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Indicate, CosmedGattCommunicationStatus.OperationNotSupported);
            }

            GattReadClientCharacteristicConfigurationDescriptorResult cccd;
            try
            {
                Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
                cccd = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                //if not notifying yet, then subscribe
                if (cccd.Status != GattCommunicationStatus.Success)
                {
                    return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Indicate,
                                                                                        ConvertStatus(cccd.Status),
                                                                                        cccd.ClientCharacteristicConfigurationDescriptor,
                                                                                        cccd.ProtocolError
                                                                                        );
                }

                if (cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Indicate)
                {
                    return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Indicate,
                                                                                        ConvertStatus(cccd.Status),
                                                                                        cccd.ClientCharacteristicConfigurationDescriptor,
                                                                                        cccd.ProtocolError
                                                                                        );
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
                return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Indicate,
                                                                    ConvertStatus(status)
                                                                    );

            }
            catch (Exception e)
            {
                if (errorAction != null)
                {
                    ErrorFoundClass.ErrorFound += errorAction;
                }
                var args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate);

                //it fires the event 
                ErrorFoundClass.Call(characteristic, args);

                ErrorFoundClass.ErrorFound -= errorAction;
                Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                Console.WriteLine(e.Message);
            }

            return new CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties.Notify,
                                                    CosmedGattCommunicationStatus.OperationNotSupported
                                                    );
        }


        //public async Task<CosmedCharacteristicSubscriptionResult> StopSubscription(Action<GattCharacteristic, GattValueChangedEventArgs> response, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null)
        //{
        //    if (IsIndicationAllowed)
        //    {
        //        GattReadClientCharacteristicConfigurationDescriptorResult cccd = null;
        //        try
        //        {
        //            Console.WriteLine("characteristec UUID: " + characteristic.Uuid.ToString());
        //            if (cccd.Status == GattCommunicationStatus.Success && cccd.ClientCharacteristicConfigurationDescriptor == GattClientCharacteristicConfigurationDescriptorValue.Indicate)
        //            {
        //                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).AsTask().ConfigureAwait(false);
        //                if (status == GattCommunicationStatus.Success)
        //                {
        //                    //adrebbe tolta la giusta action
        //                    characteristic.ValueChanged -= (sender, args) => response(sender, args);

        //                }
        //                //gives the last status
        //                return new CosmedCharacteristicSubscriptionResult(characteristic.CharacteristicProperties, cccd, status);
        //            }
        //            //gives the cccd
        //            return new CosmedCharacteristicSubscriptionResult(characteristic.CharacteristicProperties, cccd);
        //        }
        //        catch (Exception e)
        //        {
        //            if (action != null)
        //            {
        //                CharacteristicErrorFound = action;
        //            }

        //            CosmedGattErrorFoundEventArgs args;
        //            if (cccd != null)
        //                args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate, cccd);
        //            else
        //                args = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate);

        //            OnErrorFound(args);
        //            Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
        //            Console.WriteLine(e.Message);
        //        }
        //    }
        //    return new CosmedCharacteristicSubscriptionResult(characteristic.CharacteristicProperties, CosmedGattCommunicationStatus.OperationNotSupported);

        //}

        #endregion


        #region Helper Methods

        private CosmedGattCommunicationStatus ConvertStatus(GattCommunicationStatus status)
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


        protected virtual void OnErrorFound(CosmedGattErrorFoundEventArgs args)
        {
            ErrorFound?.Invoke(this, args);
        }

        protected virtual void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            ValueChanged?.Invoke(this, args);
        }


        public void ToString()
        {
            Console.WriteLine("Characteristic  Uuid: " + Uuid.ToString());
            Console.WriteLine("ProtectionLevel :" + ProtectionLevel.ToString());
            Console.WriteLine("Attribute Handle :" + AttributeHandle);
            Console.WriteLine("CharacteristicProperties :" + CharacteristicProperties.ToString());
            Console.WriteLine("user description: " + UserDescription);
            Console.WriteLine("_________Gatt presentation format:________");
            try
            {
                foreach (var pres in PresentationFormats)
                {

                    Console.WriteLine("Description: " + pres.Description);
                    Console.WriteLine("Exponent: " + pres.Exponent);
                    Console.WriteLine("FormatType: " + pres.FormatType.ToString("X2"));
                    Console.WriteLine("namepsace: " + pres.Namespace.ToString("X2"));
                    Console.WriteLine("unit: " + pres.Unit);
                    Console.WriteLine("BluetoothSigAssignedNumbers: " + GattPresentationFormat.BluetoothSigAssignedNumbers);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("______________________");
        }

        public void CleanEvents()
        {
            ErrorFound -= CharacteristicErrorFound;
            ValueChanged -= CharacteristicValueChanged;
        }

        #endregion

    }



    public class CosmedGattErrorFoundEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public GattCharacteristicProperties Property { get; private set; }
        public GattReadClientCharacteristicConfigurationDescriptorResult Result { get; private set; }

        public CosmedGattErrorFoundEventArgs(Exception exception, GattCharacteristicProperties property)
        {
            Exception = exception;
            Property = property;
        }

        public CosmedGattErrorFoundEventArgs(Exception exception, GattCharacteristicProperties property, GattReadClientCharacteristicConfigurationDescriptorResult result) : this(exception, property)
        {
            Result = result;
        }
    }



    public class CosmedCharacteristicSubscriptionResult : ICommunicationResult
    {
        public GattCharacteristicProperties Property { get; private set; }
        public GattClientCharacteristicConfigurationDescriptorValue ClientCharacteristicConfigurationDescriptor { get; private set; }
        public CosmedGattCommunicationStatus Status { get; private set; }
        public byte? ProtocolError { get; private set; }

        public CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties property, CosmedGattCommunicationStatus status)
        {
            Property = property;
            Status = status;
        }

        public CosmedCharacteristicSubscriptionResult(GattCharacteristicProperties property, CosmedGattCommunicationStatus status, GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptor, byte? protocolError)
        {
            Property = property;
            Status = status;
            ClientCharacteristicConfigurationDescriptor = clientCharacteristicConfigurationDescriptor;
            ProtocolError = protocolError;
        }
    }



    public class CosmedCharacteristicReadResult : BufferReader, ICommunicationResult
    {
        public byte? ProtocolError { get; private set; }
        public CosmedGattCommunicationStatus Status { get; private set; }

        public string ProtocolErrorString { get { return string.Format("X2", ProtocolError); } }

        public GattCharacteristicProperties Property { get; private set; }

        public CosmedCharacteristicReadResult(IBuffer buffer, CosmedGattCommunicationStatus status, byte? protocolError) : base(buffer)
        {
            Status = status;
            ProtocolError = protocolError;
        }

    }



    public class CosmedCharacteristicWriteResult : ICommunicationResult
        {
            public GattCharacteristicProperties Property { get; private set; }
            public byte? ProtocolError { get; }

            public CosmedGattCommunicationStatus Status { get; }

            public CosmedCharacteristicWriteResult(byte? protocolError, CosmedGattCommunicationStatus status)
            {
                Status = status;
                ProtocolError = protocolError;
            }
        }

}






