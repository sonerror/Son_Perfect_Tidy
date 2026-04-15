using DG.Tweening;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace sonnv
{
    public class SnapPointHint : SonMonoBehaviour
    {
        [SerializeField] private SpriteRenderer spritesHint;
        public SpriteRenderer SpritesHint => spritesHint;
        public bool canSnap = true;
        public bool isSnap;

        [Header("Anim Settings")]
        [SerializeField] private Color blueColor = new Color(0f, 0.5f, 1f, 1f);
        [SerializeField] private Color whiteColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float alphaDuration = 0.8f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 0.4f;
        [SerializeField] private UnityEvent onSnap;

        private Tween _alphaTween;
        private Color _originColor;
        public void Show()
        {
            gameObject.SetActive(true);
            Vector3 scale = Tf.localScale;
            Tf.localScale = Vector3.zero;
            Tf.DOScale(scale, 0.3f);
        }
        public void PlayBlueAnim()
        {
            if (spritesHint == null) return;
            StopAnim();
            _originColor = spritesHint.color;
            Color startColor = blueColor;
            startColor.a = maxAlpha;
            spritesHint.color = startColor;
            // _alphaTween = DOTween.To(
            //         () => spritesHint.color.a,
            //         alpha =>
            //         {
            //             Color c = spritesHint.color;
            //             c.a = alpha;
            //             spritesHint.color = c;
            //         },
            //         minAlpha,
            //         alphaDuration)
            //     .SetEase(Ease.InOutSine)
            //     .SetLoops(-1, LoopType.Yoyo);
        }
        public void StopAnim()
        {
            _alphaTween?.Kill();
            _alphaTween = null;

            if (spritesHint != null)
                spritesHint.color = _originColor;
        }
        public void OnSnap()
        {
            onSnap?.Invoke();
            if (spritesHint != null)
                spritesHint.color = _originColor;
        }
        public virtual void OnSetColorSnap()
        {
            if (spritesHint != null)
                spritesHint.color = whiteColor;
        }

        public virtual void ForceChangeSnap(bool snap)
        {
            isSnap = snap;
        }

        public void ChangeCanSnap(bool snap)
        {
            canSnap = snap;
        }

        private void OnDestroy()
        {
            _alphaTween?.Kill();
        }
        [Button]
        public void GetReferences()
        {
            spritesHint = GetComponentInChildren<SpriteRenderer>();
        }
    }
}