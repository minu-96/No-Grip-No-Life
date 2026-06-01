using UnityEngine;

public class ClimbingManager : MonoBehaviour
{
    public static ClimbingManager Instance;

    [HideInInspector] public ClimbingHand leftHand;
    [HideInInspector] public ClimbingHand rightHand;

    private CharacterController characterController;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        characterController = GetComponentInChildren<CharacterController>();
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
            if (!characterController.enabled) characterController.enabled = true;
        }
    }
}