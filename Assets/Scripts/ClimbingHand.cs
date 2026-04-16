using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbingHand : MonoBehaviour
{
    public HandGrabDetector detector;
    public InputActionProperty gripAction;

    public bool isGrabbing = false;
    public GrabPoint grabbedPoint;
    public Vector3 lastHandPosition;

    private bool prevPressed = false;

    private void OnEnable()
    {
        gripAction.action?.Enable();
    }

    private void OnDisable()
    {
        gripAction.action?.Disable();
    }

    private void Update()
    {
        float gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;
        bool pressed = gripValue > 0.5f;

        if (pressed && !prevPressed)
        {
            Debug.Log($"{gameObject.name} Grip Pressed");
            StartGrab();
        }
        else if (!pressed && prevPressed)
        {
            Debug.Log($"{gameObject.name} Grip Released");
            EndGrab();
        }

        prevPressed = pressed;
    }

    public void StartGrab()
    {
        if (detector == null)
        {
            Debug.Log("Detector 없음");
            return;
        }

        if (detector.currentPoint == null)
        {
            Debug.Log("currentPoint 없음");
            return;
        }

        grabbedPoint = detector.currentPoint;
        grabbedPoint.occupied = true;
        isGrabbing = true;
        lastHandPosition = transform.position;

        Debug.Log($"{gameObject.name} Grab 시작: {grabbedPoint.name}");
    }

    public void EndGrab()
    {
        if (grabbedPoint != null)
            grabbedPoint.occupied = false;

        isGrabbing = false;
        grabbedPoint = null;
    }
}