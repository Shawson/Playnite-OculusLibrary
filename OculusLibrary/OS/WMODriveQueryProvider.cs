using System.Collections.Generic;
using System.Management;

namespace OculusLibrary.OS
{
    public class WMODriveQueryProvider : IWMODriveQueryProvider
    {
        public List<WMODrive> GetDriveData()
        {
            var result = new List<WMODrive>();
            ManagementObjectSearcher ms = new ManagementObjectSearcher("Select DeviceId, DriveLetter from Win32_Volume");

            foreach(var o in ms.Get())
            {
                result.Add(new WMODrive {
                    DeviceId = o["DeviceId"]?.ToString() ?? string.Empty,
                    DriveLetter = o["DriveLetter"]?.ToString() ?? string.Empty
                });
            }

            return result;
        }
    }
}
