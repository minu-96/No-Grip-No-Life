using UnityEngine;
using UnityEngine.UI;

public class StaminaVignette : MonoBehaviour
{
    [SerializeField] private Image vignetteImage;

    [SerializeField] private float warningThreshold = 20f;
    [SerializeField] private float criticalThreshold = 0f;
    [SerializeField] private float maxAlpha = 0.65f;
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private bool logVignetteUpdates = false;

    private float currentAlpha;

    private void Awake()
    {
        if (vignetteImage == null)
        {
            Debug.LogError("[StaminaVignette] Vignette Image가 연결되지 않았습니다.");
            return;
        }

        vignetteImage.sprite = CreateVignetteSprite(512);
        vignetteImage.color = new Color(1f, 0f, 0f, 0f);
        vignetteImage.raycastTarget = false;

        Debug.Log("[StaminaVignette] 비네트 스프라이트 생성 완료");
    }

    public void UpdateVignette(float stamina)
    {
        if (vignetteImage == null) return;

        float targetAlpha = 0f;

        if (stamina <= warningThreshold)
        {
            float t = Mathf.InverseLerp(warningThreshold, criticalThreshold, stamina);
            targetAlpha = Mathf.Lerp(0f, maxAlpha, t);
        }

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        Color color = vignetteImage.color;
        color.a = currentAlpha;
        vignetteImage.color = color;

        if (logVignetteUpdates)
        {
            Debug.Log($"Stamina: {stamina}, Vignette Alpha: {currentAlpha}");
        }
    }

    private Sprite CreateVignetteSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDistance = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.45f, 1f, distance)
                );

                texture.SetPixel(x, y, new Color(1f, 0f, 0f, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
    }
}