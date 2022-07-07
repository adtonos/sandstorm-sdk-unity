using System;
using System.IO;
using Sandstorm.Unity;

namespace Sandstorm
{
    public class SandstormAssetsLoader : SandstormUnityAssetsLoader
    {
        private const string Tag = "SandstormAssetsLoader";
        private static readonly string UnityAssetsPath = "Assets/Resources/";

        public StreamReader ReadAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            if (!path.StartsWith(UnityAssetsPath))
            {
                path = $"{UnityAssetsPath}{path}";
            }

            try
            {
                return new StreamReader(path);
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Stream reader failed with exception {e}");
                return null;
            }
        }
    }
}