using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chess.Presentation
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        public AudioSource bgmSource;
        public AudioClip lobbyBGM;   // 메인메뉴, 인벤토리, 덱빌더 공용
        public AudioClip gameBGM;    // 인게임

        [Header("설정")]
        [Range(0f, 1f)] public float bgmVolume = 0.7f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource != null)
            {
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            PlayBGMForCurrentScene(SceneManager.GetActiveScene().name);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGMForCurrentScene(scene.name);
        }

        void PlayBGMForCurrentScene(string sceneName)
        {
            AudioClip target = sceneName == "SampleScene" ? gameBGM : lobbyBGM;
            // lobbyBGM: MainMenu, Gacha, Inventory, DeckBuilder
            // gameBGM: SampleScene
            PlayBGM(target);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void SetVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null) bgmSource.volume = bgmVolume;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}