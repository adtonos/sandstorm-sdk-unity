using System.Collections;
using Sandstorm.Unity;
using UnityEngine;

namespace Sandstorm
{
    public class SandstormCoroutineWrapperUnity<T>: SandstormCoroutineWrapper<T, Coroutine>
    {
        public static SandstormCoroutineWrapperUnity<T> Create(MonoBehaviour monoBehaviour, IEnumerator coroutine)
        {
            var coroutineObject = new SandstormCoroutineWrapperUnity<T>();
            coroutineObject.Coroutine = monoBehaviour.StartCoroutine(coroutineObject.StartCoroutine(coroutine));
            return coroutineObject;
        }
    }
}