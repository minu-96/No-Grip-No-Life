using UnityEngine;

public class GeneratorLeverController : MonoBehaviour
{
    [SerializeField]
    private Transform lever;

    [SerializeField]
    private float returnSpeed = 2f;

    [Header("Tower Lights")]
    [SerializeField]
    private string transmissionTowerName = "TransmissonTower";

    [SerializeField]
    private Light[] transmissionTowerLights;

    [SerializeField]
    private bool turnLightsOffOnStart = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isGrabbed = false;
    private bool isActivated = false;

    private void Start()
    {
        if (lever == null)
        {
            lever = transform.Find("Lever_Pivot/Lever");
        }

        if (lever != null)
        {
            initialPosition = lever.localPosition;
            initialRotation = lever.localRotation;
            Debug.Log("GeneratorLeverController initialized");
        }
        else
        {
            Debug.LogError("Lever not found!");
        }

        FindTransmissionTowerLights();

        if (turnLightsOffOnStart)
        {
            SetTransmissionTowerLights(false);
        }
    }

    private void Update()
    {
        if (lever == null) return;

        if (isGrabbed)
        {
            lever.localPosition = initialPosition;
            lever.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            ActivateGenerator();
        }
        else
        {
            lever.localPosition = Vector3.Lerp(lever.localPosition, initialPosition, Time.deltaTime * returnSpeed);
            lever.localRotation = Quaternion.Lerp(lever.localRotation, initialRotation, Time.deltaTime * returnSpeed);
        }
    }

    public void OnGrab()
    {
        isGrabbed = true;
    }

    public void OnRelease()
    {
        isGrabbed = false;
    }

    public void ActivateGenerator()
    {
        if (isActivated) return;

        isActivated = true;
        SetTransmissionTowerLights(true);
        Debug.Log("[GeneratorLeverController] Generator activated. Transmission tower lights are on.");
    }

    private void FindTransmissionTowerLights()
    {
        if (transmissionTowerLights != null && transmissionTowerLights.Length > 0) return;

        GameObject tower = GameObject.Find(transmissionTowerName);
        if (tower == null)
        {
            Debug.LogWarning($"[GeneratorLeverController] Transmission tower not found: {transmissionTowerName}");
            return;
        }

        transmissionTowerLights = tower.GetComponentsInChildren<Light>(true);
    }

    private void SetTransmissionTowerLights(bool enabled)
    {
        if (transmissionTowerLights == null || transmissionTowerLights.Length == 0)
        {
            FindTransmissionTowerLights();
        }

        if (transmissionTowerLights == null) return;

        for (int i = 0; i < transmissionTowerLights.Length; i++)
        {
            if (transmissionTowerLights[i] != null)
            {
                transmissionTowerLights[i].gameObject.SetActive(enabled);
                transmissionTowerLights[i].enabled = enabled;
            }
        }
    }
}
