using Microsoft.Win32;
using OculusLibrary.DataExtraction;
using OculusLibrary.OS;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusLibrary.DataExtraction
{
    public class OculusPathSniffer : IOculusPathSniffer
    {
        private readonly IRegistryValueProvider registryValueProvider;
        private readonly IPathNormaliser pathNormaliser;
        private readonly ILogger logger;

        public OculusPathSniffer(
            IRegistryValueProvider registryValueProvider,
            IPathNormaliser pathNormaliser,
            ILogger logger)
        {
            this.registryValueProvider = registryValueProvider;
            this.pathNormaliser = pathNormaliser;
            this.logger = logger;
        }

        private List<string> GetOculusLibraryLocations(RegistryView platformView)
        {
            var libraryPaths = new List<string>();

            logger.Debug($"Getting Oculus library locations from registry ({platformView})");

            try
            {
                var libraryKeyTitles = registryValueProvider.GetSubKeysForPath(platformView,
                                                                                RegistryHive.CurrentUser,
                                                                                @"Software\Oculus VR, LLC\Oculus\Libraries\");

                if (libraryKeyTitles == null || !libraryKeyTitles.Any())
                {
                    logger.Error("No libraries found");
                    return null;
                }

                foreach (var libraryKeyTitle in libraryKeyTitles)
                {
                    var libraryPath = registryValueProvider.GetValueForPath(platformView,
                                                                            RegistryHive.CurrentUser,
                                                                            $@"Software\Oculus VR, LLC\Oculus\Libraries\{libraryKeyTitle}",
                                                                            "Path");

                    if (!string.IsNullOrWhiteSpace(libraryPath))
                    {
                        libraryPath = pathNormaliser.Normalise(libraryPath);
                        libraryPaths.Add(libraryPath);
                        logger.Debug($"Found library: {libraryPath}");
                    }
                }

                logger.Debug($"Libraries located: {libraryPaths.Count}");

                return libraryPaths;
            }
            catch (Exception ex)
            {
                logger.Error($"Exception opening registry keys: {ex}");
                return null;
            }
        }

        public List<string> GetOculusLibraryLocations()
        {
            logger.Debug("Trying to get Oculus base path (REG64)");

            var libraryLocations = GetOculusLibraryLocations(RegistryView.Registry64);

            if (libraryLocations == null)
            {
                logger.Debug("Trying to get Oculus base path (REG32)");
                libraryLocations = GetOculusLibraryLocations(RegistryView.Registry32);
            }

            return libraryLocations;
        }
    }
}
