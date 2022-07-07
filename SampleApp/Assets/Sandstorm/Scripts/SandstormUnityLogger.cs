using UnityEngine;

namespace Sandstorm
{
    public class SandstormUnityLogger : SandstormLogger
    {
        public void LogDebug(string message)
        {
            Debug.Log(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}