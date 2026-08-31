using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    [CreateAssetMenu(fileName = "VoiceBank", menuName = "EEsto/Dialogue Voice Bank")]
    public class DialogueVoiceBank : ScriptableObject
    {
        [Serializable]
        public class Voice
        {
            public string label = "Nuevo";
            public string[] speakers;                            // varios tokens = mismo actor
            public AudioClip clip;
            public Vector2 pitch = new(0.94f, 1.06f);
            [Min(1)] public int blipEvery = 2;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.25f, 3f)] public float speedMult = 1f;
        }

        [Tooltip("Líneas sin hablante")]
        public Voice narrator;

        [Tooltip("Hablante no listado en el banco")]
        public Voice fallback;

        public Voice[] voices;

        private Dictionary<string, Voice> _map;

        public Voice Get(string speaker)
        {
            if (string.IsNullOrEmpty(speaker))
                return Valid(narrator) ? narrator : null;

            Build();
            if (_map.TryGetValue(speaker, out Voice v)) return v;

            return Valid(fallback) ? fallback : null;
        }

        private void Build()
        {
            if (_map != null) return;

            _map = new Dictionary<string, Voice>(StringComparer.OrdinalIgnoreCase);
            if (voices == null) return;

            foreach (Voice v in voices)
            {
                if (v?.speakers == null) continue;

                foreach (string s in v.speakers)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    _map[s.Trim()] = v;
                }
            }
        }

        private static bool Valid(Voice v) => v != null;

        private void OnValidate() => _map = null;   // reconstruye al editar en play mode
    }
}