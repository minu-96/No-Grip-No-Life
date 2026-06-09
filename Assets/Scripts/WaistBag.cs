using UnityEngine;

public class WaistBag : MonoBehaviour
{
    [Header("Tracking Target")]
    [SerializeField] private Transform targetCamera; // Main Camera를 드래그 앤 드롭

    [Header("Position Tuning")]
    [SerializeField] private float heightOffset = -0.55f; // 카메라 기준 허리 높이
    [SerializeField] private float forwardOffset = -0.1f;  // 허리 앞뒤 위치 조정

    [Header("Rotation Tuning")]
    // ⭐ 가방이 돌아갔을 때 인스펙터에서 이 값을 수정해 가로로 맞춥니다!
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 90f, 0f);

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. 위치 계산 (동일)
        Vector3 cameraForwardHorizontal = targetCamera.forward;
        cameraForwardHorizontal.y = 0;
        cameraForwardHorizontal.Normalize();

        Vector3 targetPosition = targetCamera.position
                                 + (Vector3.up * heightOffset)
                                 + (cameraForwardHorizontal * forwardOffset);

        transform.position = targetPosition;

        // 2. 회전 계산 (카메라 회전 + 우리가 입력한 오프셋 각도 추가)
        if (cameraForwardHorizontal.sqrMagnitude > 0.01f)
        {
            // 카메라가 바라보는 수평 방향 회전
            Quaternion lookRotation = Quaternion.LookRotation(cameraForwardHorizontal);

            // 오프셋 각도를 쿼터니언으로 변환
            Quaternion offsetRotation = Quaternion.Euler(rotationOffset);

            // 두 회전을 결합하여 가방에 적용 (순서 중요: lookRotation이 먼저 적용되어야 함)
            transform.rotation = lookRotation * offsetRotation;
        }
    }
}