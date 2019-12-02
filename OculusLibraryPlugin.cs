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
    public class OculusLibraryPlugin : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("77346DD6-B0CC-4F7D-80F0-C1D138CCAE58");

        public override string Name { get; } = "Oculus Library";

        public OculusLibraryPlugin(IPlayniteAPI api) : base(api)
        {
        }

        public override IEnumerable<GameInfo> GetGames()
        {
            var gameInfos = new List<GameInfo>();

            var oculusBasePath = GetOculusBasePath();

            using (var view = PlayniteApi.WebViews.CreateOffscreenView())
            {
                foreach (var manifest in GetOculusAppManifests(oculusBasePath))
                {
                    var executableFullPath = $@"{oculusBasePath}\Software\Software\{manifest.LaunchFile}";

                    // set a default name
                    var name = Path.GetFileNameWithoutExtension(executableFullPath);
                    
                    var icon = $@"{oculusBasePath}\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_square_image.jpg";

                    if (!File.Exists(icon))
                    {
                        icon = $@"{oculusBasePath}\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg";
                    }

                    if (!File.Exists(icon))
                    {
                        icon = executableFullPath;
                    }

                    var backgorundImage = $@"{oculusBasePath}\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\cover_landscape_image_large.png";

                    if (!File.Exists(backgorundImage))
                    {
                        backgorundImage = string.Empty;
                    }


                    // get the application id's and smash into this; (IWebView ?)
                    // robo recall 1081190428622821
                    // https://www.oculus.com/experiences/rift/<appid>/

                    //view.NavigateAndWait($"https://www.oculus.com/experiences/rift/{manifest.AppId}/");
                    //view.GetPageSource();

                    // game icons and assets; 
                    // {oculusBasePath}\CoreData\Software\StoreAssets\{manifest.CanonicalName}_assets\icon_image.jpg

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
                        Icon = executableFullPath,
                        BackgroundImage = backgorundImage
                    });
                }
            }

            return gameInfos;
            /*
            return new List<GameInfo>()
            {

                new GameInfo()
                {
                    Name = "Calculator",
                    GameId = "calc",
                    PlayAction = new GameAction()
                    {
                        Type = GameActionType.File,
                        Path = "calc.exe"
                    },
                    IsInstalled = true,
                    Icon = @"https://playnite.link/applogo.png",
                    BackgroundImage =  @"https://playnite.link/applogo.png"
                }
            };*/
        }

        internal static IEnumerable<OculusManifest> GetOculusAppManifests(string oculusBasePath)
        {
            string[] fileEntries = Directory.GetFiles($@"{oculusBasePath}\Software\Manifests\");
            var serialiser = new JavaScriptSerializer();
            foreach (string fileName in fileEntries.Where(x => x.EndsWith(".json")))
            {
                var json = File.ReadAllText(fileName);
                var manifest = serialiser.Deserialize<OculusManifest>(json);
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
