using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Presenta las opciones de un choice: crea/destruye los botones y avisa
    /// por evento cuál fue elegida. No conoce DialogueSystem ni DialogueRunner.
    /// </summary>
    public sealed class DialogueChoicePresenter : MonoBehaviour
    {
        [Header("Choices")]
        [SerializeField]
        private GameObject choicesPanel;

        [SerializeField]
        private Transform choicesContainer;

        [SerializeField]
        private GameObject choiceButtonPrefab;

        /// <summary>Se dispara cuando el jugador elige una opción.</summary>
        public event Action<ChoiceOption> OnChoiceSelected;

        public bool IsShowingChoices { get; private set; }

        private List<ChoiceOption> _currentChoices;

        private void Awake()
        {
            LocalizationManager.LanguageChanged += RefreshLocalization;
        }

        private void OnDestroy()
        {
            LocalizationManager.LanguageChanged -= RefreshLocalization;
        }

        public void Show(List<ChoiceOption> choices)
        {
            _currentChoices = choices;
            Rebuild();
        }

        public void Clear()
        {
            IsShowingChoices = false;
            _currentChoices = null;

            DestroyButtons();

            if (choicesPanel != null)
                choicesPanel.SetActive(false);
        }

        private void RefreshLocalization()
        {
            if (_currentChoices != null)
                Rebuild();
        }

        private void Rebuild()
        {
            DestroyButtons();

            bool hasChoices =
                _currentChoices != null &&
                _currentChoices.Count > 0;

            IsShowingChoices = hasChoices;

            if (choicesPanel != null)
                choicesPanel.SetActive(hasChoices);

            if (!hasChoices ||
                choicesContainer == null ||
                choiceButtonPrefab == null)
            {
                return;
            }

            foreach (ChoiceOption choice in _currentChoices)
                CreateButton(choice);
        }

        private void CreateButton(ChoiceOption choice)
        {
            GameObject buttonObject = Instantiate(
                choiceButtonPrefab,
                choicesContainer);

            TMP_Text buttonText =
                buttonObject.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = LocalizationManager.Get(
                    choice.LocKey,
                    choice.Text);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(
                    () => OnChoiceSelected?.Invoke(choice));
            }
        }

        private void DestroyButtons()
        {
            if (choicesContainer == null)
                return;

            foreach (Transform child in choicesContainer)
            {
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
