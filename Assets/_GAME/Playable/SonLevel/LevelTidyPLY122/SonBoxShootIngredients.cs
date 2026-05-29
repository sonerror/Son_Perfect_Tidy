using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace sonnv
{
    [System.Serializable]
    public class IngredientList
    {
        public List<SonDragSnap> items;
    }

    public class SonBoxShootIngredients : SonMonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("--- Tap Settings (From SonTapItem) ---")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Collider2D col;
        [SerializeField] protected float scaleAmount = 1.1f;
        [SerializeField] protected int maxLayer = 10;
        [SerializeField] protected AudioClip sfxTap;
        [SerializeField] private bool canBlock = false;

        public UnityEvent eventOnPointDown;

        private Vector3 _originalScale;
        private int _originalLayer;
        private bool _blockTap = false;
        private bool _isTap = false;
        public bool IsTap => _isTap;

        [Header("--- Shoot Settings ---")]
        [SerializeField] private bool isAutoShoot = false;
        [SerializeField] private bool isRandomOrder = false;
        [SerializeField] private bool isTapFirst = false;
        public bool IsTapFirst => isTapFirst;

        [SerializeField] private List<IngredientList> allWaves;

        [Header("--- Shoot Timing ---")]
        [SerializeField] private float shootMoveDuration = 0.3f;
        [SerializeField] private float winDelayAfterLastShoot = 0.1f;

        [Header("Grid & Logic")]
        [SerializeField] private Transform targetCornerA;
        [SerializeField] private Transform targetCornerB;
        [SerializeField] private int numberObjectInTable = 0;
        public int NumberObjectInTable => numberObjectInTable;

        public UnityEvent onWin;
        public UnityEvent onDoneShootAuto;

        private readonly Queue<SonDragSnap> _shootQueue = new Queue<SonDragSnap>();
        private readonly List<SonDragSnap> _itemShooted = new List<SonDragSnap>();
        private readonly List<SonDragSnap> _itemCompleted = new List<SonDragSnap>();

        private bool _isLevelCompleted = false;
        private bool _isAutoShooting = false;

        private Coroutine _winDelayCoroutine;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (col == null)
                col = GetComponent<Collider2D>();

            _originalLayer = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
            _originalScale = transform.localScale;

            OnInit();
        }

        private void OnDisable()
        {
            if (_winDelayCoroutine != null)
            {
                StopCoroutine(_winDelayCoroutine);
                _winDelayCoroutine = null;
            }

            _isAutoShooting = false;
        }

        private void OnInit()
        {
            _isLevelCompleted = false;
            _isAutoShooting = false;
            _isTap = false;
            _blockTap = false;
            isTapFirst = false;
            numberObjectInTable = 0;

            _shootQueue.Clear();
            _itemShooted.Clear();
            _itemCompleted.Clear();

            transform.localScale = _originalScale;

            if (col != null)
                col.enabled = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = _originalLayer;
            }

            if (allWaves == null)
                return;

            foreach (IngredientList wave in allWaves)
            {
                if (wave == null || wave.items == null || wave.items.Count == 0)
                    continue;

                List<SonDragSnap> tempWaveList = new List<SonDragSnap>(wave.items);

                if (isRandomOrder)
                    Shuffle(tempWaveList);

                foreach (SonDragSnap item in tempWaveList)
                {
                    if (item == null)
                        continue;

                    item.transform.DOKill();
                    item.transform.localScale = Vector3.zero;

                    _shootQueue.Enqueue(item);
                }
            }
        }

        #region Pointer Interaction

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (_blockTap || _isLevelCompleted || _isAutoShooting)
                return;

            _isTap = true;
            transform.localScale = _originalScale * scaleAmount;

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = maxLayer;

            if (sfxTap != null)
                SoundManager.PlaySFXOneShot(sfxTap);

            eventOnPointDown?.Invoke();

            if (canBlock)
            {
                _blockTap = true;

                if (col != null)
                    col.enabled = false;
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (isAutoShoot)
                return;

            if (_isLevelCompleted || _isAutoShooting)
                return;

            transform.localScale = _originalScale;

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = _originalLayer;

            bool didShoot = ShootNextItem();

            if (!didShoot)
                return;

            if (isTapFirst == false)
            {
                TutorialManager.Ins.SetNewTime(5f);
            }

            isTapFirst = true;
        }

        #endregion

        public void OnAutoShoot()
        {
            if (!isAutoShoot)
                return;

            if (_isLevelCompleted)
                return;

            if (_isAutoShooting)
                return;

            if (targetCornerA == null || targetCornerB == null)
                return;

            _isAutoShooting = true;

            eventOnPointDown?.Invoke();

            transform.localScale = _originalScale;

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = _originalLayer;

            int shootCount = _shootQueue.Count;

            for (int i = 0; i < shootCount; i++)
            {
                if (_shootQueue.Count <= 0)
                    break;

                ShootNextItem();
            }

            if (_shootQueue.Count == 0 && !_isLevelCompleted)
            {
                if (_winDelayCoroutine != null)
                    StopCoroutine(_winDelayCoroutine);

                _winDelayCoroutine = StartCoroutine(IE_AutoWinAfterAllMoveDone());
            }
            else
            {
                _isAutoShooting = false;
            }
        }

        private IEnumerator IE_AutoWinAfterAllMoveDone()
        {
            yield return new WaitForSeconds(shootMoveDuration + winDelayAfterLastShoot);

            ForceCompleteAllShootedItems();

            OnWin();

            if (_isLevelCompleted)
            {
                TutorialManager.Ins.enableCountTime = true;
                TutorialManager.Ins.SetNewTime(0.5f);

                onDoneShootAuto?.Invoke();
            }

            _isAutoShooting = false;
            _winDelayCoroutine = null;
        }

        private bool ShootNextItem()
        {
            if (targetCornerA == null || targetCornerB == null)
                return false;

            while (_shootQueue.Count > 0)
            {
                SonDragSnap item = _shootQueue.Dequeue();

                if (item == null)
                    continue;

                numberObjectInTable++;
                _itemShooted.Add(item);

                float fixedZ = item.transform.position.z;
                Vector3 targetPos = GetRandom2DPosition(targetCornerA.position, targetCornerB.position, fixedZ);

                MoveAndScaleRoutine(targetPos, item);

                if (_shootQueue.Count == 0 && !isAutoShoot && !_isAutoShooting)
                {
                    RequestWinAfterDelay();
                }

                return true;
            }

            return false;
        }

        private void RequestWinAfterDelay()
        {
            if (_isLevelCompleted)
                return;

            if (_winDelayCoroutine != null)
                return;

            _winDelayCoroutine = StartCoroutine(IE_WinAfterDelay());
        }

        private IEnumerator IE_WinAfterDelay()
        {
            yield return new WaitForSeconds(shootMoveDuration + winDelayAfterLastShoot);

            ForceCompleteAllShootedItems();
            OnWin();

            _winDelayCoroutine = null;
        }

        private void MoveAndScaleRoutine(Vector3 targetPos, SonDragSnap item)
        {
            if (item == null)
                return;

            item.transform.DOKill();
            item.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            seq.Append(item.transform.DOScale(Vector3.one, shootMoveDuration).SetEase(Ease.OutBack));
            seq.Join(item.transform.DOMove(targetPos, shootMoveDuration));

            seq.OnComplete(() =>
            {
                CompleteShootedItem(item);
            });

            StartCoroutine(IE_FallbackCompleteShootedItem(item));
        }

        private IEnumerator IE_FallbackCompleteShootedItem(SonDragSnap item)
        {
            yield return new WaitForSeconds(shootMoveDuration + 0.05f);

            CompleteShootedItem(item);
        }

        private void CompleteShootedItem(SonDragSnap item)
        {
            if (item == null)
                return;

            if (_itemCompleted.Contains(item))
                return;

            _itemCompleted.Add(item);

            item.SetFloatingStart(true);
            item.SetOnInit();
        }

        private void ForceCompleteAllShootedItems()
        {
            foreach (SonDragSnap item in _itemShooted)
            {
                CompleteShootedItem(item);
            }
        }

        public void ReSetBlockObj()
        {
            foreach (SonDragSnap item in _itemShooted)
            {
                if (item != null)
                {
                    item.SetStateBlockObj();
                }
            }
        }

        private Vector3 GetRandom2DPosition(Vector3 posA, Vector3 posB, float fixedZ)
        {
            float x = Random.Range(Mathf.Min(posA.x, posB.x), Mathf.Max(posA.x, posB.x));
            float y = Random.Range(Mathf.Min(posA.y, posB.y), Mathf.Max(posA.y, posB.y));

            return new Vector3(x, y, fixedZ);
        }

        private void OnWin()
        {
            if (_isLevelCompleted)
                return;

            _isLevelCompleted = true;

            if (col != null)
                col.enabled = false;

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            ReSetBlockObj();

            onWin?.Invoke();

            Debug.Log("On Win");
        }

        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;

            while (n > 1)
            {
                n--;

                int k = Random.Range(0, n + 1);

                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public void IncreaseObject(int amount = 1)
        {
            numberObjectInTable += amount;
        }

        public void DecreaseObject(int amount = 1)
        {
            numberObjectInTable -= amount;

            if (numberObjectInTable < 0)
                numberObjectInTable = 0;
        }
    }
}