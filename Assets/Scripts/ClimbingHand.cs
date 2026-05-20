using UnityEngine;
using UnityEngine.InputSystem; // [추가] 최신 인풋 시스템 네임스페이스

public class ClimbingHand : MonoBehaviour
{
    [Header("연결")]
    public HandGrabDetector grabDetector;

    [Header("최신 입력 설정 (Action-Based)")]
    // 인스펙터에서 유니티 XRI의 Grip Action을 직접 연결합니다.
    public InputActionProperty gripAction;

    public bool isGrabbing { get; private set; }
    public Vector3 handVelocity { get; private set; }

    private GrabPoint grabbedPoint;
    private Vector3 prevPosition;

    void Start()
    {
        if (grabDetector == null)
            Debug.LogError($"[ClimbingHand:{gameObject.name}] GrabDetector 연결 안 됨!");

        prevPosition = transform.position;
    }

    void Update()
    {
        // 매 프레임 손의 속도 계산
        if (Time.deltaTime > 0)
        {
            handVelocity = (transform.position - prevPosition) / Time.deltaTime;
            prevPosition = transform.position;
        }

        // [수정] 최신 인풋 시스템 방식으로 그립 버튼 값 읽기 (0.5보다 크면 눌린 것으로 판정)
        float gripValue = gripAction.action.ReadValue<float>();
        bool gripPressed = gripValue > 0.5f;

        if (gripPressed)
        {
            Debug.Log($"[{gameObject.name}] 최신 인풋 시스템 - 그립 버튼 눌림 감지됨! (Value: {gripValue})");
            TryGrab();
        }
        else
        {
            Release();
        }
    }

    void TryGrab()
    {
        if (isGrabbing && grabbedPoint != null)
            return;

        if (grabDetector == null)
            return;

        GrabPoint nearestPoint = grabDetector.currentPoint;

        if (nearestPoint != null && !nearestPoint.occupied)
        {
            grabbedPoint = nearestPoint;
            grabbedPoint.occupied = true;
            isGrabbing = true;
            Debug.Log($"[ClimbingHand:{gameObject.name}] ★★★ 그랩 성공! 벽을 잡았습니다: {grabbedPoint.name} ★★★");
        }
    }

    public void Release()
    {
        if (grabbedPoint != null)
        {
            grabbedPoint.occupied = false;
            grabbedPoint = null;
        }

        if (isGrabbing)
        {
            isGrabbing = false;
            Debug.Log($"[ClimbingHand:{gameObject.name}] 릴리즈");
        }
    }

    private void OnDisable()
    {
        Release();
    }
}