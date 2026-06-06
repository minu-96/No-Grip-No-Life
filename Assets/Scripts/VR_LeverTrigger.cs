using UnityEngine;
using UnityEngine.Events;
// 💡 최신 인풋 시스템을 쓰기 위해 필수적인 네임스페이스입니다.
using UnityEngine.InputSystem;

public class VR_LeverTrigger : MonoBehaviour
{
    private HingeJoint hinge;
    public UnityEvent onLeverDown;
    private bool isActivated = false;

    void Start()
    {
        hinge = GetComponent<HingeJoint>();
    }

    void Update()
    {
        if (isActivated) return;

        // 🎮 [최신 인풋 시스템 전용 PC 테스트 코드]
        // 현재 키보드 장치를 가져와서 숫자 1번 키가 이번 프레임에 눌렸는지 칼같이 체크합니다.
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ActivateLever();
            return;
        }

        // [VR 조작용 각도 체크] 마이너스 각도 대응 절댓값 버전
        if (hinge != null && Mathf.Abs(hinge.angle) >= hinge.limits.max * 0.9f)
        {
            ActivateLever();
        }
    }

    private void ActivateLever()
    {
        isActivated = true;
        Debug.Log("레버 작동 완료!");
        onLeverDown.Invoke();
    }
}