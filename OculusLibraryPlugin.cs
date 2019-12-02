using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace OculusLibrary
{
    public partial class OculusLibraryPlugin : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("77346DD6-B0CC-4F7D-80F0-C1D138CCAE58");

        public override string Name { get; } = "Oculus Library";

        private JavaScriptSerializer serialiser;

        public OculusLibraryPlugin(IPlayniteAPI api) : base(api)
        {
            serialiser = new JavaScriptSerializer();
        }

        public override IEnumerable<GameInfo> GetGames()
        {
            var gameInfos = new List<GameInfo>();

            var oculusBasePath = GetOculusBasePath();

            using (var view = PlayniteApi.WebViews.CreateOffscreenView())
            {
                foreach (var manifest in GetOculusAppManifests(oculusBasePath))
                {
                    var executableFullPath = $@"{oculusBasePath}Software\Software\{manifest.CanonicalName}\{manifest.LaunchFile}";

                    // set a default name
                    var name = Path.GetFileNameWithoutExtension(executableFullPath);

                    var icon = $@"{oculusBasePath}CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg";

                    if (!File.Exists(icon))
                    {
                        icon = executableFullPath;
                    }

                    var backgroundImage = $@"{oculusBasePath}CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_landscape_image_large.png";

                    if (!File.Exists(backgroundImage))
                    {
                        backgroundImage = string.Empty;
                    }

                    name = TryResolveNameFromAppId(view, manifest.AppId) ?? name;

                    gameInfos.Add(new GameInfo
                    {
                        Name = name,
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
                }
            }

            return gameInfos;
        }

        private string TryResolveNameFromAppId(IWebView view, string appId)
        {
            // get the application id's and smash into this; (IWebView ?)
            // robo recall 1081190428622821
            // https://www.oculus.com/experiences/rift/<appid>/

            view.NavigateAndWait($"https://www.oculus.com/experiences/rift/{appId}/");
            var source = view.GetPageSource();

            // get the json block from the source which contains the games meta data
            /*
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("<script type=\"application/ld+json\">(.*)</script>");
            var json = regex.Match(source);

            var manifest = serialiser.Deserialize<OculusWebsiteJson>(json.Value);

            return manifest?.Name;
            */

            return null;
        }

        internal IEnumerable<OculusManifest> GetOculusAppManifests(string oculusBasePath)
        {
            string[] fileEntries = Directory.GetFiles($@"{oculusBasePath}Software\Manifests\");
            
            foreach (string fileName in fileEntries.Where(x => x.EndsWith(".json")))
            {
                var json = File.ReadAllText(fileName);
                var manifest = serialiser.Deserialize<OculusManifest>(json);

                manifest.LaunchFile = manifest.LaunchFile.Replace("/", @"\");

                yield return manifest;
            }
        }

        internal static string GetOculusBaseFromRegistry(RegistryView platformView)
        {
            // HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment\OculusBase
            RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, platformView);
            var oculusBase = rootKey
                .OpenSubKey(@"SYSTEM\ControlSet001\Control\Session Manager\Environment")
                .GetValue("OculusBase");

            if (oculusBase == null)
            {
                return string.Empty;
            }

            return oculusBase.ToString();
        }

        internal static string GetOculusBasePath()
        {
            var resultPath = GetOculusBaseFromRegistry(RegistryView.Registry64);

            if (string.IsNullOrEmpty(resultPath))
            {
                resultPath = GetOculusBaseFromRegistry(RegistryView.Registry32);
            }

            return resultPath;
        }
    }
}
