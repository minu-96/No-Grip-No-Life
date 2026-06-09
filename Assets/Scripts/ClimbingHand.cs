using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ClimbingHand : MonoBehaviour
{
    public enum HandType { Left, Right }

    [Header("손 구분 및 필수 연결")]
    public HandType handType;
    public GameObject xrOrigin;

    [Header("인풋 설정")]
    public InputActionProperty gripAction;
    public InputActionProperty handPositionAction;

    [Header("그랩 감지")]
    public HandGrabDetector grabDetector;

    [Header("그랩 감지 범위 설정")]
    [Tooltip("손 중심에서 몇 미터(m) 안의 오브젝트를 잡을지 결정 (기본 0.15 추천)")]
    public float grabRadius = 0.15f;

    [Header("클라이밍 이동 설정")]
    [Tooltip("손 이동량이 몸 이동에 반영되는 배율입니다. 1이면 손 이동량 그대로 반영됩니다.")]
    public float climbMoveMultiplier = 1.0f;

    [Tooltip("그랩 가능한 레이어입니다. Climbable 레이어를 선택하세요.")]
    public LayerMask grabPointLayer;

    [Header("상태 확인")]
    public bool isGrabbing;
    public string targetPointName;

    [Header("햅틱 설정")]
    public bool useHaptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;

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
        if (!isGrabbing || grabbedPoint == null || xrOrigin == null) return;

        if (ClimbingManager.Instance != null &&
            !ClimbingManager.Instance.IsActiveHand(this))
        {
            return;
        }

        Vector3 currentHandWorldPos = transform.position;
        Vector3 handMoveDelta = currentHandWorldPos - previousHandWorldPos;

        if (handMoveDelta.sqrMagnitude > MOVE_DEADZONE_SQR)
        {
            xrOrigin.transform.position -= handMoveDelta;
        }

        previousHandWorldPos = transform.position;
    }
    void TryGrab()
    {
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null && stamina.currentStamina <= 0f) return;

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            grabRadius,
            grabPointLayer,
            QueryTriggerInteraction.Collide
        );

        GrabPoint closestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            GrabPoint point = col.GetComponent<GrabPoint>();
            if (point == null) point = col.GetComponentInParent<GrabPoint>();

            if (point == null) continue;

            float dist = Vector3.Distance(transform.position, point.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPoint = point;
            }
        }

        if (closestPoint == null)
        {
            Debug.LogWarning($"❌ {gameObject.name} 주변에 GrabPoint 없음");
            return;
        }

        grabbedPoint = closestPoint;
        isGrabbing = true;

        if (ClimbingManager.Instance != null)
        {
            ClimbingManager.Instance.SetActiveHand(this);
        }

        targetPointName = grabbedPoint.gameObject.name;
        previousHandWorldPos = transform.position;

        PlayGrabHaptic();

        Debug.LogWarning($"🎯 {gameObject.name}이 [{targetPointName}] 그랩 성공");
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

        if (ClimbingManager.Instance != null &&
            ClimbingManager.Instance.activeHand == this)
        {
            if (handType == HandType.Left &&
                ClimbingManager.Instance.rightHand != null &&
                ClimbingManager.Instance.rightHand.isGrabbing)
            {
                ClimbingManager.Instance.SetActiveHand(ClimbingManager.Instance.rightHand);
            }
            else if (handType == HandType.Right &&
                     ClimbingManager.Instance.leftHand != null &&
                     ClimbingManager.Instance.leftHand.isGrabbing)
            {
                ClimbingManager.Instance.SetActiveHand(ClimbingManager.Instance.leftHand);
            }
            else
            {
                ClimbingManager.Instance.SetActiveHand(null);
            }
        }
    }

    private void PlayGrabHaptic()
    {
        if (!useHaptics) return;

        XRNode node = handType == HandType.Left ? XRNode.LeftHand : XRNode.RightHand;
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (device.isValid)
        {
            device.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
    }
}