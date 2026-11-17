// CodeStart-AI Curved Carousel Menu Lite (FREE)
// Copyright (c) CodeStart-AI. All rights reserved.

using System;
using UnityEngine;

namespace CodeStartAI.CarouselMenu.Lite
{
    /// <summary>One entry in a <see cref="CurvedCarouselLite"/>: an image, a title, an optional subtitle and an id.</summary>
    [Serializable]
    public class CarouselItemDataLite
    {
        [Tooltip("Optional identifier for your own code (e.g. \"play\", \"settings\"). Not shown on the card.")]
        public string id = "";

        [Tooltip("Main label shown on the card.")]
        public string title = "";

        [Tooltip("Optional second line shown under the title.")]
        public string subtitle = "";

        [Tooltip("Artwork shown on the card (Texture Type: Sprite (2D and UI)).")]
        public Sprite image;

        [Tooltip("Disabled cards are dimmed and cannot be clicked or selected. You can still scroll past them.")]
        [SerializeField] bool disabled;

        /// <summary>Whether the card can be clicked and selected. (Stored as "Disabled" so new items are interactable by default.)</summary>
        public bool Interactable
        {
            get { return !disabled; }
            set { disabled = !value; }
        }

        /// <summary>Creates an empty item.</summary>
        public CarouselItemDataLite() { }

        /// <summary>Creates an item with the most common fields filled in.</summary>
        public CarouselItemDataLite(string title, Sprite image = null, string subtitle = null, string id = null)
        {
            this.title = title ?? "";
            this.image = image;
            this.subtitle = subtitle ?? "";
            this.id = id ?? "";
        }

        internal int ComputeSignature()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (id ?? "").GetHashCode();
                h = h * 31 + (title ?? "").GetHashCode();
                h = h * 31 + (subtitle ?? "").GetHashCode();
                h = h * 31 + (image != null ? image.GetInstanceID() : 0);
                h = h * 31 + (disabled ? 1 : 0);
                return h;
            }
        }
    }
}
