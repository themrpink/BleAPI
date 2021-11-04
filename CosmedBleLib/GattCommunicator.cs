using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace CosmedBleLib
{
    public static class GattCommunicator
    {

        public static async Task<bool> Write(GattCharacteristic characteristic, GattCommunicationStatus status)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                writer.WriteByte(0x01);

                GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask().ConfigureAwait(false);
                if (value == GattCommunicationStatus.Success)
                {
                    // posso per esempio invocare un metodo che conferma la riuscita, oppure ritornare un bool
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static async Task<GattReadResultReader> Read(GattCharacteristic characteristic, GattCommunicationStatus status)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            GattReadResultReader grr;

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                GattReadResult value = await characteristic.ReadValueAsync().AsTask().ConfigureAwait(false);
                if (status == GattCommunicationStatus.Success)
                {
                    grr = new GattReadResultReader(value.Value, value.Status, value.ProtocolError);
                    return grr;
                }
            }
            return default(GattReadResultReader); // Task<GattReadResultReader>.CompletedTask;
        }

        public static void SubscribeNotification(GattCharacteristic characteristic, GattCommunicationStatus status)
        {

        }

        public static void SubscribeIndicaction(GattCharacteristic characteristic, GattCommunicationStatus status)
        {

        }

    }
}
