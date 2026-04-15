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
        [SerializeField] private bool isRandomOrder = false;
        [SerializeField] private bool isTapFirst = false;
        public bool IsTapFirst => isTapFirst;
        [SerializeField] private List<IngredientList> allWaves;

        [Header("Grid & Logic")]
        [SerializeField] private Transform targetCornerA;
        [SerializeField] private Transform targetCornerB;
        [SerializeField] private int numberObjectInTable = 0;
        public int NumberObjectInTable => numberObjectInTable;

        public UnityEvent onWin;

        private Queue<SonDragSnap> _shootQueue = new Queue<SonDragSnap>();
        private bool _isLevelCompleted = false;

        protected virtual void Awake()
        {
            // Khởi tạo các giá trị từ SonTapItem cũ
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (col == null) col = GetComponent<Collider2D>();

            _originalLayer = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
            _originalScale = transform.localScale;

            OnInit();
        }

        private void OnInit()
        {
            _isLevelCompleted = false;
            _shootQueue.Clear();

            if (allWaves != null)
            {
                foreach (IngredientList wave in allWaves)
                {
                    if (wave.items == null || wave.items.Count == 0) continue;

                    List<SonDragSnap> tempWaveList = new List<SonDragSnap>(wave.items);
                    if (isRandomOrder) Shuffle(tempWaveList);

                    foreach (SonDragSnap item in tempWaveList)
                    {
                        if (item != null)
                        {
                            item.transform.localScale = Vector3.zero;
                            _shootQueue.Enqueue(item);
                        }
                    }
                }
            }
        }

        #region Pointer Interaction
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (_blockTap || _isLevelCompleted) return;

            _isTap = true;
            transform.localScale = _originalScale * scaleAmount;

            if (spriteRenderer != null) spriteRenderer.sortingOrder = maxLayer;
            if (sfxTap != null) SoundManager.PlaySFXOneShot(sfxTap);

            eventOnPointDown?.Invoke();

            if (canBlock)
            {
                _blockTap = true;
                if (col != null) col.enabled = false;
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            // Reset scale và layer
            transform.localScale = _originalScale;
            if (spriteRenderer != null) spriteRenderer.sortingOrder = _originalLayer;

            if (_isLevelCompleted) return;

            // Logic bắn item
            ShootNextItem();

            if (isTapFirst == false)
            {
                TutorialManager.Ins.SetNewTime(3f);
            }
            isTapFirst = true;
        }
        #endregion

        private void ShootNextItem()
        {
            if (targetCornerA == null || targetCornerB == null) return;

            if (_shootQueue.Count > 0)
            {
                numberObjectInTable++;

                SonDragSnap item = _shootQueue.Dequeue();
                float fixedZ = item.transform.position.z;
                Vector3 targetPos = GetRandom2DPosition(targetCornerA.position, targetCornerB.position, fixedZ);

                MoveAndScaleRoutine(targetPos, item);

                if (_shootQueue.Count == 0)
                {
                    OnWin();
                }
            }
        }

        private Vector3 GetRandom2DPosition(Vector3 posA, Vector3 posB, float fixedZ)
        {
            float x = Random.Range(Mathf.Min(posA.x, posB.x), Mathf.Max(posA.x, posB.x));
            float y = Random.Range(Mathf.Min(posA.y, posB.y), Mathf.Max(posA.y, posB.y));
            return new Vector3(x, y, fixedZ);
        }

        private void MoveAndScaleRoutine(Vector3 targetPos, SonDragSnap item)
        {
            if (item == null) return;
            item.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(item.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            seq.Join(item.transform.DOMove(targetPos, 0.3f));
            seq.OnComplete(() =>
            {
                item.SetFloatingStart(true);
                item.SetOnInit();
            });
        }

        private void OnWin()
        {
            _isLevelCompleted = true;
            if (col != null) col.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
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

        public void IncreaseObject(int amount = 1) => numberObjectInTable += amount;
        public void DecreaseObject(int amount = 1)
        {
            numberObjectInTable -= amount;
            if (numberObjectInTable < 0) numberObjectInTable = 0;
        }
    }
}