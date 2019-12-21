using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusLibrary.OS
{
    public class RegistryValueProvider : IRegistryValueProvider
    {
        public RegistryValueProvider() { }

        public List<string> GetSubKeysForPath(
            RegistryView platform,
            RegistryHive hive,
            string path)
        {
            RegistryKey rootKey = RegistryKey.OpenBaseKey(hive, platform);

            return rootKey
                    .OpenSubKey(path)
                    .GetSubKeyNames()
                    .ToList();
        }

        public string GetValueForPath(
            RegistryView platform,
            RegistryHive hive,
            string path,
            string keyName)
        {
            RegistryKey rootKey = RegistryKey.OpenBaseKey(hive, platform);

            return rootKey
                        .OpenSubKey(path)
                        .GetValue(keyName)
                        .ToString();
        }
    }
}
