using System;
using UnityEngine;
using Luna.Unity;
using System.Collections.Generic;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
  public static int maxLayer = 10;
  public void gotoStore()
  {
    Debug.Log("Goto Store");
    LifeCycle.GameEnded();
    Playable.InstallFullGame();
    SoundManager.Ins.Mute();
  }
  private void Update()
  {
    if (isEndGame && Input.GetMouseButtonDown(0))
    {
      gotoStore();
    }
  }
  public bool isEndGame = false;

  public void showEndGame()
  {
    Debug.Log("End Game");
    isEndGame = true;
  }
}