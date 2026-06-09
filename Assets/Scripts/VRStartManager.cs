using UnityEngine;
using Unity.XR.CoreUtils; // 네임스페이스 에러 해결

public class VRStartFixer : MonoBehaviour
{
    void Start()
    {
        // 이 스크립트가 붙어있는 XR Origin 컴포넌트를 가져옵니다.
        XROrigin xrOrigin = GetComponent<XROrigin>();

        if (xrOrigin != null)
        {
            // 1. 현재 XR Origin이 배치된 씬 뷰 위치로 VR 카메라를 이동시킵니다.
            xrOrigin.MoveCameraToWorldLocation(transform.position);

            // 2. 정확한 앞 방향(Forward)을 바라보도록 정렬합니다. (메서드명 수정 완료!)
            xrOrigin.MatchOriginUpCameraForward(transform.up, transform.forward);
        }
    }
}