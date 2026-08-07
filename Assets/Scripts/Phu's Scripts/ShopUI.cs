using System.Collections;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    [Header("UI Panels & Frames")]
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private bool isTesting;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button openButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float panelDuration = 0.35f;
    [SerializeField] private float buttonDuration = 0.2f;
    [SerializeField] private Vector3 closedScale = new Vector3(0.4f, 0.0f, 1.0f);
    [SerializeField] private Vector3 openedScale = new Vector3(1.0f, 1.0f, 1.0f);

    private Coroutine scaleCoroutine;
    private Coroutine buttonScaleCoroutine;
    private bool isSubscribed = false;
    private bool isShopUnlocked = false;

    [Header("Dynamic Cell Generation")]
    [SerializeField] private GameObject shopCellPrefab;
    [SerializeField] private Transform container1;
    [SerializeField] private Transform container2;
    [SerializeField] private Transform container3;

    public Transform Container1 => container1;
    public Transform Container2 => container2;
    public Transform Container3 => container3;

    [Header("Tooltip Configuration")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipTitleText;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipDescText;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipSpecsText1;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipSpecsText2;
    [SerializeField] private float tooltipOffset = 20f;



    private void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenShop);
            openButton.onClick.AddListener(OpenShop);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
            closeButton.onClick.AddListener(CloseShop);
        }

        // Initially closed and squashed
        if (shopPanel != null)
        {
            shopPanel.localScale = closedScale;
            shopPanel.gameObject.SetActive(false);
        }

        // Initially hide the open button until Wave 1 is completed
        if (openButton != null)
        {
            openButton.transform.localScale = closedScale;
            openButton.gameObject.SetActive(false);
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }

        TrySubscribeEvents();
    }

    private void Update()
    {
        UpdateShopAvailability();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        if (shopPanel != null)
        {
            if (shopPanel.gameObject.activeSelf)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }
    }

    private void OnEnable()
    {
        TrySubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void TrySubscribeEvents()
    {
        if (isSubscribed || WaveManager.Instance == null) return;
        WaveManager.Instance.OnWaveStateChanged += HandleWaveStateChanged;
        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || WaveManager.Instance == null) return;
        WaveManager.Instance.OnWaveStateChanged -= HandleWaveStateChanged;
        isSubscribed = false;
    }

    private void UpdateShopAvailability()
    {
        if (isShopUnlocked) return;
        if (WaveManager.Instance == null) return;

        // Check if shop has been unlocked (available after wave 1, i.e., currentWave >= 2) or if testing is enabled
        if (WaveManager.Instance.CurrentWave >= 2 || isTesting)
        {
            isShopUnlocked = true;
            if (openButton != null)
            {
                openButton.gameObject.SetActive(true);
                TriggerButtonScale(true, 0f);
            }
        }
    }

    private void HandleWaveStateChanged(WaveState state)
    {
        // Double check unlocking state on state changes
        if (!isShopUnlocked && (WaveManager.Instance != null && (WaveManager.Instance.CurrentWave >= 2 || isTesting)))
        {
            isShopUnlocked = true;
            if (openButton != null)
            {
                openButton.gameObject.SetActive(true);
                TriggerButtonScale(true, 0f);
            }
        }

        if (!isShopUnlocked) return;

        if (state == WaveState.WaveActive)
        {
            CloseShop();
            // Squash the button immediately when a wave goes active
            TriggerButtonScale(false, 0f);
        }
        else if (state == WaveState.WaveCompleted || state == WaveState.Preparing)
        {
            // Expand button back after 0.5 seconds when the wave ends or preparing starts
            TriggerButtonScale(true, 0.5f);
        }
    }

    /// <summary>
    /// Open the shop, expanding the panel scale from squashed to original size.
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel == null) return;

        // Prevent opening during active waves or if the shop isn't unlocked yet
        if (!isShopUnlocked || (WaveManager.Instance != null && WaveManager.Instance.CurrentState == WaveState.WaveActive))
        {
            Debug.Log("[ShopUI] Shop is disabled while wave is active or locked.");
            return;
        }

        // Activate the script's own GameObject and the panel to prevent coroutine start issues
        gameObject.SetActive(true);
        shopPanel.gameObject.SetActive(true);

        // Squash the open button when opening the shop
        TriggerButtonScale(false, 0f);

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleRoutine(true));
    }

    /// <summary>
    /// Close the shop, squashing the panel scale vertically/horizontally.
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel == null) return;

        HideTooltip();

        // Expand open button back when closing the shop, waiting 0.5 seconds
        if (WaveManager.Instance != null && WaveManager.Instance.CurrentState != WaveState.WaveActive)
        {
            TriggerButtonScale(true, 0.25f);
        }

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleRoutine(false));
    }

    private void TriggerButtonScale(bool expand, float delay)
    {
        if (openButton == null) return;

        if (buttonScaleCoroutine != null)
        {
            StopCoroutine(buttonScaleCoroutine);
        }
        buttonScaleCoroutine = StartCoroutine(ScaleButtonRoutine(expand, delay));
    }

    private IEnumerator ScaleButtonRoutine(bool expand, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        openButton.gameObject.SetActive(true);

        RectTransform buttonRect = openButton.GetComponent<RectTransform>();
        Vector3 startScale = buttonRect.localScale;
        Vector3 targetScale = expand ? Vector3.one : closedScale;

        float elapsed = 0f;
        while (elapsed < buttonDuration)
        {
            if (buttonRect == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / buttonDuration;

            // Smooth ease for button squash/expansion
            float ease = t * t * (3f - 2f * t);

            buttonRect.localScale = Vector3.Lerp(startScale, targetScale, ease);
            yield return null;
        }

        buttonRect.localScale = targetScale;

        if (!expand)
        {
            openButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator ScaleRoutine(bool open)
    {
        Vector3 startScale = shopPanel.localScale;
        Vector3 targetScale = open ? openedScale : closedScale;

        if (open)
        {
            shopPanel.gameObject.SetActive(true);
            
            // Unlock cursor for interacting with the shop
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float elapsed = 0f;
        while (elapsed < panelDuration)
        {
            if (shopPanel == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / panelDuration;

            // Ease Out Back for opening (creates a springy pop-in feel)
            // Ease In Back for closing (creates a springy snap-out feel)
            float ease = 0f;
            float c1 = 1.70158f;
            float c3 = c1 + 1f;

            if (open)
            {
                float x = t - 1f;
                ease = 1f + c3 * x * x * x + c1 * x * x;
            }
            else
            {
                ease = c3 * t * t * t - c1 * t * t;
            }

            shopPanel.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
            yield return null;
        }

        if (shopPanel != null)
        {
            shopPanel.localScale = targetScale;

            if (!open)
            {
                shopPanel.gameObject.SetActive(false);

                // Relock cursor for gameplay
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private Coroutine shakeCoroutine;

    public void TriggerShakeEffect()
    {
        if (shopPanel == null) return;
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeShopPanelCoroutine(0.15f, 15f));
    }

    private System.Collections.IEnumerator ShakeShopPanelCoroutine(float duration, float magnitude)
    {
        Vector2 originalPos = shopPanel.anchoredPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float xOffset = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            shopPanel.anchoredPosition = new Vector2(originalPos.x + xOffset, originalPos.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shopPanel.anchoredPosition = originalPos;
        shakeCoroutine = null;
    }

    

    public void CreateWeaponCell(Transform parent, string name, string desc, int price, Sprite icon, string[] specs, string weaponId)
    {
        if (shopCellPrefab == null || parent == null) return;
        GameObject cellObj = Instantiate(shopCellPrefab, parent);
        PhuScene.WeaponShopItem item = cellObj.AddComponent<PhuScene.WeaponShopItem>();
        AutoBindUIReferences(item, cellObj);
        item.SetupWeapon(name, desc, price, icon, specs, weaponId);
        item.InitializeRuntime();
    }

    public void CreateAmmoCell(Transform parent, string name, string desc, int price, Sprite icon, string[] specs)
    {
        if (shopCellPrefab == null || parent == null) return;
        GameObject cellObj = Instantiate(shopCellPrefab, parent);
        PhuScene.AmmoShopItem item = cellObj.AddComponent<PhuScene.AmmoShopItem>();
        AutoBindUIReferences(item, cellObj);
        item.SetupAmmo(name, desc, price, icon, specs);
        item.InitializeRuntime();
    }

    public void CreateAllyCell(Transform parent, string name, string desc, int price, Sprite icon, string[] specs, GameObject prefab, int maxCount)
    {
        if (shopCellPrefab == null || parent == null) return;
        GameObject cellObj = Instantiate(shopCellPrefab, parent);
        PhuScene.AllyShopItem item = cellObj.AddComponent<PhuScene.AllyShopItem>();
        AutoBindUIReferences(item, cellObj);
        item.SetupAlly(name, desc, price, icon, specs, prefab, maxCount);
        item.InitializeRuntime();
    }

    private void AutoBindUIReferences(PhuScene.BaseShopItem item, GameObject cellObj)
    {
        item.TitleText = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "Title");
        item.DescText = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "Description");
        item.PriceText = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "BuyPrice");
        item.SpecsText1 = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "Specs1");
        item.SpecsText2 = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "Specs2");
        item.IconImage = FindComponentByName<UnityEngine.UI.Image>(cellObj, "DisplayImage");
        item.BuyButton = cellObj.GetComponentInChildren<UnityEngine.UI.Button>(true);
        item.DisplayCount = FindChildByName(cellObj, "DisplayCount");
        item.DisplayCountText = FindComponentByName<TMPro.TextMeshProUGUI>(cellObj, "DisplayCount");
        item.DisplayTick = FindChildByName(cellObj, "DisplayTick");
    }

    private GameObject FindChildByName(GameObject root, string name)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in transforms)
        {
            if (t.gameObject.name == name)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    private T FindComponentByName<T>(GameObject root, string name) where T : Component
    {
        T[] comps = root.GetComponentsInChildren<T>(true);
        foreach (T comp in comps)
        {
            if (comp.gameObject.name == name)
            {
                return comp;
            }
        }
        return null;
    }

    public void ShowTooltip(string title, string desc, string[] specs, RectTransform targetRect)
    {
        if (tooltipPanel == null || targetRect == null) return;

        if (tooltipTitleText != null) tooltipTitleText.text = title;
        if (tooltipDescText != null) tooltipDescText.text = desc;

        System.Text.StringBuilder sb1 = new System.Text.StringBuilder();
        System.Text.StringBuilder sb2 = new System.Text.StringBuilder();
        if (specs != null)
        {
            for (int i = 0; i < specs.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (sb1.Length > 0) sb1.Append("\n");
                    sb1.Append(specs[i]);
                }
                else
                {
                    if (sb2.Length > 0) sb2.Append("\n");
                    sb2.Append(specs[i]);
                }
            }
        }
        if (tooltipSpecsText1 != null) tooltipSpecsText1.text = sb1.ToString();
        if (tooltipSpecsText2 != null) tooltipSpecsText2.text = sb2.ToString();

        tooltipPanel.gameObject.SetActive(true);

        // Position tooltip next to target button
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        // corners: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        Vector3 targetCenter = (corners[0] + corners[2]) * 0.5f;

        float screenWidth = Screen.width;
        bool isLeft = targetCenter.x < screenWidth * 0.5f;

        float currentOffset = tooltipOffset;
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                currentOffset *= canvas.scaleFactor;
            }
            else
            {
                currentOffset *= tooltipPanel.lossyScale.x;
            }
        }

        Vector3 targetPos = targetCenter;
        if (isLeft)
        {
            targetPos.x = corners[2].x + currentOffset;
            tooltipPanel.pivot = new Vector2(0f, 0.5f);
        }
        else
        {
            targetPos.x = corners[0].x - currentOffset;
            tooltipPanel.pivot = new Vector2(1f, 0.5f);
        }

        tooltipPanel.position = targetPos;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }

    private bool hasShownSwitchHint = false;

    public void OnWeaponPurchased(string weaponId)
    {
        if (!hasShownSwitchHint)
        {
            hasShownSwitchHint = true;
            HintUI foundHintList = FindFirstObjectByType<HintUI>();
            if (foundHintList != null)
            {
                foundHintList.AddHint("Press 1,2,3... to switch weapons");
            }
        }
    }
}
