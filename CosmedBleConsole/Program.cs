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

            scan.startPassiveScanning();
            Thread.Sleep(5000);

            scan.startActiveScanning();
            Thread.Sleep(5000);

            scan.stopScanning();

            Console.WriteLine("premere invio per chiudere");
            Console.ReadLine();



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
}

