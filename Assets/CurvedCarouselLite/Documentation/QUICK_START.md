# Curved Carousel Lite — Quick Start

**CodeStart-AI · FREE · v1.0.0**

No code needed.

## See the demo (30 seconds)

1. Open `CurvedCarouselLite/Demo/Scenes/CurvedCarouselLiteDemo.unity`.
2. Press **Play**.
3. Drag or swipe the cards, click a side card, press **PREVIOUS** / **NEXT**, or use the **arrow keys**. Click the centered card, press **Enter** or press **SELECT** to select it.

If Unity asks to import **TMP Essentials**, click it once and press Play again.

## Your own carousel (2 minutes)

**1. Import the package.**
Extract the ZIP into `Assets/` (or import the .unitypackage).

**2. Create a Canvas.**
*GameObject ▸ UI ▸ Canvas*.

**3. Add Curved Carousel Lite.**
Right-click the Canvas in the Hierarchy ▸ *UI ▸ CodeStart-AI ▸ Curved Carousel Lite*.
You get a carousel with five items, already using the included card and demo images.

**4. Assign the item prefab.**
Select the carousel. In **Content ▸ Item Prefab**, make sure `CarouselItemLite` is assigned (drag `Prefabs/CarouselItemLite.prefab` in if it's empty).

**5. Add your images and items.**
In **Content ▸ Items**, set **Size**, then for each item set an **Image** (a sprite) and a **Title**. Subtitle and ID are optional.
Tip: set your textures' *Texture Type* to **Sprite (2D and UI)**.

**6. Press Play.**

## Make it yours

- **Shape**: *Layout ▸ Spacing, Curve, Direction, Rotation Per Slot*
- **Center card**: *Center Item ▸ Center Scale, Center Opacity*
- **Side cards**: *Layout ▸ Scale Falloff, Opacity Falloff*
- **Speed**: *Animation ▸ Snap Duration*
- **Wrap around**: *Loop ▸ Loop*
- **Buttons**: drag any UI Buttons into *Input ▸ Previous Button / Next Button*
- **React to choices**: *Events ▸ On Item Selected ▸ +*, then pick the object and method to call

## Custom card

*Tools ▸ CodeStart-AI ▸ Curved Carousel Lite ▸ Create Card Prefab…* → save → open the prefab → restyle it → assign it in *Content ▸ Item Prefab*.

Full guide: `Documentation/README.md`.
