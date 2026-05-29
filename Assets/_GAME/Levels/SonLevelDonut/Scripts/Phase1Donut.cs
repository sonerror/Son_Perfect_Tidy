using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;


using UnityEngine;
namespace sonnv
{
    public class Phase1Donut : Phase
    {
        private void PunkScale(Transform tf)
        {
            tf.DOPunchScale(Vector3.up * 0.05f, 0.15f);
        }
        #region Phase_1
        [SerializeField] private IngredientHolder mixFlourBowl;
        [SerializeField] private Rigidbody2D rbFlourBowl;
        [SerializeField] public IngredientForwarder milkCup;
        [SerializeField] private Transform cupPosToPour;
        [SerializeField] private SpriteRenderer milkInBowl;

        private void OnStartStep0()
        {
            //SetHint(0);
            milkCup.SetIngredient(IngredientType.Milk, false);
            mixFlourBowl.SetContact(true);
            mixFlourBowl.OnIngredientDone = OnAddMilkDone;
        }
        private void OnAddMilkDone(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Milk)
            {
                mixFlourBowl.OnIngredientDone = null;
                milkCup.SetCanMoveBack(false);
                milkCup.Col.enabled = false;
                milkInBowl.DOFade(1f, 1f).SetDelay(0.4f)
                    .OnStart(() =>
                    {
                        milkInBowl.color = new Color(1f, 1f, 1f, 0f);
                    });
                milkCup.Tf.DOMove(cupPosToPour.position, 0.5f);
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    milkCup.Col.enabled = true;
                    milkCup.SetCanMoveBack(true, true);
                    OnEndStep();
                });
            }
        }
        //add sugar, salt, yeast
        [SerializeField] public IngredientForwarder spoon;
        [SerializeField] public IngredientHolder yeastBowl;
        [SerializeField] public IngredientHolder sugarBowl;
        [SerializeField] private Transform eggPosToPour;


        [SerializeField] public Transform spoonPosToPour;
        [SerializeField] private SpriteRenderer sugarInBowl;
        [SerializeField] private bool isAddSugar;
        [SerializeField] private Transform sugarPosToPour;

        [SerializeField] private SpriteRenderer yeastInBowl;
        [SerializeField] private bool isAddYeast;
        [SerializeField] private Transform yeastPosToPour;
        [SerializeField] private Animator eggAnim;
        [SerializeField] private List<IngredientForwarder> eggList;
        [SerializeField] private SpriteRenderer eggInBowl;
        [SerializeField] private SpriteRenderer eggInBowl2;
        [SerializeField] private int countEgg;

        private void OnStartStep1()
        {
            countEgg = 0;
            eggList[0].SetIngredient(IngredientType.Egg1, false);
            eggList[1].SetIngredient(IngredientType.Egg2, false);
            mixFlourBowl.OnIngredientDone = OnAddEggDone;
        }
        private void OnAddEggDone(SpriteHolderData spriteHolderData)
        {
            mixFlourBowl.Collider.enabled = false;
            int index = -1;
            switch (spriteHolderData.ingredientType)
            {
                case IngredientType.Egg1: index = 0; break;
                case IngredientType.Egg2: index = 1; break;
                default: return;
            }
            var egg = eggList[index];
            egg.SetCanMoveBack(false);
            countEgg++;
            egg.Tf.DOMove(eggPosToPour.position, 0.5f).OnComplete(() =>
            {
                egg.gameObject.SetActive(false);

                eggAnim.SetTrigger("Play");
            });
            if (countEgg == 1)
            {
                DOVirtual.DelayedCall(2.1f, () =>
                {
                    eggInBowl.SetAlpha(1);
                    PunkScale(eggInBowl.gameObject.transform);
                    mixFlourBowl.Collider.enabled = true;
                });
            }
            if (countEgg == 2)
            {
                DOVirtual.DelayedCall(2.1f, () =>
                {
                    eggInBowl2.SetAlpha(1);
                    PunkScale(eggInBowl2.gameObject.transform);
                    eggAnim.gameObject.SetActive(false);
                    mixFlourBowl.Collider.enabled = true;
                    mixFlourBowl.OnIngredientDone = null;
                    foreach (var egg111 in eggList)
                    {
                        egg111.Col.enabled = false;
                    }
                    OnEndStep();
                });
            }

        }
        //add salt, yeast
        private void OnStartStep2()
        {
            yeastBowl.SetContact(true);
            sugarBowl.SetContact(true);
            mixFlourBowl.OnIngredientDone = OnAddMixIngredient;
        }
        private void OnAddMixIngredient(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Yeast)
            {
                yeastInBowl.DOFade(1f, 0.25f).SetDelay(0.1f)
                   .OnStart(() =>
                   {
                       yeastInBowl.color = new Color(1f, 1f, 1f, 0f);
                       isAddYeast = true;
                       CheckDoneStep2();
                   });
            }
            else if (ingredientType is IngredientType.Sugar)
            {
                sugarInBowl.DOFade(1f, 0.25f).SetDelay(0.1f)
                  .OnStart(() =>
                  {
                      sugarInBowl.color = new Color(1f, 1f, 1f, 0f);
                      isAddSugar = true;
                      CheckDoneStep2();
                  });
            }
        }
        private void CheckDoneStep2()
        {

            if (isAddSugar && isAddYeast)
            {
                mixFlourBowl.OnIngredientDone = null;
                OnEndStep();
            }
        }
        // add beater and rotate 
        [SerializeField] private GameObject beaterInBowl;
        [SerializeField] public IngredientForwarder beater;
        [SerializeField] private Transform beaterPosToPour;
        public bool isRotateBeater;
        private void OnStartStep3()
        {
            beater.SetIngredient(IngredientType.Beater, false);
            mixFlourBowl.OnIngredientDone = OnAddEggBeater;
        }

        private void OnAddEggBeater(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Beater)
            {
                beater.SetCanMoveBack(false);
                beater.Col.enabled = false;
                Sequence seq = DOTween.Sequence();
                seq.Join(beater.Tf.DOMove(beaterPosToPour.position, 0.25f));
                seq.Join(beater.Tf.DORotate(Vector3.zero, 0.25f, RotateMode.Fast));
                DOVirtual.DelayedCall(.26f, () =>
                {
                    mixFlourBowl.Collider.enabled = false;
                    beater.gameObject.SetActive(false);
                    beaterInBowl.gameObject.SetActive(true);
                    Destroy(rbFlourBowl.GetComponent<Rigidbody2D>());
                    isRotateBeater = true;
                });
            }
        }

        public void CheckDoneStep3()
        {
            yeastInBowl.gameObject.SetActive(false);
            sugarInBowl.gameObject.SetActive(false);
            eggInBowl.gameObject.SetActive(false);
            eggInBowl2.gameObject.SetActive(false);
            milkInBowl.gameObject.SetActive(false);
            beaterInBowl.gameObject.SetActive(false);
            beater.Tf.DORotate(new Vector3(0, 0, 70), 0.2f, RotateMode.Fast);
            beater.Col.enabled = true;
            beater.gameObject.SetActive(true);
            beater.SetCanMoveBack(true);
            mixFlourBowl.OnIngredientDone = null;
            Rigidbody2D newRb = mixFlourBowl.gameObject.AddComponent<Rigidbody2D>();
            newRb.simulated = true;
            newRb.bodyType = RigidbodyType2D.Kinematic;
            newRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rbFlourBowl = newRb;
            mixFlourBowl.Collider.enabled = true;
            isRotateBeater = false;
            OnEndStep(true);

            // GameManager.Ins.ChangeState();
        }
        //Step 4 them bot
        [SerializeField] public IngredientForwarder flourBowl;
        [SerializeField] private Transform PosToFlourBowl;
        [SerializeField] private SpriteRenderer flourInBowl;
        [SerializeField] private Animation animFlour;
        private void OnStartStep4()
        {
            //SetHint(1);
            flourBowl.SetIngredient(IngredientType.Flour, false);
            mixFlourBowl.OnIngredientDone = OnAddFlourBotl;
        }
        private void OnAddFlourBotl(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Flour)
            {
                mixFlourBowl.OnIngredientDone = null;
                flourBowl.SetCanMoveBack(false);
                flourBowl.Col.enabled = false;
                flourInBowl.transform.rotation = Quaternion.identity;
                float animTime = animFlour["bowlPourYeast"].clip.length;
                flourBowl.Tf.DOMove(PosToFlourBowl.position, 0.25f).OnComplete(() =>
                {
                    animFlour.Play();
                    //GameManager.Ins.PlaySoundDrop();
                    flourInBowl.DOFade(1f, 1f).SetDelay(0.4f)
                        .OnStart(() =>
                        {
                            flourInBowl.color = new Color(1f, 1f, 1f, 0f);
                            flourInBowl.transform.DOScale(new Vector3(0.6f, 0.6f, 0.6f), 0.1f);
                        });
                });
                DOVirtual.DelayedCall(animTime + 0.4f, () =>
                {
                    flourBowl.Col.enabled = true;
                    flourBowl.SetCanMoveBack(true, true);
                    OnEndStep5();
                });
            }
        }

        //step 5 add salt
        [SerializeField] public IngredientHolder saltBowl;
        [SerializeField] private SpriteRenderer saltInBowl;

        private void OnStartStep5()
        {
            saltBowl.SetContact(true);
            mixFlourBowl.OnIngredientDone = OnAddSalt;
        }
        private void OnAddSalt(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Salt)
            {
                saltInBowl.DOFade(1f, 0.25f).SetDelay(0.1f)
                   .OnStart(() =>
                   {
                       saltInBowl.color = new Color(1f, 1f, 1f, 0f);
                       CheckDoneStep5();
                   });
            }
        }
        private void CheckDoneStep5()
        {
            mixFlourBowl.OnIngredientDone = null;
            OnEndStep5();
        }
        private void OnEndStep5()
        {
            OnEndStep();


        }
        // add and rotate spatulaInBowl
        [SerializeField] public GameObject spatulaInBowl;
        [SerializeField] private GameObject mixedMilkInBowl;
        [SerializeField] public IngredientForwarder spatula;
        [SerializeField] private Transform spatulaPosToPour;
        public bool isRotateSpatula;
        private void OnStartStep6()
        {
            Debug.Log("End Game Phase 1");
            GameManager.Ins.ShowEndGame();
            spatula.SetIngredient(IngredientType.Spatula, false);
            mixFlourBowl.OnIngredientDone = OnAddEggSpatula;
        }

        private void OnAddEggSpatula(SpriteHolderData spriteHolderData)
        {
            var ingredientType = spriteHolderData.ingredientType;
            if (ingredientType is IngredientType.Spatula)
            {
                spatula.SetCanMoveBack(false);
                spatula.Col.enabled = false;
                Sequence seq = DOTween.Sequence();
                seq.Join(spatula.Tf.DOMove(spatulaPosToPour.position + new Vector3(0, 1.3f, 0), 0.25f));
                seq.Join(spatula.Tf.DORotate(Vector3.zero, 0.25f, RotateMode.Fast));
                DOVirtual.DelayedCall(.26f, () =>
                {
                    mixFlourBowl.Collider.enabled = false;
                    spatula.gameObject.SetActive(false);
                    spatulaInBowl.gameObject.SetActive(true);
                    Destroy(rbFlourBowl.GetComponent<Rigidbody2D>());
                    isRotateSpatula = true;
                });
            }
        }

        public void CheckDoneStep6()
        {
            saltInBowl.gameObject.SetActive(false);
            flourInBowl.gameObject.SetActive(false);
            mixedMilkInBowl.gameObject.SetActive(false);
            spatulaInBowl.gameObject.SetActive(false);
            spatula.Tf.DORotate(new Vector3(0, 0, -90), 0.2f, RotateMode.Fast);
            spatula.Col.enabled = true;
            spatula.gameObject.SetActive(true);
            spatula.SetCanMoveBack(true);
            mixFlourBowl.OnIngredientDone = null;
            Rigidbody2D newRb = mixFlourBowl.gameObject.AddComponent<Rigidbody2D>();
            newRb.simulated = true;
            newRb.bodyType = RigidbodyType2D.Kinematic;
            newRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rbFlourBowl = newRb;
            mixFlourBowl.Collider.enabled = true;
            isRotateSpatula = false;
            OnEndStep6();
        }
        private void OnEndStep6()
        {
            OnEndStep();
            // Level628.Ins.TransitionPhaseNew(PhaseType.Phase1, PhaseType.Phase2);
        }
        public void OnCallStep0()
        {
            OnStartStep0();
        }
        public void OnCallStep1()
        {
            OnStartStep1();
        }
        public void OnCallStep2()
        {
            OnStartStep2();
        }
        public void OnCallStep3()
        {
            OnStartStep3();
        }
        public void OnCallStep4()
        {
            OnStartStep4();
        }
        public void OnCallStep5()
        {
            OnStartStep5();
        }
        public void OnCallStep6()
        {
            OnStartStep6();
        }
        #endregion
    }
}
