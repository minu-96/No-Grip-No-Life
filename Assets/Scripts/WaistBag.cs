using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;

public class WaistBag : MonoBehaviour
{
    [Header("Tracking Target")]
    [SerializeField] private Transform targetCamera;

    [Header("Position Tuning")]
    [SerializeField] private float heightOffset = -0.55f;
    [SerializeField] private float forwardOffset = -0.1f;

    [Header("Rotation Tuning")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 90f, 0f);

    [Header("Inventory")]
    [SerializeField] private string[] itemNames = { "Handbook", "Cam", "EnergyBar", "Pickel" };
    [SerializeField] private float handAccessRadius = 0.45f;
    [SerializeField] private float itemPickupRadius = 0.5f;
    [SerializeField] private float gripThreshold = 0.65f;
    [SerializeField] private float energyBarRestoreAmount = 35f;
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0f, -0.02f, 0.12f);
    [SerializeField] private Vector3 heldLocalEulerAngles = new Vector3(15f, 0f, 0f);
    [SerializeField] private bool hideCurrentItemAfterLast = true;

    [Header("Handbook Tutorial")]
    [TextArea(8, 16)]
    [SerializeField] private string handbookText =
        "NO GRIP, NO LIFE\n\n" +
        "그랩 포인트를 Grip하여\n" +
        "설산을 등반하세요.\n\n" +
        "정상에서 발전기 레버를 당겨\n" +
        "송전탑을 복구하세요.\n" +
        "생존 시스템을 다시\n" +
        "가동시켜야 합니다.\n\n" +
        "아이템 조작\n" +
        "보관: Grip 후 허리춤 가방에서\n" +
        "      다시 Grip\n" +
        "X: 캠 꺼내기 / 설치\n" +
        "Y: 피켈 꺼내기\n" +
        "A: 에너지 바 섭취\n" +
        "B: 일지 열기";
    [SerializeField] private Vector3 handbookTextLocalPosition = new Vector3(0f, 0.00015f, 0.00025f);
    [SerializeField] private Vector3 handbookTextLocalEulerAngles = new Vector3(90f, 0f, 0f);
    [SerializeField] private Vector3 handbookTextLocalScale = new Vector3(0.01f, 0.01f, 0.01f);
    [SerializeField] private float handbookTextSize = 0.002f;
    [SerializeField] private Color handbookTextColor = new Color(0.01f, 0.015f, 0.025f, 1f);
    [SerializeField] private bool mirrorHandbookTextX = true;
    [SerializeField] private bool mirrorHandbookTextY = false;
    [SerializeField] private bool useHandbookTextPanel = true;
    [SerializeField] private Color handbookPanelColor = new Color(0.96f, 0.94f, 0.86f, 0.3f);
    [SerializeField] private Vector3 handbookPanelLocalPosition = new Vector3(0f, 0.00008f, 0.00024f);
    [SerializeField] private Vector3 handbookPanelLocalScale = new Vector3(0.004f, 0.0021f, 1f);
    [TextArea(5, 10)]
    [SerializeField] private string handbookLeftPageText =
        "NO GRIP, NO LIFE\n\n" +
        "그랩 포인트를 Grip하여\n" +
        "설산을 등반하세요.\n\n" +
        "정상에서 발전기 레버를 당겨\n" +
        "송전탑을 복구하세요.\n" +
        "생존 시스템을 다시\n" +
        "가동시켜야 합니다.";
    [TextArea(5, 10)]
    [SerializeField] private string handbookRightPageText =
        "아이템 조작\n\n" +
        "보관: Grip 후 허리춤 가방에서\n" +
        "      다시 Grip\n\n" +
        "X: 캠 꺼내기 / 설치\n" +
        "Y: 피켈 꺼내기\n" +
        "A: 에너지 바 섭취\n" +
        "B: 일지 열기";
    [SerializeField] private Vector3 handbookLeftPageLocalPosition = new Vector3(-0.00105f, 0.00008f, 0.0003f);
    [SerializeField] private Vector3 handbookRightPageLocalPosition = new Vector3(0.00105f, 0.00008f, 0.0003f);
    [SerializeField] private Vector3 handbookPageLocalEulerAngles = new Vector3(0f, 0f, 180f);

    private GameObject[] inventoryItems;
    private bool[] storedItems;
    private int currentItemIndex = -1;
    private int heldItemIndex = -1;
    private bool wasLeftPressed;
    private bool wasRightPressed;
    private bool wasLeftPrimaryPressed;
    private bool wasLeftSecondaryPressed;
    private bool wasRightPrimaryPressed;
    private bool wasRightSecondaryPressed;

    private void Start()
    {
        CacheInventoryItems();
    }

    private void LateUpdate()
    {
        if (targetCamera != null)
        {
            FollowWaist();
        }

        HandleInventoryInput();
    }

    private void FollowWaist()
    {
        Vector3 cameraForwardHorizontal = targetCamera.forward;
        cameraForwardHorizontal.y = 0f;
        cameraForwardHorizontal.Normalize();

        transform.position = targetCamera.position
                             + Vector3.up * heightOffset
                             + cameraForwardHorizontal * forwardOffset;

        if (cameraForwardHorizontal.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(cameraForwardHorizontal) *
                                 Quaternion.Euler(rotationOffset);
        }
    }

    private void CacheInventoryItems()
    {
        inventoryItems = new GameObject[itemNames.Length];
        storedItems = new bool[itemNames.Length];
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        for (int i = 0; i < itemNames.Length; i++)
        {
            for (int j = 0; j < allTransforms.Length; j++)
            {
                Transform candidate = allTransforms[j];
                if (candidate == null || candidate.gameObject == gameObject) continue;
                if (candidate.name != itemNames[i]) continue;

                inventoryItems[i] = candidate.gameObject;
                break;
            }

            if (inventoryItems[i] != null && inventoryItems[i].name == "Handbook")
            {
                EnsureHandbookText(inventoryItems[i].transform);
            }
        }
    }

    private void StoreItem(int index)
    {
        if (inventoryItems == null || index < 0 || index >= inventoryItems.Length) return;

        GameObject item = inventoryItems[index];
        if (item == null) return;

        item.transform.SetParent(transform, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.SetActive(false);

        storedItems[index] = true;
        if (heldItemIndex == index) heldItemIndex = -1;
    }

    private void HandleInventoryInput()
    {
        ClimbingHand leftHand = ClimbingManager.Instance != null ? ClimbingManager.Instance.leftHand : null;
        ClimbingHand rightHand = ClimbingManager.Instance != null ? ClimbingManager.Instance.rightHand : null;

        HandleHandGripInput(leftHand, XRNode.LeftHand, ref wasLeftPressed);
        HandleHandGripInput(rightHand, XRNode.RightHand, ref wasRightPressed);
        HandleBagButtonInput(leftHand, rightHand);
    }

    private void HandleHandGripInput(ClimbingHand hand, XRNode node, ref bool wasPressed)
    {
        if (hand == null)
        {
            wasPressed = false;
            return;
        }

        bool isPressed = IsGripPressed(hand, node);
        bool pressedThisFrame = isPressed && !wasPressed;
        wasPressed = isPressed;

        if (!pressedThisFrame) return;

        Transform handTransform = GetHandTransform(hand);
        if (handTransform == null) return;

        if (Vector3.Distance(handTransform.position, transform.position) <= handAccessRadius)
        {
            UseBag(handTransform);
            return;
        }

        TryPickUpSceneItem(handTransform);
    }

    private void HandleBagButtonInput(ClimbingHand leftHand, ClimbingHand rightHand)
    {
        Transform leftTransform = GetHandTransform(leftHand);
        Transform rightTransform = GetHandTransform(rightHand);

        HandleItemButton(leftTransform, XRNode.LeftHand, UnityEngine.XR.CommonUsages.primaryButton, ref wasLeftPrimaryPressed, "Cam");
        HandleItemButton(leftTransform, XRNode.LeftHand, UnityEngine.XR.CommonUsages.secondaryButton, ref wasLeftSecondaryPressed, "Pickel");
        HandleItemButton(rightTransform, XRNode.RightHand, UnityEngine.XR.CommonUsages.primaryButton, ref wasRightPrimaryPressed, "EnergyBar");
        HandleItemButton(rightTransform, XRNode.RightHand, UnityEngine.XR.CommonUsages.secondaryButton, ref wasRightSecondaryPressed, "Handbook");
    }

    private void HandleItemButton(
        Transform handTransform,
        XRNode node,
        InputFeatureUsage<bool> button,
        ref bool wasPressed,
        string itemName)
    {
        bool isPressed = ReadButton(node, button);
        bool pressedThisFrame = isPressed && !wasPressed;
        wasPressed = isPressed;

        if (!pressedThisFrame) return;
        if (handTransform == null) return;
        if (Vector3.Distance(handTransform.position, transform.position) > handAccessRadius) return;

        DrawStoredItem(itemName, handTransform);
    }

    private bool IsGripPressed(ClimbingHand hand, XRNode node)
    {
        InputAction gripAction = hand.gripAction.action;
        if (gripAction != null)
        {
            gripAction.Enable();
            if (gripAction.ReadValue<float>() >= gripThreshold)
                return true;
        }

        XRInputDevice device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid &&
               device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) &&
               gripValue >= gripThreshold;
    }

    private bool ReadButton(XRNode node, InputFeatureUsage<bool> button)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid &&
               device.TryGetFeatureValue(button, out bool pressed) &&
               pressed;
    }

    private Transform GetHandTransform(ClimbingHand hand)
    {
        if (hand == null) return null;

        if (hand.grabDetector != null)
            return hand.grabDetector.transform;

        return hand.transform;
    }

    private void UseBag(Transform handTransform)
    {
        if (inventoryItems == null || inventoryItems.Length == 0)
        {
            CacheInventoryItems();
        }

        if (heldItemIndex >= 0)
        {
            StoreItem(heldItemIndex);
            currentItemIndex = -1;
            return;
        }
    }

    private void TryPickUpSceneItem(Transform handTransform)
    {
        if (inventoryItems == null || inventoryItems.Length == 0)
        {
            CacheInventoryItems();
        }

        if (inventoryItems == null || heldItemIndex >= 0) return;

        int nearestIndex = -1;
        float nearestDistance = itemPickupRadius;

        for (int i = 0; i < inventoryItems.Length; i++)
        {
            GameObject item = inventoryItems[i];
            if (item == null || storedItems[i]) continue;

            float distance = GetDistanceToItem(handTransform.position, item);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        if (nearestIndex >= 0)
        {
            HoldItem(nearestIndex, handTransform);
        }
    }

    private float GetDistanceToItem(Vector3 handPosition, GameObject item)
    {
        float nearestDistance = Vector3.Distance(handPosition, item.transform.position);

        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null) continue;

            float distance = Vector3.Distance(handPosition, itemCollider.ClosestPoint(handPosition));
            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer itemRenderer = renderers[i];
            if (itemRenderer == null) continue;

            float distance = Vector3.Distance(handPosition, itemRenderer.bounds.ClosestPoint(handPosition));
            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        return nearestDistance;
    }

    private void DrawStoredItem(string itemName, Transform handTransform)
    {
        if (inventoryItems == null || inventoryItems.Length == 0)
        {
            CacheInventoryItems();
        }

        if (inventoryItems == null || storedItems == null) return;

        int itemIndex = GetItemIndex(itemName);
        if (itemIndex < 0 || !storedItems[itemIndex]) return;

        if (itemName == "EnergyBar")
        {
            ConsumeEnergyBar(itemIndex);
            return;
        }

        HoldItem(itemIndex, handTransform);
    }

    private void ConsumeEnergyBar(int itemIndex)
    {
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null)
        {
            stamina.currentStamina = Mathf.Clamp(
                stamina.currentStamina + energyBarRestoreAmount,
                0f,
                stamina.maxStamina);

            if (stamina.staminaVignette != null)
                stamina.staminaVignette.UpdateVignette(stamina.currentStamina);
        }

        GameObject item = inventoryItems[itemIndex];
        if (item != null)
        {
            item.transform.SetParent(transform, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetActive(false);
        }

        storedItems[itemIndex] = false;
        if (currentItemIndex == itemIndex) currentItemIndex = -1;
        if (heldItemIndex == itemIndex) heldItemIndex = -1;
    }

    private int GetItemIndex(string itemName)
    {
        for (int i = 0; i < itemNames.Length; i++)
        {
            if (itemNames[i] == itemName)
                return i;
        }

        return -1;
    }

    private void DrawNextStoredItem(Transform handTransform)
    {
        if (inventoryItems == null || storedItems == null) return;

        for (int attempts = 0; attempts < inventoryItems.Length; attempts++)
        {
            currentItemIndex++;
            if (currentItemIndex >= inventoryItems.Length)
            {
                if (hideCurrentItemAfterLast)
                {
                    currentItemIndex = -1;
                    return;
                }

                currentItemIndex = 0;
            }

            if (inventoryItems[currentItemIndex] != null && storedItems[currentItemIndex])
            {
                HoldItem(currentItemIndex, handTransform);
                return;
            }
        }
    }

    private void HoldItem(int itemIndex, Transform handTransform)
    {
        if (inventoryItems == null || itemIndex < 0 || itemIndex >= inventoryItems.Length) return;

        if (heldItemIndex >= 0 && heldItemIndex != itemIndex)
        {
            StoreItem(heldItemIndex);
        }

        GameObject item = inventoryItems[itemIndex];
        if (item == null) return;

        currentItemIndex = itemIndex;
        heldItemIndex = itemIndex;
        storedItems[itemIndex] = false;

        item.SetActive(true);
        item.transform.SetParent(handTransform, false);
        item.transform.localPosition = heldLocalPosition;
        item.transform.localRotation = Quaternion.Euler(heldLocalEulerAngles);
    }

    private void EnsureHandbookText(Transform handbook)
    {
        Transform existingText = handbook.Find("TutorialText");
        if (existingText != null)
        {
            existingText.gameObject.SetActive(false);
        }

        EnsureHandbookPanel(handbook);

        ConfigureHandbookPage(
            handbook,
            "LeftPageText",
            handbookLeftPageText,
            handbookLeftPageLocalPosition);

        ConfigureHandbookPage(
            handbook,
            "RightPageText",
            handbookRightPageText,
            handbookRightPageLocalPosition);
    }

    private void ConfigureHandbookPage(
        Transform handbook,
        string pageName,
        string pageText,
        Vector3 pageLocalPosition)
    {
        Transform existingPage = handbook.Find(pageName);
        GameObject pageObject;
        if (existingPage != null)
        {
            pageObject = existingPage.gameObject;
        }
        else
        {
            pageObject = new GameObject(pageName);
            pageObject.transform.SetParent(handbook, false);
            pageObject.transform.localPosition = pageLocalPosition;
            pageObject.transform.localRotation = Quaternion.Euler(handbookPageLocalEulerAngles);
            pageObject.transform.localScale = handbookTextLocalScale;
        }

        pageObject.SetActive(true);
        ApplyHandbookTextMirror(pageObject.transform);

        TextMesh textMesh = pageObject.GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = pageObject.AddComponent<TextMesh>();

        textMesh.text = pageText;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 64;
        textMesh.characterSize = handbookTextSize;
        textMesh.color = handbookTextColor;

        DisableOldPagePanel(pageObject.transform);
    }

    private void EnsureHandbookPanel(Transform handbook)
    {
        if (!useHandbookTextPanel) return;

        string panelName = "ReadablePanel";
        Transform existingPanel = handbook.Find(panelName);
        GameObject panelObject;

        if (existingPanel != null)
        {
            panelObject = existingPanel.gameObject;
        }
        else
        {
            panelObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelObject.name = panelName;
            panelObject.transform.SetParent(handbook, false);
            panelObject.transform.localPosition = handbookPanelLocalPosition;
            panelObject.transform.localRotation = Quaternion.Euler(handbookPageLocalEulerAngles);
            panelObject.transform.localScale = handbookPanelLocalScale;

            Collider panelCollider = panelObject.GetComponent<Collider>();
            if (panelCollider != null)
                Destroy(panelCollider);
        }

        panelObject.SetActive(true);

        MeshRenderer renderer = panelObject.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        if (renderer.sharedMaterial == null || renderer.sharedMaterial.name != "HandbookPanelMaterial")
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader);
            material.name = "HandbookPanelMaterial";
            material.renderQueue = 2950;
            renderer.sharedMaterial = material;
        }

        ConfigureTransparentPanelMaterial(renderer.sharedMaterial);
        renderer.sharedMaterial.color = handbookPanelColor;
    }

    private void ConfigureTransparentPanelMaterial(Material material)
    {
        if (material == null) return;

        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 2950;
    }

    private void DisableOldPagePanel(Transform pageTransform)
    {
        Transform oldPanel = pageTransform.Find("ReadablePanel");
        if (oldPanel != null)
            oldPanel.gameObject.SetActive(false);
    }

    private void ApplyHandbookTextMirror(Transform pageTransform)
    {
        Vector3 currentScale = pageTransform.localScale;
        currentScale.x = mirrorHandbookTextX
            ? -Mathf.Abs(currentScale.x)
            : Mathf.Abs(currentScale.x);
        currentScale.y = mirrorHandbookTextY
            ? -Mathf.Abs(currentScale.y)
            : Mathf.Abs(currentScale.y);
        pageTransform.localScale = currentScale;
    }
}
