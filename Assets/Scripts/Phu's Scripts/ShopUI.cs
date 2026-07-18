using System.Collections;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [Header("UI Panels & Frames")]
    [SerializeField] private RectTransform shopPanel;

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

        TrySubscribeEvents();
    }

    private void Update()
    {
        UpdateShopAvailability();
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
        if (WaveManager.Instance == null) return;

        // Check if shop has been unlocked (available after wave 1, i.e., currentWave >= 2)
        if (!isShopUnlocked && WaveManager.Instance.CurrentWave >= 2)
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
        if (WaveManager.Instance != null && WaveManager.Instance.CurrentWave >= 2 && !isShopUnlocked)
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
}
