using UnityEngine;

namespace sonnv
{
    public class SonSpriteErasePainter2D : MonoBehaviour
    {
        [Header("Overlay cần xóa")]
        [SerializeField] private SpriteRenderer overlayRenderer;

        [Header("Brush")]
        [SerializeField] private int brushRadius = 24;

        [Range(1, 255)]
        [SerializeField] private int erasePower = 255;

        [Header("Debug")]
        [SerializeField] private bool logMissingHit;

        private Texture2D runtimeTexture;
        private Color32[] pixels;

        private int texWidth;
        private int texHeight;

        private bool isReady;
        private bool isDirty;
        private bool hasLastPoint;

        private int lastX;
        private int lastY;

        private Sprite originalSprite;
        private Vector2[] brushOffsets;

        private void Awake()
        {
            Init();
        }

        private void LateUpdate()
        {
            ApplyTextureIfDirty();
        }

        public void Init()
        {
            if (overlayRenderer == null)
            {
                overlayRenderer = GetComponent<SpriteRenderer>();
            }

            if (overlayRenderer == null || overlayRenderer.sprite == null)
            {
                Debug.LogError("SonSpriteErasePainter2D: Thiếu SpriteRenderer overlay.");
                return;
            }

            originalSprite = overlayRenderer.sprite;

            Rect rect = originalSprite.rect;
            texWidth = Mathf.RoundToInt(rect.width);
            texHeight = Mathf.RoundToInt(rect.height);

            Texture2D sourceTexture = originalSprite.texture;

            int startX = Mathf.RoundToInt(rect.x);
            int startY = Mathf.RoundToInt(rect.y);

            Color[] sourcePixels = sourceTexture.GetPixels(startX, startY, texWidth, texHeight);

            pixels = new Color32[sourcePixels.Length];

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                pixels[i] = sourcePixels[i];
            }

            runtimeTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            runtimeTexture.filterMode = FilterMode.Bilinear;
            runtimeTexture.wrapMode = TextureWrapMode.Clamp;
            runtimeTexture.SetPixels32(pixels);
            runtimeTexture.Apply(false, false);

            Vector2 pivot = new Vector2(
                originalSprite.pivot.x / originalSprite.rect.width,
                originalSprite.pivot.y / originalSprite.rect.height
            );

            Sprite runtimeSprite = Sprite.Create(
                runtimeTexture,
                new Rect(0, 0, texWidth, texHeight),
                pivot,
                originalSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            overlayRenderer.sprite = runtimeSprite;

            CacheBrush();
            isReady = true;
        }

        private void CacheBrush()
        {
            int radius = Mathf.Max(1, brushRadius);
            int radiusSqr = radius * radius;

            int count = 0;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSqr)
                    {
                        count++;
                    }
                }
            }

            brushOffsets = new Vector2[count];

            int index = 0;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSqr)
                    {
                        brushOffsets[index] = new Vector2(x, y);
                        index++;
                    }
                }
            }
        }

        public void BeginPaint(Vector3 worldPosition)
        {
            if (!isReady) return;

            hasLastPoint = false;
            PaintMove(worldPosition);
        }

        public void PaintMove(Vector3 worldPosition)
        {
            if (!isReady) return;

            int x;
            int y;

            if (!WorldToPixel(worldPosition, out x, out y))
            {
                if (logMissingHit)
                {
                    Debug.Log("Paint point nằm ngoài overlay.");
                }

                return;
            }

            if (hasLastPoint)
            {
                PaintLine(lastX, lastY, x, y);
            }
            else
            {
                Stamp(x, y);
            }

            lastX = x;
            lastY = y;
            hasLastPoint = true;
        }

        public void EndPaint()
        {
            hasLastPoint = false;
            ApplyTextureIfDirty();
        }

        private bool WorldToPixel(Vector3 worldPosition, out int pixelX, out int pixelY)
        {
            pixelX = 0;
            pixelY = 0;

            Vector3 localPos = overlayRenderer.transform.InverseTransformPoint(worldPosition);
            Bounds bounds = overlayRenderer.sprite.bounds;

            float u = (localPos.x - bounds.min.x) / bounds.size.x;
            float v = (localPos.y - bounds.min.y) / bounds.size.y;

            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return false;
            }

            pixelX = Mathf.Clamp((int)(u * texWidth), 0, texWidth - 1);
            pixelY = Mathf.Clamp((int)(v * texHeight), 0, texHeight - 1);

            return true;
        }

        private void PaintLine(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int distance = Mathf.Max(dx, dy);

            if (distance <= 0)
            {
                Stamp(x0, y0);
                return;
            }

            int spacing = Mathf.Max(1, brushRadius / 2);
            int steps = Mathf.Max(1, distance / spacing);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;

                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));

                Stamp(x, y);
            }
        }

        private void Stamp(int centerX, int centerY)
        {
            for (int i = 0; i < brushOffsets.Length; i++)
            {
                int x = centerX + (int)brushOffsets[i].x;
                int y = centerY + (int)brushOffsets[i].y;

                if (x < 0 || x >= texWidth || y < 0 || y >= texHeight)
                {
                    continue;
                }

                int index = y * texWidth + x;

                Color32 c = pixels[index];

                if (c.a == 0)
                {
                    continue;
                }

                int newAlpha = c.a - erasePower;

                if (newAlpha < 0)
                {
                    newAlpha = 0;
                }

                c.a = (byte)newAlpha;
                pixels[index] = c;

                isDirty = true;
            }
        }

        private void ApplyTextureIfDirty()
        {
            if (!isDirty || runtimeTexture == null)
            {
                return;
            }

            runtimeTexture.SetPixels32(pixels);
            runtimeTexture.Apply(false, false);

            isDirty = false;
        }

        public void ResetPaint()
        {
            if (!isReady || originalSprite == null)
            {
                return;
            }

            Rect rect = originalSprite.rect;
            Texture2D sourceTexture = originalSprite.texture;

            int startX = Mathf.RoundToInt(rect.x);
            int startY = Mathf.RoundToInt(rect.y);

            Color[] sourcePixels = sourceTexture.GetPixels(startX, startY, texWidth, texHeight);

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                pixels[i] = sourcePixels[i];
            }

            isDirty = true;
            ApplyTextureIfDirty();
        }

        public void ClearAll()
        {
            if (!isReady)
            {
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                c.a = 0;
                pixels[i] = c;
            }

            isDirty = true;
            ApplyTextureIfDirty();
        }
    }
}