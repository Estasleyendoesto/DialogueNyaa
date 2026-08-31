using System;
using TMPro;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public sealed class DialogueTypewriter : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField]
        private TMP_Text textComponent;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float secondsPerCharacter = 0.03f;

        [SerializeField, Min(0f)]
        private float shortPause = 0.08f;

        [SerializeField, Min(0f)]
        private float longPause = 0.20f;

        public event Action OnCompleted;
        public event Action<char> OnCharacter;

        private float _dialogueSpeed = 1f;
        private float _voiceSpeed = 1f;

        private float _timer;
        private float _extraPause;

        private int _visibleCharacters;
        private int _totalCharacters;

        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        private float CurrentSpeed =>
            _dialogueSpeed * _voiceSpeed;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Play(string content)
        {
            textComponent.text = content ?? string.Empty;
            textComponent.maxVisibleCharacters = 0;
            textComponent.ForceMeshUpdate();

            _visibleCharacters = 0;
            _totalCharacters =
                textComponent.textInfo.characterCount;

            _timer = 0f;
            _extraPause = 0f;

            if (_totalCharacters == 0 ||
                CurrentSpeed <= 0f ||
                secondsPerCharacter <= 0f)
            {
                Finish();
                return;
            }

            _isPlaying = true;
        }
        
        public void SetTextInstantly(string content)
        {
            _isPlaying = false;

            _timer = 0f;
            _extraPause = 0f;
            _visibleCharacters = 0;
            _totalCharacters = 0;

            textComponent.text = content ?? string.Empty;
            textComponent.maxVisibleCharacters = int.MaxValue;
            textComponent.ForceMeshUpdate();
        }

        public void Skip()
        {
            if (_isPlaying)
                Finish();
        }

        public void Stop()
        {
            _isPlaying = false;
            _timer = 0f;
            _extraPause = 0f;

            textComponent.maxVisibleCharacters =
                int.MaxValue;
        }

        public void SetDialogueSpeed(float multiplier)
        {
            _dialogueSpeed = multiplier;
        }

        public void SetVoiceSpeed(float multiplier)
        {
            _voiceSpeed = multiplier;
        }

        public void ResetSpeed()
        {
            _dialogueSpeed = 1f;
            _voiceSpeed = 1f;
        }

        private void Tick(float deltaTime)
        {
            if (!_isPlaying)
                return;

            if (CurrentSpeed <= 0f)
            {
                Finish();
                return;
            }

            _timer += deltaTime;

            float step = GetCurrentStep();

            while (_timer >= step &&
                   _visibleCharacters < _totalCharacters)
            {
                _timer -= step;

                RevealNextCharacter();

                _extraPause =
                    GetPauseAfter(_visibleCharacters - 1);

                step = GetCurrentStep();
            }

            textComponent.maxVisibleCharacters =
                _visibleCharacters;

            if (_visibleCharacters >= _totalCharacters)
                Finish();
        }
        
        private void RevealNextCharacter()
        {
            _visibleCharacters++;

            char character =
                GetCharacter(_visibleCharacters - 1);

            if (!char.IsWhiteSpace(character))
                OnCharacter?.Invoke(character);
        }

        private float GetCurrentStep()
        {
            return (secondsPerCharacter + _extraPause) /
                   CurrentSpeed;
        }

        private float GetPauseAfter(int index)
        {
            if (index >= _totalCharacters - 1)
                return 0f;

            char current = GetCharacter(index);
            char next = GetCharacter(index + 1);

            // En "..." solo se aplica la pausa al último punto.
            if (current == next)
                return 0f;

            switch (current)
            {
                case ',':
                case ';':
                case ':':
                    return shortPause;

                case '.':
                    return IsDecimalPoint(index, next)
                        ? 0f
                        : longPause;

                case '?':
                case '!':
                case '…':
                    return longPause;

                default:
                    return 0f;
            }
        }

        private bool IsDecimalPoint(int index, char next)
        {
            if (char.IsDigit(next))
                return true;

            if (index <= 0)
                return false;

            return char.IsDigit(GetCharacter(index - 1));
        }

        private char GetCharacter(int index)
        {
            return textComponent
                .textInfo
                .characterInfo[index]
                .character;
        }

        private void Finish()
        {
            _visibleCharacters = _totalCharacters;
            _extraPause = 0f;
            _isPlaying = false;

            textComponent.maxVisibleCharacters =
                int.MaxValue;

            OnCompleted?.Invoke();
        }
    }
}