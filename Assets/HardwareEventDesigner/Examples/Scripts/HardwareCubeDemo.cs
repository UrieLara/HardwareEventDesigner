using HardwareEventDesigner.Runtime;
using UnityEngine;

namespace HardwareEventDesigner.Examples
{
    public class HardwareCubeDemo : MonoBehaviour
    {
        [Header("Input Source")]
        [Tooltip("Arrastrá el GameObject con SerialHardwareInputProvider o MockHardwareInputProvider.")]
        [SerializeField] private MonoBehaviour providerBehaviour;

        [Header("Movement")]
        [SerializeField] private string channelIdX = "JoystickX";
        [SerializeField] private string channelIdY = "JoystickY";
        [SerializeField] private float moveRange = 5f;

        [Header("Movement Filter")]
        [SerializeField] private float movementStep = 0.2f;

        [Header("Button")]
        [SerializeField] private string buttonChannelId = "PushButton";
        [SerializeField] private float buttonThreshold = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color colorA = Color.white;
        [SerializeField] private Color colorB = Color.red;

        private IHardwareInputProvider _provider;
        private Renderer _renderer;

        private bool _isColorB;
        private bool _previousButtonState;

        private void Awake()
        {
            _provider = providerBehaviour as IHardwareInputProvider;
            _renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (_provider == null) return;

            UpdateMovement();
            UpdateButton();
        }

        private void UpdateMovement()
        {
            float x = (_provider.GetValue(channelIdX) - 0.5f) * 2f;
            float y = (_provider.GetValue(channelIdY) - 0.5f) * 2f;

            x = Mathf.Round(x / movementStep) * movementStep;
            y = Mathf.Round(y / movementStep) * movementStep;

            transform.localPosition = new Vector3(
                x * moveRange,
                y * moveRange,
                0f
            );
        }

        private void UpdateButton()
        {
            float buttonValue = _provider.GetValue(buttonChannelId);

            bool buttonPressed = buttonValue >= buttonThreshold;

            if (buttonPressed && !_previousButtonState)
            {
                ToggleColor();
            }

            _previousButtonState = buttonPressed;
        }

        private void ToggleColor()
        {
            _isColorB = !_isColorB;

            _renderer.material.color =
                _isColorB ? colorB : colorA;
        }
    }
}