using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class VR_LeverTrigger : MonoBehaviour
{
    private HingeJoint hinge;
    private GeneratorLeverController generator;
    public UnityEvent onLeverDown;
    private bool isActivated = false;

    private void Start()
    {
        hinge = GetComponent<HingeJoint>();
        generator = GetComponentInParent<GeneratorLeverController>();
    }

    private void Update()
    {
        if (isActivated) return;

        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ActivateLever();
            return;
        }

        if (hinge != null && Mathf.Abs(hinge.angle) >= hinge.limits.max * 0.9f)
        {
            ActivateLever();
        }
    }

    private void ActivateLever()
    {
        isActivated = true;
        Debug.Log("[VR_LeverTrigger] Lever activated.");

        if (generator != null)
        {
            generator.ActivateGenerator();
        }

        onLeverDown.Invoke();
    }
}
