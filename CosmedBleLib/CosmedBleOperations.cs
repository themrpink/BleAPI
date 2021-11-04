using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace CosmedBleLib
{


    //questo probabilmente non mi serve
    public interface IBleOperation
    {
        Operations operation { get; }
        void ExcecuteOperation(IBuffer buffer, GattCommunicationStatus status, byte? protocolError);
        void ExcecuteOperatio(IBuffer buffer);
        void ExcecuteOperatio(GattClientCharacteristicConfigurationDescriptorValue value);
        void print();

    }

    public abstract class BleOperation// : IBleOperation
    {
        public abstract void print();

        public async virtual Task ExcecuteOperation() { }
        public virtual void ExcecuteOperation(IBuffer buffer, GattCommunicationStatus status, byte? protocolError) { }
        public virtual void ExcecuteOperation(IBuffer buffer) { }
        public virtual void ExcecuteOperation(GattCharacteristic characteristic, GattClientCharacteristicConfigurationDescriptorValue value) { }
    }

    public class BleWriteOperation :  BleOperation
    {
        public Operations operation { get; } = Operations.Write;
        private IBuffer buffer;
        private GattCharacteristic characteristic;

        public BleWriteOperation()
        {
            
        }
        public BleWriteOperation(IBuffer buffer)
        {
            this.buffer = buffer;
        }

        public BleWriteOperation(GattCharacteristic characteristic)
        {
            this.characteristic = characteristic;
        }

        public async override Task ExcecuteOperation()
        {
            var writer = new DataWriter();
            // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
            writer.WriteByte(0x01);

            ///GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask();
            GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask().ConfigureAwait(false);
            if (value == GattCommunicationStatus.Success)
            {
                // Successfully wrote to device
            }
        }
        public override void print()
        {
            Console.WriteLine("im write");
        }

    }

    public class BleReadOperation : BleOperation
    {
        public Operations operation { get; } = Operations.Read;

        public override void print()
        {
            Console.WriteLine("im read");
        }

        public override void ExcecuteOperation(IBuffer buffer, GattCommunicationStatus status, byte? protocolError)
        {
            if (status == GattCommunicationStatus.Success)
            {
                GattReadResultReader grr = new GattReadResultReader(buffer, status, protocolError);
            }
        }
    }

    public class BleNotifyOperation : BleOperation
    {
        GattCharacteristic characteristic;
        public Operations operation { get; } = Operations.Notify;

        public BleNotifyOperation(GattCharacteristic chars)
        {
            this.characteristic = chars;
            characteristic.ValueChanged += Characteristic_ValueChanged;
        }

        public override async void ExcecuteOperation(GattCharacteristic characteristic, GattClientCharacteristicConfigurationDescriptorValue value)
        {
            GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
            
            if (status == GattCommunicationStatus.Success)
            {
                // Server has been informed of clients interest.
                
            }
        }

        public override void ExcecuteOperation(IBuffer buffer)
        {
            base.ExcecuteOperation(buffer);
        }

        void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            CharacteristicReader cr = new CharacteristicReader(args.CharacteristicValue, args.Timestamp);
            // An Indicate or Notify reported that the value has changed.

            Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
        }
        public override void print()
        {
            Console.WriteLine("im notify");
        }

    }

    public class BleIndicateOperation : BleOperation
    {
        public Operations operation { get; } = Operations.Indicate;

        public override async void ExcecuteOperation(GattCharacteristic characteristic, GattClientCharacteristicConfigurationDescriptorValue value)
        {
            GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask().ConfigureAwait(false);

            if (status == GattCommunicationStatus.Success)
            {
                // Server has been informed of clients interest.
                characteristic.ValueChanged += Characteristic_ValueChanged;
            }

            void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
            {
                // An Indicate or Notify reported that the value has changed.
                CharacteristicReader cr = new CharacteristicReader(args.CharacteristicValue, args.Timestamp);
                Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
            }
        }

        public override void print()
        {
            Console.WriteLine("im notify");
        }
    }



}


