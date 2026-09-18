using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    [DefaultExecutionOrder(-99)]
    public sealed class BubbleSystem : MonoBehaviour
    {
        public static BubbleSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool dontDestroyOnLoad = false;

        [Header("Prefab")]
        [SerializeField] private GameObject defaultBubblePrefab;

        private readonly Dictionary<Transform, GameObject> _activeBubbles = new();
        private readonly List<GameObject> _untrackedBubbles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Play(TextAsset storyFile, string startNode = "start", Transform target = null, Vector3 offset = default, GameObject bubblePrefab = null)
        {
            if (storyFile == null)
            {
                Debug.LogError("[BubbleSystem] Story TextAsset es null.");
                return;
            }

            DialogueValidator.Result validation = DialogueValidator.Validate(storyFile.text);

            foreach (var warning in validation.Warnings)
                Debug.LogWarning($"[DialogueValidator] {storyFile.name}: {warning}");

            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                    Debug.LogError($"[DialogueValidator] {storyFile.name}: {error}");
                return;
            }

            if (!DialogueValidator.HasStartNode(validation, startNode))
            {
                Debug.LogError($"[DialogueValidator] {storyFile.name}: el nodo '{startNode}' no existe.");
                return;
            }

            GameObject prefab = bubblePrefab != null ? bubblePrefab : defaultBubblePrefab;
            if (prefab == null)
            {
                Debug.LogError("[BubbleSystem] No hay prefab de burbuja asignado en BubbleSystem ni en la llamada.");
                return;
            }

            // Si el NPC ya tenía un bocadillo activo, lo destruimos para que no se solapen
            if (target != null && _activeBubbles.TryGetValue(target, out GameObject oldBubble) && oldBubble != null)
            {
                Destroy(oldBubble);
                _activeBubbles.Remove(target);
            }

            Vector3 spawnPos = target != null ? target.position + offset : offset;
            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

            if (target != null)
                _activeBubbles[target] = instance;
            else
                _untrackedBubbles.Add(instance);

            var bubbleView = instance.GetComponent<SpeechBubbleView>();
            if (bubbleView != null)
            {
                bubbleView.PlayStandalone(storyFile, startNode, target, offset, () =>
                {
                    if (target != null)
                    {
                        if (_activeBubbles.TryGetValue(target, out GameObject current) && current == instance)
                            _activeBubbles.Remove(target);
                    }
                    else
                    {
                        _untrackedBubbles.Remove(instance);
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[BubbleSystem] El prefab '{prefab.name}' no tiene componente SpeechBubbleView.");
            }
        }

        public void Stop(Transform target)
        {
            if (target != null && _activeBubbles.TryGetValue(target, out GameObject bubble) && bubble != null)
            {
                Destroy(bubble);
                _activeBubbles.Remove(target);
            }
        }

        public void StopAll()
        {
            foreach (var kvp in _activeBubbles)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }

            _activeBubbles.Clear();

            foreach (var bubble in _untrackedBubbles)
            {
                if (bubble != null)
                    Destroy(bubble);
            }

            _untrackedBubbles.Clear();
        }
    }
}
