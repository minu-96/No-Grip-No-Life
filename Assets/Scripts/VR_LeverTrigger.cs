using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class VR_LeverTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HingeJoint hinge;
    [SerializeField] private GeneratorLeverController generator;

    [Header("Activation")]
    [SerializeField] private float activationRatio = 0.9f;
    [SerializeField] private bool allowKeyboardTest = true;

    public UnityEvent onLeverDown;

    private bool isActivated = false;

    private void Start()
    {
        if (hinge == null)
            hinge = GetComponent<HingeJoint>();

        if (generator == null)
            generator = GetComponentInParent<GeneratorLeverController>();

        if (hinge == null)
            Debug.LogError("[VR_LeverTrigger] HingeJoint not found.");

        if (generator == null)
            Debug.LogError("[VR_LeverTrigger] GeneratorLeverController not found.");
    }

    private void Update()
    {
        if (isActivated) return;

        if (allowKeyboardTest &&
            Keyboard.current != null &&
            Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ActivateLever();
            return;
        }

        if (hinge == null) return;

        float maxAngle = Mathf.Abs(hinge.limits.max);
        float minAngle = Mathf.Abs(hinge.limits.min);
        float targetAngle = Mathf.Max(maxAngle, minAngle) * activationRatio;

        if (Mathf.Abs(hinge.angle) >= targetAngle)
        {
            ActivateLever();
        }
    }

    private void ActivateLever()
    {
        if (isActivated) return;

        isActivated = true;

        Debug.Log("[VR_LeverTrigger] Lever activated.");

        if (generator != null)
            generator.ActivateGenerator();

        onLeverDown?.Invoke();
    }
}