using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Orquesta una línea de diálogo y sus choices delegando en
    /// DialogueLinePresenter y DialogueChoicePresenter. No conoce typewriter,
    /// voz ni localización: eso vive en los presenters.
    /// </summary>
    public sealed class SimpleDialogueView :
        MonoBehaviour,
        IDialogueView
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject dialoguePanel;

        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private bool allowKeyboardAdvance = true;

        [Header("Presenters")]
        [SerializeField]
        private DialogueLinePresenter linePresenter;

        [SerializeField]
        private DialogueChoicePresenter choicePresenter;

        private DialogueSystem _system;

        private void Awake()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue += HandleReadyToContinue;

            if (choicePresenter)
                choicePresenter.OnChoiceSelected += HandleChoiceSelected;

            if (nextButton)
                nextButton.onClick.AddListener(HandleNextClicked);

            if (closeButton)
                closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private void OnDestroy()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue -= HandleReadyToContinue;

            if (choicePresenter)
                choicePresenter.OnChoiceSelected -= HandleChoiceSelected;

            if (nextButton)
                nextButton.onClick.RemoveListener(HandleNextClicked);

            if (closeButton)
                closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        private void Update()
        {
            if (allowKeyboardAdvance && dialoguePanel && dialoguePanel.activeSelf)
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
            choicePresenter?.Clear();

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            linePresenter?.Show(line);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            choicePresenter?.Show(choices);
        }

        public void ChangeSpeed(float multiplier)
        {
            linePresenter?.ChangeSpeed(multiplier);
        }

        public void Close()
        {
            linePresenter?.Clear();
            choicePresenter?.Clear();

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void HandleReadyToContinue()
        {
            _system?.ViewReady();
        }

        private void HandleChoiceSelected(ChoiceOption choice)
        {
            choicePresenter?.Clear();
            _system?.SelectChoice(choice);
        }

        private void HandleNextClicked()
        {
            if (choicePresenter != null && choicePresenter.IsShowingChoices)
                return;

            if (linePresenter != null && linePresenter.IsTypewriterPlaying)
            {
                linePresenter.SkipTypewriter();
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