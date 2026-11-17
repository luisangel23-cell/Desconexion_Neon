// CodeStart-AI Curved Carousel Menu Lite (FREE) - Sample
// Fill a carousel from code and react to selection.
// Usage: add this next to a CurvedCarouselLite, assign a few sprites, press Play.

using System.Collections.Generic;
using UnityEngine;

namespace CodeStartAI.CarouselMenu.Lite.Samples
{
    [AddComponentMenu("CodeStart-AI/Samples/Carousel Lite From Code")]
    public class CarouselLiteFromCode : MonoBehaviour
    {
        [SerializeField] CurvedCarouselLite carousel = null;
        [SerializeField] Sprite[] images = new Sprite[0];
        [SerializeField] string[] titles = { "PLAY", "LEVELS", "SHOP", "OPTIONS" };

        void Awake()
        {
            if (carousel == null) carousel = GetComponent<CurvedCarouselLite>();
        }

        void Start()
        {
            if (carousel == null) return;

            List<CarouselItemDataLite> items = new List<CarouselItemDataLite>();
            for (int i = 0; i < titles.Length; i++)
            {
                Sprite image = images.Length > 0 ? images[i % images.Length] : null;
                items.Add(new CarouselItemDataLite(titles[i], image, "Item " + (i + 1), titles[i].ToLowerInvariant()));
            }
            carousel.SetItems(items);
            carousel.OnItemSelected.AddListener(OnSelected);
        }

        void OnDestroy()
        {
            if (carousel != null) carousel.OnItemSelected.RemoveListener(OnSelected);
        }

        void OnSelected(int index)
        {
            CarouselItemDataLite item = carousel.GetItem(index);
            Debug.Log("Selected " + item.title + " (id: " + item.id + ")");
        }
    }
}
