using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorLeverInteraction : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public GeneratorLeverController generator;

    [Header("Hands")]
    public ClimbingHand leftClimbingHand;
    public ClimbingHand rightClimbingHand;
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
    public bool activateWhenGrabbed = false;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private Transform activeHand;
    private Vector3 grabStartHandLocalPos;

    private bool isGrabbed;
    private bool isActivated;

    private void Awake()
    {
        AutoWireReferences();

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
        AutoWireReferences();

        leftGrip.action?.Enable();
        rightGrip.action?.Enable();
    }

    private void Update()
    {
        AutoWireReferences();

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
        if (leftHand != null && IsGripPressed(leftGrip, leftClimbingHand) && IsHandNear(leftHand))
        {
            StartGrab(leftHand);
            return;
        }

        if (rightHand != null && IsGripPressed(rightGrip, rightClimbingHand) && IsHandNear(rightHand))
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

        Debug.Log("[Lever] Grabbed by " + activeHand.name);

        if (activateWhenGrabbed)
        {
            ActivateLever();
        }
    }

    private void UpdateGrab()
    {
        if (activeHand == null)
        {
            EndGrab();
            return;
        }

        bool gripStillPressed = activeHand == leftHand
            ? IsGripPressed(leftGrip, leftClimbingHand)
            : IsGripPressed(rightGrip, rightClimbingHand);

        if (!gripStillPressed)
        {
            EndGrab();
            return;
        }

        Vector3 currentHandLocalPos = pivot.InverseTransformPoint(activeHand.position);

        Vector3 handDelta = currentHandLocalPos - grabStartHandLocalPos;
        float downwardPull = Mathf.Max(0f, -handDelta.y);
        float broadPull = Mathf.Max(Mathf.Abs(handDelta.x), Mathf.Abs(handDelta.z));
        float pullAmount = Mathf.Max(downwardPull, broadPull);
        float ratio = Mathf.Clamp01(pullAmount / pullDistance);

        SetLeverRatio(ratio);

        if (!isActivated && ratio >= activateRatio)
        {
            ActivateLever();
        }
    }

    private void ActivateLever()
    {
        if (isActivated)
            return;

        isActivated = true;
        Debug.Log("[Lever] Generator lever activated");

        if (generator == null)
            generator = GetComponentInParent<GeneratorLeverController>();

        if (generator != null)
            generator.ActivateGenerator();
        else
            Debug.LogError("[Lever] GeneratorLeverController reference is missing.");

        if (stayDownAfterActivated)
            SetLeverRatio(1f);
    }

    private void EndGrab()
    {
        Debug.Log("[Lever] Released");

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

    private bool IsGripPressed(InputActionProperty action, ClimbingHand fallbackHand)
    {
        InputAction gripAction = action.action;
        if ((gripAction == null || gripAction.bindings.Count == 0) &&
            fallbackHand != null &&
            fallbackHand.gripAction.action != null)
        {
            gripAction = fallbackHand.gripAction.action;
        }

        if (gripAction == null)
            return false;

        gripAction.Enable();
        return gripAction.ReadValue<float>() > 0.5f;
    }

    private void SetLeverRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        Quaternion deltaRotation =
            Quaternion.AngleAxis(pulledAngle * ratio, localAxis.normalized);

        transform.localPosition = startLocalPosition;
        transform.localRotation = deltaRotation * startLocalRotation;
    }

    private void AutoWireReferences()
    {
        if (leftClimbingHand == null && ClimbingManager.Instance != null)
            leftClimbingHand = ClimbingManager.Instance.leftHand;

        if (rightClimbingHand == null && ClimbingManager.Instance != null)
            rightClimbingHand = ClimbingManager.Instance.rightHand;

        if (leftClimbingHand == null || rightClimbingHand == null)
        {
            ClimbingHand[] hands = FindObjectsOfType<ClimbingHand>();
            for (int i = 0; i < hands.Length; i++)
            {
                ClimbingHand hand = hands[i];
                if (hand == null) continue;

                if (hand.handType == ClimbingHand.HandType.Left && leftClimbingHand == null)
                    leftClimbingHand = hand;
                else if (hand.handType == ClimbingHand.HandType.Right && rightClimbingHand == null)
                    rightClimbingHand = hand;
            }
        }

        if (leftHand == null && leftClimbingHand != null)
            leftHand = leftClimbingHand.grabDetector != null
                ? leftClimbingHand.grabDetector.transform
                : leftClimbingHand.transform;

        if (rightHand == null && rightClimbingHand != null)
            rightHand = rightClimbingHand.grabDetector != null
                ? rightClimbingHand.grabDetector.transform
                : rightClimbingHand.transform;

        if (leftGrip.action == null && leftClimbingHand != null)
            leftGrip = leftClimbingHand.gripAction;

        if (rightGrip.action == null && rightClimbingHand != null)
            rightGrip = rightClimbingHand.gripAction;
    }
}
