using UnityEngine;
using UnityEngine.Events;

public class GeneratorLeverController : MonoBehaviour
{
    [Header("State")]
    public bool isActivated = false;

    [Header("Objects To Enable When Activated")]
    public GameObject[] objectsToEnable = new GameObject[0];

    [Header("Objects To Disable When Activated")]
    public GameObject[] objectsToDisable = new GameObject[0];

    [Header("Lights To Turn On")]
    public Light[] lightsToTurnOn = new Light[0];
    public bool autoFindTransmissionTowerLights = true;
    public bool autoFindTransmissionTowerObjects = true;
    public bool autoFindTransmissionTowerEmission = true;
    public bool keepTargetLightsOffUntilActivated = true;

    [Header("Transmission Tower Objects")]
    public GameObject[] towerObjectsToEnable = new GameObject[0];
    public GameObject[] towerObjectsToDisable = new GameObject[0];

    [Header("Renderer Emission")]
    public Renderer[] emissionRenderers = new Renderer[0];
    public Color emissionColor = Color.white;
    public float emissionIntensity = 2.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip activateSound;

    [Header("Events")]
    public UnityEvent onActivated;

    private void Awake()
    {
        AutoFindLights();
        AutoFindTowerObjects();
        AutoFindEmissionRenderers();
    }

    private void Start()
    {
        AutoFindLights();
        AutoFindTowerObjects();
        AutoFindEmissionRenderers();

        if (!isActivated && keepTargetLightsOffUntilActivated)
        {
            SetTargetLights(false);
            SetTargetTowerObjects(false);
            SetEmission(false);
        }
    }

    public void ActivateGenerator()
    {
        if (isActivated)
            return;

        isActivated = true;

        Debug.Log("[Generator] Activated");

        if (objectsToEnable != null)
        {
            for (int i = 0; i < objectsToEnable.Length; i++)
            {
                if (objectsToEnable[i] != null)
                    objectsToEnable[i].SetActive(true);
            }
        }

        if (objectsToDisable != null)
        {
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                if (objectsToDisable[i] != null)
                    objectsToDisable[i].SetActive(false);
            }
        }

        AutoFindLights();
        AutoFindTowerObjects();
        AutoFindEmissionRenderers();
        SetTargetLights(true);
        SetTargetTowerObjects(true);

        SetEmission(true);

        if (audioSource != null && activateSound != null)
            audioSource.PlayOneShot(activateSound);

        onActivated?.Invoke();
    }

    private void SetEmission(bool enabled)
    {
        if (emissionRenderers == null) return;

        for (int i = 0; i < emissionRenderers.Length; i++)
        {
            Renderer targetRenderer = emissionRenderers[i];

            if (targetRenderer == null)
                continue;

            Material mat = targetRenderer.material;

            if (mat == null)
                continue;

            if (enabled)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }
            else
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void SetTargetLights(bool enabled)
    {
        AutoFindLights(true);

        if (lightsToTurnOn == null) return;

        for (int i = 0; i < lightsToTurnOn.Length; i++)
        {
            if (lightsToTurnOn[i] != null)
            {
                lightsToTurnOn[i].gameObject.SetActive(enabled);
                lightsToTurnOn[i].enabled = enabled;
            }
        }
    }

    private void SetTargetTowerObjects(bool activated)
    {
        AutoFindTowerObjects(true);

        if (towerObjectsToEnable != null)
        {
            for (int i = 0; i < towerObjectsToEnable.Length; i++)
            {
                if (towerObjectsToEnable[i] != null)
                    towerObjectsToEnable[i].SetActive(activated);
            }
        }

        if (towerObjectsToDisable != null)
        {
            for (int i = 0; i < towerObjectsToDisable.Length; i++)
            {
                if (towerObjectsToDisable[i] != null)
                    towerObjectsToDisable[i].SetActive(!activated);
            }
        }
    }

    private void AutoFindLights()
    {
        AutoFindLights(false);
    }

    private void AutoFindLights(bool forceRefresh)
    {
        if (!autoFindTransmissionTowerLights) return;
        if (!forceRefresh && lightsToTurnOn != null && lightsToTurnOn.Length > 0) return;

        Light[] sceneLights = FindObjectsOfType<Light>(true);
        System.Collections.Generic.List<Light> foundLights = new System.Collections.Generic.List<Light>();

        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight == null) continue;

            string normalizedLightName = NormalizeName(sceneLight.name);
            if (!normalizedLightName.Contains("pointlight")) continue;
            if (!HasParentNamed(sceneLight.transform, "transmissontower") &&
                !HasParentNamed(sceneLight.transform, "transmissiontower"))
            {
                continue;
            }

            foundLights.Add(sceneLight);
        }

        if (foundLights.Count > 0)
        {
            lightsToTurnOn = foundLights.ToArray();
            Debug.Log($"[Generator] Auto-found {lightsToTurnOn.Length} transmission tower light(s).");
        }
        else
        {
            Debug.LogWarning("[Generator] No transmission tower PointLight was found.");
        }
    }

    private void AutoFindTowerObjects()
    {
        AutoFindTowerObjects(false);
    }

    private void AutoFindTowerObjects(bool forceRefresh)
    {
        if (!autoFindTransmissionTowerObjects) return;
        if (!forceRefresh && towerObjectsToEnable != null && towerObjectsToEnable.Length > 0) return;
        if (!forceRefresh && towerObjectsToDisable != null && towerObjectsToDisable.Length > 0) return;

        Transform[] sceneTransforms = FindObjectsOfType<Transform>(true);
        System.Collections.Generic.List<GameObject> enableObjects = new System.Collections.Generic.List<GameObject>();
        System.Collections.Generic.List<GameObject> disableObjects = new System.Collections.Generic.List<GameObject>();

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform == null) continue;
            if (!HasParentNamed(sceneTransform, "transmissontower") &&
                !HasParentNamed(sceneTransform, "transmissiontower"))
            {
                continue;
            }

            string objectName = NormalizeName(sceneTransform.name);
            if (objectName == "transmissontoweron" ||
                objectName == "transmissiontoweron" ||
                objectName == "pointlights")
            {
                enableObjects.Add(sceneTransform.gameObject);
            }
            else if (objectName == "transmissontoweroff" ||
                     objectName == "transmissiontoweroff")
            {
                disableObjects.Add(sceneTransform.gameObject);
            }
        }

        towerObjectsToEnable = enableObjects.ToArray();
        towerObjectsToDisable = disableObjects.ToArray();

        Debug.Log($"[Generator] Auto-found tower objects. Enable: {towerObjectsToEnable.Length}, Disable: {towerObjectsToDisable.Length}");
    }

    private void AutoFindEmissionRenderers()
    {
        if (!autoFindTransmissionTowerEmission) return;
        if (emissionRenderers != null && emissionRenderers.Length > 0) return;

        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>(true);
        System.Collections.Generic.List<Renderer> foundRenderers = new System.Collections.Generic.List<Renderer>();

        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            Renderer sceneRenderer = sceneRenderers[i];
            if (sceneRenderer == null) continue;
            if (!HasParentNamed(sceneRenderer.transform, "transmissontower") &&
                !HasParentNamed(sceneRenderer.transform, "transmissiontower"))
            {
                continue;
            }

            foundRenderers.Add(sceneRenderer);
        }

        emissionRenderers = foundRenderers.ToArray();
    }

    private bool HasParentNamed(Transform transformToCheck, string normalizedName)
    {
        Transform current = transformToCheck;
        while (current != null)
        {
            string currentName = NormalizeName(current.name);
            if (currentName.Contains(normalizedName)) return true;
            current = current.parent;
        }

        return false;
    }

    private string NormalizeName(string value)
    {
        return value.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    [ContextMenu("Test Activate Generator")]
    public void TestActivateGenerator()
    {
        ActivateGenerator();
    }

    [ContextMenu("Reset Generator")]
    public void ResetGenerator()
    {
        isActivated = false;

        SetTargetLights(false);
        SetTargetTowerObjects(false);
        SetEmission(false);

        Debug.Log("[Generator] Reset");
    }
}
