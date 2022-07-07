using System;
using System.Collections;
using System.Collections.Generic;
using Sandstorm.Unity;
using UnityEngine;

namespace Sandstorm
{
    public class SandstormDispatcherUnity : MonoBehaviour, SandstormDispatcher
    {
        private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();
        private static readonly Action[] EmptyActions = Array.Empty<Action>();

        void Awake()
        {
            if (FindObjectsOfType<SandstormDispatcherUnity>().Length > 1)
            {
                DestroyImmediate(this);
                return;
            }
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ATSandstormSDK.PauseAd();
            }
        }

        public void EnqueueCoroutine(IEnumerator routine)
        {
            EnqueueCoroutineInternal(routine: routine);
        }

        public void EnqueueMainThread(Action action)
        {
            EnqueueCoroutineInternal(routine: CoroutineWrapper(action));
        }
        private Action[] DequeueActions()
        {
            Action[] actionArray = EmptyActions;
            lock (ExecutionQueue)
            {
                if (ExecutionQueue.Count > 0)
                {
                    actionArray = new Action[ExecutionQueue.Count];
                    ExecutionQueue.CopyTo(actionArray, 0);
                    ExecutionQueue.Clear();
                }
            }
            return actionArray;
        }

        private void DequeueAndRunActions()
        {
            var actionArray = DequeueActions();
            foreach (var action in actionArray)
            {
                action?.Invoke();
            }
        }

        private void Update()
        {
            DequeueAndRunActions();
        }

        private void EnqueueCoroutineInternal(IEnumerator routine)
        {
            lock (ExecutionQueue)
            {
                ExecutionQueue.Enqueue(() =>
                {
                    StartCoroutine(routine);
                });
            }
        }

        IEnumerator CoroutineWrapper(Action callback)
        {
            callback?.Invoke();
            yield return null;
        }

        public IEnumerator CreateCoroutineWaitingTime(long timeSeconds)
        {
            yield return new WaitForSecondsRealtime(timeSeconds);
        }

        public IEnumerator CreateCoroutineWaitUntil(Func<bool> action)
        {
            yield return new WaitUntil(action);
        }

    }
}
