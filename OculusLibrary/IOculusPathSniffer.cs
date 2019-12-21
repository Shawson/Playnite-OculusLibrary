using System.Collections.Generic;

namespace OculusLibrary
{
    public interface IOculusPathSniffer
    {
        List<string> GetOculusLibraryLocations();
    }
}