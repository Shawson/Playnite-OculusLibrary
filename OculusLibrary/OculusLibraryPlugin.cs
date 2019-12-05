using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace OculusLibrary
{
    public partial class OculusLibraryPlugin : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("77346DD6-B0CC-4F7D-80F0-C1D138CCAE58");

        public override string Name { get; } = "Oculus";

        private readonly JavaScriptSerializer serialiser;
        private readonly OculusWebsiteScraper oculusScraper;
        private readonly ILogger logger;

        public OculusLibraryPlugin(IPlayniteAPI api) : base(api)
        {
            logger = LogManager.GetLogger(); 
            serialiser = new JavaScriptSerializer();
            oculusScraper = new OculusWebsiteScraper(logger);
        }

        public override IEnumerable<GameInfo> GetGames()
        {
            logger.Info($"Executing Oculus GetGames");

            var gameInfos = new List<GameInfo>();

            var oculusLibraryLocations = GetOculusLibraryLocations();

            if (oculusLibraryLocations == null || !oculusLibraryLocations.Any())
            {
                logger.Error($"Cannot ascertain Oculus library locations");
                return gameInfos;
            }

            using (var view = PlayniteApi.WebViews.CreateOffscreenView())
            {
                foreach (var oculusBasePath in oculusLibraryLocations)
                {
                    logger.Info($"Processing Oculus library location {oculusBasePath}");

                    foreach (var manifest in GetOculusAppManifests(oculusBasePath))
                    {
                        logger.Info($"Processing manifest {manifest.CanonicalName} {manifest.AppId}");

                        try
                        {
                            var executableFullPath = $@"{oculusBasePath}Software\Software\{manifest.CanonicalName}\{manifest.LaunchFile}";

                            // set a default name
                            var executableName = Path.GetFileNameWithoutExtension(executableFullPath);

                            var icon = $@"{oculusBasePath}CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg";

                            if (!File.Exists(icon))
                            {
                                logger.Debug($"Oculus store icon missing from file system- reverting to executable icon");
                                icon = executableFullPath;
                            }

                            var backgroundImage = $@"{oculusBasePath}CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_landscape_image_large.png";

                            if (!File.Exists(backgroundImage))
                            {
                                logger.Debug($"Oculus store background missing from file system- selecting no background");
                                backgroundImage = string.Empty;
                            }

                            var scrapedData = oculusScraper.ScrapeDataForApplicationId(view, manifest.AppId);

                            if (scrapedData == null)
                            {
                                logger.Debug($"Failed to retrieve scraped data for game");
                            }

                            logger.Info($"Executable {executableFullPath}");

                            gameInfos.Add(new GameInfo
                            {
                                Name = scrapedData?.Name ?? executableName,
                                Description = scrapedData?.Description ?? string.Empty,
                                GameId = manifest.AppId,
                                PlayAction = new GameAction
                                {
                                    Type = GameActionType.File,
                                    Path = executableFullPath,
                                    Arguments = manifest.LaunchParameters
                                },
                                IsInstalled = true,
                                Icon = icon,
                                BackgroundImage = backgroundImage
                            });

                            logger.Info($"Completed manifest {manifest.CanonicalName} {manifest.AppId}");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Exception while adding game for manifest {manifest.AppId} : {ex}");
                        }
                    }
                }
            }

            logger.Info($"Oculus GetGames Completing");

            return gameInfos;
        }

        private IEnumerable<OculusManifest> GetOculusAppManifests(string oculusBasePath)
        {
            logger.Debug($"Listing Oculus manifests");

            string[] fileEntries = Directory.GetFiles($@"{oculusBasePath}Software\Manifests\");

            if (!fileEntries.Any())
            {
                logger.Info($"No Oculus game manifests found");
            }
            
            foreach (string fileName in fileEntries.Where(x => x.EndsWith(".json")))
            {
                var json = File.ReadAllText(fileName);
                var manifest = serialiser.Deserialize<OculusManifest>(json);

                manifest.LaunchFile = manifest.LaunchFile.Replace("/", @"\");

                yield return manifest;
            }
        }

        private List<string> GetOculusLibraryLocations(RegistryView platformView)
        {
            var libraryPaths = new List<string>();

            logger.Debug($"Getting Oculus library locations from registry ({platformView})");

            RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, platformView);

            try
            {
                var libraryKeyTitles = rootKey
                    .OpenSubKey(@"HKEY_CURRENT_USER\Software\Oculus VR, LLC\Oculus\Libraries")
                    .GetSubKeyNames();

                if (libraryKeyTitles == null || !libraryKeyTitles.Any())
                {
                    logger.Error("No libraries found");
                    return null;
                }
                
                foreach(var libraryKeyTitle in libraryKeyTitles)
                {
                    var libraryPath = rootKey
                        .OpenSubKey($@"HKEY_CURRENT_USER\Software\Oculus VR, LLC\Oculus\Libraries\{libraryKeyTitle}")
                        .GetValue("OriginalPath")
                        .ToString();

                    if (!string.IsNullOrWhiteSpace(libraryPath))
                    {
                        libraryPaths.Add(libraryPath);
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

        private string GetOculusBaseFromRegistry(RegistryView platformView)
        {
            logger.Debug($"Getting Oculus Base path from registry ({platformView})");

            // HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment\OculusBase
            RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, platformView);

            try
            {
                var oculusBase = rootKey
                    .OpenSubKey(@"SYSTEM\ControlSet001\Control\Session Manager\Environment")
                    .GetValue("OculusBase");

                if (oculusBase == null)
                {
                    logger.Error("Registry key not found");
                    return string.Empty;
                }

                logger.Debug($"Registry key found: {oculusBase}");

                return oculusBase.ToString();
            }
            catch(Exception ex)
            {
                logger.Error($"Exception opening registry key: {ex}");
                return string.Empty;
            }
        }

        private List<string> GetOculusLibraryLocations()
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
