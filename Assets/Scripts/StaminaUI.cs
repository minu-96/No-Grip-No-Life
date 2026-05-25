using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트를 제어하기 위해 필수!

public class StaminaUI : MonoBehaviour
{
    [Header("연결할 컴포넌트")]
    public StaminaManager staminaManager; // 아까 만든 스태미나 매니저
    public Image staminaGaugeImage;       // 1단계에서 만든 'StaminaGauge' 이미지

    void Start()
    {
        // 만약 인스펙터에서 깜빡하고 연결 안 했을 때를 대비한 자동 검색 안전장치
        if (staminaManager == null)
        {
            staminaManager = FindObjectOfType<StaminaManager>();
        }
    }

    void Update()
    {
        if (staminaManager != null && staminaGaugeImage != null)
        {
            // 스태미나 비율 계산 (현재 수치 / 최대 수치) -> 0.0 ~ 1.0 사이의 값이 나옵니다.
            float staminaRatio = staminaManager.currentStamina / staminaManager.maxStamina;

            // 유니티 Image의 fillAmount에 대입하면 초록색 게이지가 비율에 맞춰 실시간으로 줄어듭니다.
            staminaGaugeImage.fillAmount = staminaRatio;

        }
    }
}
