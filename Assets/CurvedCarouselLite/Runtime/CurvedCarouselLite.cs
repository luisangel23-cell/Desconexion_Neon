// CodeStart-AI Curved Carousel Menu Lite (FREE)
// Copyright (c) CodeStart-AI. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if CAROUSEL_LITE_INPUT_SYSTEM
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

[assembly: InternalsVisibleTo("CodeStartAI.CarouselMenu.Lite.Editor")]

namespace CodeStartAI.CarouselMenu.Lite
{
    /// <summary>UnityEvent carrying an item index.</summary>
    [Serializable] public class CarouselLiteIndexEvent : UnityEvent<int> { }

    /// <summary>Position, rotation, scale and opacity of one card.</summary>
    internal struct CarouselLitePose
    {
        public Vector3 position;
        public float rotation;
        public float scale;
        public float alpha;
    }

    /// <summary>
    /// Curved Carousel Menu Lite: a curved (arc) carousel for Unity UI.
    /// Put it on a RectTransform inside a Canvas, assign a card prefab, add items and press Play.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("CodeStart-AI/Curved Carousel Lite")]
    public class CurvedCarouselLite : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Package version.</summary>
        public const string Version = "1.0.0";

        /// <summary>Which way the arc bends.</summary>
        public enum ArcDirection
        {
            /// <summary>Side cards drop below the center (rainbow).</summary>
            Down = 0,
            /// <summary>Side cards rise above the center (smile).</summary>
            Up = 1
        }

        const float MinScale = 0.2f;
        const float SnapThreshold = 0.15f;   // fraction of a slot that moves to the next card on release
        const float SwipeMomentum = 0.12f;   // seconds of swipe velocity carried into the snap
        const int MaxItemsPerSwipe = 3;
        const float Overscroll = 0.3f;       // stretch past the first/last card when not looping
        const float FitPadding = 30f;
        static readonly Vector2 DefaultCardSize = new Vector2(220f, 300f);
        static readonly Comparison<CarouselItemLite> FarthestFirst = (a, b) => b.SortKey.CompareTo(a.SortKey);

        // ---- Layout
        [Tooltip("How many cards are visible at once (center + both sides). Odd numbers look symmetric.")]
        [SerializeField, Range(1, 15)] int visibleSlots = 5;

        [Tooltip("Wrap around from the last card to the first (and back).")]
        [SerializeField] bool loop = true;

        [Tooltip("Card centered when the scene starts.")]
        [SerializeField, Min(0)] int startIndex = 0;

        [Tooltip("When the whole item list is replaced (SetItems, ClearItems), go back to Start Index. Off = keep the current card.")]
        [SerializeField] bool autoCenter = true;

        [Tooltip("Shrink the carousel automatically if the visible cards are wider than this RectTransform (portrait screens, small panels).")]
        [SerializeField] bool fitToWidth = true;

        // ---- Arc
        [Tooltip("Horizontal distance between cards.")]
        [SerializeField, Min(0f)] float spacing = 232f;

        [Tooltip("How far side cards bend away from the center line.")]
        [SerializeField, Min(0f)] float curve = 26f;

        [Tooltip("Down: side cards drop (rainbow). Up: side cards rise (smile).")]
        [SerializeField] ArcDirection direction = ArcDirection.Down;

        [Tooltip("Rotation per slot in degrees (fans the cards along the arc).")]
        [SerializeField, Range(-30f, 30f)] float tiltPerSlot = 8f;

        [Tooltip("Scale of the centered card.")]
        [SerializeField, Range(0.3f, 2f)] float centerScale = 1.08f;

        [Tooltip("Scale lost per slot away from the center.")]
        [SerializeField, Range(0f, 0.5f)] float scaleFalloff = 0.12f;

        [Tooltip("Opacity of the centered card.")]
        [SerializeField, Range(0f, 1f)] float centerOpacity = 1f;

        [Tooltip("Opacity lost per slot away from the center.")]
        [SerializeField, Range(0f, 1f)] float opacityFalloff = 0.22f;

        [Tooltip("Seconds to glide to the next card.")]
        [SerializeField, Range(0.05f, 1f)] float snapDuration = 0.25f;

        // ---- Navigation
        [Tooltip("Drag with the mouse.")]
        [SerializeField] bool drag = true;

        [Tooltip("Swipe on touch screens.")]
        [SerializeField] bool swipe = true;

        [Tooltip("Click a side card to move it to the center. Click the centered card to select it.")]
        [SerializeField] bool clickCards = true;

        [Tooltip("Left/Right arrows (or A/D) to move, Enter/Space to select. Requires the Input System package.")]
        [SerializeField] bool keyboard = true;

        [Tooltip("Optional button that shows the previous card.")]
        [SerializeField] Button previousButton = null;

        [Tooltip("Optional button that shows the next card.")]
        [SerializeField] Button nextButton = null;

        // ---- Items
        [Tooltip("Card prefab with a CarouselItemLite component on its root. Empty = built-in default card.")]
        [SerializeField] CarouselItemLite itemPrefab = null;

        [Tooltip("One card is created for each entry.")]
        [SerializeField] List<CarouselItemDataLite> items = new List<CarouselItemDataLite>();

        // ---- Events
        [Tooltip("A different card reached the center. Argument: item index.")]
        [SerializeField] CarouselLiteIndexEvent onCenterChanged = new CarouselLiteIndexEvent();

        [Tooltip("The centered card was selected (clicked, Enter, or SelectCurrentItem). Argument: item index.")]
        [SerializeField] CarouselLiteIndexEvent onItemSelected = new CarouselLiteIndexEvent();

        // ---- Runtime state
        readonly List<CarouselItemLite> cards = new List<CarouselItemLite>();
        readonly List<CarouselItemLite> sortBuffer = new List<CarouselItemLite>();
        readonly List<CarouselItemLite> lastOrder = new List<CarouselItemLite>();
        RectTransform rectTransform;
        RectTransform container;
        CarouselItemLite defaultTemplate;
        Vector2 cardSize = DefaultCardSize;
        UnityAction previousAction;
        UnityAction nextAction;
        bool built;
        bool itemsDirty;
        int builtSignature;
        CarouselItemLite builtPrefab;
        bool warnedNoPrefab;

        float position;
        bool snapping;
        float snapFrom;
        float snapTo;
        float snapTime;
        float snapLength;

        bool dragging;
        float dragStartPosition;
        float dragTarget;
        Vector2 dragStartLocal;
        float lastSampleTime;
        float lastSamplePosition;
        float velocity;

        int lastCenter = -1;
        int pendingSelect = -1;
        int lastButtonTarget = int.MinValue;

        // =====================================================================================
        // Public API
        // =====================================================================================

        /// <summary>A different card reached the center. Argument: item index.</summary>
        public CarouselLiteIndexEvent OnCenterChanged { get { return onCenterChanged; } }

        /// <summary>The centered card was selected. Argument: item index.</summary>
        public CarouselLiteIndexEvent OnItemSelected { get { return onItemSelected; } }

        /// <summary>Index of the centered card (-1 when empty).</summary>
        public int CurrentIndex
        {
            get
            {
                int count = cards.Count;
                if (count == 0) return -1;
                int i = Mathf.RoundToInt(position);
                return IsLooping ? Mod(i, count) : Mathf.Clamp(i, 0, count - 1);
            }
        }

        /// <summary>Number of items.</summary>
        public int ItemCount { get { return items.Count; } }

        /// <summary>The items (read-only view). Change them with AddItem, SetItems or ClearItems.</summary>
        public IReadOnlyList<CarouselItemDataLite> Items { get { return items; } }

        /// <summary>Whether Left/Right arrows (A/D) and Enter/Space control the carousel.</summary>
        public bool KeyboardNavigation { get { return keyboard; } set { keyboard = value; } }

        /// <summary>Show the next card.</summary>
        public void NextItem() { Step(1); }

        /// <summary>Show the previous card.</summary>
        public void PreviousItem() { Step(-1); }

        /// <summary>Glide to <paramref name="index"/>. Looping carousels take the shortest way round.</summary>
        public void GoToItem(int index)
        {
            if (!Application.isPlaying)
            {
                startIndex = Mathf.Max(0, index);
                return;
            }
            EnsureBuilt();
            int count = cards.Count;
            if (count == 0) return;
            index = Mathf.Clamp(index, 0, count - 1);

            float target = index;
            if (IsLooping)
            {
                float from = snapping ? snapTo : position;
                target = from + WrapOffset(index - Mathf.Repeat(from, count), count);
            }
            SnapTo(target);
        }

        /// <summary>Select the centered card (fires On Item Selected). While gliding, selects the card it arrives at.</summary>
        public void SelectCurrentItem()
        {
            if (!Application.isPlaying) return;
            EnsureBuilt();
            if (cards.Count == 0 || dragging) return;
            if (snapping)
            {
                pendingSelect = TargetIndex;
                return;
            }
            Select(CurrentIndex);
        }

        /// <summary>Index of the centered card (-1 when empty).</summary>
        public int GetCurrentIndex() { return CurrentIndex; }

        /// <summary>Number of items.</summary>
        public int GetItemCount() { return ItemCount; }

        /// <summary>The item at <paramref name="index"/>, or null.</summary>
        public CarouselItemDataLite GetItem(int index)
        {
            return index >= 0 && index < items.Count ? items[index] : null;
        }

        /// <summary>The centered item, or null.</summary>
        public CarouselItemDataLite GetCurrentItem() { return GetItem(CurrentIndex); }

        /// <summary>Add an item at the end. The centered card stays centered.</summary>
        public void AddItem(CarouselItemDataLite item)
        {
            items.Add(item ?? new CarouselItemDataLite());
            if (built) RebuildCards(false);
        }

        /// <summary>Replace all items. Goes back to Start Index when Auto Center is on.</summary>
        public void SetItems(IEnumerable<CarouselItemDataLite> newItems)
        {
            items.Clear();
            if (newItems != null)
            {
                foreach (CarouselItemDataLite item in newItems) items.Add(item ?? new CarouselItemDataLite());
            }
            if (built) RebuildCards(autoCenter);
        }

        /// <summary>Remove every item.</summary>
        public void ClearItems()
        {
            items.Clear();
            if (built) RebuildCards(autoCenter);
        }

        /// <summary>
        /// Re-applies item data (image, texts, Disabled) to the existing cards. Call it after editing item data from code.
        /// If the number of items changed, the cards are rebuilt. Keeps the centered card.
        /// </summary>
        public void Refresh()
        {
            if (!Application.isPlaying) return;
            if (!built)
            {
                EnsureBuilt();
                return;
            }
            if (cards.Count != items.Count || builtPrefab != itemPrefab)
            {
                RebuildCards(false);
                return;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) cards[i].SetData(items[i]);
            }
            builtSignature = ComputeSignature();
        }

        /// <summary>Destroys and recreates every card (e.g. after assigning a different Item Prefab from code). Keeps the centered card.</summary>
        public void Rebuild()
        {
            if (!Application.isPlaying) return;
            if (!built)
            {
                EnsureBuilt();
                return;
            }
            RebuildCards(false);
        }

        /// <summary>The card prefab. Setting it at runtime rebuilds the cards.</summary>
        public CarouselItemLite ItemPrefab
        {
            get { return itemPrefab; }
            set
            {
                itemPrefab = value;
                if (built) RebuildCards(false);
            }
        }

        // =====================================================================================
        // Unity lifecycle
        // =====================================================================================

        void Awake()
        {
            rectTransform = (RectTransform)transform;
            previousAction = PreviousItem;
            nextAction = NextItem;
            EnsureContainer();
            EnsureHitArea();
        }

        void OnEnable()
        {
            WireButtons(true);
        }

        void Start()
        {
            EnsureEventSystem();
            if (GetComponentInParent<Canvas>() == null)
                Debug.LogWarning("[Curved Carousel Lite] '" + name + "' is not inside a Canvas, so its cards will not be visible.", this);
            EnsureBuilt();
        }

        void OnDisable()
        {
            WireButtons(false);
            if (snapping) position = snapTo;
            snapping = false;
            dragging = false;
            pendingSelect = -1;
            NormalizePosition();
        }

        void OnValidate()
        {
            visibleSlots = Mathf.Max(1, visibleSlots);
            startIndex = Mathf.Max(0, startIndex);
            if (Application.isPlaying && built) itemsDirty = true;
        }

        void Reset()
        {
            // Give a freshly added component a usable size instead of the default 100x100.
            RectTransform rt = transform as RectTransform;
            if (rt != null && rt.anchorMin == rt.anchorMax && (rt.sizeDelta.x <= 100f || rt.sizeDelta.y <= 100f))
                rt.sizeDelta = new Vector2(1200f, 520f);
        }

        void Update()
        {
            if (!built) return;

            if (itemsDirty)
            {
                itemsDirty = false;
                if (ComputeSignature() != builtSignature) Refresh();
                WireButtons(isActiveAndEnabled);
                lastButtonTarget = int.MinValue;
            }

            HandleKeyboard();
        }

        void LateUpdate()
        {
            if (!built) return;
            StepMotion(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            UpdateLayout();
            UpdateCenter();
            UpdateButtons();
        }

        // =====================================================================================
        // Pointer input
        // =====================================================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!built || cards.Count == 0 || eventData.button != PointerEventData.InputButton.Left) return;
            if (IsTouch(eventData) ? !swipe : !drag) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.pressPosition, eventData.pressEventCamera, out local))
                return;

            snapping = false;
            pendingSelect = -1;
            dragging = true;
            dragStartLocal = local;
            dragStartPosition = position;
            dragTarget = position;
            velocity = 0f;
            lastSamplePosition = position;
            lastSampleTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out local))
                return;

            float unit = Mathf.Max(1f, spacing * (container != null ? container.localScale.x : 1f));
            float raw = dragStartPosition - (local.x - dragStartLocal.x) / unit;
            dragTarget = IsLooping ? raw : RubberBand(raw, 0f, cards.Count - 1);

            float now = Time.unscaledTime;
            float dt = now - lastSampleTime;
            if (dt > 0.0001f)
            {
                float instant = (raw - lastSamplePosition) / dt;
                velocity = Mathf.Lerp(velocity, instant, 0.6f);
                lastSamplePosition = raw;
                lastSampleTime = now;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;

            int count = cards.Count;
            if (count == 0) return;
            if (Time.unscaledTime - lastSampleTime > 0.1f) velocity = 0f; // finger rested before release

            float startCard = Mathf.Round(dragStartPosition);
            float projected = Mathf.Clamp(dragTarget + velocity * SwipeMomentum, startCard - MaxItemsPerSwipe, startCard + MaxItemsPerSwipe);
            float target = Mathf.Round(projected);
            if (Mathf.Approximately(target, startCard))
            {
                float moved = dragTarget - startCard;
                if (Mathf.Abs(moved) >= SnapThreshold) target = startCard + Mathf.Sign(moved);
            }
            if (!IsLooping) target = Mathf.Clamp(target, 0f, count - 1);
            SnapTo(target);
        }

        /// <summary>Called by <see cref="CarouselItemLite"/> when a card is clicked.</summary>
        internal void HandleCardClicked(CarouselItemLite card)
        {
            if (!clickCards || dragging || card == null || card.Owner != this) return;
            int index = card.Index;
            if (index < 0 || index >= cards.Count) return;

            if (index == TargetIndex)
            {
                if (snapping) pendingSelect = index;
                else Select(index);
            }
            else
            {
                GoToItem(index);
            }
        }

        static bool IsTouch(PointerEventData eventData)
        {
#if CAROUSEL_LITE_INPUT_SYSTEM
            ExtendedPointerEventData extended = eventData as ExtendedPointerEventData;
            if (extended != null) return extended.pointerType == UIPointerType.Touch;
#endif
            return eventData.pointerId >= 0; // legacy module: touches have ids >= 0, the mouse is negative
        }

        void HandleKeyboard()
        {
#if CAROUSEL_LITE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (!keyboard || cards.Count == 0 || dragging) return;
            Keyboard kb = Keyboard.current;
            if (kb == null || IsTyping()) return;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) PreviousItem();
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) NextItem();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) SelectCurrentItem();
#endif
        }

#if CAROUSEL_LITE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        static bool IsTyping()
        {
            EventSystem es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            TMP_InputField field = es.currentSelectedGameObject.GetComponent<TMP_InputField>();
            return field != null && field.isFocused;
        }
#endif

        // =====================================================================================
        // Motion
        // =====================================================================================

        bool IsLooping { get { return loop && cards.Count > 1; } }

        int TargetIndex
        {
            get
            {
                int count = cards.Count;
                if (count == 0) return -1;
                int i = Mathf.RoundToInt(snapping ? snapTo : position);
                return IsLooping ? Mod(i, count) : Mathf.Clamp(i, 0, count - 1);
            }
        }

        void Step(int direction)
        {
            if (!Application.isPlaying) return;
            EnsureBuilt();
            int count = cards.Count;
            if (count == 0 || dragging) return;
            float target = (snapping ? snapTo : Mathf.Round(position)) + direction;
            if (!IsLooping) target = Mathf.Clamp(target, 0f, count - 1);
            SnapTo(target);
        }

        void SnapTo(float target)
        {
            float distance = Mathf.Abs(target - position);
            if (distance < 0.0001f)
            {
                position = target;
                snapping = false;
                OnSettled();
                return;
            }
            snapFrom = position;
            snapTo = target;
            snapTime = 0f;
            snapLength = snapDuration * Mathf.Min(2f, Mathf.Sqrt(Mathf.Max(1f, distance)));
            snapping = true;
        }

        void JumpTo(int index)
        {
            snapping = false;
            dragging = false;
            pendingSelect = -1;
            position = cards.Count == 0 ? 0f : Mathf.Clamp(index, 0, cards.Count - 1);
        }

        void StepMotion(float dt)
        {
            if (cards.Count == 0)
            {
                snapping = false;
                return;
            }
            if (dragging)
            {
                position = Mathf.Lerp(position, dragTarget, 1f - Mathf.Exp(-30f * dt));
            }
            else if (snapping)
            {
                snapTime += dt;
                float t = snapLength <= 0f ? 1f : Mathf.Clamp01(snapTime / snapLength);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t); // ease-out cubic
                position = Mathf.LerpUnclamped(snapFrom, snapTo, eased);
                if (t >= 1f)
                {
                    position = snapTo;
                    snapping = false;
                    OnSettled();
                }
            }
        }

        void OnSettled()
        {
            NormalizePosition();
            if (pendingSelect >= 0)
            {
                int pending = pendingSelect;
                pendingSelect = -1;
                if (pending == CurrentIndex) Select(pending);
            }
        }

        void NormalizePosition()
        {
            int count = cards.Count;
            if (count == 0)
            {
                position = 0f;
                return;
            }
            if (dragging || snapping) return;
            position = IsLooping ? Mathf.Repeat(position, count) : Mathf.Clamp(position, 0f, count - 1);
        }

        void Select(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            CarouselItemDataLite item = items[index];
            if (item != null && !item.Interactable) return; // Disabled items cannot be selected
            if (cards[index] != null) cards[index].PlaySelectPop();
            onItemSelected.Invoke(index);
        }

        // =====================================================================================
        // Layout
        // =====================================================================================

        void UpdateLayout()
        {
            int count = cards.Count;
            if (count == 0 || container == null) return;

            float extent = VisibleExtent(count);
            float fit = fitToWidth ? ComputeFitScale(extent, cardSize) : 1f;
            if (Mathf.Abs(container.localScale.x - fit) > 0.0005f) container.localScale = new Vector3(fit, fit, 1f);

            sortBuffer.Clear();
            for (int i = 0; i < count; i++)
            {
                CarouselItemLite card = cards[i];
                if (card == null) continue;
                float offset = GetOffset(i, count, position);
                float d = Mathf.Abs(offset);
                bool visible = d <= extent + 0.5f;
                if (card.gameObject.activeSelf != visible) card.gameObject.SetActive(visible);
                if (!visible) continue;

                CarouselLitePose pose = EvaluatePose(offset, extent);
                card.ApplyPose(pose);
                card.SortKey = d;
                sortBuffer.Add(card);
            }

            // Draw order: the centered card on top.
            sortBuffer.Sort(FarthestFirst);
            bool changed = sortBuffer.Count != lastOrder.Count;
            for (int i = 0; !changed && i < sortBuffer.Count; i++) changed = sortBuffer[i] != lastOrder[i];
            if (!changed) return;
            for (int i = 0; i < sortBuffer.Count; i++) sortBuffer[i].transform.SetAsLastSibling();
            lastOrder.Clear();
            lastOrder.AddRange(sortBuffer);
        }

        void UpdateCenter()
        {
            int ci = CurrentIndex;
            if (ci == lastCenter) return;
            if (lastCenter >= 0 && lastCenter < cards.Count && cards[lastCenter] != null) cards[lastCenter].SetCentered(false, false);
            if (ci >= 0 && cards[ci] != null) cards[ci].SetCentered(true, false);
            lastCenter = ci;
            if (ci >= 0) onCenterChanged.Invoke(ci);
        }

        void UpdateButtons()
        {
            int target = TargetIndex;
            int key = target * 4 + (IsLooping ? 1 : 0) + cards.Count * 1000;
            if (key == lastButtonTarget) return;
            lastButtonTarget = key;
            int count = cards.Count;
            if (previousButton != null) previousButton.interactable = count > 1 && (IsLooping || target > 0);
            if (nextButton != null) nextButton.interactable = count > 1 && (IsLooping || target < count - 1);
        }

        /// <summary>The arc math: where a card sits when it is <paramref name="offset"/> slots from the center.</summary>
        internal CarouselLitePose EvaluatePose(float offset, float extent)
        {
            float d = Mathf.Abs(offset);
            float bend = direction == ArcDirection.Down ? 1f : -1f;
            CarouselLitePose pose;
            pose.position = new Vector3(offset * spacing, -bend * curve * offset * offset, 0f);
            pose.rotation = -bend * tiltPerSlot * offset;
            pose.scale = Mathf.Max(MinScale, centerScale - scaleFalloff * d);
            pose.alpha = Mathf.Clamp01(centerOpacity - opacityFalloff * d) * Mathf.Clamp01((extent + 0.5f - d) * 2f);
            return pose;
        }

        /// <summary>How many slots each side of the center are fully visible.</summary>
        internal float VisibleExtent(int count)
        {
            float extent = (Mathf.Max(1, visibleSlots) - 1) * 0.5f;
            if (loop && count > 1) extent = Mathf.Min(extent, (count - 1) * 0.5f); // never show a card twice
            return extent;
        }

        internal float ComputeFitScale(float extent, Vector2 size)
        {
            RectTransform rt = (RectTransform)transform;
            float halfWidth = rt.rect.width * 0.5f - FitPadding;
            if (halfWidth < 1f) return 1f;
            int reach = Mathf.Min(Mathf.FloorToInt(extent + 0.001f), 10);
            float maxX = 0f;
            for (int k = -reach; k <= reach; k++)
            {
                CarouselLitePose p = EvaluatePose(k, extent);
                if (p.alpha <= 0.001f) continue;
                maxX = Mathf.Max(maxX, Mathf.Abs(p.position.x) + size.x * 0.5f * p.scale);
            }
            return maxX < 0.01f ? 1f : Mathf.Clamp(halfWidth / maxX, 0.1f, 1f);
        }

        internal float GetOffset(int index, int count, float atPosition)
        {
            float raw = index - atPosition;
            return loop && count > 1 ? WrapOffset(raw, count) : raw;
        }

        static float WrapOffset(float offset, int count)
        {
            float half = count * 0.5f;
            return Mathf.Repeat(offset + half, count) - half;
        }

        static int Mod(int value, int count)
        {
            int r = value % count;
            return r < 0 ? r + count : r;
        }

        static float RubberBand(float value, float min, float max)
        {
            if (value < min) return min - Resist(min - value);
            if (value > max) return max + Resist(value - max);
            return value;
        }

        static float Resist(float overshoot)
        {
            return Overscroll * (1f - 1f / (overshoot / Overscroll + 1f));
        }

        // =====================================================================================
        // Building
        // =====================================================================================

        void EnsureBuilt()
        {
            if (built || !Application.isPlaying) return;
            if (rectTransform == null) Awake();
            built = true;
            BuildCards();
            JumpTo(startIndex);
            lastCenter = -1;
            UpdateLayout();
        }

        void RebuildCards(bool recenter)
        {
            int keep = Mathf.Max(0, CurrentIndex);
            BuildCards();
            JumpTo(recenter ? startIndex : keep);
            lastCenter = -1;
            lastButtonTarget = int.MinValue;
            UpdateLayout();
        }

        void BuildCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                cards[i].gameObject.SetActive(false);
                Destroy(cards[i].gameObject);
            }
            cards.Clear();
            lastOrder.Clear();

            CarouselItemLite template = itemPrefab;
            if (template == null)
            {
                if (!warnedNoPrefab)
                {
                    warnedNoPrefab = true;
                    Debug.LogWarning("[Curved Carousel Lite] No Item Prefab assigned on '" + name + "', so a built-in default card is used.", this);
                }
                if (defaultTemplate == null)
                {
                    defaultTemplate = CarouselItemLite.CreateDefault(transform);
                    defaultTemplate.gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
                template = defaultTemplate;
            }
            cardSize = GetCardSize(template);
            builtPrefab = itemPrefab;

            for (int i = 0; i < items.Count; i++)
            {
                CarouselItemLite card = Instantiate(template, container, false);
                card.name = "Card " + i;
                RectTransform rt = card.RectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = cardSize;
                if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);
                card.Setup(this, items[i], i);
                cards.Add(card);
            }
            builtSignature = ComputeSignature();
        }

        int ComputeSignature()
        {
            unchecked
            {
                int h = (itemPrefab != null ? itemPrefab.GetInstanceID() : 0) * 31 + items.Count;
                for (int i = 0; i < items.Count; i++) h = h * 31 + (items[i] != null ? items[i].ComputeSignature() : 0);
                return h;
            }
        }

        internal static Vector2 GetCardSize(CarouselItemLite template)
        {
            if (template == null) return DefaultCardSize;
            RectTransform rt = template.transform as RectTransform;
            if (rt == null) return DefaultCardSize;
            Vector2 size = rt.sizeDelta;
            if (size.x < 1f || size.y < 1f) size = rt.rect.size;
            return size.x < 1f || size.y < 1f ? DefaultCardSize : size;
        }

        void EnsureContainer()
        {
            if (container != null) return;
            GameObject go = new GameObject("Cards", typeof(RectTransform));
            go.layer = gameObject.layer;
            container = (RectTransform)go.transform;
            container.SetParent(transform, false);
            container.SetAsFirstSibling();
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.pivot = new Vector2(0.5f, 0.5f);
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
        }

        void EnsureHitArea()
        {
            // An invisible Image lets the carousel receive drags on the empty space between cards.
            if (GetComponent<Graphic>() != null) return;
            Image hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            CanvasRenderer cr = GetComponent<CanvasRenderer>();
            if (cr != null) cr.cullTransparentMesh = true;
        }

        void WireButtons(bool on)
        {
            if (previousAction == null) return;
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(previousAction);
                if (on) previousButton.onClick.AddListener(previousAction);
            }
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(nextAction);
                if (on) nextButton.onClick.AddListener(nextAction);
            }
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<EventSystem>() != null) return;
#else
            if (FindObjectOfType<EventSystem>() != null) return;
#endif
            // Created inactive so the input module is fully configured before it enables.
            GameObject go = new GameObject("EventSystem");
            go.SetActive(false);
            go.AddComponent<EventSystem>();
#if CAROUSEL_LITE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            AddInputSystemModule(go);
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            go.SetActive(true);
        }

#if CAROUSEL_LITE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Adds an InputSystemUIInputModule driven by a small UI action asset built here (pointer position,
        /// clicks and scroll for mouse, pen and touch). Every action belongs to that InputActionAsset, so the
        /// action references are valid. Arrow keys and Enter are read by the carousel itself.
        /// </summary>
        static void AddInputSystemModule(GameObject go)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "Curved Carousel Lite UI Actions";
            InputActionMap map = asset.AddActionMap("UI");

            // AddAction(name, type, binding) is used positionally, then the expected control type is set through the
            // InputAction.expectedControlType property. (AddAction has no parameter called "expectedControlType".)
            InputAction point = map.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position");
            point.expectedControlType = "Vector2";
            point.AddBinding("<Pen>/position");
            point.AddBinding("<Touchscreen>/touch*/position");

            InputAction click = map.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton");
            click.expectedControlType = "Button";
            click.AddBinding("<Pen>/tip");
            click.AddBinding("<Touchscreen>/touch*/press");

            InputAction scroll = map.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll");
            scroll.expectedControlType = "Vector2";

            InputAction middleClick = map.AddAction("MiddleClick", InputActionType.PassThrough, "<Mouse>/middleButton");
            middleClick.expectedControlType = "Button";

            InputAction rightClick = map.AddAction("RightClick", InputActionType.PassThrough, "<Mouse>/rightButton");
            rightClick.expectedControlType = "Button";

            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = asset;
            module.point = InputActionReference.Create(point);
            module.leftClick = InputActionReference.Create(click);
            module.scrollWheel = InputActionReference.Create(scroll);
            module.middleClick = InputActionReference.Create(middleClick);
            module.rightClick = InputActionReference.Create(rightClick);
        }
#endif

        // =====================================================================================
        // Editor support (Scene-view preview)
        // =====================================================================================

        internal int PreviewCount { get { return items.Count; } }
        internal float PreviewPosition { get { return Application.isPlaying && built ? position : Mathf.Min(startIndex, Mathf.Max(0, items.Count - 1)); } }
        internal Vector2 PreviewCardSize { get { return Application.isPlaying && built ? cardSize : GetCardSize(itemPrefab); } }
        internal RectTransform Container { get { return container; } }
        internal bool FitToWidth { get { return fitToWidth; } }
    }
}
