using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbingHand : MonoBehaviour
{
    public enum HandType { Left, Right }

    [Header("손 구분 및 필수 연결")]
    public HandType handType;
    public GameObject xrOrigin;

    [Header("인풋 설정")]
    public InputActionProperty gripAction;
    public InputActionProperty handPositionAction;

    [Header("그랩 감지 범위 설정")]
    [Tooltip("손 중심에서 몇 미터(m) 안의 오브젝트를 잡을지 결정 (기본 0.15 추천)")]
    public float grabRadius = 0.15f;

    [Header("상태 확인")]
    public bool isGrabbing;
    public string targetPointName;

    private GrabPoint grabbedPoint;
    private Vector3 previousHandWorldPos;
    private const float MOVE_DEADZONE_SQR = 0.000005f;

    void Start()
    {
        if (ClimbingManager.Instance != null)
        {
            if (handType == HandType.Left) ClimbingManager.Instance.leftHand = this;
            else ClimbingManager.Instance.rightHand = this;
        }
    }

    void OnEnable()
    {
        if (gripAction.action != null) gripAction.action.Enable();
        if (handPositionAction.action != null) handPositionAction.action.Enable();
    }

    void Update()
    {
        if (gripAction.action == null) return;
        float gripValue = gripAction.action.ReadValue<float>();

        if (!isGrabbing)
        {
            if (gripValue > 0.7f) TryGrab();
        }
        else
        {
            // 스태미나 0 이하일 때 추락
            StaminaManager stamina = FindObjectOfType<StaminaManager>();
            if (stamina != null && stamina.currentStamina <= 0f)
            {
                Release();
                return;
            }

            if (gripValue < 0.2f) Release();
        }
    }

    void LateUpdate()
    {
        if (isGrabbing && grabbedPoint != null && xrOrigin != null)
        {
            // 월드 좌표 기반 이동 (축 뒤틀림 완벽 방지)
            Vector3 currentHandWorldPos = transform.position;
            Vector3 handMoveDelta = currentHandWorldPos - previousHandWorldPos;

            if (handMoveDelta.sqrMagnitude > MOVE_DEADZONE_SQR)
            {
                xrOrigin.transform.position -= handMoveDelta;
            }

            // 시각적 고정
            transform.position = grabbedPoint.transform.position;
            previousHandWorldPos = transform.position;
        }
    }

    void TryGrab()
    {
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null && stamina.currentStamina <= 0f) return;

        // 반경 내 모든 콜라이더 수집
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, grabRadius);

        GrabPoint closestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (var col in hitColliders)
        {
            GrabPoint point = col.GetComponent<GrabPoint>();
            // [💥 핵심 수정]: point.occupied 조건을 완전히 삭제하여, 다른 손이 잡고 있든 말든 무조건 탐색합니다!
            if (point != null)
            {
                // 내 주먹이나 몸뚱이를 잡는 예외 처리
                if (point.transform.IsChildOf(transform.root)) continue;

                float dist = Vector3.Distance(transform.position, point.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPoint = point;
                }
            }
        }

        // 주변에 잡을 게 없으면 취소
        if (closestPoint == null) return;

        // 최종 그랩 성공 처리
        grabbedPoint = closestPoint;
        isGrabbing = true;

        targetPointName = grabbedPoint.gameObject.name;
        previousHandWorldPos = transform.position;

        // 이 로그가 안 뜰 수가 없습니다!
        Debug.LogWarning($"🎯 [그랩 성공 로그] {gameObject.name}이 [{targetPointName}]을 완벽하게 붙잡았습니다!");
    }

    public void Release()
    {
        if (isGrabbing)
        {
            isGrabbing = false;
            Debug.LogWarning($"❌ [릴리즈 로그] {gameObject.name}이 손을 놓았습니다.");
        }

        grabbedPoint = null;
        targetPointName = "";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}