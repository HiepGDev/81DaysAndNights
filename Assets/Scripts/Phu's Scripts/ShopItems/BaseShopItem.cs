using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PhuScene
{
    public abstract class BaseShopItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // Shared Information (initialized at runtime by SetupItem)
        protected string itemName;
        protected string itemDescription;
        protected int price;
        protected Sprite icon;
        protected string[] specs;

        [Header("UI References")]
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected TextMeshProUGUI descText;
        [SerializeField] protected TextMeshProUGUI priceText;
        [SerializeField] protected TextMeshProUGUI specsText1;
        [SerializeField] protected TextMeshProUGUI specsText2;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected Button buyLabel;
        [SerializeField] protected TextMeshProUGUI statusText;

        [Header("Buy Label Animation Settings")]
        [SerializeField] protected RectTransform buyLabelRect;
        [SerializeField] protected Vector2 buyLabelTuckOffset = new Vector2(0f, 50f);
        [SerializeField] protected float buyLabelAnimSpeed = 12f;

        // Public setters to bind components programmatically from ShopUI
        public TextMeshProUGUI TitleText { get => titleText; set => titleText = value; }
        public TextMeshProUGUI DescText { get => descText; set => descText = value; }
        public TextMeshProUGUI PriceText { get => priceText; set => priceText = value; }
        public TextMeshProUGUI SpecsText1 { get => specsText1; set => specsText1 = value; }
        public TextMeshProUGUI SpecsText2 { get => specsText2; set => specsText2 = value; }
        public Image IconImage { get => iconImage; set => iconImage = value; }
        public Button BuyButton { get => buyLabel; set => buyLabel = value; }
        public TextMeshProUGUI StatusText { get => statusText; set => statusText = value; }

        private RectTransform myRectTransform;
        private Vector2 buyLabelHoverPos;
        private Vector2 buyLabelTuckedPos;
        private Vector2 buyLabelTargetPos;

        protected virtual void Awake()
        {
            myRectTransform = GetComponent<RectTransform>();
            SetupAnimationPositions();
        }

        protected virtual void Start()
        {
            InitializeRuntime();
        }

        protected virtual void Update()
        {
            UpdateUIState();
            AnimateBuyLabel();
        }

        public virtual void SetupItem(string name, string desc, int price, Sprite icon, string[] specs)
        {
            this.itemName = name;
            this.itemDescription = desc;
            this.price = price;
            this.icon = icon;
            this.specs = specs;
        }

        public void InitializeRuntime()
        {
            SetupAnimationPositions();

            if (buyLabel != null)
            {
                buyLabel.onClick.RemoveListener(BuyItem);
                buyLabel.onClick.AddListener(BuyItem);
            }
            UpdateUI();
            UpdateUIState();
        }

        private void SetupAnimationPositions()
        {
            if (buyLabelRect == null)
            {
                Transform child = transform.Find("BuyLabel");
                buyLabelRect = child.GetComponent<RectTransform>();
            }

            if (buyLabelRect != null && buyLabelHoverPos == Vector2.zero && buyLabelTuckedPos == Vector2.zero)
            {
                buyLabelHoverPos = buyLabelRect.anchoredPosition;
                buyLabelTuckedPos = buyLabelHoverPos + buyLabelTuckOffset;
                buyLabelRect.anchoredPosition = buyLabelTuckedPos;
                buyLabelTargetPos = buyLabelTuckedPos;
            }
        }

        private void AnimateBuyLabel()
        {
            if (buyLabelRect != null)
            {
                buyLabelRect.anchoredPosition = Vector2.Lerp(
                    buyLabelRect.anchoredPosition, 
                    buyLabelTargetPos, 
                    Time.deltaTime * buyLabelAnimSpeed
                );
            }
        }

        public virtual void UpdateUI()
        {
            if (titleText != null) titleText.text = itemName;
            if (descText != null) descText.text = itemDescription;
            if (priceText != null) priceText.text = $"BUY - {price}";
            if (iconImage != null && icon != null) iconImage.sprite = icon;

            if (specs != null)
            {
                System.Text.StringBuilder sb1 = new System.Text.StringBuilder();
                System.Text.StringBuilder sb2 = new System.Text.StringBuilder();
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
                if (specsText1 != null) specsText1.text = sb1.ToString();
                if (specsText2 != null) specsText2.text = sb2.ToString();
            }
        }

        public virtual void UpdateUIState()
        {
            if (buyLabel == null) return;

            bool canAfford = false;
            if (WaveManager.Instance != null)
            {
                canAfford = WaveManager.Instance.Money >= price;
            }

            buyLabel.interactable = canAfford && IsPurchaseable();
        }

        public virtual void BuyItem()
        {
            if (!IsPurchaseable()) return;

            if (WaveManager.Instance != null)
            {
                if (WaveManager.Instance.Money >= price)
                {
                    if (WaveManager.Instance.TrySpendMoney(price))
                    {
                        OnPurchaseSuccess();
                        UpdateUIState();
                    }
                }
            }
        }

        protected abstract bool IsPurchaseable();
        protected abstract void OnPurchaseSuccess();

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.ShowTooltip(itemName, itemDescription, specs, myRectTransform);
            }
            buyLabelTargetPos = buyLabelHoverPos; // Slide down on hover
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.HideTooltip();
            }
            buyLabelTargetPos = buyLabelTuckedPos; // Slide up on unhover
        }

        protected virtual void OnDisable()
        {
            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.HideTooltip();
            }
            // Instantly snap to tucked position to prevent hovering glitches when disabled
            buyLabelTargetPos = buyLabelTuckedPos;
            if (buyLabelRect != null)
            {
                buyLabelRect.anchoredPosition = buyLabelTuckedPos;
            }
        }
    }
}
