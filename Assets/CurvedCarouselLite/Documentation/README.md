# Curved Carousel Menu Lite — FREE

**CodeStart-AI · Version 1.0.0**

---

## What is Curved Carousel Lite?

Curved Carousel Lite is a free curved (arc) carousel menu for Unity UI. You give it a card prefab and a list of items. It builds the cards, arranges them on an arc, highlights the centered card and snaps smoothly as players drag, swipe, click, press Previous/Next or use the arrow keys.

Use it for main menus, level and mode select, character pickers, shop tabs and settings pages. No code is needed for the basic workflow.

Curved Carousel Lite is the free edition of **Curved Carousel Menu** by CodeStart-AI. It is completely standalone: it does not need or reference the paid editions and can live in the same project as them.

---

## Features

- **Arc layout**: spacing, curve, direction (rainbow/smile), rotation per slot, scale and opacity falloff
- **Center item**: largest, fully opaque, highlighted, with configurable Center Scale and Center Opacity
- **Dynamic cards**: one card is created per item automatically; add, replace or clear items at runtime
- **Cards**: image, title, subtitle, item ID and an interactable (Disabled) state
- **Custom card prefab**: use the included card or design your own
- **Navigation**: mouse drag, touch swipe, Previous/Next buttons, click to focus/select, arrow keys and Enter
- **Smooth snapping** and **looping**
- **Events**: On Center Changed and On Item Selected (UnityEvents)
- **Simple C# API**
- **Beginner-friendly Inspector** and a Scene-view slot preview
- **Demo scene** with five cards and freely usable demo artwork

---

## Requirements

- **Unity 6** (6000.0) or newer
- **uGUI** and **TextMeshPro** (both built into Unity 6)
- **Input System** package (installed by default in new Unity 6 projects) for arrow keys and Enter, and for the automatically created EventSystem

Mouse drag, touch swipe, clicks and buttons go through Unity's EventSystem.

---

## Installation

1. Extract `CurvedCarouselLite.zip` into your project's **Assets** folder, so you have `Assets/CurvedCarouselLite/`. (Or import the `.unitypackage` / install from the Asset Store.)
2. When Unity asks, click **Import TMP Essentials**. You can also do it via *Window ▸ TextMeshPro ▸ Import TMP Essential Resources*.
3. Open `CurvedCarouselLite/Demo/Scenes/CurvedCarouselLiteDemo.unity` and press **Play**.

---

## Quick Start

1. **Import the package.**
2. **Create a Canvas**: *GameObject ▸ UI ▸ Canvas*.
3. **Add Curved Carousel Lite**: right-click the Canvas ▸ *UI ▸ CodeStart-AI ▸ Curved Carousel Lite*. This creates a carousel with five items.
4. **Assign the item prefab**: in *Content ▸ Item Prefab*, choose `Prefabs/CarouselItemLite`. The menu does this for you when the prefab exists.
5. **Add images/items**: in *Content ▸ Items*, set the size and give each item an **Image** and **Title**.
6. **Press Play.**

A step-by-step version is in `Documentation/QUICK_START.md`.

---

## How to create a carousel

**With the menu:** right-click a Canvas ▸ *UI ▸ CodeStart-AI ▸ Curved Carousel Lite*, or use *Tools ▸ CodeStart-AI ▸ Curved Carousel Lite ▸ Create Carousel*. If the scene has no Canvas, one is created.

**By hand:** create an empty UI object inside a Canvas and *Add Component ▸ CodeStart-AI ▸ Curved Carousel Lite*. A fresh component resizes its RectTransform to 1200×520.

Good to know:
- The carousel's **RectTransform** is the area that receives drags. With **Fit To Width** on, the visible cards also shrink to fit its width (handy on portrait phones).
- The card container, the invisible drag area and, if the scene has none, an **EventSystem** are created automatically when you press Play.
- While the carousel is selected, the **Scene view** outlines every visible card slot. Tune the arc without pressing Play.

---

## How to add items

In the Inspector, open **Content ▸ Items**, set **Size**, and fill in each item:

| Field | Purpose |
|---|---|
| ID | Optional identifier for your code, such as `play` or `settings` |
| Title | Main text on the card |
| Subtitle | Optional second line |
| Image | A sprite. Set the texture's *Texture Type* to **Sprite (2D and UI)** |
| Disabled | Tick to dim the card and block clicking/selecting it. You can still scroll past it |

Changing the Items list during Play mode updates the carousel immediately.

---

## How to customize cards

A card is any UI prefab with a **Carousel Item Lite** component on its root:

| Field | Purpose |
|---|---|
| Image | Receives the item's image |
| Title / Subtitle | TextMeshPro texts that receive the item's texts |
| Highlight | Optional object faded in on the centered card (glow, frame, outline) |
| Button | Optional; without it, clicks on any raycast target in the card still work |
| Hide Empty Fields | Hides the image/text objects when an item leaves them empty |

Ways to start:
- **Duplicate** `Prefabs/CarouselItemLite.prefab` and restyle it: colors, fonts, sizes and layout.
- **Create a fresh one** with *Tools ▸ CodeStart-AI ▸ Curved Carousel Lite ▸ Create Card Prefab…*.

Tips:
- The root's width and height define the card size. Keep the root's anchors centered (the default).
- The carousel moves, rotates, scales and fades the **root**. Style and animate the children.
- Want artwork to keep its shape? Tick *Preserve Aspect* on the card's Image, or add an *Aspect Ratio Fitter*. Its ratio is matched to each sprite automatically.
- Without an Item Prefab, a plain built-in card is used and the Console shows one warning.
- Advanced: subclass `CarouselItemLite` and override `Refresh()` to fill extra UI (call `base.Refresh()` first).

---

## How to use the API

Namespace: `CodeStartAI.CarouselMenu.Lite`

```csharp
using CodeStartAI.CarouselMenu.Lite;
using UnityEngine;

public class MenuExample : MonoBehaviour
{
    [SerializeField] CurvedCarouselLite carousel;
    [SerializeField] Sprite playSprite, optionsSprite;

    void Start()
    {
        carousel.SetItems(new[]
        {
            new CarouselItemDataLite("PLAY", playSprite, "Start a new game", "play"),
            new CarouselItemDataLite("OPTIONS", optionsSprite, "Sound and controls", "options"),
        });
    }
}
```

| Method / property | What it does |
|---|---|
| `NextItem()` / `PreviousItem()` | Glide to the next / previous card |
| `GoToItem(int index)` | Glide to a card (shortest way round when looping) |
| `SelectCurrentItem()` | Select the centered card (fires On Item Selected) |
| `GetCurrentIndex()` / `CurrentIndex` | Index of the centered card (-1 when empty) |
| `GetItemCount()` / `ItemCount` | Number of items |
| `GetItem(int index)` / `GetCurrentItem()` | Item data, or null |
| `AddItem(CarouselItemDataLite item)` | Add an item at the end |
| `SetItems(IEnumerable<CarouselItemDataLite>)` | Replace all items |
| `ClearItems()` | Remove every item |
| `Refresh()` | Re-apply item data to the cards after you edited it from code |
| `Rebuild()` | Destroy and recreate all cards |
| `ItemPrefab` | Get or set the card prefab (setting it rebuilds) |
| `KeyboardNavigation` | Turn arrow keys / Enter on or off |
| `OnCenterChanged` / `OnItemSelected` | The two events (see below) |

To make an item unselectable from code, use `item.Interactable = false;` then `carousel.Refresh();`.

---

## How to use events

| Event | When it fires | Argument |
|---|---|---|
| **On Center Changed** | A different card reaches the center (also while dragging) | item index |
| **On Item Selected** | The centered card is clicked, Enter/Space is pressed, or `SelectCurrentItem()` runs. Disabled items never fire it | item index |

**No code:** in *Events*, click **+**, drag in an object and pick a method.

**In code:**

```csharp
carousel.OnCenterChanged.AddListener(index => description.text = carousel.GetItem(index).subtitle);
carousel.OnItemSelected.AddListener(index => SceneManager.LoadScene(carousel.GetItem(index).id));
```

---

## How to use keyboard navigation

With the Input System package installed (default in Unity 6), keyboard navigation works automatically:

| Key | Action |
|---|---|
| Left Arrow or A | Previous card |
| Right Arrow or D | Next card |
| Enter, Keypad Enter or Space | Select the centered card |

- Turn it off in *Input ▸ Keyboard*, or from code with `carousel.KeyboardNavigation = false`. This is useful when two carousels share a screen.
- Keys are ignored while a TextMeshPro input field is being typed in.
- Your own UI buttons won't steal the arrow keys if their *Navigation* is set to *None*, as in the demo.

---

## How to use loop mode

In the **Loop** section:

- **Loop on**: after the last card comes the first, in both directions, and `GoToItem` takes the shortest way round.
- **Loop off**: the carousel stops at the first and last card. Dragging past the ends stretches slightly and springs back, and the Previous/Next buttons become non-interactable at the ends.
- **Auto Center**: when the whole item list is replaced (`SetItems`, `ClearItems`), jump back to *Start Index*. Off = keep the current card.

With fewer items than Visible Slots, each card is shown once and fades out at the wrap point, so no card is ever doubled.

---

## How to generate the demo

The demo scene, card prefab and art are included. If you delete or break them, rebuild them with:

**Tools ▸ CodeStart-AI ▸ Curved Carousel Lite ▸ Generate Demo**

This rebuilds `Prefabs/CarouselItemLite.prefab` and `Demo/Scenes/CurvedCarouselLiteDemo.unity`, and repaints any missing art. It never creates Input System actions in the editor. The carousel sets up its EventSystem when you press Play.

Other menu items:
- *Create Carousel*: adds a carousel to the open scene
- *Create Card Prefab…*: saves a fresh card prefab where you choose
- *Open Demo Scene*
- *Documentation*

---

## Troubleshooting

| Problem | Solution |
|---|---|
| Nothing appears | The carousel must be inside a Canvas, and Items must not be empty |
| Text is missing | Import TMP Essential Resources (*Window ▸ TextMeshPro ▸ Import TMP Essential Resources*) |
| Plain grey cards | No Item Prefab is assigned. Choose `Prefabs/CarouselItemLite` in *Content ▸ Item Prefab* |
| Can't drag or click | The scene needs an EventSystem. One is created at Play if none exists. If your scene already has one with a **Standalone Input Module** while *Project Settings ▸ Player ▸ Active Input Handling* is *Input System Package (New)*, select it and click **Replace with InputSystemUIInputModule** |
| Arrow keys do nothing | Check that the Input System package is installed and *Active Input Handling* is *Input System Package (New)* or *Both*, and that *Input ▸ Keyboard* is ticked |
| Enter doesn't select | The centered item may be marked **Disabled** |
| Cards look tiny | The carousel's RectTransform is narrower than the cards need, and Fit To Width shrinks them. Widen the RectTransform or turn Fit To Width off |
| An image looks stretched | Tick *Preserve Aspect* on the card's Image, or use artwork with the same shape as the card's image area |
| "Generate Demo" asks for TMP | Import TMP Essential Resources first, then run it again |

---

## Lite vs Standard vs Pro

| Feature | Lite (FREE) | Standard | Pro ¹ |
|---|:---:|:---:|:---:|
| Arc layout | ✓ | ✓ | ✓ |
| Flat, 3D Ring, Vertical Wheel layouts | – | ✓ | ✓ |
| Cover Flow, Perspective Arc, Ellipse, Cylinder, 3D Wheel, Deck | – | – | ✓ |
| Animated morphing between layouts | – | ✓ | ✓ |
| Image, title, subtitle, ID, disabled state | ✓ | ✓ | ✓ |
| Icons, badges, locked items, custom content | – | ✓ | ✓ |
| Drag, swipe, Previous/Next, click to focus/select | ✓ | ✓ | ✓ |
| Arrow keys + Enter | ✓ | ✓ (custom keys, hold-to-repeat) | ✓ |
| Gamepad, mouse wheel, Input Action overrides | – | ✓ | ✓ |
| Snapping and looping | ✓ | ✓ | ✓ |
| Easing choices, momentum, free scroll, distance curves | – | ✓ | ✓ |
| Spring snapping, inertia, 3D depth and perspective | – | – | ✓ |
| Center highlight | ✓ | ✓ (plus pop and pulse) | ✓ (plus glow, outline, center effects) |
| Events | 2 | 8 + C# callbacks | 8 + C# callbacks |
| ScriptableObject item data | – | ✓ | ✓ |
| Style and responsive profiles | – | – | ✓ |
| Card recycling for very large lists | – (one card per item) | ✓ | ✓ |
| Inspector | Simple | Sectioned, test buttons, Scene gizmos | Advanced editor window, presets, debug tools |

¹ Pro features as planned by CodeStart-AI. Check the CodeStart-AI store page for current availability.

---

## License

Curved Carousel Menu Lite is free. © CodeStart-AI. All rights reserved.

When downloaded from the Unity Asset Store it is licensed under the Unity Asset Store End User License Agreement. You may use it in unlimited personal and commercial projects. You may not resell or redistribute the package, its source code or its assets on their own.

The included demo artwork (`Demo/Art`, `Prefabs/Sprites`) was generated procedurally by code written with the aid of AI (see AI disclosure below). It contains no third-party or copyrighted material and may be used in your projects.

---

## AI disclosure

This package was created with the aid of AI. Its contents were generated, completely or in part, with Claude (an AI assistant by Anthropic):

- **Code:** all C# scripts (runtime, Editor tools, demo and sample) and the assembly definitions.
- **Prefab and demo scene:** produced by build scripts that were written with AI.
- **Demo images and UI sprites** (`Demo/Art`, `Prefabs/Sprites`): generated procedurally by code written with AI. No image-generation model was used and no third-party images are included.
- **Documentation:** this guide, the Quick Start and the changelog.

CodeStart-AI defined the product and its features, tested the package in Unity 6000.3.16f1 and had the problems found during testing fixed before release.

---

## Support

Questions, bug reports and feature requests: use the support contact on the **CodeStart-AI** publisher page of the Unity Asset Store. Please include your Unity version, Input System version and the Console message if there is one.

Enjoying Lite? Curved Carousel Menu Standard adds more layouts, input options and customization.
