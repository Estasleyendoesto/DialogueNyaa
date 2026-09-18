using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Vista para leer documentos, cartas, notas o carteles en pantalla central.
    /// </summary>
    public sealed class DocumentDialogueView : MonoBehaviour, IDialogueView
    {
        [Header("Panel")]
        [SerializeField] private GameObject documentPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button nextPageButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Choices (Opcional)")]
        [SerializeField] private DialogueChoicePresenter choicePresenter;

        private DialogueSystem _system;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClicked);

            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(HandleNextPageClicked);

            if (choicePresenter != null)
                choicePresenter.OnChoiceSelected += HandleChoiceSelected;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (nextPageButton != null)
                nextPageButton.onClick.RemoveListener(HandleNextPageClicked);

            if (choicePresenter != null)
                choicePresenter.OnChoiceSelected -= HandleChoiceSelected;
        }

        private void Update()
        {
            if (documentPanel && documentPanel.activeSelf)
            {
                var kb = Keyboard.current;
                if (kb == null) return;

                if (kb.escapeKey.wasPressedThisFrame)
                {
                    HandleCloseClicked();
                }
                else if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    HandleNextPageClicked();
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

            if (documentPanel != null)
                documentPanel.SetActive(true);

            if (titleText != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(line.Speaker);
                titleText.gameObject.SetActive(hasTitle);
                if (hasTitle)
                    titleText.text = LocalizationManager.Get(line.SpeakerKey, line.Speaker);
            }

            if (bodyText != null)
            {
                bodyText.text = LocalizationManager.Get(line.LocKey, line.Text);
            }

            _system?.ViewReady();
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            choicePresenter?.Show(choices);
        }

        public void ChangeSpeed(float multiplier) { }

        public void Close()
        {
            choicePresenter?.Clear();

            if (documentPanel != null)
                documentPanel.SetActive(false);
        }

        private void HandleChoiceSelected(ChoiceOption choice)
        {
            choicePresenter?.Clear();
            _system?.SelectChoice(choice);
        }

        private void HandleNextPageClicked()
        {
            if (choicePresenter != null && choicePresenter.IsShowingChoices)
                return;

            _system?.Continue();
        }

        private void HandleCloseClicked()
        {
            _system?.Stop();
        }
    }
}
