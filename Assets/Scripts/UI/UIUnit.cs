using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIUnit : MonoBehaviour
{
    private RectTransform _rectTf;
    public RectTransform RectTf
    {
        get
        {
            if (_rectTf == null) _rectTf = GetComponent<RectTransform>();
            return _rectTf;
        }
    }
}