using System;
using TMPro;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Presenta una única línea de diálogo: speaker, texto/typewriter y voz.
    /// No conoce DialogueSystem ni DialogueRunner: solo avisa por evento
    /// cuando terminó de mostrar la línea y está listo para continuar.
    /// </summary>
    public sealed class DialogueLinePresenter : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField]
        private TMP_Text speakerText;

        [Tooltip("Obligatorio si no hay Typewriter asignado.")]
        [SerializeField]
        private TMP_Text messageText;

        [Header("Optional Typewriter")]
        [SerializeField]
        private DialogueTypewriter typewriter;

        [Header("Optional Voice")]
        [SerializeField]
        private DialogueVoiceBank voiceBank;

        [SerializeField]
        private DialogueVoicePlayer voicePlayer;

        /// <summary>Se dispara cuando la línea terminó de representarse.</summary>
        public event Action OnReadyToContinue;

        private DialogueLine _currentLine;
        private bool _notifyReady;

        public bool IsTypewriterPlaying =>
            typewriter != null && typewriter.IsPlaying;

        private void Awake()
        {
            LocalizationManager.LanguageChanged += RefreshLocalization;

            if (typewriter != null)
            {
                typewriter.OnCompleted += HandleTypingCompleted;
                typewriter.OnCharacter += HandleCharacter;
            }
        }

        private void LateUpdate()
        {
            if (!_notifyReady)
                return;

            _notifyReady = false;
            OnReadyToContinue?.Invoke();
        }

        private void OnDestroy()
        {
            LocalizationManager.LanguageChanged -= RefreshLocalization;

            if (typewriter != null)
            {
                typewriter.OnCompleted -= HandleTypingCompleted;
                typewriter.OnCharacter -= HandleCharacter;
            }
        }

        public void Show(DialogueLine line)
        {
            _currentLine = line;

            ShowSpeaker(line);
            ConfigureVoice(line.Speaker);
            ShowMessage(line, restartTypewriter: true);
        }

        public void SkipTypewriter()
        {
            typewriter?.Skip();
        }

        public void ChangeSpeed(float multiplier)
        {
            typewriter?.SetDialogueSpeed(multiplier);
        }

        public void Clear()
        {
            _notifyReady = false;
            _currentLine = null;

            if (typewriter != null)
            {
                typewriter.Stop();
                typewriter.ResetSpeed();
            }

            voicePlayer?.Stop();
        }

        private void RefreshLocalization()
        {
            if (_currentLine == null)
                return;

            ShowSpeaker(_currentLine);

            bool restartTypewriter =
                typewriter != null &&
                typewriter.IsPlaying;

            ShowMessage(_currentLine, restartTypewriter);
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

                return;
            }

            if (messageText != null)
                messageText.text = localizedText;

            // Sin typewriter, avisamos en el siguiente LateUpdate para no
            // reentrar sincrónicamente en DialogueRunner/DialogueSystem
            // mientras todavía se está procesando ShowLine().
            if (restartTypewriter)
                _notifyReady = true;
        }

        private void ConfigureVoice(string speaker)
        {
            DialogueVoiceBank.Voice voice =
                voiceBank != null
                    ? voiceBank.Get(speaker)
                    : null;

            voicePlayer?.SetVoice(voice);

            if (typewriter == null)
                return;

            float voiceSpeed =
                voice != null
                    ? voice.speedMult
                    : 1f;

            typewriter.SetVoiceSpeed(voiceSpeed);
        }

        private void HandleCharacter(char character)
        {
            voicePlayer?.PlayCharacter(character);
        }

        private void HandleTypingCompleted()
        {
            _notifyReady = true;
        }
    }
}
