using UnityEngine;
using UnityEngine.XR;

public class ClimbingHand : MonoBehaviour
{
    [Header("손 설정")]
    public bool isLeftHand = false;

    [Header("연결")]
    public HandGrabDetector grabDetector;

    public bool isGrabbing { get; private set; }
    public Vector3 handVelocity { get; private set; }

    private GrabPoint grabbedPoint;
    private InputDevice controller;
    private Vector3 prevPosition;

    void Start()
    {
        if (grabDetector == null)
            Debug.LogError($"[ClimbingHand:{gameObject.name}] GrabDetector 연결 안 됨!");

        prevPosition = transform.position;
    }

    void Update()
    {
        // 매 프레임 손의 속도 계산 (추후 던지기 등 확장용)
        if (Time.deltaTime > 0)
        {
            handVelocity = (transform.position - prevPosition) / Time.deltaTime;
            prevPosition = transform.position;
        }

        // XR 컨트롤러 입력 장치 연결 매핑
        if (!controller.isValid)
        {
            controller = InputDevices.GetDeviceAtXRNode(
                isLeftHand ? XRNode.LeftHand : XRNode.RightHand
            );
        }

        bool gripPressed = false;
        if (controller.isValid)
        {
            // 그립 버튼 입력을 확인
            controller.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed);
        }

        // 그립 버튼 누름 여부에 따른 처리
        if (gripPressed)
            TryGrab();
        else
            Release();
    }

    void TryGrab()
    {
        // 이미 무언가를 잡고 있다면 중복 그랩 방지
        if (isGrabbing && grabbedPoint != null)
            return;

        if (grabDetector == null)
            return;

        GrabPoint nearestPoint = grabDetector.currentPoint;

        // 잡을 수 있는 포인트가 있고, 다른 손이 선점하지 않았다면 그랩 성공
        if (nearestPoint != null && !nearestPoint.occupied)
        {
            grabbedPoint = nearestPoint;
            grabbedPoint.occupied = true;
            isGrabbing = true;
            Debug.Log($"[ClimbingHand:{gameObject.name}] 그랩 성공: {grabbedPoint.name}");
        }
    }

    public void Release()
    {
        // 잡고 있던 포인트 해제
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
        // 오브젝트가 비활성화될 때 물리 버그 방지를 위해 강제 릴리즈
        Release();
    }
}