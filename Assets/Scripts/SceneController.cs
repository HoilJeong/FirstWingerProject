using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNameConstants
{
    public static string TitleScene = "TitleScene";
    public static string LoadingScene = "LoadingScene";
    public static string FirstWinger = "FirstWinger";
}


public class SceneController : MonoBehaviour
{
    private static SceneController instance = null;

    public static SceneController Instance
    {
        get
        {
            if (instance == null)
            {
                // 최초 사용시 클래스명과 같은 이름의 게임오브젝트를 만들어서 어태치
                GameObject go = GameObject.Find("SceneController");
                if (go == null)
                {
                    go = new GameObject("SceneController");

                    SceneController controller = go.AddComponent<SceneController>();
                    return controller;
                }
                else
                {
                    instance = go.GetComponent<SceneController>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Can't have two instance of singletone.");
            DestroyImmediate(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this); // 파괴되지 않게 한다
    }

    private void Start()
    {
        // Scene 변화에 따른 이벤트 메소드를 매핑
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Update()
    {
        
    }

    public void LoadSceneImmediate(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 이전 Scene을 Unload 하고 로딩
    /// </summary>
    /// <param name="sceneName"> 로딩할 Scene 이름 </param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Single));
    }

    /// <summary>
    /// 이전 Scene의 Unload 없이 로딩
    /// </summary>
    /// <param name="sceneName"> 로딩할 Scene 이름 </param>
    public void LoadSceneAdditive(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Additive));
    }

    IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

        while (!asyncOperation.isDone)
            yield return null;

        Debug.Log("LoadSceneAsync is complete");
    }

    public void OnActiveSceneChanged(Scene scene0, Scene scene1)
    {
        Debug.Log("OnActiveSceneChanged is called! scene0 = " + scene0.name + ", scene1 = " + scene1.name);
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log("OnSceneLoaded is  called! scene = " + scene.name + ", loadSceneMode = " + loadSceneMode.ToString());

        BaseSceneMain baseSceneMain = GameObject.FindObjectOfType<BaseSceneMain>();
        Debug.Log("OnSceneLoaded! baseSceneMain.name = " + baseSceneMain.name);
        SystemManager.Instance.CurrentSceneMain = baseSceneMain;
    }

    public void OnSceneUnloaded(Scene scene)
    {
        Debug.Log("OnSceneUnloaded is called! scene = " + scene.name);
    }
}
