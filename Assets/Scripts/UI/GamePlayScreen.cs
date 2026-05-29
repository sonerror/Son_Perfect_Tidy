using UnityEngine;

public class GamePlayScreen : UIScreen
{
  [Header("Orientation Layouts")]
  [SerializeField] private GameObject panelLandscape1;
  [SerializeField] private GameObject panelLandscape2;

  [SerializeField] private GameObject panelPortrait1;
  [SerializeField] private GameObject panelPortrait2;

  [Header("Main")]
  public GameObject btnPlayLandscape;
  public GameObject btnPlayPortrait;
  private bool _isTap = false;

  public void GotoStore()
  {
    GameManager.Ins.gotoStore();
  }

  private void Update()
  {
    if (Input.GetMouseButtonDown(0) && !_isTap)
    {
      if (btnPlayLandscape != null) btnPlayLandscape.SetActive(true);
      if (btnPlayPortrait != null) btnPlayPortrait.SetActive(true);
      //SoundManager.Ins.PlayBgm();
      _isTap = true;
    }
  }

  public override void Resize(Vector2 gameSize)
  {
    base.Resize(gameSize);

    bool isLandscape = Screen.width > Screen.height;

    if (panelLandscape1 != null) panelLandscape1.SetActive(isLandscape);
    if (panelLandscape2 != null) panelLandscape2.SetActive(isLandscape);


    if (panelPortrait1 != null) panelPortrait1.SetActive(!isLandscape);
    if (panelPortrait2 != null) panelPortrait2.SetActive(!isLandscape);
    Debug.Log("shooooowwwwwwwwwwwwww UI");
  }
}