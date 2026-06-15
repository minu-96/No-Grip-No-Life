using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

public class ClimbingManager : MonoBehaviour
{
    public static ClimbingManager Instance;

    [HideInInspector] public ClimbingHand leftHand;
    [HideInInspector] public ClimbingHand rightHand;
    [HideInInspector] public ClimbingHand activeHand;

    public float handoffGraceTime = 0.25f;

    [Header("Snap Turn")]
    public bool enableRightStickSnapTurn = true;
    public float snapTurnAngle = 30f;
    public float snapTurnThreshold = 0.65f;
    public float snapTurnResetThreshold = 0.25f;

    private CharacterController characterController;
    private Camera xrCamera;
    private GravityProvider[] gravityProviders;
    private bool[] originalGravityEnabled;
    private float handoffGraceUntilTime;
    private bool snapTurnArmed = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        characterController = GetComponentInChildren<CharacterController>();
        xrCamera = GetComponentInChildren<Camera>();
        CacheGravityProviders();
    }

    public void SetActiveHand(ClimbingHand hand)
    {
        activeHand = hand;
        if (hand != null)
        {
            ExtendHandoffGrace();
            SetExternalGravityEnabled(false);
            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
            }
        }
    }

    public bool IsActiveHand(ClimbingHand hand)
    {
        return activeHand == hand;
    }

    public void NotifyGrabStateChanged()
    {
        ExtendHandoffGrace();
    }

    public bool HasAnyHandGrabbing()
    {
        return (leftHand != null && leftHand.isGrabbing) ||
               (rightHand != null && rightHand.isGrabbing);
    }

    public bool IsClimbingOrHandingOff()
    {
        return HasAnyHandGrabbing() || Time.time < handoffGraceUntilTime;
    }

    private void ExtendHandoffGrace()
    {
        handoffGraceUntilTime = Time.time + handoffGraceTime;
    }

    void LateUpdate()
    {
        HandleRightStickSnapTurn();

        bool isClimbing = IsClimbingOrHandingOff();

        if (isClimbing)
        {
            SetExternalGravityEnabled(false);
            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
            }
        }
        else
        {
            activeHand = null;
            SetExternalGravityEnabled(true);

            if (characterController != null && !characterController.enabled)
            {
                characterController.enabled = true;
            }
        }
    }

    private void HandleRightStickSnapTurn()
    {
        if (!enableRightStickSnapTurn) return;
        if (IsClimbingOrHandingOff()) return;

        InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightHandDevice.isValid) return;
        if (!rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) return;

        if (Mathf.Abs(axis.x) < snapTurnResetThreshold)
        {
            snapTurnArmed = true;
            return;
        }

        if (!snapTurnArmed || Mathf.Abs(axis.x) < snapTurnThreshold) return;

        float turnAmount = axis.x > 0f ? snapTurnAngle : -snapTurnAngle;
        RotateAroundCamera(turnAmount);
        snapTurnArmed = false;
    }

    private void RotateAroundCamera(float angle)
    {
        if (xrCamera == null)
        {
            xrCamera = GetComponentInChildren<Camera>();
        }

        Vector3 pivot = xrCamera != null ? xrCamera.transform.position : transform.position;
        transform.RotateAround(pivot, Vector3.up, angle);
    }

    private void CacheGravityProviders()
    {
        gravityProviders = FindObjectsOfType<GravityProvider>(true);
        originalGravityEnabled = new bool[gravityProviders.Length];

        for (int i = 0; i < gravityProviders.Length; i++)
        {
            originalGravityEnabled[i] = gravityProviders[i] != null && gravityProviders[i].useGravity;
        }
    }

    private void SetExternalGravityEnabled(bool enabled)
    {
        if (gravityProviders == null || gravityProviders.Length == 0)
        {
            CacheGravityProviders();
        }

        if (gravityProviders == null) return;

        for (int i = 0; i < gravityProviders.Length; i++)
        {
            GravityProvider provider = gravityProviders[i];
            if (provider == null) continue;

            bool restoreOriginal = originalGravityEnabled != null &&
                                   i < originalGravityEnabled.Length &&
                                   originalGravityEnabled[i];
            bool targetEnabled = enabled && restoreOriginal;

            if (provider.useGravity != targetEnabled)
            {
                provider.useGravity = targetEnabled;
            }

            if (!targetEnabled)
            {
                provider.ResetFallForce();
            }
        }
    }
}
