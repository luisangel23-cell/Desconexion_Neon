// CodeStart-AI Curved Carousel Menu Lite (FREE)
// Copyright (c) CodeStart-AI. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodeStartAI.CarouselMenu.Lite
{
    /// <summary>
    /// A carousel card. Put this on the root of your card prefab and assign the parts your design has.
    /// Every reference is optional.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("CodeStart-AI/Carousel Item Lite")]
    public class CarouselItemLite : MonoBehaviour, IPointerClickHandler
    {
        const float HighlightFadeTime = 0.15f;
        const float PopTime = 0.25f;
        const float PopAmount = 0.06f;
        const float DisabledOpacity = 0.45f;

        [Header("Card Parts")]
        [Tooltip("Receives the item's Image.")]
        [SerializeField] Image image = null;
        [Tooltip("Receives the item's Title.")]
        [SerializeField] TMP_Text title = null;
        [Tooltip("Receives the item's Subtitle.")]
        [SerializeField] TMP_Text subtitle = null;

        [Header("Optional")]
        [Tooltip("Shown (faded in) while this card is the centered card: a glow, frame or outline.")]
        [SerializeField] GameObject highlight = null;
        [Tooltip("Optional Button. If empty, clicks are detected on any raycast target inside the card.")]
        [SerializeField] Button button = null;
        [Tooltip("Hide the image or text objects when the item has no value for them.")]
        [SerializeField] bool hideEmptyFields = true;

        bool initialized;
        RectTransform rectTransform;
        CanvasGroup canvasGroup;
        CanvasGroup highlightGroup;
        AspectRatioFitter imageFitter;

        bool hasPose;
        Vector3 lastPosition;
        float lastRotation;
        float lastScale = -1f;
        float lastAlpha = -1f;
        float layoutScale = 1f;
        float stateOpacity = 1f;

        float highlightValue;
        float highlightTarget;
        float popTimer = -1f;
        float popValue;

        /// <summary>Index of the item shown by this card.</summary>
        public int Index { get; private set; } = -1;

        /// <summary>The item shown by this card.</summary>
        public CarouselItemDataLite Data { get; private set; }

        /// <summary>The carousel that created this card.</summary>
        public CurvedCarouselLite Owner { get; private set; }

        /// <summary>True while this card is the centered card.</summary>
        public bool IsCentered { get; private set; }

        /// <summary>False when the item is marked Disabled.</summary>
        public bool IsInteractable { get { return Data == null || Data.Interactable; } }

        /// <summary>Cached RectTransform.</summary>
        public RectTransform RectTransform
        {
            get { if (rectTransform == null) rectTransform = (RectTransform)transform; return rectTransform; }
        }

        /// <summary>Image that receives the item's artwork.</summary>
        public Image ImageTarget { get { return image; } set { image = value; initialized = false; } }

        /// <summary>Text that receives the item's title.</summary>
        public TMP_Text TitleText { get { return title; } set { title = value; } }

        /// <summary>Text that receives the item's subtitle.</summary>
        public TMP_Text SubtitleText { get { return subtitle; } set { subtitle = value; } }

        /// <summary>Object shown while the card is centered.</summary>
        public GameObject Highlight { get { return highlight; } set { highlight = value; highlightGroup = null; initialized = false; } }

        /// <summary>The card's Button (may be null).</summary>
        public Button CardButton { get { return button; } }

        /// <summary>Distance from the center, used for draw order.</summary>
        internal float SortKey { get; set; }

        /// <summary>
        /// Copies <see cref="Data"/> into the card's image and texts.
        /// Override it in a subclass to fill extra UI (call base.Refresh() first).
        /// </summary>
        public virtual void Refresh()
        {
            Initialize();
            CarouselItemDataLite data = Data;

            Sprite sprite = data != null ? data.image : null;
            if (image != null)
            {
                image.sprite = sprite;
                image.enabled = sprite != null || !hideEmptyFields;
                if (imageFitter != null && sprite != null && sprite.rect.height > 0f)
                    imageFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }

            string titleText = data != null ? data.title : "";
            if (title != null)
            {
                title.text = titleText ?? "";
                SetVisible(title, !string.IsNullOrEmpty(titleText) || !hideEmptyFields);
            }

            string subtitleText = data != null ? data.subtitle : "";
            if (subtitle != null)
            {
                subtitle.text = subtitleText ?? "";
                SetVisible(subtitle, !string.IsNullOrEmpty(subtitleText) || !hideEmptyFields);
            }

            bool interactable = IsInteractable;
            stateOpacity = interactable ? 1f : DisabledOpacity;
            if (canvasGroup != null) canvasGroup.interactable = interactable;
            hasPose = false; // re-apply opacity with the new state
        }

        // ------------------------------------------------------------------ Carousel-facing
        internal void Setup(CurvedCarouselLite owner, CarouselItemDataLite data, int index)
        {
            Initialize();
            Owner = owner;
            Data = data;
            Index = index;
            popTimer = -1f;
            popValue = 0f;
            Refresh();
            SetCentered(false, true);
        }

        internal void SetData(CarouselItemDataLite data)
        {
            Data = data;
            Refresh();
        }

        internal void ApplyPose(in CarouselLitePose pose)
        {
            RectTransform rt = RectTransform;
            if (!hasPose || (pose.position - lastPosition).sqrMagnitude > 1e-6f)
            {
                rt.localPosition = pose.position;
                lastPosition = pose.position;
            }
            if (!hasPose || Mathf.Abs(pose.rotation - lastRotation) > 0.001f)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
                lastRotation = pose.rotation;
            }

            layoutScale = pose.scale;
            ApplyScale(!hasPose);

            if (canvasGroup != null)
            {
                float alpha = pose.alpha * stateOpacity;
                if (!hasPose || Mathf.Abs(alpha - lastAlpha) > 0.001f)
                {
                    canvasGroup.alpha = alpha;
                    lastAlpha = alpha;
                }
                bool clickable = pose.alpha > 0.05f;
                if (canvasGroup.blocksRaycasts != clickable) canvasGroup.blocksRaycasts = clickable;
            }
            hasPose = true;
        }

        internal void SetCentered(bool centered, bool instant)
        {
            IsCentered = centered;
            highlightTarget = centered ? 1f : 0f;
            if (instant)
            {
                highlightValue = highlightTarget;
                ApplyHighlight();
            }
        }

        internal void PlaySelectPop()
        {
            popTimer = 0f;
        }

        // ------------------------------------------------------------------ Unity
        void Awake()
        {
            Initialize();
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnButtonClicked);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            if (!Mathf.Approximately(highlightValue, highlightTarget))
            {
                highlightValue = Mathf.MoveTowards(highlightValue, highlightTarget, dt / HighlightFadeTime);
                ApplyHighlight();
            }

            if (popTimer >= 0f)
            {
                popTimer += dt;
                float t = popTimer / PopTime;
                if (t >= 1f)
                {
                    popTimer = -1f;
                    popValue = 0f;
                }
                else
                {
                    popValue = PopAmount * Mathf.Sin(t * Mathf.PI);
                }
                ApplyScale(false);
            }
        }

        /// <summary>Click detection for cards without a Button.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null) return; // the Button reports the click instead
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Owner != null) Owner.HandleCardClicked(this);
        }

        // ------------------------------------------------------------------ Helpers
        void Initialize()
        {
            if (initialized) return;
            initialized = true;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (highlight != null && highlight != gameObject)
            {
                highlightGroup = highlight.GetComponent<CanvasGroup>();
                if (highlightGroup == null) highlightGroup = highlight.AddComponent<CanvasGroup>();
                highlightGroup.blocksRaycasts = false;
                highlightGroup.interactable = false;
            }

            imageFitter = image != null ? image.GetComponent<AspectRatioFitter>() : null;

            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
                button.onClick.AddListener(OnButtonClicked);
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.None; // arrow keys drive the carousel, not Unity's UI navigation
                button.navigation = nav;
            }
        }

        void OnButtonClicked()
        {
            if (Owner != null) Owner.HandleCardClicked(this);
        }

        void ApplyScale(bool force)
        {
            float s = layoutScale * (1f + popValue);
            if (force || Mathf.Abs(s - lastScale) > 0.0001f)
            {
                RectTransform.localScale = new Vector3(s, s, 1f);
                lastScale = s;
            }
        }

        void ApplyHighlight()
        {
            if (highlightGroup != null) highlightGroup.alpha = highlightValue;
            else if (highlight != null && highlight != gameObject && highlight.activeSelf != highlightValue > 0.5f)
                highlight.SetActive(highlightValue > 0.5f);
        }

        void SetVisible(Component component, bool visible)
        {
            if (component == null) return;
            if (component.gameObject == gameObject)
            {
                Behaviour behaviour = component as Behaviour;
                if (behaviour != null) behaviour.enabled = visible;
                return;
            }
            if (component.gameObject.activeSelf != visible) component.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------ Default card
        /// <summary>
        /// Builds a simple card in code (same layout as Prefabs/CarouselItemLite.prefab, without sprites).
        /// Used when no Item Prefab is assigned. The returned GameObject is inactive.
        /// </summary>
        public static CarouselItemLite CreateDefault(Transform parent)
        {
            int layer = parent != null ? parent.gameObject.layer : 5; // 5 = built-in "UI" layer

            GameObject root = new GameObject("CarouselItemLite", typeof(RectTransform));
            root.SetActive(false);
            root.layer = layer;
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.SetParent(parent, false);
            rootRt.sizeDelta = new Vector2(220f, 300f);
            root.AddComponent<CanvasGroup>();

            RectTransform highlightRt = CreateChild("Highlight", rootRt, layer);
            SetRect(highlightRt, Vector2.zero, Vector2.one, new Vector2(-6f, -6f), new Vector2(6f, 6f));
            Image highlightImage = highlightRt.gameObject.AddComponent<Image>();
            highlightImage.color = new Color(0.22f, 0.78f, 1f, 1f);
            highlightImage.raycastTarget = false;

            RectTransform backgroundRt = CreateChild("Background", rootRt, layer);
            SetRect(backgroundRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = backgroundRt.gameObject.AddComponent<Image>();
            background.color = new Color(0.1f, 0.11f, 0.14f, 1f);

            RectTransform imageRt = CreateChild("Image", rootRt, layer);
            SetRect(imageRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -200f), new Vector2(-10f, -10f));
            Image cardImage = imageRt.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.25f, 0.27f, 0.33f, 1f);
            cardImage.raycastTarget = false;

            RectTransform titleRt = CreateChild("Title", rootRt, layer);
            SetRect(titleRt, Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 46f), new Vector2(-12f, 84f));
            TextMeshProUGUI titleText = AddText(titleRt, "Title", 24f, Color.white, FontStyles.Bold);
            titleText.characterSpacing = 4f;

            RectTransform subtitleRt = CreateChild("Subtitle", rootRt, layer);
            SetRect(subtitleRt, Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 16f), new Vector2(-12f, 44f));
            AddText(subtitleRt, "Subtitle", 15f, new Color(0.62f, 0.68f, 0.78f, 1f), FontStyles.Normal);

            Button cardButton = root.AddComponent<Button>();
            cardButton.targetGraphic = background;
            ColorBlock colors = cardButton.colors;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.86f, 0.95f, 1f);
            colors.fadeDuration = 0.08f;
            cardButton.colors = colors;
            Navigation nav = cardButton.navigation;
            nav.mode = Navigation.Mode.None;
            cardButton.navigation = nav;

            CarouselItemLite item = root.AddComponent<CarouselItemLite>();
            item.image = cardImage;
            item.title = titleText;
            item.subtitle = subtitleRt.GetComponent<TMP_Text>();
            item.highlight = highlightRt.gameObject;
            item.button = cardButton;
            return item;
        }

        static RectTransform CreateChild(string name, Transform parent, int layer)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        static TextMeshProUGUI AddText(RectTransform rt, string text, float size, Color color, FontStyles style)
        {
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.overflowMode = TextOverflowModes.Truncate;
            t.raycastTarget = false;
            return t;
        }
    }
}
