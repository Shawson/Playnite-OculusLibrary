using System.Collections.Generic;
using System.Management;

namespace OculusLibrary.OS
{
    public interface IWMODriveQueryProvider
    {
        List<WMODrive> GetDriveData();
    }
}