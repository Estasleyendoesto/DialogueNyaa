using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public sealed class DialogueVoicePlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;

        private DialogueVoiceBank.Voice _voice;
        private int _characterCount;

        public void SetVoice(DialogueVoiceBank.Voice voice)
        {
            _voice = voice != null && voice.clip != null
                ? voice
                : null;

            _characterCount = 0;
        }

        public void PlayCharacter(char character)
        {
            if (_voice == null || audioSource == null)
                return;

            int blipEvery = Mathf.Max(1, _voice.blipEvery);

            _characterCount++;

            if (_characterCount % blipEvery != 0)
                return;

            audioSource.pitch = Random.Range(
                _voice.pitch.x,
                _voice.pitch.y);

            audioSource.PlayOneShot(
                _voice.clip,
                _voice.volume);
        }

        public void Stop()
        {
            _voice = null;
            _characterCount = 0;

            if (audioSource != null)
                audioSource.Stop();
        }
    }
}