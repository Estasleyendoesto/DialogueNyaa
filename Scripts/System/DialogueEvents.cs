using System;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    internal static class DialogueEvents
    {
        public static event Action<TextAsset, string, GameObject> OnPlay;

        public static event Action OnStop;
        public static event Action<object[]> OnReturn;

        public static void Play(TextAsset storyFile, string startNode = "start", GameObject dialoguePrefab = null)
        {
            OnPlay?.Invoke(storyFile, startNode, dialoguePrefab);
        }

        public static void Stop()
        {
            OnStop?.Invoke();
        }

        public static void Return(object[] args)
        {
            OnReturn?.Invoke(args);
        }
    }
}