using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class ScreenShot : MonoBehaviour
{
    #region --- 變數設定 ---

    //[Header("執行後，按住 Space 開始拍圖")]

    [Header("圖片設定")]
    [Tooltip("輸出解析度為基礎解析度*採樣數")]
    [Range(1, 8)] [SerializeField] private int _sample = 1;
    [Tooltip("背景是否為透明")]
    [SerializeField] private bool _isBackgroundTransparent = true;

    [Header("輸出設定")]
    [Tooltip("自訂後綴名")]
    [SerializeField] private string _suffix = "MonsterIcon";
    [Tooltip("是否要自訂輸出路徑")]
    [SerializeField] private bool _isUsingPath = true;
    [Tooltip("設定自訂輸出路徑 (若目標路徑不存在，則會自動創建目標路徑) ")]
    [SerializeField] private string _path = "D:/ScreenShots/";
    [Tooltip("算圖結束後是否要打開輸出資料夾")]
    [SerializeField] private bool _openFolderAfterSave = true;
    //[SerializeField] private bool _openPictureAfterSave;

    [Header("模型設定")]
    [Tooltip("模型是否要停止動作")]
    [SerializeField] private bool _autoDisableAnimator = true;
    [Tooltip("模型是否要T-Pose")]
    [SerializeField] public bool _isTpose = false;
    [Tooltip("是否要隱藏武器")]
    [SerializeField] public bool _isHideWeapon = true;



    private int _index;

    private int _resWidth = Screen.width;
    private int _resHeight = Screen.height;
    private bool _takeShot = false;
    private Camera _camera;
    private string _filename;
    private string _exportPath;

    [Header("自訂選擇模型")]
    public List<Transform> transforms;
    [HideInInspector]
    public List<DisableAnimator> disableAnimators;

    #endregion


    [ContextMenu(nameof(GetTransformsInChildren))]
    public void GetTransformsInChildren()
    {
        transforms.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (transforms.Contains(child))
                continue;
            transforms.Add(child);
        }
    }

    [ContextMenu(nameof(OpenFolder))]
    public void OpenFolder()
    {
        CheckFolderExists();
        Application.OpenURL(_path);
    }


    private void Awake()
    {
        Debug.Log(gameObject.name + ".ScreenShot.Awake();");
        if (transforms.Count == 0)
            GetTransformsInChildren();
        else
            Debug.Log("已有手動設定transforms，故不自動設定。");

        if (!_autoDisableAnimator)
            return;

        foreach (var i in transforms)
        {
            DisableAnimator disableAnimator = i.gameObject.AddComponent(typeof(DisableAnimator)) as DisableAnimator;
            disableAnimator.isTpose = _isTpose;
            disableAnimator.isHideWeapon = _isHideWeapon;
            disableAnimators.Add(disableAnimator);
        }
    }

    private void OnEnable()
    {
        Debug.Log(gameObject.name + ".ScreenShot.OnEnable();");
        _camera = Camera.main;

        if (_isBackgroundTransparent)
            _camera.clearFlags = CameraClearFlags.Depth;

        if (_resWidth <= 0 || _resHeight <= 0)
        {
            _resWidth = Screen.width;
            _resHeight = Screen.height;
        }
        _resWidth *= _sample;
        _resHeight *= _sample;

        CheckFolderExists();
    }

    private void Start() => StartCoroutine("Initialization");
    IEnumerator Initialization()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (var item in transforms)
            item.gameObject.SetActive(false);
        transforms[0].gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        _takeShot |= Input.GetKey(KeyCode.Space);
        if (_takeShot && _index < transforms.Count)
        {
            RenderPicture();
            _takeShot = false;
            EnableNextObject();
        }
    }

    private void RenderPicture()
    {
        RenderTexture renderTexture = new RenderTexture(_resWidth, _resHeight, 24);
        _camera.targetTexture = renderTexture;
        Texture2D screenShot = new Texture2D(_resWidth, _resHeight, TextureFormat.ARGB32, false);
        _camera.Render();
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(new Rect(0, 0, _resWidth, _resHeight), 0, 0);
        _camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        byte[] bytes = screenShot.EncodeToPNG();
        SetPath();
        string filename = _exportPath;
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log("檔案輸出：" + filename);
    }

    private void SetPath()
    {
        _filename = transforms[_index].gameObject.name;
        if(_suffix.Length>0)
            _filename = _filename.Replace("Monster_Prefab", _suffix);
        else
            _filename = _filename.Replace("Monster_Prefab", "MonsterIcon");
        if (!_isUsingPath)
            _exportPath = Application.dataPath + "/" + _filename + ".png";
        else
            _exportPath = _path + _filename + ".png";
    }

    private void EnableNextObject()
    {
        if (transforms.Count <= _index + 1)
        {
            Debug.Log("執行結束。");
            transforms[_index].gameObject.SetActive(false);
            _index = 0;
            transforms[_index].gameObject.SetActive(true);
            OpenFolder(_path);
            return;
        }
        transforms[_index].gameObject.SetActive(false);
        transforms[++_index].gameObject.SetActive(true);
    }

    private void OpenFolder(string _path)
    {
        if (_openFolderAfterSave)
        {
            _openFolderAfterSave = false;
            Application.OpenURL(_path);
        }
    }

    private void CheckFolderExists()
    {
        if (!Directory.Exists(_path))
        {
            Debug.Log("因指定路徑未創建資料夾，因此系統自動產生：" + _path);
            Directory.CreateDirectory(_path);
        }
    }
}