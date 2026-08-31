using System;
using TMPro;

namespace EEsto.DialogueNyaa
{
    // Teletipo por maxVisibleCharacters: una malla, respeta rich text, sin corrutinas.
    public class DialogueSequencer
    {
        private readonly TMP_Text _text;
        private readonly float _baseDelay;

        private float _speed = 1f;        // :: Speed del guion
        private float _voiceSpeed = 1f;   // ritmo del personaje
        private float _timer;
        private float _extraPause;
        private int _visible;
        private int _total;
        private bool _isPlaying;

        public Action OnTypingComplete;
        public Action<char> OnCharacter;

        public float PauseShort = 0.08f;   // , ; :
        public float PauseLong  = 0.20f;   // . ? ! …

        public DialogueSequencer(TMP_Text textComponent, float baseDelay = 0.03f)
        {
            _text = textComponent;
            _baseDelay = baseDelay;
        }

        public bool IsPlaying => _isPlaying;

        private float Rate => _speed * _voiceSpeed;
        private float Step => (_baseDelay + _extraPause) / Rate;

        public void Play(string content)
        {
            _text.maxVisibleCharacters = 0;
            _text.text = content;
            _text.ForceMeshUpdate();

            _total      = _text.textInfo.characterCount;
            _visible    = 0;
            _timer      = 0f;
            _extraPause = 0f;

            if (_total == 0 || Rate <= 0f || _baseDelay <= 0f)
            {
                Finish();
                return;
            }
            _isPlaying = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isPlaying) return;

            if (Rate <= 0f)
            {
                Finish();
                return;
            }

            _timer += deltaTime;

            float step = Step;
            while (_timer >= step && _visible < _total)
            {
                _timer -= step;
                _visible++;

                char c = _text.textInfo.characterInfo[_visible - 1].character;
                if (!char.IsWhiteSpace(c)) OnCharacter?.Invoke(c);

                _extraPause = PauseAfter(_visible - 1);
                step = Step;
            }

            _text.maxVisibleCharacters = _visible;

            if (_visible >= _total) Finish();
        }

        public void Skip()
        {
            if (_isPlaying) Finish();
        }

        // Corta sin avisar (cierre de historia)
        public void Stop()
        {
            _isPlaying = false;
            _text.maxVisibleCharacters = int.MaxValue;
        }

        // 1 = normal, 2 = doble de rápido, 0 = instantáneo
        public void SetSpeed(float multiplier) => _speed = multiplier;

        public void SetVoiceSpeed(float multiplier) => _voiceSpeed = multiplier;

        public void ResetSpeed()
        {
            _speed = 1f;
            _voiceSpeed = 1f;
        }

        private float PauseAfter(int index)
        {
            if (index >= _total - 1) return 0f;

            char c = _text.textInfo.characterInfo[index].character;
            char next = _text.textInfo.characterInfo[index + 1].character;

            if (next == c) return 0f;   // "..." pausa solo al final

            switch (c)
            {
                case ',':
                case ';':
                case ':':
                    return PauseShort;

                case '.':
                    if (char.IsDigit(next)) return 0f;                       // 1.5
                    if (index > 0 && char.IsDigit(_text.textInfo.characterInfo[index - 1].character)) return 0f;
                    return PauseLong;

                case '?':
                case '!':
                case '…':
                    return PauseLong;

                default:
                    return 0f;
            }
        }

        private void Finish()
        {
            _visible = _total;
            _text.maxVisibleCharacters = int.MaxValue;
            _extraPause = 0f;
            _isPlaying = false;
            OnTypingComplete?.Invoke();
        }
    }
}