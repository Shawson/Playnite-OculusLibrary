using System;

namespace OculusLibrary.OS
{
    public interface IPathNormaliser: IDisposable
    {
        string Normalise(string path);
    }
}