using Newtonsoft.Json;
using System;

namespace OculusLibrary
{
    internal class OculusManifest
    {
        public string AppId { get; set; }
        public string LaunchFile { get; set; }
        public string LaunchParameters { get; set; }
        public string CanonicalName { get; set; }

        public static OculusManifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON string cannot be null and empty");
            }

            var manifest = JsonConvert.DeserializeObject<OculusManifest>(json);

            if (manifest == null)
            {
                throw new ManifestParseException("Could not deserialise json");
            }

            manifest.LaunchFile = manifest?.LaunchFile?.Replace("/", @"\");

            return manifest;
        }
    }
}