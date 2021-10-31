using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace CosmedBleLib
{
    public class BleService
    {
        private List<BleCharacteristic> characteristics;
        public GattDeviceService Service { get; }
        public GattCommunicationStatus GattCommunicationStatus { get; }
        public ushort AttributeHandle { get; }
        public string DeviceId { get; }
        public Guid Uuid { get; }
        public DeviceAccessInformation DeviceAccessInformation { get; }
        public GattSession Session { get; }
        public GattSharingMode SharingMode { get; }
        

        public BleService(GattDeviceService service, GattCommunicationStatus status)
        {
            characteristics = new List<BleCharacteristic>();
            Service = service;
            GattCommunicationStatus = status;
            AttributeHandle = service.AttributeHandle;
            DeviceId = service.DeviceId;
            Uuid = service.Uuid;
            DeviceAccessInformation = service.DeviceAccessInformation;
            Session = service.Session;
            SharingMode = service.SharingMode;
        }


        public async void GetBleCharactiristics()
        {
            GattCharacteristicsResult resultCharacteristics = await Service.GetCharacteristicsAsync();

            if (resultCharacteristics.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattCharacteristic> characteristicResults = resultCharacteristics.Characteristics;

                foreach (GattCharacteristic chars in characteristicResults)
                {
                    BleCharacteristic bleCharacteristic = new BleCharacteristic(chars, GattCommunicationStatus);
                    characteristics.Add(bleCharacteristic);                 
                }
            }
        }
    }

    public class BleCharacteristic
    {
        private List<IBleOperation> operations;
        public GattCharacteristic Characteristic { get; }
        public GattCommunicationStatus GattCommunicationStatus { get; private set; }
        public GattProtectionLevel ProtectionLevel { get; set; }
        public ushort AttributeHandle { get; }
        public GattCharacteristicProperties CharacteristicProperties { get; }
        public IReadOnlyList<GattPresentationFormat> PresentationFormats { get; }
        public string UserDescription { get; }
        public Guid Uuid { get; }
        public GattDeviceService Service { get; }
        public GattDescriptorsResult GattDescriptorsResult { get; private set; }
        public IReadOnlyList<IBleOperation> Óperations { get { return operations.AsReadOnly(); }  }
        public string ProtocolError { get; private set; } = "";


        public BleCharacteristic(GattCharacteristic characteristic, GattCommunicationStatus status)
        {
            GattCommunicationStatus = status;
            operations = new List<IBleOperation>();
            _ = GetDescriptorsResult();
            CharacteristicProperties = characteristic.CharacteristicProperties;
        }


        public async Task<GattDescriptorsResult> GetDescriptorsResult()
        {
            GattDescriptorsResult gdr = await Characteristic.GetDescriptorsAsync();
            GattDescriptorsResult = gdr;
            if(gdr.ProtocolError != null)
            {
                ProtocolError = string.Format("X2", gdr.ProtocolError);
            }
            return gdr;
        }

        public async Task GetSupportedOperations()
        {
            if (CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            {
                // This characteristic supports reading from it.
                //GattReadResult value = await characteristic.ReadValueAsync().AsTask();
                GattReadResult value = await Characteristic.ReadValueAsync();
                if (GattCommunicationStatus == GattCommunicationStatus.Success)
                {
                    GattReadResultReader grr = new GattReadResultReader(value.Value, value.Status, value.ProtocolError);
                    
                }
            }
        }

        public void PrintValues()
        {
            Console.WriteLine("Characteristic, user descriptio: " + Characteristic.UserDescription);
            Console.WriteLine("UUID: " + Characteristic.Uuid.ToString());
            Console.WriteLine("Attribute handle: " + Characteristic.AttributeHandle.ToString("X2"));
            Console.WriteLine("Protection level: " + Characteristic.ProtectionLevel.ToString());
            Console.WriteLine("Properties: " + Characteristic.CharacteristicProperties.ToString());

            foreach (var pf in Characteristic.PresentationFormats)
            {
                Console.WriteLine(" - Presentation format - ");
                Console.WriteLine("Description" + pf.Description);
                Console.WriteLine("" + pf.FormatType.ToString("X2"));
                Console.WriteLine("Unit: " + pf.Unit);
                Console.WriteLine("Exponent: " + pf.Exponent);
                Console.WriteLine("Namespace" + pf.Namespace.ToString("X2"));
                Console.WriteLine();
            }

            Console.WriteLine(" - descriptors - ");

            foreach (var descriptor in GattDescriptorsResult.Descriptors)
            {
                Console.WriteLine("protection level: " + descriptor.ProtectionLevel);
                Console.WriteLine("Uuid: " + descriptor.Uuid.ToString());
                Console.WriteLine("Attribute Handler" + descriptor.AttributeHandle.ToString("X2"));
            }

            Console.WriteLine("Status: " + GattDescriptorsResult.Status.ToString());
            if(GattDescriptorsResult.Status != GattCommunicationStatus)
            {
                //do somethings
            }

            if (GattDescriptorsResult.ProtocolError != null)
            {
                Console.WriteLine("Protocol error: " + GattDescriptorsResult.ProtocolError.Value.ToString("X2"));
            }
        }
    }
}
