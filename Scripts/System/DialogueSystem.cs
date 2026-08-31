using System;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public sealed class DialogueSystem : MonoBehaviour
    {
        [Header("Dialogue UI")]
        [SerializeField]
        private GameObject defaultDialoguePrefab;

        public Action OnStoryEnd;

        private DialogueRunner _runner;

        private IDialogueView _view;
        private GameObject _viewInstance;
        private GameObject _currentPrefab;

        public DialogueRunnerState State => _runner.State;
        public bool IsPaused => _runner.IsPaused;
        public bool IsWaitingForView => _runner.IsWaitingForView;
        public bool IsWaitingForContinue => _runner.IsWaitingForContinue;
        public bool IsWaitingForTimer => _runner.IsWaitingForTimer;
        public bool IsWaitingForChoice => _runner.IsWaitingForChoice;
        public int RunId => _runner.RunId;
        
        public DialoguePlaybackMode PlaybackMode => _runner.PlaybackMode;
        
        public bool IsAutoPlaying => _runner.PlaybackMode == DialoguePlaybackMode.AutoPlay;
        public bool IsSkipping => _runner.PlaybackMode == DialoguePlaybackMode.Skip;

        
        private void Awake()
        {
            _runner = new DialogueRunner();

            _runner.OnDialogLine += ShowLine;
            _runner.OnChoices += ShowChoices;
            _runner.OnSpeedChanged += ChangeSpeed;
            _runner.OnStoryEnd += HandleStoryEnd;
        }

        private void OnEnable()
        {
            DialogueEvents.OnPlay += Play;
            DialogueEvents.OnStop += Stop;
        }

        private void OnDisable()
        {
            DialogueEvents.OnPlay -= Play;
            DialogueEvents.OnStop -= Stop;
        }

        private void OnDestroy()
        {
            if (_runner == null)
                return;

            _runner.OnDialogLine -= ShowLine;
            _runner.OnChoices -= ShowChoices;
            _runner.OnSpeedChanged -= ChangeSpeed;
            _runner.OnStoryEnd -= HandleStoryEnd;
        }

        private void Update()
        {
            _runner.Tick(Time.deltaTime);
        }

        public void Play(
            TextAsset storyFile,
            string startNode = "start",
            GameObject dialoguePrefab = null)
        {
            if (storyFile == null)
            {
                Debug.LogError(
                    "[DialogueSystem] Story TextAsset is null.");

                return;
            }

            DialogueValidator.Result validation =
                DialogueValidator.Validate(storyFile.text);

            foreach (DialogueValidator.Issue warning in validation.Warnings)
            {
                Debug.LogWarning(
                    $"[DialogueValidator] {storyFile.name}: {warning}");
            }

            foreach (DialogueValidator.Issue error in validation.Errors)
            {
                Debug.LogError(
                    $"[DialogueValidator] {storyFile.name}: {error}");
            }

            if (!validation.IsValid)
                return;

            if (!DialogueValidator.HasStartNode(validation, startNode))
            {
                Debug.LogError(
                    $"[DialogueValidator] {storyFile.name}: el nodo inicial '{startNode}' no existe en el guion.");

                return;
            }

            GameObject prefab = dialoguePrefab != null
                ? dialoguePrefab
                : defaultDialoguePrefab;

            if (!InitializeView(prefab))
                return;

            DialogueProgram program =
                DialogueParser.Parse(
                    storyFile.name,
                    storyFile.text);

            _runner.Play(program, startNode);
        }

        public void Continue()
        {
            _runner.Continue();
        }

        public void ViewReady()
        {
            _runner.ViewReady();
        }

        public void SelectChoice(ChoiceOption choice)
        {
            _runner.Select(choice);
        }

        public void Pause()
        {
            _runner.Pause();
        }

        public void Stop()
        {
            _runner.Stop();
            _view?.Close();
        }
        
        public void StartAutoPlay(float delay = 0.5f)
        {
            _runner.StartAutoPlay(delay);
        }

        public void StartSkip(
            float speed = 20f,
            float delay = 0.02f)
        {
            _runner.StartSkip(speed, delay);
        }

        public void StopAutomaticPlayback()
        {
            _runner.StopAutomaticPlayback();
        }

        public void ToggleAutoPlay(float delay = 0.5f)
        {
            _runner.ToggleAutoPlay(delay);
        }

        public void ToggleSkip(
            float speed = 20f,
            float delay = 0.02f)
        {
            _runner.ToggleSkip(speed, delay);
        }

        private void ShowLine(DialogueLine line)
        {
            _view?.ShowLine(line);
        }

        private void ShowChoices(
            System.Collections.Generic.List<ChoiceOption> choices)
        {
            _view?.ShowChoices(choices);
        }

        private void ChangeSpeed(float multiplier)
        {
            _view?.ChangeSpeed(multiplier);
        }

        private void HandleStoryEnd()
        {
            _view?.Close();
            OnStoryEnd?.Invoke();
        }

        private bool InitializeView(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError(
                    "[DialogueSystem] No hay prefab de diálogo asignado.");

                return false;
            }

            if (_currentPrefab == prefab &&
                _viewInstance != null)
            {
                return true;
            }

            DestroyCurrentView();

            _currentPrefab = prefab;
            _viewInstance = Instantiate(prefab, transform);
            _view = _viewInstance.GetComponent<IDialogueView>();

            if (_view == null)
            {
                Debug.LogError(
                    $"[DialogueSystem] El prefab '{prefab.name}' " +
                    "no implementa IDialogueView.");

                DestroyCurrentView();
                return false;
            }

            _view.Initialize(this);
            _view.Close();

            return true;
        }

        private void DestroyCurrentView()
        {
            _view = null;
            _currentPrefab = null;

            if (_viewInstance == null)
                return;

            Destroy(_viewInstance);
            _viewInstance = null;
        }
    }
}