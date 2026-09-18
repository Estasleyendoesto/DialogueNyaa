using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Vista mínima para diálogos ambientales de fondo (burbujas de NPCs).
    /// Puede ser controlada por DialogueSystem o funcionar de forma 100% autónoma
    /// a través de BubbleSystem sin generar conflictos.
    /// </summary>
    public sealed class SpeechBubbleView : MonoBehaviour, IDialogueView
    {
        [Header("Panel")]
        [SerializeField] private GameObject bubblePanel;

        [Header("Presenter")]
        [SerializeField] private DialogueLinePresenter linePresenter;

        [Tooltip("Segundos que la burbuja se queda visible tras terminar de escribir una línea, antes de pasar a la siguiente.")]
        [SerializeField, Min(0f)] private float holdDelay = 1.5f;

        [Header("World Space Positioning")]
        [Tooltip("Si no se asigna, buscará Camera.main al inicializar.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Si es true, la burbuja siempre rotará orientada hacia la cámara activa.")]
        [SerializeField] private bool billboard = true;

        private DialogueSystem _system;
        private DialogueRunner _localRunner;
        private Transform _followTarget;
        private Vector3 _followOffset;
        private Action _onComplete;
        private Transform _camTransform;

        private void Awake()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue += HandleReadyToContinue;

            ResolveCamera();
        }

        private void OnDestroy()
        {
            if (linePresenter)
                linePresenter.OnReadyToContinue -= HandleReadyToContinue;
        }

        private void Update()
        {
            _localRunner?.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_followTarget)
                transform.position = _followTarget.position + _followOffset;

            if (billboard)
            {
                if (!_camTransform)
                    ResolveCamera();

                if (_camTransform)
                    transform.forward = _camTransform.forward;
            }
        }

        public void Initialize(DialogueSystem dialogueSystem)
        {
            _system = dialogueSystem;
        }

        /// <summary>
        /// Permite inyectar una cámara específica en tiempo de ejecución (útil para multi-cámara o cinemáticas).
        /// </summary>
        public void SetCamera(Camera customCamera)
        {
            targetCamera = customCamera;
            _camTransform = customCamera != null ? customCamera.transform : null;
        }

        /// <summary>
        /// Arranca la reproducción autónoma de la burbuja (usado por BubbleSystem).
        /// </summary>
        public void PlayStandalone(TextAsset story, string startNode, Transform target, Vector3 offset, Action onComplete)
        {
            _followTarget = target;
            _followOffset = offset;
            _onComplete = onComplete;

            DialogueProgram program = DialogueParser.Parse(story.name, story.text);

            _localRunner = new DialogueRunner();
            _localRunner.OnDialogLine += ShowLine;
            _localRunner.OnStoryEnd += HandleStoryEnd;
            _localRunner.Play(program, startNode);
        }

        public void ShowLine(DialogueLine line)
        {
            if (_localRunner != null)
                _localRunner.StartAutoPlay(holdDelay);
            else
                _system?.StartAutoPlay(holdDelay);

            if (bubblePanel)
                bubblePanel.SetActive(true);

            linePresenter?.Show(line);
        }

        public void ShowChoices(List<ChoiceOption> choices)
        {
            Debug.LogWarning("[SpeechBubbleView] El guion presenta un choice, pero esta vista no soporta elecciones.");

            if (_localRunner != null)
                HandleStoryEnd();
            else
                _system?.Stop();
        }

        public void ChangeSpeed(float multiplier)
        {
            linePresenter?.ChangeSpeed(multiplier);
        }

        public void Close()
        {
            linePresenter?.Clear();

            if (bubblePanel)
                bubblePanel.SetActive(false);

            if (_localRunner != null)
            {
                _localRunner.Stop();
                HandleStoryEnd();
            }
        }

        private void HandleReadyToContinue()
        {
            if (_localRunner != null)
                _localRunner.ViewReady();
            else
                _system?.ViewReady();
        }

        private void HandleStoryEnd()
        {
            _onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void ResolveCamera()
        {
            if (!targetCamera)
                targetCamera = Camera.main;

            if (targetCamera)
                _camTransform = targetCamera.transform;
        }
    }
}