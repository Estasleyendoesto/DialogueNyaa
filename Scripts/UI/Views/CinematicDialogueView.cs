using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Vista minimalista para subtítulos, pensamientos y monólogos interiores.
    /// Sin panel opaco ni botones pesados.
    /// </summary>
    public sealed class CinematicDialogueView : MonoBehaviour, IDialogueView
    {
        [Header("UI")]
        [SerializeField] private GameObject cinematicPanel;
        [SerializeField] private DialogueLinePresenter linePresenter;

        [Header("Choices (Opcional)")]
        [SerializeField] private DialogueChoicePresenter choicePresenter;

        [Header("Settings")]
        [SerializeField] private bool autoAdvance = false;
        [SerializeField, Min(0f)] private float holdDelay = 2.0f;
        [SerializeField] private bool allowInputAdvance = true;

        private DialogueSystem _system;

        private void Awake()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue += HandleReadyToContinue;

            if (choicePresenter)
                choicePresenter.OnChoiceSelected += HandleChoiceSelected;
        }

        private void OnDestroy()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue -= HandleReadyToContinue;

            if (choicePresenter)
                choicePresenter.OnChoiceSelected -= HandleChoiceSelected;
        }

        private void Update()
        {
            if (choicePresenter && choicePresenter.IsShowingChoices)
                return;

            if (allowInputAdvance && cinematicPanel && cinematicPanel.activeSelf)
            {
                var kb = Keyboard.current;
                var mouse = Mouse.current;
                bool kbPressed = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
                bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

                if (kbPressed || mousePressed)
                {
                    if (linePresenter && linePresenter.IsTypewriterPlaying)
                    {
                        linePresenter.SkipTypewriter();
                        return;
                    }

                    _system?.Continue();
                }
            }
        }

        public void Initialize(DialogueSystem dialogueSystem)
        {
            _system = dialogueSystem;
        }

        public void ShowLine(DialogueLine line)
        {
            choicePresenter?.Clear();

            if (autoAdvance)
                _system?.StartAutoPlay(holdDelay);

            if (cinematicPanel != null)
                cinematicPanel.SetActive(true);

            linePresenter?.Show(line);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            if (choicePresenter != null)
            {
                choicePresenter.Show(choices);
            }
            else
            {
                _system?.Stop();
            }
        }

        public void ChangeSpeed(float multiplier)
        {
            linePresenter?.ChangeSpeed(multiplier);
        }

        public void Close()
        {
            linePresenter?.Clear();
            choicePresenter?.Clear();

            if (cinematicPanel != null)
                cinematicPanel.SetActive(false);
        }

        private void HandleChoiceSelected(ChoiceOption choice)
        {
            choicePresenter?.Clear();
            _system?.SelectChoice(choice);
        }

        private void HandleReadyToContinue()
        {
            _system?.ViewReady();
        }
    }
}
