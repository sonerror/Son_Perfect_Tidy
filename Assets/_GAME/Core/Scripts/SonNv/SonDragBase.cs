using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace sonnv
{
    [RequireComponent(typeof(Collider2D))]
    public class SonDragBase : SonMonoBehaviour, SonISnapObject,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // ─── Duration constants ──────────────────────────────────────────────
        private const float DURATION_SNAP = 0.20f;
        private const float DURATION_MOVE_BACK = 0.30f;
        private const float DURATION_ROTATE = 0.30f;
        private const float DURATION_TRANSITION = 0.15f;
        private const float DURATION_FADE = 0.30f;

        // ─── Inspector ───────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private EmojiControl emoji;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] protected Collider2D col;
        [SerializeField] protected Transform tfRotate;

        [Header("Sprites")]
        private SpriteRenderer[] sprites = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer sprIng;

        /// <summary>Primary renderer — renderer đầu tiên trong mảng sprites.</summary>
        private SpriteRenderer PrimarySprite => sprites.Length > 0 ? sprites[0] : null;

        // Offset sorting order của mỗi renderer so với base layer (tính lúc Start)
        private int[] _spriteBaseOffsets;

        [Header("Snap")]
        [SerializeField] protected SonSnapPoint[] snapToPosition;
        [SerializeField] private float snapDistance;
        [SerializeField] private bool attachToSnapPoint;
        [SerializeField] protected bool isKnife;

        [Header("Drag")]
        [SerializeField] private bool useUpdateToDragLerp;
        [SerializeField, Range(0f, 1f)] private float interpolateSpeed = 0.8f;
        [SerializeField] private bool ignoreRigidBody;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float scaleOnDrag = 1f;
        [SerializeField] private bool rotateOnDrag;
        [SerializeField] private float onDragRotateZ = 10f;
        [SerializeField] protected bool isRotate;

        [Header("Sorting Order")]
        [SerializeField] private int onDropOrderLayer;
        [SerializeField] private int onDragOrderLayer;
        [SerializeField] private int onSnapOrderLayer;

        [Header("Move Back")]
        [SerializeField] protected bool moveBack;
        [SerializeField] private bool isSetScaleAfterMoveBack;

        [Header("Scale on Snap")]
        [SerializeField] private bool isChangeScaleAfterSnap;
        [SerializeField] private bool isChangeScaleMoveToSnap;
        [SerializeField] private float scaleAfterSnap = 1f;

        [Header("Step Validation")]
        [SerializeField] protected bool isTrueStep;
        [SerializeField] protected bool isCheckCheckFail;
        [SerializeField] protected float timeCount = 1.5f;
        [SerializeField] protected bool isInZoneSnap;
        [SerializeField] protected bool isBlockShowEmoji;

        [Header("Floating (Idle Bounce)")]
        [SerializeField] private bool isBounceLoop;
        [SerializeField] protected float durationFloatingIdle = 2.41f;
        [SerializeField] protected float curveTimeOffset = 0.25f;
        [SerializeField] protected float speedFloatingIdle = 0.1f;
        [SerializeField] protected bool isFloatingStart;

        [Header("Transition")]
        [SerializeField] protected bool isTrans;
        [SerializeField] protected float rotateTrans = 45f;
        [SerializeField] private FxType soundPlay = FxType.None;

        [Header("Audio")]
        [SerializeField] private AudioData onDragAudio;
        [SerializeField] private AudioData onSnapAudio;
        [SerializeField] private AudioData onTransAudio;

        [Header("Events")]
        [SerializeField] protected UnityEvent onSnap;
        [SerializeField] protected UnityEvent onStartDrag;
        [SerializeField] protected UnityEvent onDrop;
        [SerializeField] protected UnityEvent onMouseUp;
        [SerializeField] protected UnityEvent onMovebackDone;
        [SerializeField] protected UnityEvent notSnapWhenNearSnapPoint;
        [SerializeField] protected UnityEvent onSnapFail;
        [SerializeField] private UnityEvent onEndMoveBack;
        [SerializeField] protected UnityEvent onTrans;

        // ─── Public API ──────────────────────────────────────────────────────
        public SonSnapPoint SnapPoint => _snapPoint;
        public bool IsSnap { get; private set; }
        public bool IsDragging => _isDragging;
        public bool CanMoveBack => moveBack;
        public bool IsTrueStep => isTrueStep;
        public Collider2D Col => col;

        public UnityEvent OnSnap => onSnap;
        public UnityEvent OnTrans => onTrans;
        public UnityEvent OnMovebackDone => onMovebackDone;

        /// <summary>Raised when move-back tween completes. Subscribe in code instead of Inspector.</summary>
        public System.Action onMoveBackEnd;

        // ─── Floating state ──────────────────────────────────────────────────
        protected Vector3 floatingAnchor;
        protected float timeOffsetFloating;
        protected bool isFloating;

        // ─── Private state ───────────────────────────────────────────────────
        private Camera _mainCam;
        private bool _isDragging;
        private bool _canInteract = true;

        private Vector3 _initScale;
        private float _initZRot;
        private Vector3 _mousePos;
        private Vector3 _initLocalPos;

        private SonSnapPoint _snapPoint;
        private Tween _moveBackTween;
        private Tween _rotateTween;
        private Tween _dragTimeoutTween;

        // ════════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _mainCam = Camera.main;
            offset.z += Tf.position.z;

            if (moveBack)
                _initLocalPos = Tf.localPosition;

            OnAwake();
        }

        private void Start()
        {
            _initScale = Tf.localScale;
            _initZRot = Tf.eulerAngles.z;

            InitSpriteOffsets();
            SetSortingLayer(onDropOrderLayer);

            if (ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;

            OnStart();

            if (isFloatingStart)
                StartFloating();
        }

        private void InitSpriteOffsets()
        {
            _spriteBaseOffsets = new int[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                _spriteBaseOffsets[i] = sprites[i].sortingOrder - onDropOrderLayer;
            }
        }


        private void SetSortingLayer(int baseLayer)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                sprites[i].sortingOrder = baseLayer + _spriteBaseOffsets[i];
            }
        }

        private void Update()
        {
            if (!useUpdateToDragLerp || !_isDragging || IsSnap) return;

            Tf.position = Vector3.Lerp(Tf.position, _mousePos, interpolateSpeed);

            if (!isRotate)
                Tf.localRotation = Quaternion.Slerp(Tf.localRotation, Quaternion.identity, interpolateSpeed);
        }

        // ════════════════════════════════════════════════════════════════════
        // Pointer events
        // ════════════════════════════════════════════════════════════════════

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_canInteract || _isDragging || IsSnap) return;

            BeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_canInteract || !_isDragging || IsSnap) return;

            UpdateMousePos(eventData);
            if (!useUpdateToDragLerp)
                Tf.position = _mousePos;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging || IsSnap) return;

            _isDragging = false;
            _dragTimeoutTween?.Kill();

            RestorePhysicsAfterDrag();
            Tf.localScale = _initScale;

            if (_canInteract && TrySnap())
                return;

            HandleDrop();
        }

        // ════════════════════════════════════════════════════════════════════
        // Drag helpers
        // ════════════════════════════════════════════════════════════════════

        private void BeginDrag(PointerEventData eventData)
        {
            if (isCheckCheckFail)
                StartFailTimer();

            _isDragging = true;
            StopFloating();
            SetSortingLayer(onDragOrderLayer);

            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;

            col.isTrigger = true;

            UpdateMousePos(eventData);
            if (!useUpdateToDragLerp)
            {
                Tf.position = _mousePos;
                Tf.localRotation = Quaternion.identity;
            }

            Tf.localScale = _initScale * scaleOnDrag;
            SoundManager.PlaySFX(onDragAudio.clip, onDragAudio.volume);

            if (rotateOnDrag)
            {
                _rotateTween?.Kill();
                _rotateTween = Tf.DORotate(new Vector3(0f, 0f, onDragRotateZ), DURATION_ROTATE);
            }

            _moveBackTween?.Kill();
            OnStartDrag();
            onStartDrag.Invoke();
        }

        private void StartFailTimer()
        {
            _dragTimeoutTween?.Kill();
            _dragTimeoutTween = DOVirtual.DelayedCall(timeCount, () =>
            {
                if (_isDragging && !isTrueStep)
                    HandleSnapFail();
            });
        }

        private void RestorePhysicsAfterDrag()
        {
            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Dynamic;

            col.isTrigger = false;
            StartFloating();
        }

        private void UpdateMousePos(PointerEventData eventData)
        {
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(eventData.position);
            worldPos += offset;
            worldPos.z = offset.z;
            _mousePos = worldPos;
        }

        // ════════════════════════════════════════════════════════════════════
        // Snap logic
        // ════════════════════════════════════════════════════════════════════

        /// <returns>true if successfully snapped to a point.</returns>
        private bool TrySnap()
        {
            foreach (SonSnapPoint snapPoint in snapToPosition)
            {
                if (!IsWithinSnapDistance(snapPoint)) continue;

                // Feedback for nearby but invalid snap points
                if (!snapPoint.isSnap && !snapPoint.canSnap && !isBlockShowEmoji)
                    emoji.ShowNegative();

                if (isKnife && snapPoint.isSnap && snapPoint.canSnap)
                    emoji.ShowNegative();

                if (isCheckCheckFail && !isTrueStep)
                    HandleSnapFail();

                // Skip inactive or already-snapped / non-snap-capable points
                if (!snapPoint.Tf.gameObject.activeSelf) continue;
                if (snapPoint.isSnap || !snapPoint.canSnap) continue;

                _snapPoint = snapPoint;

                if (!CanSnap())
                {
                    notSnapWhenNearSnapPoint.Invoke();
                    break;
                }

                ExecuteSnap(snapPoint);
                return true;
            }
            return false;
        }

        private void ExecuteSnap(SonSnapPoint snapPoint)
        {
            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;

            if (attachToSnapPoint)
                Tf.SetParent(snapPoint.Tf);

            IsSnap = true;
            snapPoint.isSnap = true;
            snapPoint.OnSnap();

            SetSortingLayer(onSnapOrderLayer);
            col.enabled = false;

            SoundManager.PlaySFX(onSnapAudio.clip, onSnapAudio.volume);
            OnSnapObject();

            if (!isTrans)
            {
                Tf.DOMove(snapPoint.Tf.position, DURATION_SNAP)
                  .OnComplete(() =>
                  {
                      if (isChangeScaleMoveToSnap)
                          Tf.localScale = Vector3.one * scaleAfterSnap;

                      onSnap.Invoke();
                  });
            }
        }

        private void HandleSnapFail()
        {
            emoji.ShowNegative();
            onSnapFail?.Invoke();
            MoveBack();
        }

        private bool IsWithinSnapDistance(SonSnapPoint snapPoint)
            => DistanceToInSqrVec2(snapPoint.Tf) < snapDistance;

        // ════════════════════════════════════════════════════════════════════
        // Drop (no snap)
        // ════════════════════════════════════════════════════════════════════

        private void HandleDrop()
        {
            onMouseUp.Invoke();
            OnDrop();

            if (rotateOnDrag)
            {
                _rotateTween?.Kill();
                _rotateTween = Tf.DORotate(new Vector3(0f, 0f, _initZRot), DURATION_ROTATE);
            }

            if (moveBack)
            {
                _moveBackTween = Tf.DOLocalMove(_initLocalPos, DURATION_MOVE_BACK)
                    .OnComplete(OnMoveBackComplete);
            }
            else
            {
                SetSortingLayer(onDropOrderLayer);
                onDrop.Invoke();
            }
        }

        private void OnMoveBackComplete()
        {
            SetSortingLayer(onDropOrderLayer);
            onEndMoveBack?.Invoke();
            onDrop.Invoke();
        }

        // ════════════════════════════════════════════════════════════════════
        // Public move-back
        // ════════════════════════════════════════════════════════════════════

        public void MoveBack()
        {
            if (!moveBack) return;

            _moveBackTween?.Kill();
            onMouseUp?.Invoke();

            _isDragging = false;
            IsSnap = false;
            _snapPoint = null;

            col.enabled = true;
            col.isTrigger = false;
            Tf.localScale = _initScale;

            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Dynamic;

            _moveBackTween = Tf.DOLocalMove(_initLocalPos, DURATION_MOVE_BACK)
                .OnComplete(() =>
                {
                    SetSortingLayer(onDropOrderLayer);

                    if (isSetScaleAfterMoveBack)
                    {
                        Tf.localScale = Vector3.zero;
                        onEndMoveBack?.Invoke();
                    }

                    onMovebackDone?.Invoke();
                    onMoveBackEnd?.Invoke();
                    onDrop.Invoke();
                });

            if (rotateOnDrag)
            {
                _rotateTween?.Kill();
                _rotateTween = Tf.DORotate(new Vector3(0f, 0f, _initZRot), DURATION_ROTATE);
            }
        }

        public void DelayMoveBack(float delay)
            => DOVirtual.DelayedCall(delay, MoveBack);

        // ════════════════════════════════════════════════════════════════════
        // Snap object callbacks
        // ════════════════════════════════════════════════════════════════════

        private void OnSnapObject()
        {
            if (isChangeScaleAfterSnap)
                Tf.localScale = Vector3.one * scaleAfterSnap;

            StopFloating();
            OnTransition();
        }

        // ════════════════════════════════════════════════════════════════════
        // Transition effect
        // ════════════════════════════════════════════════════════════════════

        private void OnTransition()
        {
            if (!isTrans) return;

            // Dùng sprIng (overlay riêng) nếu có, nếu không fallback về primary sprite
            SpriteRenderer fadeTarget = sprIng != null ? sprIng : PrimarySprite;
            if (fadeTarget == null) return;

            col.enabled = false;

            Tf.DOMove(tfRotate.position, DURATION_TRANSITION)
              .OnComplete(() =>
              {
                  if (soundPlay != FxType.None)
                      SoundManager.Ins.PlayFx(soundPlay);

                  Tf.DORotate(new Vector3(0f, 0f, rotateTrans), DURATION_ROTATE)
                    .OnComplete(() =>
                    {
                        SoundManager.PlaySFX(onTransAudio.clip, onTransAudio.volume);

                        FadeSprite(fadeTarget, 1f, DURATION_FADE, Ease.Linear, () =>
                        {
                            onTrans?.Invoke();

                            FadeSprite(fadeTarget, 0f, DURATION_FADE, Ease.Linear, () =>
                            {
                                Tf.DORotate(new Vector3(0f, 0f, _initZRot), DURATION_ROTATE)
                                  .OnComplete(MoveBack);
                            });
                        });
                    });
              });
        }

        private void FadeSprite(SpriteRenderer target, float alpha, float duration,
                                Ease ease = Ease.Linear, System.Action onDone = null)
        {
            target.DOFade(alpha, duration)
                  .SetEase(ease)
                  .OnComplete(() => onDone?.Invoke());
        }

        // ════════════════════════════════════════════════════════════════════
        // Floating idle
        // ════════════════════════════════════════════════════════════════════

        public void StartFloating()
        {
            if (!isBounceLoop) return;
            isFloating = true;
            floatingAnchor = Tf.position;
            timeOffsetFloating =
                (Mathf.Round(Time.time / durationFloatingIdle) + curveTimeOffset)
                * durationFloatingIdle - Time.time;
        }

        public void StopFloating() => isFloating = false;

        // ════════════════════════════════════════════════════════════════════
        // Public setters (kept for external callers)
        // ════════════════════════════════════════════════════════════════════

        public void ChangeCheckActionStepMix(bool value) => isTrueStep = value;
        public void ChangeSetScale() => isSetScaleAfterMoveBack = true;

        // ════════════════════════════════════════════════════════════════════
        // Virtual extension points
        // ════════════════════════════════════════════════════════════════════

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual bool CanSnap() => true;
        protected virtual void OnStartDrag() { }
        protected virtual void OnDrop() { }
    }
}
