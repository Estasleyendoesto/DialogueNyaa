using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Vista mínima para diálogos ambientales de fondo (burbujas de NPCs).
    /// Avanza sola (AutoPlay) y no soporta choices: pensada para vivir en
    /// un Canvas world-space anclado al NPC, sin botones ni input del jugador.
    ///
    /// Cada NPC que habla de fondo debería tener su propio DialogueSystem +
    /// SpeechBubbleView, y ser disparado directamente por su propio script
    /// (ej. npcDialogueSystem.Play(asset, node, bubblePrefab)), NUNCA a través
    /// del bus estático DialogueEvents que usa el diálogo principal — así
    /// pueden convivir varios a la vez sin pisarse entre ellos.
    /// </summary>
    public sealed class SpeechBubbleView : MonoBehaviour, IDialogueView
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject bubblePanel;

        [Header("Presenter")]
        [SerializeField]
        private DialogueLinePresenter linePresenter;

        [Tooltip(
            "Segundos que la burbuja se queda visible tras terminar de " +
            "escribir una línea, antes de pasar a la siguiente.")]
        [SerializeField, Min(0f)]
        private float holdDelay = 1.5f;

        private DialogueSystem _system;

        private void Awake()
        {
            if (linePresenter != null)
                linePresenter.OnReadyToContinue += HandleReadyToContinue;
        }

        private void OnDestroy()
        {
            if (linePresenter != null)
                linePresenter.OnReadyToContinue -= HandleReadyToContinue;
        }

        public void Initialize(DialogueSystem dialogueSystem)
        {
            _system = dialogueSystem;
        }

        public void ShowLine(DialogueLine line)
        {
            // DialogueRunner ya sabe hacer auto-avance (AutoPlay);
            // solo hay que pedírselo, no hace falta reimplementar el ritmo aquí.
            _system?.StartAutoPlay(holdDelay);

            if (bubblePanel != null)
                bubblePanel.SetActive(true);

            linePresenter?.Show(line);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            // Charla ambiental: no hay jugador esperando ni botones que
            // mostrar. Si el guion trae un choice por error, mejor cortar
            // limpio que quedarse colgado en WaitingForChoice para siempre.
            Debug.LogWarning(
                "[SpeechBubbleView] El guion presenta un choice, pero esta " +
                "vista no soporta elecciones (es solo charla ambiental). " +
                "Usa SimpleDialogueView (o una vista con DialogueChoicePresenter) " +
                "si el NPC necesita ser interactivo.");

            _system?.Stop();
        }

        public void ChangeSpeed(float multiplier)
        {
            linePresenter?.ChangeSpeed(multiplier);
        }

        public void Close()
        {
            linePresenter?.Clear();

            if (bubblePanel != null)
                bubblePanel.SetActive(false);
        }

        private void HandleReadyToContinue()
        {
            _system?.ViewReady();
        }
    }
}
