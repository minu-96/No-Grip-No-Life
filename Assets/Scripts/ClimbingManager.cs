using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

public class ClimbingManager : MonoBehaviour
{
    public static ClimbingManager Instance;

    [HideInInspector] public ClimbingHand leftHand;
    [HideInInspector] public ClimbingHand rightHand;
    [HideInInspector] public ClimbingHand activeHand;

    public float handoffGraceTime = 0.25f;

    private CharacterController characterController;
    private GravityProvider[] gravityProviders;
    private bool[] originalGravityEnabled;
    private float handoffGraceUntilTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        characterController = GetComponentInChildren<CharacterController>();
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
