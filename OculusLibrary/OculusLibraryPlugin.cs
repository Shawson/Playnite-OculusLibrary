using Microsoft.Win32;
using Newtonsoft.Json;
using OculusLibrary.DataExtraction;
using OculusLibrary.OS;
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

        private readonly IOculusPathSniffer pathSniffer;
        private readonly OculusWebsiteScraper oculusScraper;
        private readonly ILogger logger;

        public OculusLibraryPlugin(IPlayniteAPI api) : base(api)
        {
            logger = LogManager.GetLogger();
            pathSniffer = new OculusPathSniffer(new RegistryValueProvider(), new PathNormaliser(new WMODriveQueryProvider()), logger);
            oculusScraper = new OculusWebsiteScraper(logger);
        }

        public override IEnumerable<GameInfo> GetGames()
        {
            logger.Info($"Executing Oculus GetGames");

            var gameInfos = new List<GameInfo>();

            var oculusLibraryLocations = pathSniffer.GetOculusLibraryLocations();

            if (oculusLibraryLocations == null || !oculusLibraryLocations.Any())
            {
                logger.Error($"Cannot ascertain Oculus library locations");
                return gameInfos;
            }

            using (var view = PlayniteApi.WebViews.CreateOffscreenView())
            {
                foreach (var currentLibraryBasePath in oculusLibraryLocations)
                {
                    logger.Info($"Processing Oculus library location {currentLibraryBasePath}");

                    foreach (var manifest in GetOculusAppManifests(currentLibraryBasePath))
                    {
                        logger.Info($"Processing manifest {manifest.CanonicalName} {manifest.AppId}");

                        try
                        {
                            var installationPath = $@"{currentLibraryBasePath}\Software\{manifest.CanonicalName}";
                            var executableFullPath = $@"{installationPath}\{manifest.LaunchFile}";

                            // set a default name
                            var executableName = Path.GetFileNameWithoutExtension(executableFullPath);

                            var icon = $@"{currentLibraryBasePath}\..\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg";

                            if (!File.Exists(icon))
                            {
                                logger.Debug($"Oculus store icon missing from file system- reverting to executable icon");
                                icon = executableFullPath;
                            }

                            var backgroundImage = $@"{currentLibraryBasePath}\..\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_landscape_image_large.png";

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
                                InstallDirectory = installationPath,
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

        private List<OculusManifest> GetOculusAppManifests(string oculusBasePath)
        {
            logger.Debug($"Listing Oculus manifests");

            string[] fileEntries = Directory.GetFiles($@"{oculusBasePath}\Manifests\");

            if (!fileEntries.Any())
            {
                logger.Info($"No Oculus game manifests found");
            }

            var manifests = new List<OculusManifest>();

            foreach (string fileName in fileEntries.Where(x => x.EndsWith(".json")))
            {
                try
                {
                    if (fileName.EndsWith("_assets.json"))
                    {
                        // not interested in the asset json files
                        continue;
                    }

                    var json = File.ReadAllText(fileName);
                    
                    var manifest = OculusManifest.Parse(json);

                    manifests.Add(manifest);
                }
                catch (Exception ex)
                {
                    logger.Error($"Exception while processing manifest ({fileName}) : {ex}");
                }
            }

            return manifests;
        }
    }
}