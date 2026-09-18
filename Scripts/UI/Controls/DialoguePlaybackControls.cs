using UnityEngine;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Botones opcionales de AutoPlay/Skip. Totalmente independiente de
    /// IDialogueView: solo necesita una referencia a DialogueSystem y puede
    /// vivir en cualquier parte de la escena, incluso fuera del prefab de
    /// la vista de diálogo.
    /// </summary>
    public sealed class DialoguePlaybackControls : MonoBehaviour
    {
        [SerializeField]
        private DialogueSystem system;

        [Header("Buttons")]
        [SerializeField]
        private Button autoPlayButton;

        [SerializeField]
        private Button skipButton;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float autoPlayDelay = 0.5f;

        [SerializeField, Min(1f)]
        private float skipSpeed = 20f;

        [SerializeField, Min(0.001f)]
        private float skipDelay = 0.02f;

        private void Awake()
        {
            if (autoPlayButton != null)
            {
                autoPlayButton.onClick.AddListener(
                    HandleAutoPlayClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(
                    HandleSkipClicked);
            }
        }

        private void OnDestroy()
        {
            if (autoPlayButton != null)
            {
                autoPlayButton.onClick.RemoveListener(
                    HandleAutoPlayClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(
                    HandleSkipClicked);
            }
        }

        private void HandleAutoPlayClicked()
        {
            system?.ToggleAutoPlay(autoPlayDelay);
        }

        private void HandleSkipClicked()
        {
            system?.ToggleSkip(skipSpeed, skipDelay);
        }
    }
}
