using UnityEngine;
using UnityEngine.XR;

public class HMDPoseDriverFallback : MonoBehaviour
{
    void Update()
    {
        ApplyHMDPose();
    }

    void OnBeforeRender()
    {
        ApplyHMDPose();
    }

    private void ApplyHMDPose()
    {
        UnityEngine.XR.InputDevice hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (!hmd.isValid) return;

        if (hmd.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            transform.localPosition = position;
        }

        if (hmd.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            transform.localRotation = rotation;
        }
    }
}