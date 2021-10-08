using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CosmedBleLib;

namespace CosmedBleConsole
{


    /// <summary>
    /// Wraps and makes use of <see cref="BluetoothLEAdvertisementWatcher"/>
    /// </summary>
    public class ppppp
    {
        static void Main(String[] args)
        {
            CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher();
            int count = 0;
            scan.startPassiveScanning();
            Thread.Sleep(3000);
            scan.stopScanning();
            /*while (true)
            {
                //  Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<passive");
                scan.startPassiveScanning();
                Thread.Sleep(2000);
                //   Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>active");
                scan.startActiveScanning();
                Thread.Sleep(2000);
                //   Console.WriteLine("--------------------------------stop");             
                scan.startPassiveScanning();
                Thread.Sleep(2000);
                //   Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>active");
                scan.startActiveScanning();
                Thread.Sleep(2000);
                //   Console.WriteLine("--------------------------------stop");
                scan.stopScanning();
                Thread.Sleep(2000);
                object o = scan.allDiscoveredDevices;
                count += 1;
                Console.WriteLine( count);
            }*/

    }
}

/*
 * algo per aggiornare lista di device:
    posso semplicemente sovrascrivere
 * quindi ogni volta blocco accesso a struttura e scrivo/sovrascrivo. Posso anche usare un campo che ha il numero di elementi
 * quando leggo anche blocco la scrittura, che si metterá in attesa se possibile. questo va verificato.
 * la struttura dovrá anche avere un metodo di accesso protetto.
 * Come proteggere quindi una struttura? la lettura deve essere fatta dopo una scrittura, per evitare incongruenze (tipo uno stesso
 * device compare 2 volte nella lista)
 * 
 * 
 */