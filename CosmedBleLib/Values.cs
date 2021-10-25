using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmedBleLib
{
    class Values
    {
        /*
        #define BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE         (0x01)   //< LE Limited Discoverable Mode. 
        #define BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE         (0x02)   //< LE General Discoverable Mode. 
        #define BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED         (0x04)   //< BR/EDR not supported. 
        #define BLE_GAP_ADV_FLAG_LE_BR_EDR_CONTROLLER         (0x08)   //< Simultaneous LE and BR/EDR, Controller. 
        #define BLE_GAP_ADV_FLAG_LE_BR_EDR_HOST               (0x10)   //< Simultaneous LE and BR/EDR, Host.
        #define BLE_GAP_ADV_FLAGS_LE_ONLY_LIMITED_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE Limited Discoverable Mode, BR/EDR not supported. (05)
        #define BLE_GAP_ADV_FLAGS_LE_ONLY_GENERAL_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE General Discoverable Mode, BR/EDR not supported. (06)
        */


        /*
         // Data structure: sono gli advertiment types (vedi cherry sez. advertising & scanning, link a 
        //https://docs.silabs.com/bluetooth/latest/general/adv-and-scanning/bluetooth-adv-data-basics
        Packet.ADTypes = {
            0x01 : { name : "Flags", resolve: toStringArray },
            0x02 : { name : "Incomplete List of 16-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 2)},
            0x03 : { name: "Complete List of 16-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 2) },
            0x04 : { name: "Incomplete List of 32-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 4) },
            0x05 : { name: "Complete List of 32-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 4) },
            0x06 : { name: "Incomplete List of 128-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 16) },
            0x07 : { name: "Complete List of 128-bit Service Class UUIDs", resolve: toOctetStringArray.bind(null, 16) },
            0x08 : { name: "Shortened Local Name", resolve: toString },
            0x09 : { name: "Complete Local Name", resolve: toString },
            0x0A : { name: "Tx Power Level", resolve: toSignedInt },
            0x0D : { name: "Class of Device", resolve: toOctetString.bind(null, 3) },
            0x0E : { name: "Simple Pairing Hash C", resolve: toOctetString.bind(null, 16) },
            0x0F : { name: "Simple Pairing Randomizer R", resolve: toOctetString.bind(null, 16) },
            0x10 : { name: "Device ID", resolve: toOctetString.bind(null, 16) },
            // 0x10 : { name : "Security Manager TK Value", resolve: null }
            0x11 : { name: "Security Manager Out of Band Flags", resolve: toOctetString.bind(null, 16) },
            0x12 : { name: "Slave Connection Interval Range", resolve: toOctetStringArray.bind(null, 2) },
            0x14 : { name: "List of 16-bit Service Solicitation UUIDs", resolve: toOctetStringArray.bind(null, 2) },
            0x1F : { name: "List of 32-bit Service Solicitation UUIDs", resolve: toOctetStringArray.bind(null, 4) },
            0x15 : { name: "List of 128-bit Service Solicitation UUIDs", resolve: toOctetStringArray.bind(null, 8) },
            0x16 : { name: "Service Data", resolve: toOctetStringArray.bind(null, 1) },
            0x17 : { name: "Public Target Address", resolve: toOctetStringArray.bind(null, 6) },
            0x18 : { name: "Random Target Address", resolve: toOctetStringArray.bind(null, 6) },
            0x19 : { name: "Appearance" , resolve: null },
            0x1A : { name: "Advertising Interval" , resolve: toOctetStringArray.bind(null, 2)  },
            0x1B : { name: "LE Bluetooth Device Address", resolve: toOctetStringArray.bind(null, 6) },
            0x1C : { name: "LE Role", resolve: null },
            0x1D : { name: "Simple Pairing Hash C-256", resolve: toOctetStringArray.bind(null, 16) },
            0x1E : { name: "Simple Pairing Randomizer R-256", resolve: toOctetStringArray.bind(null, 16) },
            0x20 : { name: "Service Data - 32-bit UUID", resolve: toOctetStringArray.bind(null, 4) },
            0x21 : { name: "Service Data - 128-bit UUID", resolve: toOctetStringArray.bind(null, 16) },
            0x3D : { name: "3D Information Data", resolve: null },
            0xFF : { name: "Manufacturer Specific Data", resolve: null },
         }*/

        /*
         public enum BluetoothLEAdvertisementFlags : uint
            None = 0,
            LimitedDiscoverableMode = 1,
            GeneralDiscoverableMode = 2,
            ClassicNotSupported = 4,
            DualModeControllerCapable = 8,
            DualModeHostCapable = 16

            0     =  0
            1     =  1
            10    =  2
            100   =  4
            1000  =  8
            10000 = 16
         */

    }
}
