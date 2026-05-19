using UnityEngine;
using System.Collections;

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float drainRate = 5f;       // 등반 중 초당 감소량
    public float regainRate = 15f;     // 체크포인트 초당 회복량

    [Header("Player States")]
    public bool isClimbing = false;
    public bool isInCheckpoint = false;
    public bool isFalling = false;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        // 1. 낙하 중이면 스태미너 로직을 타지 않음
        if (isFalling) return;

        // 2. 체크포인트에 있을 때 회복
        if (isInCheckpoint)
        {
            RegainStamina();
        }
        // 3. 등반 중일 때 스태미너 감소
        else if (isClimbing)
        {
            DrainStamina();
        }

        // 4. 스태미너가 0이 되면 낙하 처리
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

    private void Fall()
    {
        isFalling = true;
        isClimbing = false;

        Debug.Log("스태미너 고갈! 추락합니다.");

        // TODO: 여기에 실제 VR 플레이어의 리지드바디(Rigidbody)를 중력으로 떨어뜨리거나,
        // 등반 고정 상태(XR Grab Interactable 등)를 강제로 해제하는 코드를 넣으세요.
    }

    // 외부 VR 입력 스크립트나 Grab 이벤트에서 호출할 함수들
    public void StartClimbing()
    {
        if (isFalling) return; // 낙하 중엔 다시 잡기 방지 (필요에 따라 수정 가능)
        isClimbing = true;
    }

    public void StopClimbing()
    {
        isClimbing = false;
    }

    public void ResetFalling()
    {
        isFalling = false;
        currentStamina = maxStamina; // 바닥에 닿거나 리스폰 시 초기화용
    }
}