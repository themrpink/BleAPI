using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    public class CosmedBluetoothLEAdvertisementFilter
    {
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; }
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; }

        public CosmedBluetoothLEAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();
            SignalStrengthFilter = new BluetoothSignalStrengthFilter
            {
                //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //will start to be considered "in-range"
                InRangeThresholdInDBm = -70,

                // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeThresholdInDBm = -75,

                // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000)
            };

        }


    }
}


/*
 * 
 * TODO:
 * dal video di angelsix vedere che struttura dati utilizza per mettere i dati sui device.
 * come crea l´oggetto device
 * Questa struttura verrà scritta dall´evento onAdvertisementReceived
 * in qualche modo si dovrà controllare se questo già esiste nella struttura , in tal caso sovrascriverlo
 * la struttura deve essere protetta da un mutex per scrittura produttore e lettura consumatore
 * devo anche capire come eliminare i device non piú validi
 * 
 * 
 * poi pensare a come usare il filtro:
 * 1) cosa filtrare:
 *      nome
 *      codice
 *      segnale
 *      white list
 * 2) come
 *      passare un filtro come parametro del costruttore, creare dei metodi per filtrare?
 *      creare (wrappare) oggetti nuovi o usare quelli preesistenti nella libreria?
 * 
 * 3)il watcher contiene la struttura dati? questa viene creata a ogni richiesta o sempre disponibile
 * con un metodo? la posso passare separata dall´oggetto (watcher) che l´ha creata? oppure é meglio tenerla attaccatta?
 * penso sia meglio tenerla attaccata. Ci vuole anche la possibilitá di verificare che lo stato del watcher sia attivo
 * cioè non stoppato, altrimenti la struttura dati deve essere invalidata.
 * */