using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace OculusLibrary.DataExtraction
{
    public class OculusWebsiteScraper
    {
        private readonly JavaScriptSerializer serialiser;
        private readonly ILogger logger;

        public OculusWebsiteScraper(ILogger logger)
        {            
            serialiser = new JavaScriptSerializer();
            this.logger = logger;
        }

        public OculusWebsiteJson ScrapeDataForApplicationId(IWebView view, string appId)
        {
            logger.Debug($"Trying to scrape {appId}");

            // robo recall 1081190428622821
            try
            {
                view.NavigateAndWait($"https://www.oculus.com/experiences/rift/{appId}/");
                var source = view.GetPageSource();

                // get the json block from the source which contains the games meta data

                Regex regex = new Regex(@"<script type=""application\/ld\+json"">([\s\S]*?)<\/script>");
                var json = regex.Match(source);

                if (json == null)
                {
                    logger.Error($"json file was null");
                    return null;
                }
                if (json.Groups.Count < 2)
                {
                    logger.Error($"json had {json.Groups.Count} regex match groups- was expecting 2 or more");
                    return null;
                }

                var manifest = serialiser.Deserialize<OculusWebsiteJson>(json.Groups[1].Value);

                return manifest;
            }
            catch (Exception ex)
            {
                logger.Error($"Exception trying to scrape {appId} : {ex}");
                return null;
            }
        }
    }
}
