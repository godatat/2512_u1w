using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.5f;

    private static SceneTransition _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (_instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Transition");
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                _instance = instance.GetComponent<SceneTransition>();
                if (_instance == null)
                {
                    _instance = instance.AddComponent<SceneTransition>();
                }
                DontDestroyOnLoad(instance);
            }
            else
            {
                Debug.LogError("SceneTransition: TransitionプレハブがResourcesフォルダに見つかりません。");
            }
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.gameObject.SetActive(true);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void LoadScene(string sceneName)
    {
        if (_instance != null)
        {
            _instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void LoadSceneWithFade(string sceneName)
    {
        // AudioListenerを使って全音声をフェードアウト
        if (AudioListener.volume > 0f)
        {
            DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 0f, _fadeDuration);
        }
        
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.gameObject.SetActive(true);
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.DOFade(1f, _fadeDuration).OnComplete(() => {
                SceneManager.LoadScene(sceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // シーン読み込み時にフェードイン
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 1f;
            _fadeCanvasGroup.DOFade(0f, _fadeDuration).OnComplete(() => {
                _fadeCanvasGroup.gameObject.SetActive(false);
            });
        }
        
        // AudioListenerを使って全音声をフェードイン
        AudioListener.volume = 0f;
        DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 1f, _fadeDuration);
    }
}

