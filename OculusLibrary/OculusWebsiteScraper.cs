using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace OculusLibrary
{
    public class OculusWebsiteScraper
    {
        private readonly JavaScriptSerializer serialiser;

        public OculusWebsiteScraper()
        {            
            serialiser = new JavaScriptSerializer();
        }

        public OculusWebsiteJson ScrapeDataForApplicationId(IWebView view, string appId)
        {
            // robo recall 1081190428622821

            view.NavigateAndWait($"https://www.oculus.com/experiences/rift/{appId}/");
            var source = view.GetPageSource();

            // get the json block from the source which contains the games meta data

            Regex regex = new Regex(@"<script type=""application\/ld\+json"">([\s\S]*?)<\/script>");
            var json = regex.Match(source);

            if (json == null || json.Groups.Count < 2)
            {
                return null;
            }

            var manifest = serialiser.Deserialize<OculusWebsiteJson>(json.Groups[1].Value);

            return manifest;
        }
    }
}
