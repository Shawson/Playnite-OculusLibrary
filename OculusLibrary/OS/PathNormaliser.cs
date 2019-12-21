using OculusLibrary.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace OculusLibrary
{
    public class PathNormaliser : IPathNormaliser
    {
        private static List<WMODrive> wmoVolumes;
        private static object locker = new object();
        private readonly IWMODriveQueryProvider windowsManagementObjectDriveQueryProvider;

        private List<WMODrive> WmoVolumes
        {
            get
            {

                if (wmoVolumes == null)
                {
                    lock (locker)
                    {
                        wmoVolumes = windowsManagementObjectDriveQueryProvider.GetDriveData();
                    }
                }

                return wmoVolumes;
            }
        }

        public PathNormaliser(IWMODriveQueryProvider windowsManagementObjectQueryProvider)
        {
            this.windowsManagementObjectDriveQueryProvider = windowsManagementObjectQueryProvider;
        }

        public void Dispose()
        {
            wmoVolumes = null;
        }

        public string Normalise(string path)
        {
            var regex = new Regex(@"\\\\\?\\Volume{([^}]+)}\\");
            var driveLetter = string.Empty;
            var deviceIdMatches = regex.Match(path);

            if (deviceIdMatches.Success)
            {
                var deviceGuid = deviceIdMatches.Value;

                if (WmoVolumes.Any())
                {
                    foreach (WMODrive mo in WmoVolumes)
                    {
                        if (mo.DeviceId == deviceGuid)
                        {
                            driveLetter = mo.DriveLetter;
                            break;
                        }
                    }

                    if (driveLetter != string.Empty)
                    {
                        return regex.Replace(path, $@"{driveLetter}\");
                    }
                }
            }

            return path;
        }
    }
}
