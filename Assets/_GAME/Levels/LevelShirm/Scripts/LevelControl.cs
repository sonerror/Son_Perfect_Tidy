using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using sonnv;
using UnityEngine.Events;

public class LevelControl : LevelBase
{
    [SerializeField] private Camera cam;
    [SerializeField] private TutorialManager tutManager;

    protected override void Awake()
    {
        base.Awake();
        DOTween.Init();
        maxLayer = 30;
    }
    protected virtual void Start()
    {
        Invoke(nameof(StartStep), 0.1f);
    }
    [SerializeField] private int currentStep = 0;
    public int CurrentStep => currentStep;
    private bool isDoneStep;
    public bool IsDoneStep => isDoneStep;
    private bool istap = false;
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && istap == false)
        {
            istap = true;
        }
    }

    private Tween _delayTween;
    private void StartDelayStep(float delay)
    {
        _delayTween?.Kill();
        _delayTween = DOVirtual.DelayedCall(delay, () =>
        {
            CookManager.Ins.OnStartStep();
        });
    }
    IEnumerator IE_DelayStart()
    {
        yield return new WaitForSeconds(0.75f);
    }
    public void SetStateDoneStep(bool value)
    {
        isDoneStep = value;
    }
    public EmojiControl emoji;
    public static int maxLayer = 30;
    private void SetNewEmoji(EmojiControl _emoji)
    {
        emoji = _emoji;
    }
    public void ShowNegative()
    {
        emoji.ShowNegative();
    }
    protected virtual void DoneStep()
    {
        emoji.ShowPositive();
        isDoneStep = true;
        EventManager.TriggerEvent(EventType.IncreaseProgress.ToString());
    }
    private void MoveCamera(
               Camera _cam,
               float targetOrthoSize,
               float targetLocalY,
               float duration,
               System.Action onComplete = null)
    {
        if (_cam == null) return;
        Sequence seq = DOTween.Sequence();
        if (targetOrthoSize < 3.55f)
        {
            targetOrthoSize = 3.55f;
        }
        seq.Append(_cam.DOOrthoSize(targetOrthoSize, duration));
        seq.Join(_cam.transform.DOLocalMoveY(targetLocalY, duration));
        if (onComplete != null)
        {
            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
    protected virtual void TryNextStep()
    {
        currentStep++;
        StartStep();
    }
    private void StartStep()
    {
        Debug.Log("Luna Debug: StartStep called. CurrentStep = " + currentStep);
        isDoneStep = false;

        switch (currentStep)
        {
            case 0:
                tutManager.SetNewTime(0.5f);
                OnStartStep1();
                break;
            case 1:
                OnStartStep2();
                break;
            case 2:
                OnStartStep3();
                break;
            case 3:
                OnStartStep4();
                break;
        }
    }
    public void OnCompleteStage(int currentStageIndex, float x)
    {
        emoji.ShowPositive();
        if (cam != null)
            cam.transform.DOMoveX(x, 1f).SetDelay(1f);
    }
    public void IncreaseMaxLayer(int increment = 4)
    {
        maxLayer += increment;
    }
    public void ShowPositiveEmojiAtPos(Vector3 position)
    {
        emoji.transform.position = position + Vector3.up * 0.5f + Vector3.left * 0.5f;
        emoji.ShowPositive();
    }
    [SerializeField] private List<SonSnapPoint> listSnapPointBin;
    [SerializeField] private List<SonSnapObject> listSnapObjectBin;


    [SerializeField] private int countSnapWin = 5;
    private int countSnap = 0;

    private void OnStartStep1()
    {
        countSnapWin = listSnapObjectBin.Count;
        tutManager.enableCountTime = true;
        SetSnapObject();
    }
    private void SetSnapObject()
    {
        foreach (SonSnapPoint point in listSnapPointBin)
        {
            point.ChangeCanSnap(true);
        }
        foreach (SonSnapObject obj in listSnapObjectBin)
        {
            SonSnapObject cache = obj;

            cache.OnSnap.AddListener(() => OnSnapHandler(cache));
        }
    }
    private void OnSnapHandler(SonSnapObject obj)
    {
        if (obj == null) return;
        Debug.Log("Snap");

        obj.OnSnap.RemoveAllListeners();

        if (listSnapObjectBin.Contains(obj))
        {
            int removedIndex = listSnapObjectBin.IndexOf(obj);
            if (removedIndex < 0) return;
            tutManager.ListTfTrash.RemoveAt(removedIndex);
            listSnapObjectBin.Remove(obj);
        }
        countSnap++;
        if (countSnap == 1)
        {
            tutManager.SetNewTime(5f);
        }
        if (countSnap >= countSnapWin || listSnapObjectBin.Count == 0)
        {
            DoneStep();
            TryNextStep();
            tutManager.enableCountTime = false;
        }
    }

    //step2
    [SerializeField] private ShowObjectEffect showStep1;
    [SerializeField] private ShowObjectEffect showStep2;

    [SerializeField] private RevealImage imgStep1;
    [SerializeField] private float targetOrthoSize = 12.75f;
    [SerializeField] private float targetLocalY = -3f;


    private void OnStartStep2()
    {
        StartCoroutine(IE_DelayStep2());
    }
    IEnumerator IE_DelayStep2()
    {
        yield return new WaitForSeconds(0.5f);
        MoveCamera(cam, cam.orthographicSize + targetOrthoSize, cam.transform.position.y, 0.75f, () =>
        {
            showStep1.Hide();
            showStep2.Show(0.75f);
            showStep2.onShowComplete.AddListener(() =>
            {
                imgStep1.SetCanPaint(true);
                imgStep1.OnComplete.AddListener(() =>
                {
                    DoneStep();
                    TryNextStep();
                });
            });
        });

    }
    //step 3
    [SerializeField] private RevealImage imgStep2;
    [SerializeField] private RevealPen dragTowl;
    private void OnStartStep3()
    {
        dragTowl.SetCurrentStep(currentStep);
        imgStep2.SetCanPaint(true);
        imgStep2.OnComplete.AddListener(() =>
        {
            DoneStep();
            TryNextStep();
            tutManager.enableCountTime = false;
        });
    }


    [SerializeField] private ShowObjectEffect showStep1Stage2;
    [SerializeField] private float targetLocalYStep2 = -3f;
    private void OnStartStep4()
    {
        StartCoroutine(IE_DelayStep4());
    }
    IEnumerator IE_DelayStep4()
    {
        yield return new WaitForSeconds(0.5f);
        showStep2.Hide();
        MoveCamera(cam, cam.orthographicSize, cam.transform.position.y - targetLocalYStep2, 0.75f, () =>
        {
            showStep1Stage2.Show(0.75f);
            showStep1Stage2.onShowComplete.AddListener(() =>
            {
                shootIngredients.onDoneShootAuto.AddListener(() =>
                {
                    SetSnapObjectPhase5();
                    CheckDoneStep1();
                });
                shootIngredients.OnAutoShoot();

            });
        });

    }


    [SerializeField] private SonBoxShootIngredients shootIngredients;
    [SerializeField] private List<SonDragSnap> listSnapObjectRollDone;
    public List<SonDragSnap> ListSnapObjectRollDone => listSnapObjectRollDone;
    [SerializeField] private int countSnapWinPhase2 = 5;
    private int countSnapPhase2 = 0;
    private void SetSnapObjectPhase5()
    {
        foreach (SonDragSnap obj in listSnapObjectRollDone)
        {
            SonDragSnap cache = obj;

            cache.OnSnap.AddListener(() => OnSnapHandler(cache));
        }
    }
    [SerializeField] private VFXStart vfxPrefab;
    private void OnSnapHandler(SonDragSnap obj)
    {
        if (obj == null) return;
        Debug.Log("Snap");
        if (vfxPrefab != null)
        {
            VFXStart vfx = Instantiate(vfxPrefab, obj.SnapToPosition.transform.position, Quaternion.identity);
            vfx.gameObject.SetActive(true);
        }
        obj.OnSnap.RemoveAllListeners();
        if (listSnapObjectRollDone.Contains(obj))
        {
            listSnapObjectRollDone.Remove(obj);
        }
        countSnapPhase2++;
        if(countSnapPhase2 == 1)
        {
            tutManager.SetNewTime(5f);
        }
        if (shootIngredients != null)
            shootIngredients.DecreaseObject();
       
    }

    private void CheckDoneStep1()
    {
        GameManager.Ins.ShowEndGame();
    }
}