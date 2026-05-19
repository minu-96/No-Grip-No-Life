using UnityEngine;
using UnityEngine.UI; // UI 제어를 위해 반드시 필요합니다!

public class StaminaManagerWithUI : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float drainRate = 5f;       // 등반 중 초당 감소량
    public float regainRate = 15f;     // 체크포인트 초당 회복량

    [Header("UI Reference")]
    // 중요: 여기에 유니티 에디터에서 원형 스태미너 이미지(Filled 설정된 것)를 드래그 앤 드롭 하세요.
    public Image staminaCircleBar;

    [Header("Player States")]
    public bool isClimbing = false;
    public bool isInCheckpoint = false;
    public bool isFalling = false;

    private void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    private void Update()
    {
        if (isFalling) return;

        // 1. 체크포인트 회복 로직
        if (isInCheckpoint)
        {
            RegainStamina();
        }
        // 2. 등반 중 감소 로직
        else if (isClimbing)
        {
            DrainStamina();
        }

        // 3. 실시간으로 원형 UI 바 업데이트
        UpdateStaminaUI();

        // 4. 탈진 시 추락 처리
        if (currentStamina <= 0)
        {
            Fall();
        }
    }

    private void DrainStamina()
    {
        currentStamina -= drainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void RegainStamina()
    {
        currentStamina += regainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // 원형 게이지를 갱신하는 핵심 함수
    private void UpdateStaminaUI()
    {
        if (staminaCircleBar != null)
        {
            // Image의 fillAmount는 0.0(0%) ~ 1.0(100%) 사이의 값을 가집니다.
            staminaCircleBar.fillAmount = currentStamina / maxStamina;
        }
    }

    private void Fall()
    {
        isFalling = true;
        isClimbing = false;
        Debug.Log("스태미너 전멸! 추락!");

        // VR 리지드바디 해제 등의 추락 로직을 여기에 작성하세요.
    }

    public void StartClimbing() { if (!isFalling) isClimbing = true; }
    public void StopClimbing() { isClimbing = false; }
    public void ResetFalling() { isFalling = false; currentStamina = maxStamina; }
}