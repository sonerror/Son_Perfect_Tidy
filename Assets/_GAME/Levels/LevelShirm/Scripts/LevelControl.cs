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
                Debug.Log("Luna Debug: Entered Step 0 logic");
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


    [SerializeField] private SonBoxShootIngredients shootIngredients;
    [SerializeField] private List<SonDragSnap> listSnapObjectRollDone;
    public List<SonDragSnap> ListSnapObjectRollDone => listSnapObjectRollDone;
    [SerializeField] private int countSnapWin = 5;
    [SerializeField] private TutorialManager tutManager;
    private int countSnap = 0;

    private void OnStartStep1()
    {
        tutManager.enableCountTime = true;
        SetSnapObject();
    }
    private void SetSnapObject()
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

        countSnap++;

        if (shootIngredients != null)
            shootIngredients.DecreaseObject();

        if (countSnap >= countSnapWin || listSnapObjectRollDone.Count == 0)
        {
            CheckDoneStep1();
        }
    }
    private void CheckDoneStep1()
    {
        // DoneStep();
        // TryNextStep();
        GameManager.Ins.showEndGame();
    }
}
