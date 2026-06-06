using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GeneratorLeverInteraction : MonoBehaviour
{
    private GeneratorLeverController controller;
    private XRSimpleInteractable interactable;

    private void Start()
    {
        controller = GetComponentInParent<GeneratorLeverController>();
        interactable = GetComponent<XRSimpleInteractable>();

        if (controller == null)
        {
            Debug.LogError("GeneratorLeverController not found in parent!");
            return;
        }

        if (interactable == null)
        {
            Debug.LogError("XRSimpleInteractable not found!");
            return;
        }

        // 상호작용 이벤트 연결
        interactable.selectEntered.AddListener(_ => controller.OnGrab());
        interactable.selectExited.AddListener(_ => controller.OnRelease());

        // Rigidbody 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        Debug.Log("GeneratorLeverInteraction initialized");
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(_ => controller.OnGrab());
            interactable.selectExited.RemoveListener(_ => controller.OnRelease());
        }
    }
}