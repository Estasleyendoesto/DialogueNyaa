using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Vista para llamadas, walkie-talkie o transmisiones de radio en una esquina del HUD.
    /// </summary>
    public sealed class TransmissionDialogueView : MonoBehaviour, IDialogueView
    {
        [Header("Panel")]
        [SerializeField] private GameObject transmissionPanel;
        [SerializeField] private TMP_Text channelText;
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private Button nextButton;

        [Header("Line Presenter")]
        [SerializeField] private DialogueLinePresenter linePresenter;

        [Header("Settings")]
        [SerializeField] private bool autoAdvance = true;
        [SerializeField, Min(0f)] private float holdDelay = 1.5f;
        [SerializeField] private bool allowKeyboardAdvance = true;

        private DialogueSystem _system;

        private void Awake()
        {
            if (linePresenter != null)
                linePresenter.OnReadyToContinue += HandleReadyToContinue;

            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNextClicked);
        }

        private void OnDestroy()
        {
            if (linePresenter != null)
                linePresenter.OnReadyToContinue -= HandleReadyToContinue;

            if (nextButton != null)
                nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        private void Update()
        {
            if (allowKeyboardAdvance && transmissionPanel != null && transmissionPanel.activeSelf)
            {
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                {
                    HandleNextClicked();
                }
            }
        }

        public void Initialize(DialogueSystem dialogueSystem)
        {
            _system = dialogueSystem;
        }

        public void ShowLine(DialogueLine line)
        {
            if (autoAdvance)
                _system?.StartAutoPlay(holdDelay);

            if (transmissionPanel != null)
                transmissionPanel.SetActive(true);

            if (channelText != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(line.Speaker);
                channelText.gameObject.SetActive(hasSpeaker);
                if (hasSpeaker)
                    channelText.text = LocalizationManager.Get(line.SpeakerKey, line.Speaker);
            }

            if (activeIndicator != null)
                activeIndicator.SetActive(true);

            linePresenter?.Show(line);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            // Las transmisiones de fondo se detienen limpiamente si reciben un choice inesperado
            _system?.Stop();
        }

        public void ChangeSpeed(float multiplier)
        {
            linePresenter?.ChangeSpeed(multiplier);
        }

        public void Close()
        {
            linePresenter?.Clear();

            if (activeIndicator != null)
                activeIndicator.SetActive(false);

            if (transmissionPanel != null)
                transmissionPanel.SetActive(false);
        }

        private void HandleReadyToContinue()
        {
            if (activeIndicator != null)
                activeIndicator.SetActive(false);

            _system?.ViewReady();
        }

        private void HandleNextClicked()
        {
            if (linePresenter != null && linePresenter.IsTypewriterPlaying)
            {
                linePresenter.SkipTypewriter();
                return;
            }

            _system?.Continue();
        }
    }
}
