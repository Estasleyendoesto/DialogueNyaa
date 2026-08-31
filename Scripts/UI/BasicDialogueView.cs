using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    public sealed class BasicDialogueView :
        MonoBehaviour,
        IDialogueView
    {
        [Header("UI References")]
        [SerializeField]
        private GameObject dialoguePanel;

        [SerializeField]
        private TMP_Text speakerText;

        [SerializeField]
        private DialogueTypewriter typewriter;

        [Header("Controls")]
        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private Button closeButton;

        private DialogueSystem _system;
        private DialogueLine _currentLine;
        private bool _notifyViewReady;

        private void Awake()
        {
            LocalizationManager.LanguageChanged +=
                RefreshLocalization;

            if (typewriter != null)
            {
                typewriter.OnCompleted +=
                    HandleTypingCompleted;
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(
                    HandleNextClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(
                    HandleCloseClicked);
            }
        }

        private void LateUpdate()
        {
            if (!_notifyViewReady)
                return;

            _notifyViewReady = false;
            _system?.ViewReady();
        }

        private void OnDestroy()
        {
            LocalizationManager.LanguageChanged -=
                RefreshLocalization;

            if (typewriter != null)
            {
                typewriter.OnCompleted -=
                    HandleTypingCompleted;
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(
                    HandleNextClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    HandleCloseClicked);
            }
        }

        public void Initialize(DialogueSystem dialogueSystem)
        {
            _system = dialogueSystem;
        }

        public void ShowLine(DialogueLine line)
        {
            _currentLine = line;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            ShowSpeaker(line);
            ShowMessage(line, restartTypewriter: true);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            // Esta vista básica no maneja opciones/elecciones.
        }

        public void ChangeSpeed(float multiplier)
        {
            typewriter?.SetDialogueSpeed(multiplier);
        }

        public void Close()
        {
            _notifyViewReady = false;
            _currentLine = null;

            if (typewriter != null)
            {
                typewriter.Stop();
                typewriter.ResetSpeed();
            }

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void RefreshLocalization()
        {
            if (_currentLine == null)
                return;

            ShowSpeaker(_currentLine);

            bool restartTypewriter =
                typewriter != null &&
                typewriter.IsPlaying;

            ShowMessage(
                _currentLine,
                restartTypewriter);
        }

        private void ShowSpeaker(DialogueLine line)
        {
            if (speakerText == null)
                return;

            bool hasSpeaker =
                !string.IsNullOrEmpty(line.Speaker);

            speakerText.gameObject.SetActive(hasSpeaker);

            if (!hasSpeaker)
                return;

            speakerText.text = LocalizationManager.Get(
                line.SpeakerKey,
                line.Speaker);
        }

        private void ShowMessage(
            DialogueLine line,
            bool restartTypewriter)
        {
            string localizedText =
                LocalizationManager.Get(
                    line.LocKey,
                    line.Text);

            if (typewriter != null)
            {
                if (restartTypewriter)
                    typewriter.Play(localizedText);
                else
                    typewriter.SetTextInstantly(localizedText);
            }
            else
            {
                _notifyViewReady = true;
            }
        }

        private void HandleTypingCompleted()
        {
            _notifyViewReady = true;
        }

        private void HandleNextClicked()
        {
            if (typewriter != null && typewriter.IsPlaying)
            {
                typewriter.Skip();
                return;
            }

            _system?.Continue();
        }

        private void HandleCloseClicked()
        {
            _system?.Stop();
        }
    }
}
