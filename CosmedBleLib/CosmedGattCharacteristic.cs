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
        
        public static Action<GattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged { get; set; }

        public static Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound { get; set; }


        #region Properties
        public GattCharacteristic characteristic { get; private set; }
        public GattProtectionLevel ProtectionLevel { get; set; }
        public ushort AttributeHandle { get; private set; }
        public GattCharacteristicProperties CharacteristicProperties { get; private set; }
        public IReadOnlyList<GattPresentationFormat> PresentationFormats { get; private set; }
        public string UserDescription { get; private set; }
        public Guid Uuid { get; private set; }

        private event TypedEventHandler<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> ErrorFound;

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

        public CosmedGattCharacteristic(GattCharacteristic characteristic, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> characteristicErrorFound) : this(characteristic)
        {
            CharacteristicErrorFound = characteristicErrorFound;
            ErrorFound += (sender, args) => { CharacteristicErrorFound(sender, args); };
        }

        #endregion


        #region Operation Methods

        public async Task<CosmedGattCommunicationStatus> Write(byte value, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null, ushort? maxPduSize = null)
        {

            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                if (maxPduSize != null)
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
                    if (action != null)
                    {
                        CharacteristicErrorFound = action;
                    }
                    var arg = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Write);
                    OnErrorFound(arg);
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return CosmedGattCommunicationStatus.WriteNotPermitted;
        }


        public async Task<GattReadResultReader> Read(Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null)
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
            return new GattReadResultReader(null, CosmedGattCommunicationStatus.ReadNotPermitted, null);
        }


        public async Task<CosmedGattCommunicationStatus> SubscribeToNotifications( Action<GattCharacteristic, GattValueChangedEventArgs> response, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null)
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
                catch (Exception e)
                {
                    if (action != null)
                    {
                        CharacteristicErrorFound = action;
                    }
                    var arg = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Notify);
                    OnErrorFound(arg);
                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return CosmedGattCommunicationStatus.NotifyNotPermitted;
        }


        public async Task<CosmedGattCommunicationStatus> SubscribeToIndications(Action<GattCharacteristic, GattValueChangedEventArgs> response, Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> action = null)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

            if (properties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                try
                {
                    //var conf = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();
                    //if (conf.Status == GattCommunicationStatus.Success)
                    {
                        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask().ConfigureAwait(false);
                        if (status == GattCommunicationStatus.Success)
                        {
                            characteristic.ValueChanged += (sender, args) => response(sender, args);
                            return ConvertStatus(status);
                        }
                    }
                }
                catch (Exception e)
                {
                    if(action != null)
                    {
                        CharacteristicErrorFound = action;
                    }
                    var arg = new CosmedGattErrorFoundEventArgs(e, GattCharacteristicProperties.Indicate);
                    OnErrorFound(arg);

                    Console.WriteLine("error catched with characteristic: " + characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
            }
            return CosmedGattCommunicationStatus.IndicateNotPermitted;
        }

        #endregion


        #region Helper Methods
        public CosmedGattCommunicationStatus ConvertStatus(GattCommunicationStatus status)
        {
            switch (status)
            {
                case GattCommunicationStatus.Success: return CosmedGattCommunicationStatus.Success;
                case GattCommunicationStatus.AccessDenied: return CosmedGattCommunicationStatus.AccessDenied;
                case GattCommunicationStatus.ProtocolError: return CosmedGattCommunicationStatus.ProtocolError;
                case GattCommunicationStatus.Unreachable: return CosmedGattCommunicationStatus.Unreachable;
                default: return CosmedGattCommunicationStatus.Unknown;
            }
        }


        protected virtual void OnErrorFound(CosmedGattErrorFoundEventArgs args)
        {
            ErrorFound?.Invoke(this, args);
        }


        #endregion

    }


    public class CosmedGattErrorFoundEventArgs : EventArgs
    {
        public Exception Exception {get; private set;}
        public GattCharacteristicProperties Property { get; private set; }

        public CosmedGattErrorFoundEventArgs (Exception exception, GattCharacteristicProperties property)
        {
            Exception = exception;
            Property = property;
        }
    }

}
