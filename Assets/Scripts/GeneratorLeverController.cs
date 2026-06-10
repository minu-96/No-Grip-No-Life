using UnityEngine;
using UnityEngine.Events;

public class GeneratorLeverController : MonoBehaviour
{
    [Header("State")]
    public bool isActivated = false;

    [Header("Objects To Enable When Activated")]
    public GameObject[] objectsToEnable;

    [Header("Objects To Disable When Activated")]
    public GameObject[] objectsToDisable;

    [Header("Lights To Turn On")]
    public Light[] lightsToTurnOn;

    [Header("Renderer Emission")]
    public Renderer[] emissionRenderers;
    public Color emissionColor = Color.yellow;
    public float emissionIntensity = 2.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip activateSound;

    [Header("Events")]
    public UnityEvent onActivated;

    public void ActivateGenerator()
    {
        if (isActivated)
            return;

        isActivated = true;

        Debug.Log("[Generator] Activated");

        for (int i = 0; i < objectsToEnable.Length; i++)
        {
            if (objectsToEnable[i] != null)
                objectsToEnable[i].SetActive(true);
        }

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            if (objectsToDisable[i] != null)
                objectsToDisable[i].SetActive(false);
        }

        for (int i = 0; i < lightsToTurnOn.Length; i++)
        {
            if (lightsToTurnOn[i] != null)
                lightsToTurnOn[i].enabled = true;
        }

        ApplyEmission();

        if (audioSource != null && activateSound != null)
            audioSource.PlayOneShot(activateSound);

        onActivated?.Invoke();
    }

    private void ApplyEmission()
    {
        for (int i = 0; i < emissionRenderers.Length; i++)
        {
            Renderer targetRenderer = emissionRenderers[i];

            if (targetRenderer == null)
                continue;

            Material mat = targetRenderer.material;

            if (mat == null)
                continue;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        }
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

        for (int i = 0; i < lightsToTurnOn.Length; i++)
        {
            if (lightsToTurnOn[i] != null)
                lightsToTurnOn[i].enabled = false;
        }

        Debug.Log("[Generator] Reset");
    }
}