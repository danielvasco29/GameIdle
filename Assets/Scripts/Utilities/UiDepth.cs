using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Shared helper that gives translucent panels a "frosted glass" feel:
    // a soft drop shadow, a thin bright outline and a faint top sheen. Uses
    // UGUI Shadow/Outline components so the effect toggles with the panel and
    // never covers its content. Keeps the look consistent across every modal.
    public static class UiDepth
    {
        public static void AddGlassTrim(GameObject panel)
        {
            if (panel == null) return;
            var bg = panel.GetComponent<Image>();
            if (bg == null) return;
            AddGlassTrim(bg);
        }

        public static void AddGlassTrim(Image bg)
        {
            if (bg == null) return;

            // Drop shadow under the panel (depth off the scene)
            var shadow = bg.GetComponent<Shadow>();
            if (shadow == null) shadow = bg.gameObject.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(3f, -5f);

            // Bright outline (glass rim) — added after Shadow so it renders as a
            // crisp edge on top of the shadow.
            var outline = bg.GetComponent<Outline>();
            if (outline == null) outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor    = new Color(0.60f, 0.78f, 1f, 0.45f);
            outline.effectDistance = new Vector2(1.6f, 1.6f);

            // Faint top sheen
            var existing = bg.transform.Find("GlassSheen");
            if (existing != null) { existing.SetParent(null); Object.Destroy(existing.gameObject); }
            var sheen = new GameObject("GlassSheen", typeof(RectTransform), typeof(Image));
            sheen.transform.SetParent(bg.transform, false);
            sheen.transform.SetSiblingIndex(0); // behind the panel's content
            var sgrt = sheen.GetComponent<RectTransform>();
            sgrt.anchorMin = new Vector2(0f, 0.5f); sgrt.anchorMax = Vector2.one;
            sgrt.offsetMin = new Vector2(3f, 0f); sgrt.offsetMax = new Vector2(-3f, -3f);
            var sgImg = sheen.GetComponent<Image>();
            sgImg.sprite = UiSpriteFactory.VerticalGradient();
            sgImg.color = new Color(1f, 1f, 1f, 0.06f);
            sgImg.raycastTarget = false;
        }
    }
}
