using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using sonnv;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Utilities;
namespace sonnv
{
    [RequireComponent(typeof(Collider2D))]
    public class SonSnapObject : SonMonoBehaviour, SonISnapObject,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private EmojiControl emoji;
        [SerializeField] protected SonSnapPoint[] snapToPosition;
        private SonSnapPoint _snapPoint;
        public SonSnapPoint SnapPoint => _snapPoint;
        [SerializeField] protected SpriteRenderer sprite;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] protected Collider2D col;

        [SerializeField] protected bool moveBackOnDrop = true;

        [SerializeField] private bool useUpdateToDragLerp;
        [SerializeField, Range(0f, 1f)] private float interpolateSpeed = 0.8f;
        [SerializeField] private bool ignoreRigidBody;
        [SerializeField] private bool attachToSnapPoint;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float snapDistance;
        [SerializeField] private float scaleOnDrag = 1;
        [SerializeField] private int onDropOrderLayer;
        [SerializeField] private int onDragOrderLayer;
        [SerializeField] private int onSnapOrderLayer;
        [SerializeField] protected bool isRotate;

        [SerializeField] protected bool isRb = false;

        [SerializeField] private AudioData onDragAudio;
        [SerializeField] private AudioData onSnapAudio;

        [SerializeField] protected UnityEvent onSnap;
        [SerializeField] protected UnityEvent onStartDrag;
        [SerializeField] protected UnityEvent onDrop;
        [SerializeField] protected UnityEvent onMouseUp;
        [SerializeField] protected UnityEvent notSnapWhenNearSnapPoint;
        [SerializeField] protected UnityEvent onMovebackDone;
        [SerializeField] protected UnityEvent onSnapFail;

        [SerializeField] protected UnityEvent onSnapBin;
        [SerializeField] private AudioData onBinAudio;


        [SerializeField] private bool isEnableCollideWhenEnableThis;
        [SerializeField] private bool moveBack;
        [SerializeField] private bool isChangeScaleAffterSnap = false;
        [SerializeField] private bool isChangeScaleMoveToSnap = false;
        [SerializeField] private float scaleAffterSnap = 1;

        [SerializeField] private bool rotateOnDrag;
        [SerializeField] private float onDragRotateZ = 10f;

        [SerializeField] private bool isBounceLoop = false;
        [SerializeField] protected float durationFloatingIdle = 2.41f;
        [SerializeField] protected float curveTimeOffSet = 0.25f;
        [SerializeField] protected float speedFloatingIdle = 0.1f;
        [SerializeField] protected bool isFloatingStart;
        [SerializeField] protected bool isKnife = false;
        [SerializeField] protected bool isTrueStep = false;
        public bool IsTrueStep => isTrueStep;
        public void ChangeCheckActionStepMix(bool value)
        {
            isTrueStep = value;
        }
        [SerializeField] protected bool isCheckCheckFail = false;
        [SerializeField] protected float timeCount = 1.5f;
        [SerializeField] protected bool isInZoneSnap = false;
        [SerializeField] protected bool isBlockShowEmoji = false;
        [SerializeField] protected bool isSetScaleAffterMoveBack = false;
        [SerializeField] private UnityEvent onEndMoveBack;
        public void ChangeSetScale()
        {
            isSetScaleAffterMoveBack = true;
        }
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

        private Tween _moveBackTween;
        private Tween _rotateTween;
        private Tween _dragTimeoutTween;
        public bool IsSnap { get; private set; }
        public bool CanMoveBack => moveBack;
        public bool IsDragging => _isDragging;

        public UnityEvent OnSnap => onSnap;
        public System.Action onMoveBackEnd;
        public UnityEvent OnMovebackDone => onMovebackDone;
        public Collider2D Col => col;
        [SerializeField] protected bool isTrans = false;
        [SerializeField] protected float rotateTrans = 45f;
        [SerializeField] protected Transform tfRotate;
        [SerializeField] private SpriteRenderer sprIng;
        [SerializeField] protected UnityEvent onTrans;
        public UnityEvent OnTrans => onTrans;
        [SerializeField] private AudioData onTransAudio;
        [SerializeField] FxType soundPlay = FxType.None;

        private void OnTransiton()
        {
            if (isTrans)
            {
                col.enabled = false;
                Tf.DOMove(tfRotate.position, 0.15f)
                    .OnComplete(() =>
                    {
                        if (soundPlay != FxType.None)
                        {
                            SoundManager.Ins.PlayFx(soundPlay);
                        }
                        Tf.DORotate(new Vector3(0, 0, rotateTrans), 0.3f).OnComplete(() =>
                            {
                                SoundManager.PlaySFX(onTransAudio.clip, onTransAudio.volume);
                                FadeSprite(sprIng, 1, 0.3f, Ease.Linear, () =>
                                {
                                    onTrans?.Invoke();
                                    FadeSprite(sprIng, 0, 0.3f, Ease.Linear, () =>
                                    {
                                        Tf.DORotate(new Vector3(0, 0, _initZRot), 0.3f).OnComplete(() =>
                                            {
                                                MoveBack();
                                            });
                                    });

                                });
                            });
                    });
            }
        }
        void FadeSprite(SpriteRenderer sprite, float alpha, float time, Ease ease = Ease.Linear, System.Action onDone = null)
        {
            sprite.DOFade(alpha, time)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    onDone?.Invoke();
                });

        }
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

            sprite.sortingOrder = onDropOrderLayer;

            if (ignoreRigidBody && rb)
                rb.bodyType = RigidbodyType2D.Static;

            OnStart();

            if (isFloatingStart)
                StartFloating();
        }

        private void Update()
        {
            if (!useUpdateToDragLerp) return;
            if (!_isDragging || IsSnap) return;
            Tf.position = Vector3.Lerp(Tf.position, _mousePos, interpolateSpeed);

            if (!isRotate)
                Tf.localRotation = Quaternion.Slerp(
                    Tf.localRotation,
                    Quaternion.identity,
                    interpolateSpeed
                );
        }
        private void CheckAndHandleSnapZone()
        {
            if (!CheckInZoneSnap())
            {
                emoji.ShowNegative();
                onSnapFail?.Invoke();
                MoveBack();
            }
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_canInteract || _isDragging || IsSnap) return;
            if (isCheckCheckFail)
            {
                _dragTimeoutTween?.Kill();
                _dragTimeoutTween = DOVirtual.DelayedCall(timeCount, () =>
                {
                    if (_isDragging && !isTrueStep)
                    {
                        emoji.ShowNegative();
                        onSnapFail?.Invoke();
                        MoveBack();
                    }
                });

            }
            _isDragging = true;
            StopFloating();
            sprite.sortingOrder = onDragOrderLayer;
            if (!ignoreRigidBody && rb && !isRb)
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
                if (_rotateTween != null) _rotateTween.Kill();
                _rotateTween = Tf.DORotate(
                    new Vector3(0, 0, onDragRotateZ),
                    0.3f
                );
            }
            if (_moveBackTween != null) _moveBackTween.Kill();
            OnStartDrag();
            onStartDrag.Invoke();
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (!_canInteract || !_isDragging || IsSnap) return;
            UpdateMousePos(eventData);
            if (!useUpdateToDragLerp)
                Tf.position = _mousePos;
        }
        private bool CheckInZoneSnap()
        {
            for (int i = 0; i < snapToPosition.Length; i++)
            {
                SonSnapPoint snapPoint = snapToPosition[i];
                if (DistanceToInSqrVec2(snapPoint.Tf) < snapDistance)
                {
                    if (!snapPoint.isSnap && !snapPoint.canSnap)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging || IsSnap) return;
            _isDragging = false;
            if (isCheckCheckFail)
            {
                _dragTimeoutTween?.Kill();
            }
            if (isInZoneSnap)
            {
                _dragTimeoutTween?.Kill();
            }
            StartFloating();
            if (!ignoreRigidBody && rb && !isRb)
                rb.bodyType = RigidbodyType2D.Dynamic;
            col.isTrigger = false;
            Tf.localScale = _initScale;
            if (_canInteract)
            {
                for (int i = 0; i < snapToPosition.Length; i++)
                {
                    SonSnapPoint snapPoint = snapToPosition[i];
                    if ((DistanceToInSqrVec2(snapPoint.Tf) < snapDistance))
                    {
                        if (!snapPoint.isSnap && !snapPoint.canSnap && !isBlockShowEmoji)
                        {
                            emoji.ShowNegative();
                            onSnapFail?.Invoke();
                        }
                        if (isKnife)
                        {
                            if (snapPoint.isSnap && snapPoint.canSnap)
                            {
                                emoji.ShowNegative();
                            }
                        }
                        if (isCheckCheckFail)
                        {
                            if (isTrueStep == false)
                            {
                                emoji.ShowNegative();
                                onSnapFail?.Invoke();
                            }
                        }
                    }
                    if (!snapPoint.Tf.gameObject.activeSelf) continue;
                    if (snapPoint.isSnap || !snapPoint.canSnap) continue;
                    if (!(DistanceToInSqrVec2(snapPoint.Tf) < snapDistance)) continue;
                    _snapPoint = snapPoint;
                    if (!CanSnap())
                    {
                        notSnapWhenNearSnapPoint.Invoke();
                        break;
                    }
                    if (!ignoreRigidBody && rb && !isRb)
                        rb.bodyType = RigidbodyType2D.Static;

                    if (attachToSnapPoint)
                        Tf.SetParent(snapPoint.Tf);
                    IsSnap = true;
                    snapPoint.isSnap = true;
                    snapPoint.OnSnap();
                    sprite.sortingOrder = onSnapOrderLayer;
                    col.enabled = false;
                    SoundManager.PlaySFX(onSnapAudio.clip, onSnapAudio.volume);
                    OnSnapObject();
                    if (!isTrans)
                    {
                        if (isRb == true && rb)
                        {
                            sfxBin.Play();
                        }
                        Tf.DOMove(snapPoint.Tf.position, 0.15f)
                          .OnComplete(() =>
                          {
                              if (isChangeScaleMoveToSnap)
                              {
                                  Tf.localScale = Vector3.one * scaleAffterSnap;
                              }
                              if (isRb == true && rb)
                              {
                                  rb.bodyType = RigidbodyType2D.Dynamic;
                                  StartCoroutine(IE_DelayHideObj());
                              }
                              onSnap.Invoke();
                          });
                    }
                    return;
                }
            }
            onMouseUp.Invoke();
            OnDrop();
            if (rotateOnDrag)
            {
                if (_rotateTween != null) _rotateTween.Kill();
                _rotateTween = Tf.DORotate(
                    new Vector3(0, 0, _initZRot),
                    0.3f
                );
            }
            if (moveBack)
            {
                _moveBackTween = Tf.DOLocalMove(_initLocalPos, 0.3f)
                    .OnComplete(() =>
                    {
                        sprite.sortingOrder = onDropOrderLayer;
                        if (onMoveBackEnd != null)
                            onMoveBackEnd.Invoke();
                        onDrop.Invoke();
                    });
            }
            else
            {
                sprite.sortingOrder = onDropOrderLayer;
                onDrop.Invoke();
            }
        }
        [SerializeField] protected AudioSource sfxBin;
        IEnumerator IE_DelayHideObj()
        {
            yield return new WaitForSeconds(0.55f);
            Tf.localScale = Vector3.one * 0;
            rb.bodyType = RigidbodyType2D.Static;
            onSnapBin?.Invoke();
        }
        private void SetScaleTF()
        {
            
        }
        private void UpdateMousePos(PointerEventData eventData)
        {
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(eventData.position);
            worldPos += offset;
            worldPos.z = offset.z;
            _mousePos = worldPos;
        }
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual bool CanSnap() { return true; }
        protected virtual void OnStartDrag() { }
        protected virtual void OnDrop() { }

        private void OnSnapObject()
        {
            if (isChangeScaleAffterSnap)
                Tf.localScale = Vector3.one * scaleAffterSnap;

            StopFloating();
            OnTransiton();
        }
        public void StartFloating()
        {
            if (!isBounceLoop) return;
            isFloating = true;
            floatingAnchor = Tf.position;
            timeOffsetFloating =
                (Mathf.Round(Time.time / durationFloatingIdle) + curveTimeOffSet)
                * durationFloatingIdle - Time.time;
        }
        public void StopFloating()
        {
            isFloating = false;
        }
        public void DelayMoveBack(float delay)
        {
            DOVirtual.DelayedCall(delay, MoveBack);
        }
        public void MoveBack()
        {
            if (!moveBack) return;
            if (_moveBackTween != null)
                _moveBackTween.Kill();
            onMouseUp?.Invoke();
            _isDragging = false;
            IsSnap = false;
            _snapPoint = null;
            Tf.localScale = Vector3.one * 1;
            col.enabled = true;
            col.isTrigger = false;
            if (!ignoreRigidBody && rb)
                rb.bodyType = RigidbodyType2D.Dynamic;
            Tf.localScale = _initScale;
            _moveBackTween = Tf.DOLocalMove(_initLocalPos, 0.3f)
                .OnComplete(() =>
                {
                    sprite.sortingOrder = onDropOrderLayer;
                    if (isSetScaleAffterMoveBack == true)
                    {
                        Tf.localScale = Vector3.zero;
                        onEndMoveBack?.Invoke();
                    }
                    onMovebackDone?.Invoke();
                    if (onMoveBackEnd != null)
                        onMoveBackEnd.Invoke();
                    onDrop.Invoke();
                });
            if (rotateOnDrag)
            {
                if (_rotateTween != null)
                    _rotateTween.Kill();
                _rotateTween = Tf.DORotate(
                    new Vector3(0, 0, _initZRot),
                    0.3f
                );
            }
        }
        [Button]
        public void GetReferences()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponentInChildren<Rigidbody2D>();
            col = GetComponentInChildren<Collider2D>();
        }
    }
}