using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

namespace sonnv
{
    [RequireComponent(typeof(Collider2D))]
    public class TapSpatula : SonMonoBehaviour, IPointerDownHandler
    {
        [Header("References")]
        [SerializeField] private SonSnapPoint snapPoint;
        public SonSnapPoint SnapPoint => snapPoint;
        [SerializeField] private SpriteRenderer spriteCream;
        [SerializeField] private GameObject objCream;
        [SerializeField] private Collider2D col;

        [Space(10)]
        [Header("Items")]
        [SerializeField] private ItemBySpatula itemBase;
        [SerializeField] private ItemBySpatula itemKiwi;

        [Space(10)]
        [Header("Transforms")]
        [SerializeField] private Transform handTransform;
        [SerializeField] private Transform tfTarget;
        [SerializeField] private Transform tfNextStep;

        [Space(10)]
        [Header("Tap Settings")]
        [SerializeField] private float tfY = 50f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;
        [SerializeField] private float returnDuration = 0.3f;
        [SerializeField] private Ease returnEase = Ease.InQuad;

        [Space(10)]
        [Header("Win Settings")]
        [SerializeField] private int tapToWin = 2;
        [SerializeField] private UnityEvent onWin;
        public UnityEvent OnWin => onWin;

        [Space(10)]
        [Header("Audio")]
        [SerializeField] private AudioClip sfxTap;
        [SerializeField] private AudioClip sfxHit;

        [Space(10)]
        [Header("Events")]
        [SerializeField] private UnityEvent onStartTap;
        [SerializeField] private UnityEvent onReachTarget;
        [SerializeField] private UnityEvent onReturnStart;

        [Space(10)]
        [Header("State")]
        [SerializeField] private bool canTap = true;

        private Vector3 _startLocalPosition;
        private Sequence _tapSequence;
        private int _tapCount = 0;

        private void Awake()
        {
            if (handTransform == null) handTransform = transform;
            _startLocalPosition = handTransform.localPosition;
        }

        public void SetItemInMortar()
        {
            itemBase = null;
            if (itemKiwi != null) itemBase = itemKiwi;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!canTap) return;
            canTap = false;
            PlayTap();
        }

        private void PlayTap()
        {
            _tapSequence?.Kill();
            handTransform.localPosition = _startLocalPosition;
            onStartTap?.Invoke();
            SoundManager.PlaySFXOneShot(sfxTap);

            float targetY = _startLocalPosition.y - tfY;
            if (tfTarget != null)
            {
                Vector3 localTarget = handTransform.parent != null
                    ? handTransform.parent.InverseTransformPoint(tfTarget.position)
                    : tfTarget.position;
                targetY = localTarget.y;
            }

            _tapSequence = DOTween.Sequence();
            _tapSequence.SetAutoKill(true);

            // Đi xuống
            _tapSequence.Append(handTransform.DOLocalMoveY(targetY, moveDuration)
                .SetEase(moveEase));

            // Chạm đích
            _tapSequence.AppendCallback(() =>
            {
                SoundManager.PlaySFXOneShot(sfxHit);
                itemBase?.OnPlayEffect();
                onReachTarget?.Invoke();
            });

            // Trả về
            _tapSequence.Append(handTransform.DOLocalMoveY(_startLocalPosition.y, returnDuration)
                .SetEase(returnEase));

            // Fade cream + reset
            _tapSequence.AppendCallback(() =>
            {
                objCream.SetActive(false);
                itemBase = itemKiwi;
                onReturnStart?.Invoke();
            });
            _tapSequence.Append(spriteCream.DOFade(0.5f, 0.1f).SetEase(Ease.OutQuad));

            // Move sang vị trí tiếp theo
            if (tfNextStep != null)
            {
                Vector3 nextLocalPos = handTransform.parent != null
                    ? handTransform.parent.InverseTransformPoint(tfNextStep.position)
                    : tfNextStep.position;

                _tapSequence.Append(handTransform.DOLocalMove(nextLocalPos, 0.1f)
                    .SetEase(Ease.OutQuad));
            }

            // Check win
            _tapSequence.AppendCallback(() =>
            {
                _startLocalPosition = handTransform.localPosition;
                _tapCount++;
                // TutorialManager.Ins.CountStep2();
                if (_tapCount >= tapToWin)
                {
                    col.enabled = false;
                    canTap = false;
                    handTransform.localScale = Vector3.zero;
                    spriteCream.DOFade(1f, 0.1f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => onWin?.Invoke());
                }
                else
                {
                    canTap = true;
                }
            });
        }

        [Button("Reset Tap State")]
        public void ResetTap()
        {
            StopTap();
            _tapCount = 0;
            _startLocalPosition = handTransform.localPosition;
            handTransform.localScale = Vector3.one;
            handTransform.gameObject.SetActive(true);
            canTap = true;
        }

        public void StopTap()
        {
            _tapSequence?.Kill();
            _tapSequence = null;
            handTransform.localPosition = _startLocalPosition;
        }

        private void OnDestroy()
        {
            _tapSequence?.Kill();
        }
    }
}