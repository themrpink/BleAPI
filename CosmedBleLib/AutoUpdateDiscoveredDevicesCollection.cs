using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmedBleLib
{



    public class AutoUpdateDiscoveredDevicesCollection : IAdvertisedDevicesCollection
    { 

        private readonly object ThreadLock = new object();
        private List<CosmedBleAdvertisedDevice> lastDiscoveredDevices;

        public AutoUpdateDiscoveredDevicesCollection()
        {
            this.lastDiscoveredDevices = new List<CosmedBleAdvertisedDevice>();
        }

        public IReadOnlyCollection<CosmedBleAdvertisedDevice> GetLastDiscoveredDevices()
        {
            lock (ThreadLock)
            {
                IReadOnlyList<CosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.AsReadOnly();
                lastDiscoveredDevices = new List<CosmedBleAdvertisedDevice>();
                return lastDiscoveredDevicesTemp;
            }
        }

        public void NewDiscoveredDeviceHandler(CosmedBleAdvertisedDevice device)
        {
            lock (ThreadLock)
            {
                //aggiungo quelli già inseriti in una lista a parte, tranne quello nuovo(che potrebbe essere già presente ma non aggiornato)
                List<CosmedBleAdvertisedDevice> temp = lastDiscoveredDevices.Where(dev => dev.DeviceAddress != device.DeviceAddress).ToList();
                
                //aggiungo alla copia della lista quello nuovo
                temp.Add(device);
                
                //ricreo la lista readOnly aggiornata
                lastDiscoveredDevices = temp;
            }
        } 
    }









    /*
    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public class AutoUpdateDiscoveredDevicesCollection2 : IAdvertisedDevicesCollection2
    {
        // private bool isCollectingDevices = false;
        private readonly object ThreadLock = new object();

        private List<CosmedBleAdvertisedDevice> lastDiscoveredDevices;

        public AutoUpdateDiscoveredDevicesCollection2()
        {
            this.lastDiscoveredDevices = new List<CosmedBleAdvertisedDevice>();
        }


        public IReadOnlyCollection<CosmedBleAdvertisedDevice> allDiscoveredDevices { get; private set; } = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> recentDiscoveredDevices { get; private set; } = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> getLastDiscoveredDevices()
        {
            lock (ThreadLock)
            {
                IReadOnlyList<CosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.AsReadOnly();
                lastDiscoveredDevices = new List<CosmedBleAdvertisedDevice>();
                return lastDiscoveredDevicesTemp;
            }
        }
        

        public void RecentlyUpdatedDevicesHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> devices)
        {
            recentDiscoveredDevices = devices;
        }


        public void NewDiscoveredDeviceHandler(CosmedBleAdvertisedDevice device)
        {
            lock (ThreadLock)
            {
                //aggiungo quelli già inseriti in una lista a parte, tranne quello nuovo(che potrebbe essere già presente ma non aggiornato)
                List<CosmedBleAdvertisedDevice> temp = lastDiscoveredDevices.Where(dev => dev.DeviceAddress != device.DeviceAddress).ToList();
                //aggiungo alla copia della lista quello nuovo
                temp.Add(device);
                //ricreo la lista readOnly aggiornata
                lastDiscoveredDevices = temp;
            }
        }


        public void AllDevicesUpdatedHandler(IReadOnlyCollection<CosmedBleAdvertisedDevice> devices)
        {
            allDiscoveredDevices = devices;
        }


        public void ClearCollections()
        {
            allDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
            recentDiscoveredDevices = Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly();
            lastDiscoveredDevices = new List<CosmedBleAdvertisedDevice>();
        }


        public void Dispose()
        {      
            allDiscoveredDevices = null;
            recentDiscoveredDevices = null;
            lastDiscoveredDevices = null;
        }
    }
    */
}
