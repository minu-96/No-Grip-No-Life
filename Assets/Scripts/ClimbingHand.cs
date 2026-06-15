using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ClimbingHand : MonoBehaviour
{
    public enum HandType { Left, Right }

    [Header("Hand")]
    public HandType handType;
    public GameObject xrOrigin;

    [Header("Input")]
    public InputActionProperty gripAction;
    public InputActionProperty handPositionAction;

    [Header("Grab Detection")]
    public HandGrabDetector grabDetector;
    public float grabRadius = 0.15f;
    public float handoffGrabRadius = 0.75f;
    public LayerMask grabPointLayer;
    public float minGrabHoldTime = 0.25f;
    public bool releaseOnGripRelease = true;
    public float releaseThreshold = 0.08f;
    public float releaseConfirmTime = 0.2f;

    [Header("Climbing")]
    public float climbMoveMultiplier = 1.0f;
    public ClimbRespawnManager climbRespawnManager;
    [Tooltip("Extra multiplier applied only to vertical climbing. Raise this for more upward progress without amplifying horizontal jitter.")]
    public float verticalClimbMultiplier = 4.2f;
    [Tooltip("Multiplier for horizontal hand movement while climbing. Keep near 1 to avoid VR jitter.")]
    public float horizontalClimbMultiplier = 1.0f;
    [Tooltip("Maximum XR Origin movement applied by one hand in a single frame.")]
    public float maxClimbMovePerFrame = 0.22f;
    [Tooltip("Higher values follow the hand faster. Lower values smooth tracking noise more.")]
    public float climbMoveSmoothing = 20f;

    [Header("State")]
    public bool isGrabbing;
    public string targetPointName;

    [Header("Haptics")]
    public bool useHaptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;

    private GrabPoint grabbedPoint;
    private Vector3 previousHandWorldPos;
    private Vector3 smoothedClimbMove;
    private float nextNoGrabLogTime;
    private float ignoreReleaseUntilTime;
    private float releaseStartedTime = -1f;

    private const float MOVE_DEADZONE_SQR = 0.000005f;
    private const float GRAB_THRESHOLD = 0.7f;
    private const float NO_GRAB_LOG_INTERVAL = 0.5f;

    void Start()
    {
        if (ClimbingManager.Instance != null)
        {
            if (handType == HandType.Left) ClimbingManager.Instance.leftHand = this;
            else ClimbingManager.Instance.rightHand = this;
        }

        if (climbRespawnManager == null)
        {
            climbRespawnManager = FindObjectOfType<ClimbRespawnManager>();
        }
    }

    void OnEnable()
    {
        if (gripAction.action != null) gripAction.action.Enable();
        if (handPositionAction.action != null) handPositionAction.action.Enable();
    }

    void Update()
    {
        if (gripAction.action == null) return;

        float gripValue = gripAction.action.ReadValue<float>();

        if (!isGrabbing)
        {
            if (gripValue > GRAB_THRESHOLD) TryGrab();
            return;
        }

        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null && stamina.currentStamina <= 0f)
        {
            Release();
            return;
        }

        if (releaseOnGripRelease && gripValue < releaseThreshold)
        {
            if (Time.time < ignoreReleaseUntilTime)
            {
                return;
            }

            if (releaseStartedTime < 0f)
            {
                releaseStartedTime = Time.time;
                return;
            }

            if (Time.time - releaseStartedTime >= releaseConfirmTime)
            {
                Release();
                return;
            }
        }
        else
        {
            releaseStartedTime = -1f;
        }

        TrySwitchGrabPoint();
    }

    void LateUpdate()
    {
        if (!isGrabbing || grabbedPoint == null || xrOrigin == null) return;

        if (ClimbingManager.Instance != null)
        {
            ClimbingManager.Instance.SetActiveHand(this);
            ClimbingManager.Instance.NotifyGrabStateChanged();
        }

        if (ClimbingManager.Instance != null &&
            !ClimbingManager.Instance.IsActiveHand(this))
        {
            return;
        }

        Vector3 currentHandWorldPos = transform.position;
        Vector3 handMoveDelta = currentHandWorldPos - previousHandWorldPos;

        if (handMoveDelta.sqrMagnitude > MOVE_DEADZONE_SQR)
        {
            Vector3 climbMove = GetScaledClimbMove(handMoveDelta);
            float smoothing = 1f - Mathf.Exp(-Mathf.Max(climbMoveSmoothing, 0f) * Time.deltaTime);
            smoothedClimbMove = Vector3.Lerp(smoothedClimbMove, climbMove, smoothing);
            xrOrigin.transform.position += smoothedClimbMove;
        }
        else
        {
            smoothedClimbMove = Vector3.zero;
        }

        previousHandWorldPos = currentHandWorldPos;
    }

    private Vector3 GetScaledClimbMove(Vector3 handMoveDelta)
    {
        float managerMultiplier = climbRespawnManager != null
            ? climbRespawnManager.climbMultiplier
            : 1.0f;

        Vector3 climbMove = -handMoveDelta;
        float horizontalMultiplier = climbMoveMultiplier * horizontalClimbMultiplier;
        float verticalMultiplier = climbMoveMultiplier * managerMultiplier * verticalClimbMultiplier;

        climbMove.x *= horizontalMultiplier;
        climbMove.z *= horizontalMultiplier;
        climbMove.y *= verticalMultiplier;

        float maxMove = Mathf.Max(maxClimbMovePerFrame, 0.01f);
        if (climbMove.magnitude > maxMove)
        {
            climbMove = climbMove.normalized * maxMove;
        }

        return climbMove;
    }

    void TryGrab()
    {
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null && stamina.currentStamina <= 0f) return;

        GrabPoint closestPoint = FindClosestGrabPoint();
        if (closestPoint == null)
        {
            LogNoGrabPointFound();
            return;
        }

        Grab(closestPoint);
    }

    private void TrySwitchGrabPoint()
    {
        GrabPoint closestPoint = FindClosestGrabPoint();
        if (closestPoint == null || closestPoint == grabbedPoint) return;

        Grab(closestPoint, true);
    }

    private GrabPoint FindClosestGrabPoint()
    {
        if (grabDetector != null && grabDetector.currentPoint != null)
        {
            return grabDetector.currentPoint;
        }

        Vector3 probePosition = GetGrabProbePosition();
        GrabPoint closestPoint = FindClosestGrabPointInRadius(probePosition, grabRadius);
        if (closestPoint != null)
        {
            return closestPoint;
        }

        if (ClimbingManager.Instance != null && ClimbingManager.Instance.IsClimbingOrHandingOff())
        {
            return FindClosestGrabPointInRadius(probePosition, Mathf.Max(grabRadius, handoffGrabRadius));
        }

        return null;
    }

    private GrabPoint FindClosestGrabPointInRadius(Vector3 probePosition, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            probePosition,
            radius,
            grabPointLayer,
            QueryTriggerInteraction.Collide);

        GrabPoint closestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            GrabPoint point = col.GetComponent<GrabPoint>();
            if (point == null) point = col.GetComponentInParent<GrabPoint>();
            if (point == null) continue;

            float distance = Vector3.Distance(probePosition, point.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    private Vector3 GetGrabProbePosition()
    {
        return grabDetector != null ? grabDetector.transform.position : transform.position;
    }

    private void Grab(GrabPoint point, bool switched = false)
    {
        grabbedPoint = point;
        isGrabbing = true;
        targetPointName = grabbedPoint.gameObject.name;
        ReleaseOtherHand();
        previousHandWorldPos = transform.position;
        smoothedClimbMove = Vector3.zero;
        ignoreReleaseUntilTime = Time.time + minGrabHoldTime;
        releaseStartedTime = -1f;

        if (ClimbingManager.Instance != null)
        {
            ClimbingManager.Instance.SetActiveHand(this);
            ClimbingManager.Instance.NotifyGrabStateChanged();
        }

        PlayGrabHaptic();

        string action = switched ? "switched grab to" : "grabbed";
        Debug.LogWarning($"[ClimbingHand] {gameObject.name} {action} {targetPointName}");
    }

    private void ReleaseOtherHand()
    {
        if (ClimbingManager.Instance == null) return;

        ClimbingHand otherHand = handType == HandType.Left
            ? ClimbingManager.Instance.rightHand
            : ClimbingManager.Instance.leftHand;

        if (otherHand != null && otherHand != this && otherHand.isGrabbing)
        {
            otherHand.ForceReleaseForHandoff();
        }
    }

    public void ForceReleaseForHandoff()
    {
        if (!isGrabbing) return;

        isGrabbing = false;
        grabbedPoint = null;
        targetPointName = "";
        releaseStartedTime = -1f;
        previousHandWorldPos = transform.position;
        smoothedClimbMove = Vector3.zero;
        Debug.LogWarning($"[ClimbingHand] {gameObject.name} released for handoff");
    }

    private void LogNoGrabPointFound()
    {
        if (Time.time < nextNoGrabLogTime) return;

        nextNoGrabLogTime = Time.time + NO_GRAB_LOG_INTERVAL;
        Debug.LogWarning($"[ClimbingHand] {gameObject.name} found no GrabPoint nearby. probe={GetGrabProbePosition()}, radius={grabRadius}, handoffRadius={handoffGrabRadius}, layerMask={grabPointLayer.value}");
    }

    public void Release()
    {
        if (isGrabbing)
        {
            float gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : -1f;
            Debug.LogWarning($"[ClimbingHand] {gameObject.name} released grab. grip={gripValue:F2}");
        }

        isGrabbing = false;
        grabbedPoint = null;
        targetPointName = "";
        smoothedClimbMove = Vector3.zero;

        if (ClimbingManager.Instance != null &&
            ClimbingManager.Instance.activeHand == this)
        {
            if (handType == HandType.Left &&
                ClimbingManager.Instance.rightHand != null &&
                ClimbingManager.Instance.rightHand.isGrabbing)
            {
                ClimbingManager.Instance.SetActiveHand(ClimbingManager.Instance.rightHand);
            }
            else if (handType == HandType.Right &&
                     ClimbingManager.Instance.leftHand != null &&
                     ClimbingManager.Instance.leftHand.isGrabbing)
            {
                ClimbingManager.Instance.SetActiveHand(ClimbingManager.Instance.leftHand);
            }
            else
            {
                ClimbingManager.Instance.SetActiveHand(null);
            }
        }

        if (ClimbingManager.Instance != null)
        {
            ClimbingManager.Instance.NotifyGrabStateChanged();
        }
    }

    private void PlayGrabHaptic()
    {
        if (!useHaptics) return;

        XRNode node = handType == HandType.Left ? XRNode.LeftHand : XRNode.RightHand;
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (device.isValid)
        {
            device.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
    }
}
