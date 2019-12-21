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
        private readonly IOculusPathSniffer pathSniffer;
        private readonly OculusWebsiteScraper oculusScraper;
        private readonly ILogger logger;

        public OculusLibraryPlugin(IPlayniteAPI api) : base(api)
        {
            logger = LogManager.GetLogger(); 
            serialiser = new JavaScriptSerializer();
            pathSniffer = new OculusPathSniffer(new RegistryValueProvider(), logger);
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
                foreach (var oculusBasePath in oculusLibraryLocations)
                {
                    logger.Info($"Processing Oculus library location {oculusBasePath}");

                    foreach (var manifest in GetOculusAppManifests(oculusBasePath))
                    {
                        logger.Info($"Processing manifest {manifest.CanonicalName} {manifest.AppId}");

                        try
                        {
                            var executableFullPath = $@"{oculusBasePath}\Software\{manifest.CanonicalName}\{manifest.LaunchFile}";

                            // set a default name
                            var executableName = Path.GetFileNameWithoutExtension(executableFullPath);

                            var icon = $@"{oculusBasePath}\..\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg";

                            if (!File.Exists(icon))
                            {
                                logger.Debug($"Oculus store icon missing from file system- reverting to executable icon");
                                icon = executableFullPath;
                            }

                            var backgroundImage = $@"{oculusBasePath}\..\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_landscape_image_large.png";

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

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        logger.Error($"JSON file is empty ({fileName})");
                        continue;
                    }

                    var manifest = serialiser.Deserialize<OculusManifest>(json);

                    if (manifest == null)
                    {
                        logger.Error($"Could not deserialise json ({fileName})");
                    }

                    manifest.LaunchFile = manifest?.LaunchFile?.Replace("/", @"\");

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