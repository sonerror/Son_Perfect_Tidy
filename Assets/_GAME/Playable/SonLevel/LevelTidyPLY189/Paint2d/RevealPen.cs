using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace sonnv
{
    [RequireComponent(typeof(Collider2D))]
    public class RevealPen : SonMonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        [SerializeField] private AudioSource sfxDrag;

        [Header("Paint")]
        [SerializeField] private Transform penTip;
        [SerializeField] private RevealImage[] revealImages;
        [SerializeField] private float paintInterval = 0.01f;

        [Header("Move Like SonSnapObject")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;
        [SerializeField] private SpriteRenderer sprite;

        [SerializeField] private bool useUpdateToDragLerp;
        [SerializeField, Range(0f, 1f)] private float interpolateSpeed = 0.8f;

        [SerializeField] private bool ignoreRigidBody;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float scaleOnDrag = 1f;

        [SerializeField] private bool moveBack = true;

        [Header("Sorting Order")]
        [SerializeField] private int onDropOrderLayer;
        [SerializeField] private int onDragOrderLayer;

        [Header("Check Step")]
        [SerializeField] private bool isCheckStep = false;
        [SerializeField] private int currentStep = 0;
        [SerializeField] private float wrongStepDelay = 0.1f;
        [SerializeField] private EmojiControl emoji;
        [SerializeField] private LevelControl levelControl;

        [Header("Rotate On Drag")]
        [SerializeField] private bool rotateOnDrag;
        [SerializeField] private float onDragRotateZ = 10f;

        [Header("Events")]
        [SerializeField] protected UnityEvent onStartDrag;
        [SerializeField] protected UnityEvent onDrop;
        [SerializeField] protected UnityEvent onMouseUp;
        [SerializeField] protected UnityEvent onMovebackDone;

        private Camera mainCam;

        private float lastPaintTime;
        private bool isPainting;
        private bool isDragging;
        private bool canInteract = true;
        private bool isWaitingWrongStepFail;
        private bool isMovingBack;

        private int stepCheckToken;

        private Vector3 mousePos;
        private Vector3 initLocalPos;
        private Vector3 initScale;
        private float initZRot;

        private Tween moveBackTween;
        private Tween rotateTween;
        private Tween tweenCheckFail;

        public bool IsDragging => isDragging;

        public UnityEvent OnStartDragEvent => onStartDrag;
        public UnityEvent OnDropEvent => onDrop;
        public UnityEvent OnMouseUpEvent => onMouseUp;
        public UnityEvent OnMovebackDone => onMovebackDone;

        private Vector3 PenTipPosition
        {
            get
            {
                return penTip != null ? penTip.position : Tf.position;
            }
        }

        public void SetCurrentStep(int _current)
        {
            currentStep = _current;
        }

        private void Awake()
        {
            mainCam = Camera.main;

            if (col == null)
            {
                col = GetComponent<Collider2D>();
            }

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (sprite == null)
            {
                sprite = GetComponentInChildren<SpriteRenderer>();
            }

            offset.z += Tf.position.z;
            initLocalPos = Tf.localPosition;

            OnAwake();
        }

        private void Start()
        {
            initScale = Tf.localScale;
            initZRot = Tf.eulerAngles.z;

            if (sprite != null)
            {
                sprite.sortingOrder = onDropOrderLayer;
            }

            if (ignoreRigidBody && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }

            OnStart();
        }

        private void Update()
        {
            if (!useUpdateToDragLerp)
            {
                return;
            }

            if (!isDragging)
            {
                return;
            }

            Tf.position = Vector3.Lerp(Tf.position, mousePos, interpolateSpeed);

            Tf.localRotation = Quaternion.Slerp(
                Tf.localRotation,
                Quaternion.identity,
                interpolateSpeed
            );

            if (!isWaitingWrongStepFail)
            {
                TryPaintByInterval();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!canInteract || isDragging)
            {
                return;
            }

            if (sfxDrag != null)
            {
                sfxDrag.Play();
            }

            isDragging = true;
            isPainting = true;
            isWaitingWrongStepFail = false;
            isMovingBack = false;
            lastPaintTime = -999f;

            stepCheckToken++;

            if (moveBackTween != null)
            {
                moveBackTween.Kill();
                moveBackTween = null;
            }

            if (tweenCheckFail != null)
            {
                tweenCheckFail.Kill();
                tweenCheckFail = null;
            }

            if (sprite != null)
            {
                sprite.sortingOrder = onDragOrderLayer;
            }

            if (!ignoreRigidBody && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }

            UpdateMousePos(eventData);

            if (!useUpdateToDragLerp)
            {
                Tf.position = mousePos;
                Tf.localRotation = Quaternion.identity;
            }

            Tf.localScale = initScale * scaleOnDrag;

            if (rotateOnDrag)
            {
                if (rotateTween != null)
                {
                    rotateTween.Kill();
                    rotateTween = null;
                }

                rotateTween = Tf.DORotate(
                    new Vector3(0, 0, onDragRotateZ),
                    0.3f
                );
            }

            OnStartDrag();
            onStartDrag?.Invoke();

            if (isCheckStep)
            {
                StartWrongStepCheck();
            }

            if (!isWaitingWrongStepFail)
            {
                DoPaint(PenTipPosition);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canInteract || !isDragging)
            {
                return;
            }

            UpdateMousePos(eventData);

            if (!useUpdateToDragLerp)
            {
                Tf.position = mousePos;

                if (!isWaitingWrongStepFail)
                {
                    TryPaintByInterval();
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (sfxDrag != null)
            {
                sfxDrag.Stop();
            }

            if (!isDragging)
            {
                return;
            }

            // Không check step ở đây.
            // Không CancelWrongStepCheck ở đây.
            // Check step đã được đăng ký từ PointerDown và tự xử lý khi tới thời điểm.

            isDragging = false;
            isPainting = false;

            if (!isWaitingWrongStepFail)
            {
                DoPaint(PenTipPosition);
            }

            onMouseUp?.Invoke();
            OnDrop();

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            Tf.localScale = initScale;

            if (rotateOnDrag)
            {
                if (rotateTween != null)
                {
                    rotateTween.Kill();
                    rotateTween = null;
                }

                rotateTween = Tf.DORotate(
                    new Vector3(0, 0, initZRot),
                    0.3f
                );
            }

            if (moveBack)
            {
                MoveBackAfterDrop();
            }
            else
            {
                if (!ignoreRigidBody && rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }

                if (sprite != null)
                {
                    sprite.sortingOrder = onDropOrderLayer;
                }

                onDrop?.Invoke();
            }
        }

        private void StartWrongStepCheck()
        {
            if (levelControl == null)
            {
                Debug.LogWarning("RevealPen: levelControl chưa được gán.", this);
                return;
            }

            int myToken = stepCheckToken;

            isWaitingWrongStepFail = true;
            isPainting = false;

            if (tweenCheckFail != null)
            {
                tweenCheckFail.Kill();
                tweenCheckFail = null;
            }

            tweenCheckFail = DOVirtual.DelayedCall(wrongStepDelay, () =>
            {
                tweenCheckFail = null;

                if (myToken != stepCheckToken)
                {
                    return;
                }

                if (!isWaitingWrongStepFail)
                {
                    return;
                }

                isWaitingWrongStepFail = false;

                if (currentStep != levelControl.CurrentStep)
                {
                    if (emoji != null)
                    {
                        emoji.ShowNegative();
                    }

                    // Nếu đang kéo thì bắt buộc kéo về ngay.
                    if (isDragging)
                    {
                        MoveBack();
                        return;
                    }

                    // Nếu đã thả tay rồi và đang tự quay về thì không gọi MoveBack lặp.
                    if (isMovingBack)
                    {
                        return;
                    }

                    // Trường hợp moveBack = false hoặc object chưa quay về.
                    MoveBack();
                    return;
                }

                // Đúng step thì cho tiếp tục tô/kéo.
                if (isDragging)
                {
                    isPainting = true;
                    DoPaint(PenTipPosition);
                }
                else
                {
                    isPainting = false;
                }
            });
        }

        private void CancelWrongStepCheck()
        {
            isWaitingWrongStepFail = false;
            stepCheckToken++;

            if (tweenCheckFail != null)
            {
                tweenCheckFail.Kill();
                tweenCheckFail = null;
            }
        }

        private void MoveBackAfterDrop()
        {
            if (moveBackTween != null)
            {
                moveBackTween.Kill();
                moveBackTween = null;
            }

            isMovingBack = true;

            if (!ignoreRigidBody && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            moveBackTween = Tf.DOLocalMove(initLocalPos, 0.3f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    moveBackTween = null;
                    isMovingBack = false;

                    if (sprite != null)
                    {
                        sprite.sortingOrder = onDropOrderLayer;
                    }

                    if (!ignoreRigidBody && rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Dynamic;
                    }

                    onMovebackDone?.Invoke();
                    onDrop?.Invoke();
                });
        }

        public void MoveBack()
        {
            CancelWrongStepCheck();

            isDragging = false;
            isPainting = false;
            isWaitingWrongStepFail = false;
            isMovingBack = true;

            if (sfxDrag != null)
            {
                sfxDrag.Stop();
            }

            if (moveBackTween != null)
            {
                moveBackTween.Kill();
                moveBackTween = null;
            }

            onMouseUp?.Invoke();

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            if (!ignoreRigidBody && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }

            Tf.localScale = initScale;

            if (rotateOnDrag)
            {
                if (rotateTween != null)
                {
                    rotateTween.Kill();
                    rotateTween = null;
                }

                rotateTween = Tf.DORotate(
                    new Vector3(0, 0, initZRot),
                    0.3f
                );
            }

            moveBackTween = Tf.DOLocalMove(initLocalPos, 0.3f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    moveBackTween = null;
                    isMovingBack = false;

                    if (sprite != null)
                    {
                        sprite.sortingOrder = onDropOrderLayer;
                    }

                    if (!ignoreRigidBody && rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Dynamic;
                    }

                    onMovebackDone?.Invoke();
                    onDrop?.Invoke();
                });
        }

        public void SetCanInteract(bool value)
        {
            canInteract = value;
        }

        private void UpdateMousePos(PointerEventData eventData)
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

            if (mainCam == null)
            {
                return;
            }

            Vector3 worldPos = mainCam.ScreenToWorldPoint(eventData.position);
            worldPos += offset;
            worldPos.z = offset.z;

            mousePos = worldPos;
        }

        private void TryPaintByInterval()
        {
            if (!isPainting)
            {
                return;
            }

            if (Time.time - lastPaintTime < paintInterval)
            {
                return;
            }

            DoPaint(PenTipPosition);
            lastPaintTime = Time.time;
        }

        private void DoPaint(Vector3 worldPos)
        {
            if (revealImages == null)
            {
                return;
            }

            for (int i = 0; i < revealImages.Length; i++)
            {
                if (revealImages[i] == null)
                {
                    continue;
                }

                if (!revealImages[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                revealImages[i].Paint(worldPos);
            }
        }

        private void OnDisable()
        {
            isDragging = false;
            isPainting = false;
            isWaitingWrongStepFail = false;
            isMovingBack = false;

            CancelWrongStepCheck();

            if (sfxDrag != null)
            {
                sfxDrag.Stop();
            }

            if (moveBackTween != null)
            {
                moveBackTween.Kill();
                moveBackTween = null;
            }

            if (rotateTween != null)
            {
                rotateTween.Kill();
                rotateTween = null;
            }
        }

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnStartDrag() { }
        protected virtual void OnDrop() { }
    }
}