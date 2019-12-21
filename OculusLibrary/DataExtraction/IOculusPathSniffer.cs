using System.Collections.Generic;

namespace OculusLibrary.DataExtraction
{
    public interface IOculusPathSniffer
    {
        List<string> GetOculusLibraryLocations();
    }
}