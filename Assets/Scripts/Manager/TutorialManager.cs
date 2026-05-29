using System.Collections;
using System.Collections.Generic;
using HoangHH;
using UnityEngine;
using sonnv;

public class TutorialManager : Singleton<TutorialManager>
{
    [SerializeField] private bool blockShowHint = false;

    public bool disableHand = false;
    [SerializeField] float TimeHint = 5f;
    public bool enableCountTime = false;
    public float timeCountHint = 2f;
    [SerializeField] public HandCtrl handCtrl;
    [SerializeField] public HandCtrl handCtrlMakeup;
    [SerializeField] private LevelControl _level;
    [SerializeField] private int countHintStep1 = 0;
    private int CountStepDone = 0;
    [SerializeField] private List<Transform> tutorialNode = new List<Transform>();
    [SerializeField] private List<SonSnapObject> tfItem = new List<SonSnapObject>();
    public List<Transform> TutorialNode => tutorialNode;
    public List<SonSnapObject> TfItem => tfItem;
    int countCollectFail = 0;
    private bool isTap = false;
    private void Update()
    {
        if (blockShowHint) return;
        if (Input.GetMouseButtonDown(0))
        {
            HideHint();
            timeCountHint = countCollectFail >= 3 ? 1.5f : TimeHint;
            enableCountTime = true;
            return;
        }
        if (Input.GetMouseButton(0))
        {
            return;
        }
        if (!enableCountTime) return;
        if (handCtrl.gameObject.activeSelf) return;
        if (handCtrlMakeup.gameObject.activeSelf) return;
        CalculateTimeHint();
    }
    private void CalculateTimeHint()
    {
        timeCountHint -= Time.deltaTime;
        if (timeCountHint <= 0)
        {

            ShowHint();
        }
    }
    //step 1
    [SerializeField] private Transform tfBin;
    [SerializeField] private List<Transform> listTfTrash;
    public List<Transform> ListTfTrash => listTfTrash;
    //step 2
    [SerializeField] private Transform tfPaint;
    [SerializeField] private Transform tfTable;
    [SerializeField] private Transform tfTowel;




    //Step 5
    [SerializeField] private Transform tfBox;
    [SerializeField] private SonBoxShootIngredients shootIngredients;

    void ShowHint()
    {
        if (disableHand) return;
        enableCountTime = false;
        int index = _level.CurrentStep;
        switch (index)
        {
            case 0:
                if (listTfTrash.Count >= 0)
                {
                    handCtrl.gameObject.SetActive(true);
                    handCtrl.ShowHandPosToPos(listTfTrash[0].position, tfBin.position);
                }
                return;
            case 1:
               // handCtrl.gameObject.SetActive(true);
               // handCtrl.ShowHandPosToPos(tfPaint.position, tfTable.position);
                return;
            case 2:
               // handCtrl.gameObject.SetActive(true);
               // handCtrl.ShowHandPosToPos(tfTowel.position, tfTable.position);
                return;
            case 3:
                if (shootIngredients.NumberObjectInTable <= 0)
                {
                    handCtrl.gameObject.SetActive(true);
                    handCtrl.ShowHandPosToPos(tfBox.position, tfBox.position);
                }
                else
                {
                    TutorialSort(0);
                }
                return;
            case 4:
                return;
            default:
                resetTimeHint();
                return;
        }
    }
    private void TutorialSort(int indexStep)
    {
        if (handCtrl == null) return;
        if (_level == null) return;
        if (indexStep >= _level.ListSnapObjectRollDone.Count) return;
        handCtrl.gameObject.SetActive(true);
        handCtrl.ShowHandPosToPos(_level.ListSnapObjectRollDone[indexStep].transform.position, _level.ListSnapObjectRollDone[indexStep].SnapToPosition.transform.position);
    }
    public void SetStateIsTap(bool value)
    {
        isTap = value;
    }
    void HideHint()
    {
        handCtrl.gameObject.SetActive(false);
        handCtrlMakeup.gameObject.SetActive(false);
    }
    public void resetTimeHint()
    {
        HideHint();
        timeCountHint = countCollectFail >= 3 ? 1.5f : TimeHint;
        enableCountTime = true;
    }
    public void OnStepDone()
    {
        CountStepDone++;
    }
    public void IncreaseTimeHide()
    {
        TimeHint = 5f;
        resetTimeHint();
    }
    public void OnCollectFail()
    {
        countCollectFail++;

        if (countCollectFail >= 3)
        {
            timeCountHint = 1.5f;
        }
    }
    public void OnCollectSuccess()
    {
        countCollectFail = 0;
        resetTimeHint();
    }
    public void SetNewTime(float timer)
    {
        TimeHint = timer;
        timeCountHint = timer;
    }

}