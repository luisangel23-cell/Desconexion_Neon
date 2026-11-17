// CodeStart-AI Curved Carousel Menu Lite (FREE)
// Copyright (c) CodeStart-AI. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodeStartAI.CarouselMenu.Lite.EditorTools
{
    /// <summary>Beginner-friendly Inspector for <see cref="CurvedCarouselLite"/>.</summary>
    [CustomEditor(typeof(CurvedCarouselLite))]
    [CanEditMultipleObjects]
    public class CurvedCarouselLiteEditor : UnityEditor.Editor
    {
        static readonly Color HeaderColor = new Color(0.16f, 0.17f, 0.21f, 1f);
        static readonly Color AccentColor = new Color(0.22f, 0.78f, 1f, 1f);

        SerializedProperty itemPrefab, items;
        SerializedProperty visibleSlots, startIndex, spacing, curve, direction, tiltPerSlot, scaleFalloff, opacityFalloff, fitToWidth;
        SerializedProperty snapDuration;
        SerializedProperty drag, swipe, clickCards, previousButton, nextButton;
#if CAROUSEL_LITE_INPUT_SYSTEM
        SerializedProperty keyboard;
#endif
        SerializedProperty loop, autoCenter;
        SerializedProperty centerScale, centerOpacity;
        SerializedProperty onCenterChanged, onItemSelected;

        GUIStyle titleStyle, subtitleStyle, sectionStyle, footerStyle;

        void OnEnable()
        {
            itemPrefab = serializedObject.FindProperty("itemPrefab");
            items = serializedObject.FindProperty("items");
            visibleSlots = serializedObject.FindProperty("visibleSlots");
            startIndex = serializedObject.FindProperty("startIndex");
            spacing = serializedObject.FindProperty("spacing");
            curve = serializedObject.FindProperty("curve");
            direction = serializedObject.FindProperty("direction");
            tiltPerSlot = serializedObject.FindProperty("tiltPerSlot");
            scaleFalloff = serializedObject.FindProperty("scaleFalloff");
            opacityFalloff = serializedObject.FindProperty("opacityFalloff");
            fitToWidth = serializedObject.FindProperty("fitToWidth");
            snapDuration = serializedObject.FindProperty("snapDuration");
            drag = serializedObject.FindProperty("drag");
            swipe = serializedObject.FindProperty("swipe");
            clickCards = serializedObject.FindProperty("clickCards");
            previousButton = serializedObject.FindProperty("previousButton");
            nextButton = serializedObject.FindProperty("nextButton");
#if CAROUSEL_LITE_INPUT_SYSTEM
            keyboard = serializedObject.FindProperty("keyboard");
#endif
            loop = serializedObject.FindProperty("loop");
            autoCenter = serializedObject.FindProperty("autoCenter");
            centerScale = serializedObject.FindProperty("centerScale");
            centerOpacity = serializedObject.FindProperty("centerOpacity");
            onCenterChanged = serializedObject.FindProperty("onCenterChanged");
            onItemSelected = serializedObject.FindProperty("onItemSelected");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            DrawTitle();
            DrawWarnings();

            Section("Content");
            EditorGUILayout.PropertyField(itemPrefab);
            EditorGUILayout.PropertyField(items, true);
            DrawPlayControls();

            Section("Layout");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(new GUIContent("Layout Mode", "Curved Carousel Lite uses the Arc layout."), "Arc");
            EditorGUILayout.PropertyField(visibleSlots);
            EditorGUILayout.PropertyField(startIndex);
            EditorGUILayout.PropertyField(spacing);
            EditorGUILayout.PropertyField(curve);
            EditorGUILayout.PropertyField(direction);
            EditorGUILayout.PropertyField(tiltPerSlot, new GUIContent("Rotation Per Slot", tiltPerSlot.tooltip));
            EditorGUILayout.PropertyField(scaleFalloff);
            EditorGUILayout.PropertyField(opacityFalloff);
            EditorGUILayout.PropertyField(fitToWidth);

            Section("Animation");
            EditorGUILayout.PropertyField(snapDuration);

            Section("Input");
            EditorGUILayout.PropertyField(drag);
            EditorGUILayout.PropertyField(swipe);
            EditorGUILayout.PropertyField(clickCards);
#if CAROUSEL_LITE_INPUT_SYSTEM
            EditorGUILayout.PropertyField(keyboard, new GUIContent("Keyboard", keyboard.tooltip));
#else
            EditorGUILayout.HelpBox("Install the Input System package to enable arrow keys and Enter. Drag, swipe, clicks and buttons work without it.", MessageType.None);
#endif
            EditorGUILayout.PropertyField(previousButton);
            EditorGUILayout.PropertyField(nextButton);

            Section("Loop");
            EditorGUILayout.PropertyField(loop);
            EditorGUILayout.PropertyField(autoCenter);

            Section("Center Item");
            EditorGUILayout.PropertyField(centerScale);
            EditorGUILayout.PropertyField(centerOpacity);
            EditorGUILayout.LabelField("The card's Highlight object fades in on the centered card.", footerStyle);

            Section("Events");
            EditorGUILayout.PropertyField(onCenterChanged);
            EditorGUILayout.PropertyField(onItemSelected);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("More layouts (Flat, 3D Ring, Vertical Wheel), gamepad and mouse-wheel input, data assets and advanced animation " +
                                       "are available in Curved Carousel Menu Standard by CodeStart-AI.", footerStyle);
        }

        void DrawTitle()
        {
            Rect r = GUILayoutUtility.GetRect(10f, 42f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, HeaderColor);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2f, r.width, 2f), AccentColor);
            GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 20f, 22f), "CURVED CAROUSEL LITE", titleStyle);
            GUI.Label(new Rect(r.x + 10f, r.y + 22f, r.width - 20f, 16f), "FREE  ·  v" + CurvedCarouselLite.Version + "  ·  CodeStart-AI", subtitleStyle);
            EditorGUILayout.Space(2);
        }

        void DrawWarnings()
        {
            if (targets.Length != 1) return;
            CurvedCarouselLite carousel = (CurvedCarouselLite)target;

            if (carousel.GetComponentsInParent<Canvas>(true).Length == 0)
                EditorGUILayout.HelpBox("This carousel is not inside a Canvas. Move it under a Canvas (GameObject ▸ UI ▸ Canvas).", MessageType.Error);
            if (itemPrefab.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No Item Prefab: a simple built-in card will be used. Assign Prefabs/CarouselItemLite, or create one with Tools ▸ CodeStart-AI ▸ Curved Carousel Lite ▸ Create Card Prefab.", MessageType.Info);
            if (items.arraySize == 0)
                EditorGUILayout.HelpBox("No items yet. Increase Items ▸ Size and give each item an Image and Title.", MessageType.Warning);
            if (!Application.isPlaying && Object.FindAnyObjectByType<EventSystem>() == null)
                EditorGUILayout.HelpBox("This scene has no EventSystem. One is created automatically when you press Play.", MessageType.None);
        }

        void DrawPlayControls()
        {
            if (!Application.isPlaying || targets.Length != 1) return;
            CurvedCarouselLite carousel = (CurvedCarouselLite)target;
            CarouselItemDataLite current = carousel.GetCurrentItem();
            string label = carousel.CurrentIndex + (current != null && !string.IsNullOrEmpty(current.title) ? "  (" + current.title + ")" : "");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Current Index", label);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ Previous")) carousel.PreviousItem();
                if (GUILayout.Button("Select")) carousel.SelectCurrentItem();
                if (GUILayout.Button("Next ▶")) carousel.NextItem();
            }
        }

        void Section(string label)
        {
            EditorGUILayout.Space(6);
            Rect r = GUILayoutUtility.GetRect(10f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, HeaderColor);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 3f, r.height), AccentColor);
            GUI.Label(new Rect(r.x + 10f, r.y + 2f, r.width - 12f, r.height - 4f), label.ToUpperInvariant(), sectionStyle);
            EditorGUILayout.Space(2);
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            titleStyle.normal.textColor = Color.white;
            subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
            subtitleStyle.normal.textColor = new Color(0.6f, 0.68f, 0.78f, 1f);
            sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.normal.textColor = Color.white;
            footerStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            footerStyle.normal.textColor = new Color(0.55f, 0.58f, 0.64f, 1f);
        }
    }

    /// <summary>Scene-view preview of the card slots while a Lite carousel is selected (no objects are created).</summary>
    static class CurvedCarouselLiteGizmos
    {
        static GUIStyle labelStyle;

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void Draw(CurvedCarouselLite carousel, GizmoType gizmoType)
        {
            if (carousel == null || !carousel.isActiveAndEnabled) return;
            RectTransform root = carousel.transform as RectTransform;
            if (root == null) return;

            int count = carousel.PreviewCount;
            bool placeholder = count == 0;
            int n = placeholder ? 5 : count;
            float extent = carousel.VisibleExtent(n);
            Vector2 size = carousel.PreviewCardSize;

            Matrix4x4 m;
            if (Application.isPlaying && carousel.Container != null)
            {
                m = carousel.Container.localToWorldMatrix;
            }
            else
            {
                float fit = carousel.FitToWidth ? carousel.ComputeFitScale(extent, size) : 1f;
                m = root.localToWorldMatrix * Matrix4x4.Translate(root.rect.center) * Matrix4x4.Scale(new Vector3(fit, fit, 1f));
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleCenter;
            }

            Color accent = new Color(0.22f, 0.78f, 1f, 1f);
            Color center = new Color(1f, 0.62f, 0.2f, 1f);

            // Arc guide
            Handles.color = new Color(accent.r, accent.g, accent.b, 0.7f);
            Vector3 previous = Vector3.zero;
            bool hasPrevious = false;
            for (float o = -extent - 0.5f; o <= extent + 0.5001f; o += 0.1f)
            {
                Vector3 w = m.MultiplyPoint3x4(carousel.EvaluatePose(o, extent).position);
                if (hasPrevious) Handles.DrawLine(previous, w);
                previous = w;
                hasPrevious = true;
            }

            // Card slots
            float position = carousel.PreviewPosition;
            Vector3[] corners = new Vector3[4];
            for (int i = 0; i < n; i++)
            {
                float offset = placeholder ? i - (n - 1) * 0.5f : carousel.GetOffset(i, n, position);
                if (Mathf.Abs(offset) > extent + 0.5f) continue;
                CarouselLitePose pose = carousel.EvaluatePose(offset, extent);
                if (pose.alpha <= 0.01f) continue;

                Matrix4x4 im = m * Matrix4x4.TRS(pose.position, Quaternion.Euler(0f, 0f, pose.rotation), new Vector3(pose.scale, pose.scale, 1f));
                float hw = size.x * 0.5f;
                float hh = size.y * 0.5f;
                corners[0] = im.MultiplyPoint3x4(new Vector3(-hw, -hh, 0f));
                corners[1] = im.MultiplyPoint3x4(new Vector3(-hw, hh, 0f));
                corners[2] = im.MultiplyPoint3x4(new Vector3(hw, hh, 0f));
                corners[3] = im.MultiplyPoint3x4(new Vector3(hw, -hh, 0f));

                Color c = Mathf.Abs(offset) < 0.5f ? center : accent;
                Color face = c;
                face.a = 0.05f + 0.1f * pose.alpha;
                Color line = c;
                line.a = Mathf.Max(0.3f, pose.alpha);
                Handles.DrawSolidRectangleWithOutline(corners, face, line);

                string label = placeholder ? "slot" : i.ToString();
                if (!placeholder)
                {
                    CarouselItemDataLite data = carousel.GetItem(i);
                    if (data != null && !string.IsNullOrEmpty(data.title)) label = i + " · " + data.title;
                }
                Handles.Label(im.MultiplyPoint3x4(Vector3.zero), label, labelStyle);
            }
        }
    }
}
