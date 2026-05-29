using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace sonnv
{
    public class RevealImage : MonoBehaviour
    {
        [SerializeField] private bool isPaint = false;
        public void SetCanPaint(bool value)
        {
            isPaint = value;
        }
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Material revealMaterial;
        [SerializeField] private int textureSize = 256;
        [SerializeField] private int brushRadius = 15;
        [SerializeField] private float threshold = 0.85f;
        [SerializeField] private bool eraseMode;
        [SerializeField] private bool paintEnabled = true;
        [SerializeField] private AudioClip completeSfx;

        public UnityEvent OnComplete;

        private Texture2D maskTex;
        private Color32[] maskPixels;
        private Color32[] brushPattern;
        private Material matInstance;
        private bool isComplete;
        private bool isDirty;
        private int revealedCount;
        private int totalPixels;
        private Bounds spriteBounds;

        public bool IsComplete => isComplete;
        public bool IsEraseMode => eraseMode;

        [Preserve] public void EnablePaint() => paintEnabled = true;
        [Preserve] public void DisablePaint() => paintEnabled = false;
        [Preserve] public void SetPaintEnabled(bool enabled) => paintEnabled = enabled;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError("RevealImage: SpriteRenderer is not assigned!", this);
                return;
            }

            if (revealMaterial == null)
            {
                Debug.LogError("RevealImage: Reveal Material is not assigned!", this);
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                Debug.LogError("RevealImage: SpriteRenderer has no sprite!", this);
                return;
            }

            totalPixels = textureSize * textureSize;

            maskTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            maskTex.filterMode = FilterMode.Bilinear;
            maskTex.wrapMode = TextureWrapMode.Clamp;
            maskPixels = new Color32[totalPixels];

            // Erase mode: start fully visible (white). Draw mode: start hidden (black).
            Color32 initColor = eraseMode
                ? new Color32(255, 255, 255, 255)
                : new Color32(0, 0, 0, 255);

            for (int i = 0; i < totalPixels; i++)
                maskPixels[i] = initColor;

            revealedCount = eraseMode ? totalPixels : 0;

            maskTex.SetPixels32(maskPixels);
            maskTex.Apply(false);

            matInstance = new Material(revealMaterial);
            matInstance.SetTexture("_MaskTex", maskTex);
            matInstance.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            spriteRenderer.material = matInstance;

            BuildBrush();
            spriteBounds = spriteRenderer.bounds;
        }

        private void LateUpdate()
        {
            if(isPaint == false) return;
            if (!isDirty) return;
            isDirty = false;
            maskTex.SetPixels32(maskPixels);
            maskTex.Apply(false);
        }

        /// <summary>
        /// Called by RevealPen. Automatically paints or erases based on eraseMode.
        /// </summary>
        public void Paint(Vector3 worldPos)
        {
            if (isPaint == false) return;

            if (isComplete || !paintEnabled || spriteRenderer == null) return;

            spriteBounds = spriteRenderer.bounds;
            float u = (worldPos.x - spriteBounds.min.x) / spriteBounds.size.x;
            float v = (worldPos.y - spriteBounds.min.y) / spriteBounds.size.y;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return;

            int cx = Mathf.RoundToInt(u * (textureSize - 1));
            int cy = Mathf.RoundToInt(v * (textureSize - 1));
            int diameter = brushRadius * 2;
            int brushIndex = 0;

            for (int by = 0; by < diameter; by++)
            {
                int py = cy - brushRadius + by;
                if (py < 0 || py >= textureSize) { brushIndex += diameter; continue; }
                int rowStart = py * textureSize;

                for (int bx = 0; bx < diameter; bx++)
                {
                    int px = cx - brushRadius + bx;
                    if (px < 0 || px >= textureSize) { brushIndex++; continue; }

                    byte brushAlpha = brushPattern[brushIndex].a;
                    brushIndex++;
                    if (brushAlpha == 0) continue;

                    int idx = rowStart + px;
                    byte current = maskPixels[idx].r;

                    if (eraseMode)
                    {
                        int nv = current - brushAlpha;
                        if (nv < 0) nv = 0;
                        if (nv == current) continue;
                        if (current > 128 && nv <= 128) revealedCount--;
                        byte b = (byte)nv;
                        maskPixels[idx] = new Color32(b, b, b, 255);
                    }
                    else
                    {
                        int nv = current + brushAlpha;
                        if (nv > 255) nv = 255;
                        if (nv == current) continue;
                        if (current <= 128 && nv > 128) revealedCount++;
                        byte b = (byte)nv;
                        maskPixels[idx] = new Color32(b, b, b, 255);
                    }
                }
            }

            isDirty = true;
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            if (isComplete) return;

            if (eraseMode)
            {
                // Erase: complete when enough pixels are erased (black)
                float erasedRatio = 1f - (float)revealedCount / totalPixels;
                if (erasedRatio < threshold) return;

                isComplete = true;
                Color32 black = new Color32(0, 0, 0, 255);
                for (int i = 0; i < totalPixels; i++)
                    maskPixels[i] = black;
                revealedCount = 0;
            }
            else
            {
                // Draw: complete when enough pixels are revealed (white)
                float revealRatio = (float)revealedCount / totalPixels;
                if (revealRatio < threshold) return;

                isComplete = true;
                Color32 white = new Color32(255, 255, 255, 255);
                for (int i = 0; i < totalPixels; i++)
                    maskPixels[i] = white;
                revealedCount = totalPixels;
            }

            isDirty = true;
            if (completeSfx != null) SoundManager.PlaySFX(completeSfx);
            OnComplete?.Invoke();
        }

        private void BuildBrush()
        {
            int diameter = brushRadius * 2;
            brushPattern = new Color32[diameter * diameter];
            float center = brushRadius;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = dist < brushRadius ? Mathf.Clamp01((1f - dist / brushRadius) * 2f) : 0f;
                    byte ab = (byte)(a * 255);
                    brushPattern[y * diameter + x] = new Color32(255, 255, 255, ab);
                }
            }
        }

        public void SetBrushSize(int radius)
        {
            brushRadius = radius;
            BuildBrush();
        }

        private void OnDestroy()
        {
            if (maskTex != null) Destroy(maskTex);
            if (matInstance != null) Destroy(matInstance);
        }
    }
}
