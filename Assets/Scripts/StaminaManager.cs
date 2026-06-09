using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("필수 연결")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;
    public StaminaVignette staminaVignette;

    [Header("스태미나 설정")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("초당 변화량")]
    public float consumeRate = 5f;
    public float recoverRate = 15f;

    [Header("상태 확인")]
    public bool isInsideCheckpoint = false;

    void Start()
    {
        currentStamina = maxStamina;

        if (staminaVignette != null)
        {
            staminaVignette.UpdateVignette(currentStamina);
        }
    }

    void Update()
    {
        bool isGrabbingAny = false;

        if (leftHand != null && leftHand.isGrabbing) isGrabbingAny = true;
        if (rightHand != null && rightHand.isGrabbing) isGrabbingAny = true;

        if (isGrabbingAny)
        {
            currentStamina -= consumeRate * Time.deltaTime;
        }
        else if (isInsideCheckpoint)
        {
            currentStamina += recoverRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (staminaVignette != null)
        {
            staminaVignette.UpdateVignette(currentStamina);
        }

        if (currentStamina <= 0f && isGrabbingAny)
        {
            ForceReleaseAllHands();
        }
    }

    void ForceReleaseAllHands()
    {
        Debug.LogWarning("[스태미나 부족] 모든 손을 강제로 놓습니다!");

        if (leftHand != null && leftHand.isGrabbing) leftHand.Release();
        if (rightHand != null && rightHand.isGrabbing) rightHand.Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            isInsideCheckpoint = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            isInsideCheckpoint = false;
        }
    }
}