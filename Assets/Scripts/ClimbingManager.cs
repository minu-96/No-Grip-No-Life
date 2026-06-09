using UnityEngine;

public class ClimbingManager : MonoBehaviour
{
    public static ClimbingManager Instance;

    [HideInInspector] public ClimbingHand leftHand;
    [HideInInspector] public ClimbingHand rightHand;
    [HideInInspector] public ClimbingHand activeHand;

    private CharacterController characterController;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        characterController = GetComponentInChildren<CharacterController>();
    }

    public void SetActiveHand(ClimbingHand hand)
    {
        activeHand = hand;
    }

    public bool IsActiveHand(ClimbingHand hand)
    {
        return activeHand == hand;
    }

    void LateUpdate()
    {
        if (characterController == null) return;

        bool isClimbing = (leftHand != null && leftHand.isGrabbing) ||
                          (rightHand != null && rightHand.isGrabbing);

        if (isClimbing)
        {
            if (characterController.enabled) characterController.enabled = false;
        }
        else
        {
            activeHand = null;

            if (!characterController.enabled) characterController.enabled = true;
        }
    }
}