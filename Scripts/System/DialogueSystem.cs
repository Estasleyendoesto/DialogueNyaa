using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    [DefaultExecutionOrder(-100)]
    public sealed class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private bool dontDestroyOnLoad = false;

        [Header("UI Prefab")]
        [SerializeField] private GameObject defaultDialoguePrefab;

        // ================= Eventos del Ciclo de Vida =================
        public event Action<string, string> OnDialogueStarted; // fileName, startNode
        public event Action OnDialogueEnded;
        public event Action OnDialoguePaused;
        public event Action OnDialogueResumed;
        public event Action<DialogueLine> OnLineDisplayed;
        public event Action<List<ChoiceOption>> OnChoicesPresented;
        public event Action<ChoiceOption> OnChoiceSelected;
        public event Action<DialoguePlaybackMode> OnPlaybackModeChanged;
        public event Action<object[]> OnReturn;
        public event Action<string, string[]> OnCustomEvent;

        private readonly DialogueRunner _runner = new();
        private IDialogueView _view;
        private GameObject _viewInstance;
        private GameObject _currentPrefab;

        // ================= Consultas de Estado =================
        public DialogueRunnerState State => _runner.State;
        public DialoguePlaybackMode PlaybackMode => _runner.PlaybackMode;
        public bool IsRunning => _runner.State != DialogueRunnerState.Stopped;
        public bool IsPaused => _runner.IsPaused;
        public bool IsWaitingForView => _runner.IsWaitingForView;
        public bool IsWaitingForContinue => _runner.IsWaitingForContinue;
        public bool IsWaitingForTimer => _runner.IsWaitingForTimer;
        public bool IsWaitingForChoice => _runner.IsWaitingForChoice;
        public int RunId => _runner.RunId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            HookRunnerEvents();
            DialogueCommands.RegisterDefaults(args 
                => OnReturn?.Invoke(args), (eventName, eventArgs) 
                => OnCustomEvent?.Invoke(eventName, eventArgs)
            );
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            UnhookRunnerEvents();
            DialogueCommands.UnregisterDefaults();
            Instance = null;
        }

        private void Update()
        {
            _runner.Tick(Time.deltaTime);
        }

        // ================= Control de Reproducción =================

        public void Play(TextAsset storyFile, string startNode = "start", GameObject dialoguePrefab = null)
        {
            if (storyFile == null)
            {
                Debug.LogError("[DialogueSystem] Story TextAsset es null.");
                return;
            }

            DialogueValidator.Result validation = DialogueValidator.Validate(storyFile.text);

            foreach (var warning in validation.Warnings)
                Debug.LogWarning($"[DialogueValidator] {storyFile.name}: {warning}");

            foreach (var error in validation.Errors)
                Debug.LogError($"[DialogueValidator] {storyFile.name}: {error}");

            if (!validation.IsValid)
                return;

            if (!DialogueValidator.HasStartNode(validation, startNode))
            {
                Debug.LogError($"[DialogueValidator] {storyFile.name}: el nodo inicial '{startNode}' no existe.");
                return;
            }

            GameObject prefab = dialoguePrefab != null ? dialoguePrefab : defaultDialoguePrefab;
            if (!InitializeView(prefab))
                return;

            DialogueProgram program = DialogueParser.Parse(storyFile.name, storyFile.text);
            _runner.Play(program, startNode);

            OnDialogueStarted?.Invoke(storyFile.name, startNode);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _runner.Stop();
            _view?.Close();
            OnDialogueEnded?.Invoke();
        }

        public void Pause()
        {
            if (!IsRunning || IsPaused) return;

            _runner.Pause();
            OnDialoguePaused?.Invoke();
        }

        public void Resume()
        {
            if (!IsRunning || !IsPaused) return;

            _runner.Resume();
            OnDialogueResumed?.Invoke();
        }

        public void Continue() => _runner.Continue();
        public void ViewReady() => _runner.ViewReady();

        public void SelectChoice(ChoiceOption choice)
        {
            _runner.Select(choice);
            OnChoiceSelected?.Invoke(choice);
        }

        // ================= Modos Automáticos =================

        public void StartAutoPlay(float delay = 0.5f) => _runner.StartAutoPlay(delay);
        public void StartSkip(float speed = 20f, float delay = 0.02f) => _runner.StartSkip(speed, delay);
        public void StopAutomaticPlayback() => _runner.StopAutomaticPlayback();
        public void ToggleAutoPlay(float delay = 0.5f) => _runner.ToggleAutoPlay(delay);
        public void ToggleSkip(float speed = 20f, float delay = 0.02f) => _runner.ToggleSkip(speed, delay);

        // ================= Handlers Internos =================

        private void HookRunnerEvents()
        {
            _runner.OnDialogLine += HandleLine;
            _runner.OnChoices += HandleChoices;
            _runner.OnSpeedChanged += HandleSpeed;
            _runner.OnPlaybackModeChanged += HandlePlaybackMode;
            _runner.OnStoryEnd += HandleEnd;
        }

        private void UnhookRunnerEvents()
        {
            _runner.OnDialogLine -= HandleLine;
            _runner.OnChoices -= HandleChoices;
            _runner.OnSpeedChanged -= HandleSpeed;
            _runner.OnPlaybackModeChanged -= HandlePlaybackMode;
            _runner.OnStoryEnd -= HandleEnd;
        }

        private void HandleLine(DialogueLine line)
        {
            _view?.ShowLine(line);
            OnLineDisplayed?.Invoke(line);
        }

        private void HandleChoices(List<ChoiceOption> choices)
        {
            _view?.ShowChoices(choices);
            OnChoicesPresented?.Invoke(choices);
        }

        private void HandleSpeed(float speed) => _view?.ChangeSpeed(speed);
        private void HandlePlaybackMode(DialoguePlaybackMode mode) => OnPlaybackModeChanged?.Invoke(mode);

        private void HandleEnd()
        {
            _view?.Close();
            OnDialogueEnded?.Invoke();
        }

        // ================= Gestión de UI =================

        private bool InitializeView(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[DialogueSystem] No hay prefab de UI asignado.");
                return false;
            }

            if (_currentPrefab == prefab && _viewInstance != null)
                return true;

            DestroyCurrentView();

            _currentPrefab = prefab;
            _viewInstance = Instantiate(prefab, transform);
            _view = _viewInstance.GetComponent<IDialogueView>();

            if (_view == null)
            {
                Debug.LogError($"[DialogueSystem] El prefab '{prefab.name}' no implementa IDialogueView.");
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

            if (_viewInstance != null)
            {
                Destroy(_viewInstance);
                _viewInstance = null;
            }
        }
    }
}