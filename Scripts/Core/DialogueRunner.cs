using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Recorre y ejecuta las instrucciones de un diálogo.
    /// No conoce Unity ni su representación visual.
    /// </summary>
    public sealed class DialogueRunner
    {
        private enum TimerKind
        {
            None,
            Wait,
            Automatic
        }

        public Action<DialogueLine> OnDialogLine;
        public Action<List<ChoiceOption>> OnChoices;
        public Action<float> OnSpeedChanged;
        public Action<DialoguePlaybackMode> OnPlaybackModeChanged;
        public Action OnStoryEnd;

        private DialogueProgram _program;
        private int _programCounter = -1;
        private int _runId;

        private DialogueRunnerState _state =
            DialogueRunnerState.Stopped;

        private DialoguePlaybackMode _playbackMode =
            DialoguePlaybackMode.Manual;

        private bool _isPaused;

        private float _timer;
        private TimerKind _timerKind;

        private float _autoPlayDelay = 0.5f;
        private float _autoOnceDelay = -1f;

        private float _dialogueSpeed = 1f;
        private float _skipSpeed = 20f;
        private float _skipDelay = 0.02f;

        public DialogueRunnerState State => _state;
        public DialoguePlaybackMode PlaybackMode => _playbackMode;

        public bool IsPaused => _isPaused;
        public int RunId => _runId;

        public bool IsWaitingForView =>
            _state == DialogueRunnerState.WaitingForView;

        public bool IsWaitingForContinue =>
            _state == DialogueRunnerState.WaitingForContinue;

        public bool IsWaitingForTimer =>
            _state == DialogueRunnerState.WaitingForTimer;

        public bool IsWaitingForChoice =>
            _state == DialogueRunnerState.WaitingForChoice;

        public void Play(DialogueProgram program, string startNode = "start")
        {
            _runId++;

            _program = program;
            _programCounter = program.Labels[startNode];

            _state = DialogueRunnerState.Running;
            _playbackMode = DialoguePlaybackMode.Manual;

            _isPaused = false;

            _timer = 0f;
            _timerKind = TimerKind.None;

            _autoPlayDelay = 0.5f;
            _autoOnceDelay = -1f;

            _dialogueSpeed = 1f;

            OnSpeedChanged?.Invoke(_dialogueSpeed);
            OnPlaybackModeChanged?.Invoke(_playbackMode);

            Continue();
        }

        // Límite de instrucciones "no bloqueantes" (Jump/If/Auto/Speed/...)
        // que se pueden encadenar sin ceder el control en un solo Continue().
        // Es una válvula de seguridad: un guion con un ciclo de ':: jump'
        // sin ningún Say/Choice/Command/End en medio jamás debería existir,
        // pero si ocurre, esto evita colgar Unity por completo.
        private const int MaxInstructionsPerContinue = 100_000;

        public void Continue()
        {
            if (!CanContinue())
                return;

            _isPaused = false;

            _timer = 0f;
            _timerKind = TimerKind.None;
            _state = DialogueRunnerState.Running;

            int guard = 0;

            while (HasInstruction())
            {
                if (++guard > MaxInstructionsPerContinue)
                {
                    // La única línea que conecta co Unity
                    // Tras quitarla, el motor puede usarse en otros contextos sin Unity.
                    Debug.LogError(
                        "[DialogueRunner] Se superó el límite de " +
                        $"{MaxInstructionsPerContinue} instrucciones en un solo " +
                        "Continue(). Probablemente hay un ciclo de ':: jump' sin " +
                        "ningún Say/Choice/Command/End en medio. Deteniendo la " +
                        "historia para no colgar el juego.");

                    EndStory();
                    return;
                }

                Instruction instruction =
                    _program.Code[_programCounter++];

                switch (instruction.Op)
                {
                    case OpCode.Say:
                        ShowLine(instruction.Line);
                        return;

                    case OpCode.Choice:
                        ShowChoices(instruction.Options);
                        return;

                    case OpCode.Jump:
                        _programCounter = instruction.Target;
                        break;

                    case OpCode.JumpIfFalse:
                        JumpIfFalse(instruction);
                        break;

                    case OpCode.Wait:
                        if (ExecuteWait(instruction.Seconds))
                            return;
                        break;

                    case OpCode.Auto:
                        SetAutoPlayFromInstruction(
                            instruction.Seconds);
                        break;

                    case OpCode.AutoOnce:
                        _autoOnceDelay = instruction.Seconds;
                        break;

                    case OpCode.Speed:
                        SetDialogueSpeed(
                            instruction.SpeedMultiplier);
                        break;

                    case OpCode.Command:
                        if (ExecuteCommand(instruction))
                            return;
                        break;

                    case OpCode.End:
                        EndStory();
                        return;
                }
            }

            EndStory();
        }

        /// <summary>
        /// La View avisa de que terminó de representar la línea.
        /// </summary>
        public void ViewReady()
        {
            if (_state != DialogueRunnerState.WaitingForView)
                return;

            float autoOnceDelay = ConsumeAutoOnce();

            if (_playbackMode == DialoguePlaybackMode.Skip)
            {
                ConsumeFollowingWait();
                StartTimer(_skipDelay, TimerKind.Automatic);
                return;
            }

            if (TryConsumeFollowingWait(out float waitDelay))
            {
                if (!StartTimer(waitDelay, TimerKind.Wait))
                    Continue();

                return;
            }

            if (autoOnceDelay >= 0f)
            {
                if (!StartTimer(
                        autoOnceDelay,
                        TimerKind.Automatic))
                {
                    Continue();
                }

                return;
            }

            if (_playbackMode == DialoguePlaybackMode.AutoPlay)
            {
                if (!StartTimer(
                        _autoPlayDelay,
                        TimerKind.Automatic))
                {
                    Continue();
                }

                return;
            }

            _state = DialogueRunnerState.WaitingForContinue;
        }

        public void Tick(float deltaTime)
        {
            if (_isPaused ||
                _state != DialogueRunnerState.WaitingForTimer)
            {
                return;
            }

            _timer -= deltaTime;

            if (_timer > 0f)
                return;

            _timer = 0f;
            _timerKind = TimerKind.None;
            _state = DialogueRunnerState.Running;

            Continue();
        }

        public void Select(ChoiceOption option)
        {
            if (_state != DialogueRunnerState.WaitingForChoice ||
                option == null)
            {
                return;
            }

            _programCounter = option.Target;
            _state = DialogueRunnerState.Running;

            Continue();
        }

        public void StartAutoPlay(float delay = 0.5f)
        {
            if (!CanChangePlaybackMode())
                return;

            _autoPlayDelay = Math.Max(0f, delay);

            SetPlaybackMode(DialoguePlaybackMode.AutoPlay);
            ContinueAutomaticallyIfReady(_autoPlayDelay);
        }

        public void StartSkip(float speed = 20f, float delay = 0.02f)
        {
            if (!CanChangePlaybackMode())
                return;

            _skipSpeed = Math.Max(1f, speed);
            _skipDelay = Math.Max(0.001f, delay);

            SetPlaybackMode(DialoguePlaybackMode.Skip);

            if (_state == DialogueRunnerState.WaitingForContinue ||
                _state == DialogueRunnerState.WaitingForTimer)
            {
                StartTimer(
                    _skipDelay,
                    TimerKind.Automatic);
            }
        }

        public void StopAutomaticPlayback()
        {
            SetPlaybackMode(DialoguePlaybackMode.Manual);

            if (_state == DialogueRunnerState.WaitingForTimer &&
                _timerKind == TimerKind.Automatic)
            {
                _timer = 0f;
                _timerKind = TimerKind.None;
                _state = DialogueRunnerState.WaitingForContinue;
            }
        }

        public void ToggleAutoPlay(float delay = 0.5f)
        {
            if (_playbackMode == DialoguePlaybackMode.AutoPlay)
                StopAutomaticPlayback();
            else
                StartAutoPlay(delay);
        }

        public void ToggleSkip(float speed = 20f, float delay = 0.02f)
        {
            if (_playbackMode == DialoguePlaybackMode.Skip)
                StopAutomaticPlayback();
            else
                StartSkip(speed, delay);
        }

        public void Pause()
        {
            if (_state != DialogueRunnerState.Stopped)
                _isPaused = true;
        }
        
        public void Resume()
        {
            _isPaused = false;
        }

        public void Stop()
        {
            _runId++;

            _program = null;
            _programCounter = -1;

            _state = DialogueRunnerState.Stopped;
            _playbackMode = DialoguePlaybackMode.Manual;

            _isPaused = false;

            _timer = 0f;
            _timerKind = TimerKind.None;

            _autoOnceDelay = -1f;
            _dialogueSpeed = 1f;

            OnPlaybackModeChanged?.Invoke(_playbackMode);
        }

        private bool CanContinue()
        {
            switch (_state)
            {
                case DialogueRunnerState.Running:
                case DialogueRunnerState.WaitingForContinue:
                case DialogueRunnerState.WaitingForTimer:
                    return true;

                default:
                    return false;
            }
        }

        private bool CanChangePlaybackMode()
        {
            return _state != DialogueRunnerState.Stopped &&
                   _state != DialogueRunnerState.WaitingForChoice;
        }

        private bool HasInstruction()
        {
            return _program != null &&
                   _programCounter >= 0 &&
                   _programCounter < _program.Code.Count;
        }

        private void ShowLine(DialogueLine line)
        {
            _state = DialogueRunnerState.WaitingForView;
            OnDialogLine?.Invoke(line);
        }

        private void ShowChoices(ChoiceOption[] options)
        {
            // AutoPlay y Skip siempre se detienen ante una decisión.
            SetPlaybackMode(DialoguePlaybackMode.Manual);

            _autoOnceDelay = -1f;
            _timer = 0f;
            _timerKind = TimerKind.None;

            _state = DialogueRunnerState.WaitingForChoice;

            OnChoices?.Invoke(FilterChoices(options));
        }

        private void JumpIfFalse(Instruction instruction)
        {
            bool result =
                DialogueContext.EvaluateCondition(instruction.Arg);

            if (!result)
                _programCounter = instruction.Target;
        }

        private bool ExecuteWait(float seconds)
        {
            // Skip ignora las esperas del guion.
            if (_playbackMode == DialoguePlaybackMode.Skip)
                return false;

            return StartTimer(seconds, TimerKind.Wait);
        }

        private void SetAutoPlayFromInstruction(float delay)
        {
            if (delay < 0f)
            {
                SetPlaybackMode(DialoguePlaybackMode.Manual);
                return;
            }

            _autoPlayDelay = delay;
            SetPlaybackMode(DialoguePlaybackMode.AutoPlay);
        }

        private void SetDialogueSpeed(float multiplier)
        {
            _dialogueSpeed = multiplier;
            PublishEffectiveSpeed();
        }

        private void SetPlaybackMode(
            DialoguePlaybackMode mode)
        {
            if (_playbackMode == mode)
                return;

            _playbackMode = mode;

            PublishEffectiveSpeed();
            OnPlaybackModeChanged?.Invoke(mode);
        }

        private void PublishEffectiveSpeed()
        {
            float speed =
                _playbackMode == DialoguePlaybackMode.Skip
                    ? _skipSpeed
                    : _dialogueSpeed;

            OnSpeedChanged?.Invoke(speed);
        }

        private void ContinueAutomaticallyIfReady(float delay)
        {
            if (_state != DialogueRunnerState.WaitingForContinue)
                return;

            if (!StartTimer(delay, TimerKind.Automatic))
                Continue();
        }

        private bool ExecuteCommand(Instruction instruction)
        {
            int runId = _runId;

            DialogueContext.ExecuteCommand(
                instruction.Arg,
                instruction.Args);

            return _isPaused ||
                   runId != _runId ||
                   _state == DialogueRunnerState.Stopped;
        }

        private bool StartTimer(float seconds, TimerKind timerKind)
        {
            if (seconds <= 0f)
                return false;

            _timer = seconds;
            _timerKind = timerKind;
            _state = DialogueRunnerState.WaitingForTimer;

            return true;
        }

        private float ConsumeAutoOnce()
        {
            float delay = _autoOnceDelay;
            _autoOnceDelay = -1f;

            return delay;
        }

        private bool TryConsumeFollowingWait(out float seconds)
        {
            seconds = 0f;

            if (!HasInstruction())
                return false;

            Instruction next =
                _program.Code[_programCounter];

            if (next.Op != OpCode.Wait)
                return false;

            _programCounter++;
            seconds = next.Seconds;

            return true;
        }

        private void ConsumeFollowingWait()
        {
            if (!HasInstruction())
                return;

            if (_program.Code[_programCounter].Op == OpCode.Wait)
                _programCounter++;
        }

        private List<ChoiceOption> FilterChoices(ChoiceOption[] options)
        {
            var validChoices =
                new List<ChoiceOption>(options.Length);

            foreach (ChoiceOption option in options)
            {
                bool isValid =
                    string.IsNullOrEmpty(option.Condition) ||
                    DialogueContext.EvaluateCondition(
                        option.Condition);

                if (isValid)
                    validChoices.Add(option);
            }

            return validChoices;
        }

        private void EndStory()
        {
            _program = null;
            _programCounter = -1;

            _state = DialogueRunnerState.Stopped;
            _playbackMode = DialoguePlaybackMode.Manual;

            _isPaused = false;

            _timer = 0f;
            _timerKind = TimerKind.None;

            _autoOnceDelay = -1f;
            _dialogueSpeed = 1f;

            OnPlaybackModeChanged?.Invoke(_playbackMode);
            OnStoryEnd?.Invoke();
        }
    }
}