using System;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public static class DialogueEvents
    {
        public static event Action<TextAsset, string, GameObject> OnPlay;

        public static event Action OnStop;
        public static event Action<object[]> OnReturn;

        public static void RaisePlay(TextAsset storyFile, string startNode = "start", GameObject dialoguePrefab = null)
        {
            OnPlay?.Invoke(storyFile, startNode, dialoguePrefab);
        }

        public static void RaiseStop()
        {
            OnStop?.Invoke();
        }

        public static void RaiseReturn(object[] args)
        {
            OnReturn?.Invoke(args);
        }
    }
}