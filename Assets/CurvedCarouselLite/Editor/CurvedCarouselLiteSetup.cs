// CodeStart-AI Curved Carousel Menu Lite (FREE)
// Copyright (c) CodeStart-AI. All rights reserved.
//
// Menus and the demo generator. The package already ships the demo scene, card prefab and art;
// "Generate Demo" rebuilds the prefab and scene (and repaints any missing art) if you ever need a fresh copy.
// No EventSystem or Input System action is created in the editor: the carousel creates a correctly
// configured EventSystem at runtime when a scene has none.

using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CodeStartAI.CarouselMenu.Lite.EditorTools
{
    /// <summary>Locates the Lite package wherever it was imported.</summary>
    internal static class LitePaths
    {
        const string ScriptSuffix = "/Runtime/CurvedCarouselLite.cs";

        public static string Root
        {
            get
            {
                foreach (string guid in AssetDatabase.FindAssets("CurvedCarouselLite t:Script"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                    if (path.EndsWith(ScriptSuffix)) return path.Substring(0, path.Length - ScriptSuffix.Length);
                }
                return "Assets/CurvedCarouselLite";
            }
        }

        public static string CardPrefab { get { return Root + "/Prefabs/CarouselItemLite.prefab"; } }
        public static string CardSprites { get { return Root + "/Prefabs/Sprites"; } }
        public static string DemoArt { get { return Root + "/Demo/Art"; } }
        public static string DemoScene { get { return Root + "/Demo/Scenes/CurvedCarouselLiteDemo.unity"; } }
        public static string Readme { get { return Root + "/Documentation/README.md"; } }

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }

    /// <summary>Menu entries for Curved Carousel Lite.</summary>
    internal static class CurvedCarouselLiteMenu
    {
        const string ToolsRoot = "Tools/CodeStart-AI/Curved Carousel Lite/";

        [MenuItem("GameObject/UI/CodeStart-AI/Curved Carousel Lite", false, 2101)]
        static void CreateCarouselMenu(MenuCommand command)
        {
            CreateCarousel(command.context as GameObject);
        }

        [MenuItem(ToolsRoot + "Create Carousel", false, 0)]
        static void CreateCarouselFromTools()
        {
            CreateCarousel(Selection.activeGameObject);
        }

        [MenuItem(ToolsRoot + "Create Card Prefab…", false, 1)]
        static void CreateCardPrefabMenu()
        {
            if (!LiteDemoGenerator.CheckTextMeshPro()) return;
            string path = EditorUtility.SaveFilePanelInProject("Create Card Prefab", "MyCarouselCard", "prefab",
                "Choose where to save your card prefab. Edit it freely, then assign it as the carousel's Item Prefab.");
            if (string.IsNullOrEmpty(path)) return;
            CarouselItemLite prefab = LiteDemoGenerator.CreateCardPrefab(path, LiteDemoGenerator.LoadSprites());
            if (prefab != null)
            {
                Selection.activeObject = prefab.gameObject;
                EditorGUIUtility.PingObject(prefab.gameObject);
            }
        }

        [MenuItem(ToolsRoot + "Generate Demo", false, 20)]
        static void GenerateDemo()
        {
            LiteDemoGenerator.GenerateAll();
        }

        [MenuItem(ToolsRoot + "Open Demo Scene", false, 21)]
        static void OpenDemo()
        {
            string path = LitePaths.DemoScene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                if (EditorUtility.DisplayDialog("Curved Carousel Lite", "The demo scene is missing. Generate it now?", "Generate", "Cancel"))
                    LiteDemoGenerator.GenerateAll();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        [MenuItem(ToolsRoot + "Documentation", false, 40)]
        static void OpenDocs()
        {
            Object readme = AssetDatabase.LoadAssetAtPath<Object>(LitePaths.Readme);
            if (readme != null) AssetDatabase.OpenAsset(readme);
        }

        static void CreateCarousel(GameObject context)
        {
            Transform parent = null;
            if (context != null && context.GetComponentInParent<Canvas>() != null) parent = context.transform;
            if (parent == null)
            {
                Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    canvas = LiteDemoGenerator.CreateCanvas("Canvas");
                    Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create Canvas");
                }
                parent = canvas.transform;
            }

            GameObject go = new GameObject("Curved Carousel Lite", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Curved Carousel Lite");
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1200f, 520f);
            rt.anchoredPosition = Vector2.zero;

            CurvedCarouselLite carousel = go.AddComponent<CurvedCarouselLite>();
            SerializedObject so = new SerializedObject(carousel);
            CarouselItemLite prefab = AssetDatabase.LoadAssetAtPath<CarouselItemLite>(LitePaths.CardPrefab);
            if (prefab != null) so.FindProperty("itemPrefab").objectReferenceValue = prefab;

            SerializedProperty list = so.FindProperty("items");
            list.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = "item_" + (i + 1);
                element.FindPropertyRelative("title").stringValue = "ITEM " + (i + 1);
                element.FindPropertyRelative("subtitle").stringValue = "Replace me";
                element.FindPropertyRelative("image").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(LitePaths.DemoArt + "/Item_" + (i + 1) + ".png");
                element.FindPropertyRelative("disabled").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = go;
        }
    }

    /// <summary>Builds the demo card prefab and demo scene (and repaints missing art).</summary>
    internal static class LiteDemoGenerator
    {
        const string DemoScriptType = "CodeStartAI.CarouselMenu.Lite.Demo.CarouselLiteDemo, CodeStartAI.CarouselMenu.Lite.Demo";
        const string DemoTitle = "CodeStart-AI \u2014 Curved Carousel Lite";
        static readonly Color Accent = new Color(0.22f, 0.78f, 1f, 1f);
        static readonly Color Muted = new Color(0.58f, 0.65f, 0.76f, 1f);
        static readonly Color ButtonColor = new Color(0.17f, 0.19f, 0.24f, 1f);
        static readonly Color DarkText = new Color(0.04f, 0.06f, 0.09f, 1f);

        static readonly string[] DemoSubtitles =
        {
            "Drag or swipe me", "Snaps to the center", "Click a side card", "Try Previous and Next", "Loops back to ITEM 1"
        };

        static readonly Color[][] Palettes =
        {
            new[] { new Color(0.1f, 0.62f, 0.72f), new Color(0.03f, 0.12f, 0.2f) },
            new[] { new Color(0.55f, 0.35f, 0.95f), new Color(0.09f, 0.05f, 0.2f) },
            new[] { new Color(1f, 0.66f, 0.25f), new Color(0.2f, 0.08f, 0.04f) },
            new[] { new Color(0.95f, 0.36f, 0.5f), new Color(0.18f, 0.04f, 0.1f) },
            new[] { new Color(0.3f, 0.55f, 1f), new Color(0.04f, 0.07f, 0.18f) },
        };

        internal sealed class Sprites
        {
            public Sprite rounded, glow, background;
            public readonly Sprite[] items = new Sprite[5];
        }

        // ------------------------------------------------------------------ Entry point
        public static void GenerateAll()
        {
            if (!CheckTextMeshPro()) return;
            string root = LitePaths.Root;
            if (!root.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog("Curved Carousel Lite", "The package must be inside the Assets folder to generate the demo.\nCurrent location: " + root, "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Curved Carousel Lite", "Checking art…", 0.2f);
                Sprites sprites = LoadSprites();

                EditorUtility.DisplayProgressBar("Curved Carousel Lite", "Building the card prefab…", 0.5f);
                LitePaths.EnsureFolder(root + "/Prefabs");
                CarouselItemLite card = CreateCardPrefab(LitePaths.CardPrefab, sprites);

                EditorUtility.DisplayProgressBar("Curved Carousel Lite", "Building the demo scene…", 0.8f);
                LitePaths.EnsureFolder(root + "/Demo/Scenes");
                BuildDemoScene(LitePaths.DemoScene, card, sprites);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Curved Carousel Lite] Demo ready: " + LitePaths.DemoScene + ". Press Play.");
        }

        internal static bool CheckTextMeshPro()
        {
            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings != null && TMP_Settings.defaultFontAsset != null) return true;
            EditorUtility.DisplayDialog("Curved Carousel Lite",
                "TextMeshPro Essential Resources are not imported yet.\n\nUse Window ▸ TextMeshPro ▸ Import TMP Essential Resources, then try again.", "OK");
            return false;
        }

        // ------------------------------------------------------------------ Sprites (shipped; repainted only if missing)
        internal static Sprites LoadSprites()
        {
            LitePaths.EnsureFolder(LitePaths.CardSprites);
            LitePaths.EnsureFolder(LitePaths.DemoArt);
            Sprites s = new Sprites();
            s.rounded = LoadOrPaint(LitePaths.CardSprites + "/RoundedRect.png", PaintRounded, new Vector4(20f, 20f, 20f, 20f));
            s.glow = LoadOrPaint(LitePaths.CardSprites + "/Glow.png", PaintGlow, new Vector4(56f, 56f, 56f, 56f));
            s.background = LoadOrPaint(LitePaths.DemoArt + "/Background.png", PaintBackground, Vector4.zero);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                s.items[i] = LoadOrPaint(LitePaths.DemoArt + "/Item_" + (i + 1) + ".png", () => PaintItem(index), Vector4.zero);
            }
            return s;
        }

        static Sprite LoadOrPaint(string path, Func<Texture2D> paint, Vector4 border)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = paint();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = border;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D PaintRounded()
        {
            return Paint(64, 64, (x, y) => new Color(1f, 1f, 1f, Coverage(RoundedBox(x, y, 64f, 64f, 16f))));
        }

        static Texture2D PaintGlow()
        {
            return Paint(128, 128, (x, y) =>
            {
                float d = RoundedBox(x - 24f, y - 24f, 80f, 80f, 16f);
                return new Color(1f, 1f, 1f, d <= 0f ? 1f : Mathf.Exp(-(d / 11f) * (d / 11f)));
            });
        }

        static Texture2D PaintBackground()
        {
            return Paint(16, 256, (x, y) => Color.Lerp(new Color(0.03f, 0.035f, 0.05f, 1f), new Color(0.1f, 0.115f, 0.16f, 1f), y / 255f));
        }

        static Texture2D PaintItem(int index)
        {
            const int W = 400;
            const int H = 380;
            Color light = Palettes[index][0];
            Color dark = Palettes[index][1];
            Vector2 orb = new Vector2(W * 0.7f, H * 0.62f);
            return Paint(W, H, (x, y) =>
            {
                float t = Mathf.Clamp01(y / (float)H * 0.8f + x / (float)W * 0.25f);
                Color c = Color.Lerp(dark, light, t * t);
                float glow = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), orb) / 150f);
                c = Color.Lerp(c, Color.white, glow * glow * 0.35f);
                c.a = Coverage(RoundedBox(x, y, W, H, 24f));
                return c;
            });
        }

        static Texture2D Paint(int width, int height, Func<float, float, Color> pixel)
        {
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x + 0.5f, y + 0.5f);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Signed distance (pixels) to a rounded box covering [0,w]x[0,h]. Negative = inside.</summary>
        static float RoundedBox(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(x - w * 0.5f) - w * 0.5f + r;
            float qy = Mathf.Abs(y - h * 0.5f) - h * 0.5f + r;
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static float Coverage(float distance)
        {
            return Mathf.Clamp01(0.5f - distance);
        }

        // ------------------------------------------------------------------ Card prefab
        internal static CarouselItemLite CreateCardPrefab(string path, Sprites s)
        {
            CarouselItemLite item = CarouselItemLite.CreateDefault(null);
            GameObject go = item.gameObject;
            go.name = Path.GetFileNameWithoutExtension(path);

            Image background = item.CardButton != null ? item.CardButton.targetGraphic as Image : null;
            SetSprite(background, s.rounded, new Color(0.1f, 0.11f, 0.14f, 1f));

            Image highlight = item.Highlight != null ? item.Highlight.GetComponent<Image>() : null;
            SetSprite(highlight, s.glow, Accent);
            if (highlight != null)
            {
                highlight.rectTransform.offsetMin = new Vector2(-20f, -20f);
                highlight.rectTransform.offsetMax = new Vector2(20f, 20f);
            }

            SetSprite(item.ImageTarget, s.rounded, new Color(0.25f, 0.27f, 0.33f, 1f));

            foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            }

            go.SetActive(true);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return saved != null ? saved.GetComponent<CarouselItemLite>() : null;
        }

        static void SetSprite(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }

        // ------------------------------------------------------------------ Demo scene
        internal static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void BuildDemoScene(string path, CarouselItemLite card, Sprites s)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = cameraGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.04f, 0.055f, 1f);
            cam.orthographic = true;

            Canvas canvas = CreateCanvas("Demo Canvas");
            RectTransform canvasRt = (RectTransform)canvas.transform;

            RectTransform background = Rect("Background", canvasRt);
            Anchor(background, Vector2.zero, Vector2.one);
            Img(background, s.background, Color.white, false);

            RectTransform title = Rect("Title", canvasRt);
            Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1600f, 70f));
            Text(title, DemoTitle, 48f, Color.white, FontStyles.Bold, 4f);

            RectTransform badge = Rect("Free Badge", canvasRt);
            Place(badge, new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(96f, 34f));
            Img(badge, s.rounded, Accent, true).pixelsPerUnitMultiplier = 1.4f;
            RectTransform badgeLabel = Rect("Label", badge);
            Anchor(badgeLabel, Vector2.zero, Vector2.one);
            Text(badgeLabel, "FREE", 18f, DarkText, FontStyles.Bold, 4f);

            RectTransform subtitle = Rect("Subtitle", canvasRt);
            Place(subtitle, new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(1400f, 36f));
            Text(subtitle, "Simple curved menus for Unity UI", 24f, Muted, FontStyles.Normal, 0f);

            RectTransform carouselRt = Rect("Curved Carousel Lite", canvasRt);
            Anchor(carouselRt, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.78f));
            Image hitArea = Img(carouselRt, null, new Color(0f, 0f, 0f, 0f), false);
            hitArea.raycastTarget = true;
            hitArea.canvasRenderer.cullTransparentMesh = true;
            CurvedCarouselLite carousel = carouselRt.gameObject.AddComponent<CurvedCarouselLite>();

            RectTransform centerTitle = Rect("Center Title", canvasRt);
            Anchor(centerTitle, new Vector2(0f, 0.2f), new Vector2(1f, 0.25f));
            TextMeshProUGUI centerTitleText = Text(centerTitle, "ITEM 1", 32f, Color.white, FontStyles.Bold, 8f);

            RectTransform centerSubtitle = Rect("Center Subtitle", canvasRt);
            Anchor(centerSubtitle, new Vector2(0f, 0.168f), new Vector2(1f, 0.2f));
            TextMeshProUGUI centerSubtitleText = Text(centerSubtitle, DemoSubtitles[0], 19f, Muted, FontStyles.Normal, 0f);

            RectTransform status = Rect("Status", canvasRt);
            Anchor(status, new Vector2(0f, 0.125f), new Vector2(1f, 0.16f));
            TextMeshProUGUI statusText = Text(status, "Click the center card or press Enter to select it.", 17f, Accent, FontStyles.Normal, 0f);

            Button previousButton = MakeButton(canvasRt, "Previous Button", "PREVIOUS", new Vector2(-210f, 85f), s, ButtonColor, Color.white);
            Button selectButton = MakeButton(canvasRt, "Select Button", "SELECT", new Vector2(0f, 85f), s, Accent, DarkText);
            Button nextButton = MakeButton(canvasRt, "Next Button", "NEXT", new Vector2(210f, 85f), s, ButtonColor, Color.white);

            RectTransform hint = Rect("Hint", canvasRt);
            Anchor(hint, Vector2.zero, new Vector2(1f, 0.035f));
            Text(hint, "Drag or swipe   |   Previous / Next   |   Click a side card to focus it   |   Arrow keys + Enter",
                14f, new Color(0.45f, 0.5f, 0.6f, 1f), FontStyles.Normal, 0f);

            SerializedObject so = new SerializedObject(carousel);
            so.FindProperty("itemPrefab").objectReferenceValue = card;
            so.FindProperty("previousButton").objectReferenceValue = previousButton;
            so.FindProperty("nextButton").objectReferenceValue = nextButton;
            SerializedProperty list = so.FindProperty("items");
            list.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = "item_" + (i + 1);
                element.FindPropertyRelative("title").stringValue = "ITEM " + (i + 1);
                element.FindPropertyRelative("subtitle").stringValue = DemoSubtitles[i];
                element.FindPropertyRelative("image").objectReferenceValue = s.items[i];
                element.FindPropertyRelative("disabled").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // SELECT is wired in the Inspector (Button ▸ On Click ▸ CurvedCarouselLite.SelectCurrentItem): no code needed.
            UnityEventTools.AddPersistentListener(selectButton.onClick, carousel.SelectCurrentItem);
            EditorUtility.SetDirty(selectButton);

            Type demoType = Type.GetType(DemoScriptType);
            if (demoType != null)
            {
                Component demo = new GameObject("Demo").AddComponent(demoType);
                SerializedObject dso = new SerializedObject(demo);
                SetRef(dso, "carousel", carousel);
                SetRef(dso, "centerTitle", centerTitleText);
                SetRef(dso, "centerSubtitle", centerSubtitleText);
                SetRef(dso, "statusText", statusText);
                dso.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = carousel.gameObject;
        }

        static void SetRef(SerializedObject so, string property, Object value)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p != null) p.objectReferenceValue = value;
        }

        // ------------------------------------------------------------------ UI helpers
        static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static Image Img(RectTransform rt, Sprite sprite, Color color, bool sliced)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, FontStyles style, float spacing)
        {
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = style;
            t.characterSpacing = spacing;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 position, Sprites s, Color color, Color labelColor)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, new Vector2(0.5f, 0f), position, new Vector2(190f, 56f));
            Image bg = Img(rt, s.rounded, color, true);
            bg.raycastTarget = true;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
            colors.colorMultiplier = 1.25f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            RectTransform labelRt = Rect("Label", rt);
            Anchor(labelRt, Vector2.zero, Vector2.one);
            Text(labelRt, label, 18f, labelColor, FontStyles.Bold, 3f);
            return button;
        }
    }
}
