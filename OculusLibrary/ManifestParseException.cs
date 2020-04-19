using System;

namespace OculusLibrary
{
    [Serializable]
    public class ManifestParseException : Exception
    {
        public ManifestParseException() { }
        public ManifestParseException(string message) : base(message) { }
        public ManifestParseException(string message, Exception inner) : base(message, inner) { }
        protected ManifestParseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}