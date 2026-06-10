using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorLeverInteraction : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public GeneratorLeverController generator;

    [Header("Hands")]
    public Transform leftHand;
    public Transform rightHand;
    public InputActionProperty leftGrip;
    public InputActionProperty rightGrip;

    [Header("Lever Setting")]
    public float grabRange = 0.45f;
    public Vector3 localAxis = Vector3.right;
    public float pulledAngle = -70f;
    public float pullDistance = 0.35f;
    public float activateRatio = 0.9f;
    public bool stayDownAfterActivated = true;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private Transform activeHand;
    private Vector3 grabStartHandLocalPos;

    private bool isGrabbed;
    private bool isActivated;

    private void Awake()
    {
        if (pivot == null)
            pivot = transform.parent;

        if (generator == null)
            generator = GetComponentInParent<GeneratorLeverController>();

        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void OnEnable()
    {
        leftGrip.action?.Enable();
        rightGrip.action?.Enable();
    }

    private void Update()
    {
        if (isActivated && stayDownAfterActivated)
        {
            SetLeverRatio(1f);
            return;
        }

        if (!isGrabbed)
        {
            TryStartGrab();
        }
        else
        {
            UpdateGrab();
        }
    }

    private void TryStartGrab()
    {
        if (leftHand != null && IsGripPressed(leftGrip) && IsHandNear(leftHand))
        {
            StartGrab(leftHand);
            return;
        }

        if (rightHand != null && IsGripPressed(rightGrip) && IsHandNear(rightHand))
        {
            StartGrab(rightHand);
            return;
        }
    }

    private void StartGrab(Transform hand)
    {
        activeHand = hand;
        isGrabbed = true;

        grabStartHandLocalPos = pivot.InverseTransformPoint(activeHand.position);

        Debug.Log("[Lever] 레버 잡힘: " + activeHand.name);
    }

    private void UpdateGrab()
    {
        if (activeHand == null)
        {
            EndGrab();
            return;
        }

        bool gripStillPressed =
            activeHand == leftHand ? IsGripPressed(leftGrip) : IsGripPressed(rightGrip);

        if (!gripStillPressed)
        {
            EndGrab();
            return;
        }

        Vector3 currentHandLocalPos = pivot.InverseTransformPoint(activeHand.position);

        float pullAmount = grabStartHandLocalPos.y - currentHandLocalPos.y;
        float ratio = Mathf.Clamp01(pullAmount / pullDistance);

        SetLeverRatio(ratio);

        if (!isActivated && ratio >= activateRatio)
        {
            isActivated = true;
            Debug.Log("[Lever] 발전기 작동");

            if (generator != null)
                generator.ActivateGenerator();

            if (stayDownAfterActivated)
                SetLeverRatio(1f);
        }
    }

    private void EndGrab()
    {
        Debug.Log("[Lever] 레버 놓음");

        isGrabbed = false;
        activeHand = null;

        if (isActivated && stayDownAfterActivated)
            SetLeverRatio(1f);
        else
            SetLeverRatio(0f);
    }

    private bool IsHandNear(Transform hand)
    {
        float distance = Vector3.Distance(hand.position, transform.position);
        return distance <= grabRange;
    }

    private bool IsGripPressed(InputActionProperty action)
    {
        if (action.action == null)
            return false;

        return action.action.ReadValue<float>() > 0.5f;
    }

    private void SetLeverRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        Quaternion deltaRotation =
            Quaternion.AngleAxis(pulledAngle * ratio, localAxis.normalized);

        transform.localPosition = deltaRotation * startLocalPosition;
        transform.localRotation = deltaRotation * startLocalRotation;
    }
}