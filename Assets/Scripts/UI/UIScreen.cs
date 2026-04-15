using UnityEngine;

public class UIScreen : UIUnit
{
    private Transform _tf;
    public Transform Tf
    {
        get
        {
            if (_tf == null) _tf = transform;
            return _tf;
        }
    }

    [Header("Screen Size Constraints")]
    public bool limitX = false;
    public bool limitY = false;
    [SerializeField] private Vector2 maxResolution = new Vector2(1080f, 1920f);

    public virtual void OnCreate() { }
    public virtual void OnShow() { }
    public virtual void OnHide() { }
    public virtual void OnClose() { }

    public virtual void Resize(Vector2 gameSize)
    {
        // Gán kích thước đã được giới hạn an toàn
        RectTf.sizeDelta = FormatSize(gameSize);
    }

    private Vector2 FormatSize(Vector2 gameSize)
    {
        float targetX = limitX ? Mathf.Min(gameSize.x, maxResolution.x) : gameSize.x;
        float targetY = limitY ? Mathf.Min(gameSize.y, maxResolution.y) : gameSize.y;
        return new Vector2(targetX, targetY);
    }
}