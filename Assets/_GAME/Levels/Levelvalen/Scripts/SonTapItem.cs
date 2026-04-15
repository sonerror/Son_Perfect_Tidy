using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;

namespace sonnv
{
    public class SonTapItem : SonMonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        //[SerializeField] private bool isShowEffect = false;
        //[SerializeField] private List<ShowObjectEffect> listEffect;
        [SerializeField] private Sprite spriteNew;
        [SerializeField] protected int maxLayer = 10;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected float scaleAmount = 1.1f;
        [SerializeField] protected Collider2D col;
        public Collider2D ColD => col;
        [SerializeField] protected AudioClip sfxTap;
        [SerializeField] private bool canBlock = false;
        [SerializeField] private bool isTap = false;
        public bool IsTap => isTap;
        public UnityEvent eventOnPointDown;
        protected Vector3 _originalScale;
        protected int _originnalLayer;
        private bool blockTap = false;

        protected virtual void Awake()
        {
            // if (col == null)
            //     col = GetComponent<Collider2D>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            _originnalLayer = spriteRenderer.sortingOrder;
            _originalScale = Tf.localScale;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (blockTap) return;
            isTap = true;
            Tf.localScale = _originalScale * scaleAmount;
            spriteRenderer.sortingOrder = maxLayer;
            if (sfxTap != null)
                SoundManager.PlaySFXOneShot(sfxTap);
            //ShowEffect();
            eventOnPointDown?.Invoke();
            if (canBlock)
            {
                blockTap = true;
                if (col != null)
                {
                    col.enabled = false;

                }
            }

        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            Tf.localScale = _originalScale;
            spriteRenderer.sortingOrder = _originnalLayer;
        }

    }
}