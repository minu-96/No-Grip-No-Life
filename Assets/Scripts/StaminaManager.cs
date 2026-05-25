using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("필수 연결 (인스펙터에서 직접 드래그)")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;

    [Header("스태미나 설정")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("초당 변화량")]
    public float consumeRate = 5f;    // 그랩 시 초당 감소량
    public float recoverRate = 15f;   // 체크포인트 초당 회복량

    [Header("상태 확인 (자동 반영)")]
    public bool isInsideCheckpoint = false;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        // 1. 양손 중 하나라도 무언가를 잡고 있는지 체크
        bool isGrabbingAny = false;
        if (leftHand != null && leftHand.isGrabbing) isGrabbingAny = true;
        if (rightHand != null && rightHand.isGrabbing) isGrabbingAny = true;

        // 2. 상황별 스태미나 계산
        if (isGrabbingAny)
        {
            // 잡고 있을 때는 무조건 감소
            currentStamina -= consumeRate * Time.deltaTime;
        }
        else if (isInsideCheckpoint)
        {
            // 손을 놓고 체크포인트 영역 안에 서 있을 때 회복
            currentStamina += recoverRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // 3. 스태미나가 0이 되면 강제 낙하 처리
        if (currentStamina <= 0f && isGrabbingAny)
        {
            ForceReleaseAllHands();
        }
    }

    void ForceReleaseAllHands()
    {
        Debug.LogWarning("[스태미나 고갈] 모든 손을 강제로 놓습니다!");
        if (leftHand != null && leftHand.isGrabbing) leftHand.Release();
        if (rightHand != null && rightHand.isGrabbing) rightHand.Release();
    }

    // --- 체크포인트 충돌 감지 ---
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