using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Linq;


namespace sonnv
{
    [RequireComponent(typeof(Collider2D))]
    public class SonDragSnap : SonMonoBehaviour, SonISnapObject,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float DURATION_SNAP = 0.20f;
        private const float DURATION_MOVE_BACK = 0.30f;
        private const float DURATION_ROTATE = 0.30f;
        private const float DURATION_TRANSITION = 0.15f;
        private const float DURATION_FADE = 0.30f;

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] protected Collider2D col;
        [SerializeField] protected Transform tfRotate;
        [Header("Sprites")]
        [SerializeField] private SpriteRenderer sprites;
        [SerializeField] private SpriteRenderer sprIng;

        private SpriteRenderer PrimarySprite => sprites;


        [Header("Snap")]
        [SerializeField] protected SnapPointHint snapToPosition;
        public SnapPointHint SnapToPosition => snapToPosition;
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
        public void SetFloatingStart(bool value)
        {
            isFloatingStart = value;
        }

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

        public SnapPointHint SnapPoint => _snapPoint;
        public bool IsSnap { get; private set; }
        public bool IsDragging => _isDragging;
        public bool CanMoveBack => moveBack;
        public bool IsTrueStep => isTrueStep;
        public Collider2D Col => col;

        public UnityEvent OnSnap => onSnap;
        public UnityEvent OnTrans => onTrans;
        public UnityEvent OnMovebackDone => onMovebackDone;

        public System.Action onMoveBackEnd;

        protected Vector3 floatingAnchor;
        protected float timeOffsetFloating;
        protected bool isFloating;

        private Camera _mainCam;
        private bool _isDragging;
        private bool _canInteract = true;

        private Vector3 _initScale;
        private float _initZRot;
        private Vector3 _mousePos;
        private Vector3 _initLocalPos;
        private SnapPointHint _snapPoint;
        private Tween _moveBackTween;
        private Tween _rotateTween;
        private Tween _dragTimeoutTween;
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
            if (ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;
            OnStart();
            if (isFloatingStart)
                StartFloating();
        }
        public void SetOnInit()
        {
            _initScale = Tf.localScale;
            _initZRot = Tf.eulerAngles.z;
            if (ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;
            OnStart();
            if (isFloatingStart)
                StartFloating();
        }

        private void SetSortingLayer(int baseLayer)
        {
            sprites.sortingOrder = baseLayer;

        }
        private void Update()
        {
            if (!useUpdateToDragLerp || !_isDragging || IsSnap) return;

            Tf.position = Vector3.Lerp(Tf.position, _mousePos, interpolateSpeed);

            if (!isRotate)
                Tf.localRotation = Quaternion.Slerp(Tf.localRotation, Quaternion.identity, interpolateSpeed);
        }
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

            Tf.DOKill();

            if (_canInteract && TrySnap())
            {
                Tf.localScale = _initScale;
                return;
            }

            RestorePhysicsAfterDrag();
            Tf.localScale = _initScale;
            HandleDrop();
        }
        private void BeginDrag(PointerEventData eventData)
        {
            if (isCheckCheckFail)
                StartFailTimer();
            _isDragging = true;
            StopFloating();
            if (snapToPosition != null)
            {
                snapToPosition.PlayBlueAnim();
            }
            GameManager.maxLayer++;
            SetSortingLayer(GameManager.maxLayer);
            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;
            col.isTrigger = true;
            UpdateMousePos(eventData);
            Tf.localScale = _initScale * scaleOnDrag;
            SoundManager.PlaySFX(onDragAudio.clip, onDragAudio.volume);
            _rotateTween?.Kill();
            _rotateTween = Tf.DORotate(new Vector3(0f, 0f, 0f), DURATION_ROTATE);
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
        private bool TrySnap()
        {
            if (snapToPosition == null) return false;
            if (!IsWithinSnapDistance(snapToPosition)) return false;

            if (isCheckCheckFail && !isTrueStep)
                HandleSnapFail();

            if (!snapToPosition.Tf.gameObject.activeSelf) return false;
            if (snapToPosition.isSnap || !snapToPosition.canSnap) return false;

            _snapPoint = snapToPosition;
            if (!CanSnap())
            {
                notSnapWhenNearSnapPoint.Invoke();
                return false;
            }

            ExecuteSnap(snapToPosition);
            return true;
        }
        private void ExecuteSnap(SnapPointHint snapPoint)
        {
            if (!ignoreRigidBody && rb != null)
                rb.bodyType = RigidbodyType2D.Static;

            if (attachToSnapPoint)
                Tf.SetParent(snapPoint.Tf);

            IsSnap = true;
            snapPoint.isSnap = true;
            snapPoint.OnSnap();

            col.enabled = false;

            SoundManager.PlaySFX(onSnapAudio.clip, onSnapAudio.volume);

            snapPoint.StopAnim();
            StopFloating();

            Tf.DOKill();

            if (isTrans)
            {
                OnTransition();
            }
            else
            {
                Vector3 finalScale = (isChangeScaleMoveToSnap || isChangeScaleAfterSnap)
                    ? Vector3.one * scaleAfterSnap
                    : _initScale;

                float stage1 = DURATION_SNAP * 1.2f;
                float stage2 = DURATION_SNAP * 0.8f;

                Tf.DOMove(snapPoint.Tf.position + new Vector3(0f, 0.3f, 0f), stage1)
                    .SetEase(Ease.OutSine);
                Tf.DOScale(_initScale * 1.15f, stage1)
                    .SetEase(Ease.OutSine)
                    .OnComplete(() =>
                    {
                        Tf.DOMove(snapPoint.Tf.position, stage2).SetEase(Ease.InSine);
                        Tf.localScale = finalScale;
                        snapPoint.OnSetColorSnap();
                        Tf.DOScale(Vector3.zero, stage2)
                            .SetEase(Ease.InSine)
                            .OnComplete(() =>
                            {
                                SetSortingLayer(onSnapOrderLayer);
                                onSnap?.Invoke();
                            });
                    });
            }
        }

        private void HandleSnapFail()
        {
            onSnapFail?.Invoke();
            MoveBack();
        }

        private bool IsWithinSnapDistance(SnapPointHint snapPoint)
            => DistanceToInSqrVec2(snapPoint.Tf) < snapDistance;
        private void HandleDrop()
        {
            onMouseUp.Invoke();
            OnDrop();
            _rotateTween?.Kill();
            _rotateTween = Tf.DORotate(new Vector3(0f, 0f, _initZRot), DURATION_ROTATE);
            if (snapToPosition != null)
            {
                snapToPosition.StopAnim();
            }
            if (moveBack)
            {
                _moveBackTween = Tf.DOLocalMove(_initLocalPos, DURATION_MOVE_BACK)
                    .OnComplete(OnMoveBackComplete);
            }
            else
            {
                onDrop.Invoke();
            }
        }

        private void OnMoveBackComplete()
        {
            onEndMoveBack?.Invoke();
            onDrop.Invoke();
        }
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
                    if (isSetScaleAfterMoveBack)
                    {
                        Tf.localScale = Vector3.zero;
                        onEndMoveBack?.Invoke();
                    }
                    onMovebackDone?.Invoke();
                    onMoveBackEnd?.Invoke();
                    onDrop.Invoke();
                });
            _rotateTween?.Kill();
            _rotateTween = Tf.DORotate(new Vector3(0f, 0f, _initZRot), DURATION_ROTATE);
        }

        public void DelayMoveBack(float delay)
            => DOVirtual.DelayedCall(delay, MoveBack);

        private void OnSnapObject()
        {
            snapToPosition.StopAnim();
            StopFloating();
            OnTransition();
            Tf.DOMove(snapToPosition.transform.position, 0.3f).OnComplete(() =>
            {
                snapToPosition.OnSetColorSnap();
                if (isChangeScaleAfterSnap)
                    Tf.localScale = Vector3.one * scaleAfterSnap;

            });

        }
        private void OnTransition()
        {
            if (!isTrans) return;

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
        private Tween _floatingTween;
        public void StartFloating()
        {
            if (!isBounceLoop) return;

            _floatingTween?.Kill();
            isFloating = true;
            floatingAnchor = Tf.position;

            _floatingTween = Tf.DOMoveY(floatingAnchor.y + speedFloatingIdle, durationFloatingIdle / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(timeOffsetFloating);
        }

        public void StopFloating()
        {
            if (!isFloating) return;
            isFloating = false;
            _floatingTween?.Kill();
            _floatingTween = null;
            //Tf.position = floatingAnchor;
        }
        public void ChangeCheckActionStepMix(bool value) => isTrueStep = value;
        public void ChangeSetScale() => isSetScaleAfterMoveBack = true;
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual bool CanSnap() => true;
        protected virtual void OnStartDrag() { }
        protected virtual void OnDrop() { }


        [Button]
        public void GetReferences()
        {
            sprites = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponentInChildren<Rigidbody2D>();
            col = GetComponentInChildren<Collider2D>();
        }
        [Button]
        public void FindSnapPointWithSameName()
        {
            string gobName = gameObject.name;
            snapToPosition = FindObjectsOfType<SnapPointHint>().FirstOrDefault(s => s.name == gobName);
        }
    }
}
