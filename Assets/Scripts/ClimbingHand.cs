using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbingHand : MonoBehaviour
{
    [Header("필수 연결 (XR Origin만 드래그해서 넣어주세요)")]
    public GameObject xrOrigin;

    [Header("인풋 설정")]
    public InputActionProperty gripAction;

    public bool isGrabbing { get; private set; }

    private HandGrabDetector grabDetector;
    private CharacterController characterController;
    private GrabPoint grabbedPoint;

    // 그랩 성공 순간의 '진짜 월드 좌표'를 기억할 변수
    private Vector3 grabbedWorldPosition;
    private Quaternion grabbedWorldRotation;

    void Start()
    {
        grabDetector = GetComponent<HandGrabDetector>();

        if (xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        float gripValue = gripAction.action.ReadValue<float>();
        bool gripPressed = gripValue > 0.5f;

        if (gripPressed)
        {
            if (!isGrabbing) TryGrab();
        }
        else
        {
            if (isGrabbing) Release();
        }

        // Q/E 키 직통 이동
        if (isGrabbing && grabbedPoint != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed)
                {
                    if (characterController != null) characterController.enabled = false;
                    xrOrigin.transform.position += Vector3.up * Time.deltaTime * 5.0f;
                    if (characterController != null) characterController.enabled = true;
                }
                else if (keyboard.eKey.isPressed)
                {
                    if (characterController != null) characterController.enabled = false;
                    xrOrigin.transform.position += Vector3.down * Time.deltaTime * 5.0f;
                    if (characterController != null) characterController.enabled = true;
                }
            }
        }
    }

    // [최종 마침표] 몸(XR Origin)이 움직여서 자식인 손을 강제로 끌고 올라가려고 할 때,
    // 매 프레임 가장 마지막 단계(LateUpdate)에서 손을 원래 그랩했던 월드 좌표로 찍어 눌러버립니다.
    void LateUpdate()
    {
        if (isGrabbing)
        {
            transform.position = grabbedWorldPosition;
            transform.rotation = grabbedWorldRotation;
        }
    }

    void TryGrab()
    {
        // [추가된 안전장치] 스태미나가 0이면 애초에 잡기 시도 자체를 막습니다.
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null && stamina.currentStamina <= 0f) return;

        if (grabDetector == null) return;
        GrabPoint nearestPoint = grabDetector.currentPoint;

        if (nearestPoint != null && !nearestPoint.occupied)
        {
            grabbedPoint = nearestPoint;
            grabbedPoint.occupied = true;
            isGrabbing = true;

            // 1. 마우스 트래킹 장치를 끕니다.
            SetTrackingEnabled(false);

            // 2. [핵심] 그랩한 순간 내 눈에 보이던 '그 절대적인 월드 좌표'를 복사해 둡니다.
            grabbedWorldPosition = transform.position;
            grabbedWorldRotation = transform.rotation;

            Debug.Log($"{gameObject.name} 그랩 성공! 절대 좌표 기억 및 LateUpdate 잠금 시작.");
        }
    }

    public void Release()
    {
        if (isGrabbing)
        {
            isGrabbing = false;

            // 손을 놓으면 트래킹 장치를 다시 켜서 마우스를 따르게 합니다.
            SetTrackingEnabled(true);

            Debug.Log($"{gameObject.name} 릴리즈! 트래킹 복구.");
        }

        if (grabbedPoint != null)
        {
            grabbedPoint.occupied = false;
            grabbedPoint = null;
        }
    }

    private void SetTrackingEnabled(bool enabled)
    {
        Component[] components = GetComponentsInParent<Component>();
        foreach (var comp in components)
        {
            string name = comp.GetType().Name;
            if (name.Contains("TrackedPoseDriver") || name.Contains("XRController") || name.Contains("ActionBasedController"))
            {
                if (comp is MonoBehaviour mono)
                {
                    mono.enabled = enabled;
                }
            }
        }
    }
}