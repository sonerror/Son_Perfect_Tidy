using UnityEngine;

namespace sonnv
{
    public class SonDragPaintCaller : MonoBehaviour
    {
        [Header("Object kéo")]
        [SerializeField] private SonSnapObject snapObject;

        [Header("Điểm dùng để tô/xóa")]
        [SerializeField] private Transform brushPoint;

        [Header("Painter target")]
        [SerializeField] private SonSpriteErasePainter2D painter;

        [Header("Setting")]
        [SerializeField] private bool paintWhenDragging = true;

        private bool wasDragging;

        private void Awake()
        {
            if (snapObject == null)
            {
                snapObject = GetComponent<SonSnapObject>();
            }

            if (brushPoint == null)
            {
                brushPoint = transform;
            }
        }

        private void LateUpdate()
        {
            if (!paintWhenDragging) return;
            if (snapObject == null) return;
            if (painter == null) return;
            if (brushPoint == null) return;

            bool isDragging = snapObject.IsDragging;

            if (isDragging && !wasDragging)
            {
                painter.BeginPaint(brushPoint.position);
            }
            else if (isDragging && wasDragging)
            {
                painter.PaintMove(brushPoint.position);
            }
            else if (!isDragging && wasDragging)
            {
                painter.EndPaint();
            }

            wasDragging = isDragging;
        }

        public void SetPainter(SonSpriteErasePainter2D newPainter)
        {
            painter = newPainter;
        }

        public void SetBrushPoint(Transform newBrushPoint)
        {
            brushPoint = newBrushPoint;
        }

        public void EnablePaint(bool value)
        {
            paintWhenDragging = value;
        }
    }
}