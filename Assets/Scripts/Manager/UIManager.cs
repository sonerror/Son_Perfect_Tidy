using System.Collections.Generic;
using UnityEngine;

public enum UIID
{
  GamePlayScreen = 0,
  ChangeLevelScreen = 1,
  GameLoseScreen = 2,
}

public class UIManager : Singleton<UIManager>
{
  private Transform _tf;
  public Transform Tf => _tf != null ? _tf : _tf = transform;

  [Header("UI Configuration")]
  [SerializeField] private List<UIScreen> screens = new List<UIScreen>();

  [SerializeField] private List<RectTransform> canvasParents = new List<RectTransform>();

  private UIScreen[] _uiActive;

  private Vector2 _gameSize = new Vector2(1080f, 1920f);
  public Vector2 GameSize => _gameSize;

  // Cache lại kích thước màn hình để tránh gọi tính toán liên tục
  private int _lastScreenWidth = 0;
  private int _lastScreenHeight = 0;

  protected override void Awake()
  {
    base.Awake();

    _uiActive = new UIScreen[screens.Count];
  }

  private void Start()
  {
    CheckAndCalculateScreenSize();
    OpenUI(UIID.GamePlayScreen);
  }

  private void Update()
  {
    if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
    {
      CheckAndCalculateScreenSize();
    }
  }

  private void CheckAndCalculateScreenSize()
  {
    _lastScreenWidth = Screen.width;
    _lastScreenHeight = Screen.height;

    float ratio = (float)_lastScreenWidth / (float)_lastScreenHeight;
    float heightTmp = 1080f / ratio;
    float widthTmp = 1920f * ratio;

    if (heightTmp <= 1920f)
    {
      _gameSize.x = widthTmp;
      _gameSize.y = 1920f;
    }
    else
    {
      _gameSize.x = 1080f;
      _gameSize.y = heightTmp;
    }

    ResizeAllUI();
  }

  private void ResizeAllUI()
  {
    for (int i = 0; i < _uiActive.Length; i++)
    {
      if (_uiActive[i] != null)
      {
        _uiActive[i].Resize(_gameSize);
      }
    }
  }
  public UIScreen GetUI(UIID id)
  {
    int index = (int)id;
    if (_uiActive[index] == null)
    {
      Transform parent = (canvasParents.Count > index && canvasParents[index] != null) ? canvasParents[index] : Tf;

      _uiActive[index] = Instantiate(screens[index].gameObject, parent).GetComponent<UIScreen>();
      _uiActive[index].Resize(_gameSize);
      _uiActive[index].OnCreate();
    }
    return _uiActive[index];
  }

  public UIScreen OpenUI(UIID id)
  {
    int index = (int)id;
    UIScreen screen = GetUI(id);

    if (!screen.gameObject.activeInHierarchy)
    {
      screen.gameObject.SetActive(true);
      screen.OnShow();
    }
    return screen;
  }

  public void CloseUI(UIID id)
  {
    int index = (int)id;
    if (_uiActive[index] != null && _uiActive[index].gameObject.activeInHierarchy)
    {
      _uiActive[index].gameObject.SetActive(false);
      _uiActive[index].OnHide();
    }
  }

  public bool IsOpenedUI(UIID id)
  {
    int index = (int)id;
    return _uiActive[index] != null && _uiActive[index].gameObject.activeInHierarchy;
  }
}