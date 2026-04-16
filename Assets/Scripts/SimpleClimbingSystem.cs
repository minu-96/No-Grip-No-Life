using UnityEngine;

public class SimpleClimbingSystem : MonoBehaviour
{
    public Transform playerRoot;
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;
    public CharacterController characterController;

    public float moveMultiplier = 3.0f;
    public float maxMovePerFrame = 0.5f;

    void Update()
    {
        Vector3 move = Vector3.zero;
        int count = 0;

        if (leftHand != null && leftHand.isGrabbing)
        {
            Vector3 delta = leftHand.transform.position - leftHand.lastHandPosition;
            Debug.Log("왼손 delta: " + delta);
            move += -delta;
            count++;
        }

        if (rightHand != null && rightHand.isGrabbing)
        {
            Vector3 delta = rightHand.transform.position - rightHand.lastHandPosition;
            Debug.Log("오른손 delta: " + delta);
            move += -delta;
            count++;
        }

        if (count > 0)
        {
            Vector3 testMove = Vector3.up * 0.05f;
            Debug.Log("강제 위 이동");

            if (characterController != null)
                characterController.Move(testMove);
            else if (playerRoot != null)
                playerRoot.position += testMove;

            if (leftHand != null && leftHand.isGrabbing)
                leftHand.lastHandPosition = leftHand.transform.position;

            if (rightHand != null && rightHand.isGrabbing)
                rightHand.lastHandPosition = rightHand.transform.position;
        }
    }
}