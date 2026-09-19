using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chess.Presentation
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // BGM
        [Header("BGM")]
        [SerializeField] private float crossfadeDuration = 1f;

        public AudioClip lobbyBGM;
        public AudioClip gameBGM;

        // Database
        [Header("Database")]
        [SerializeField] private AudioDatabase database;

        // SFX Pool
        [Header("SFX Pool")]
        [SerializeField] private int sfxPoolSize = 8;
        private AudioSource[] _sfxPool;
        private int _sfxPoolIndex;

        // UI Channel
        [Header("UI")]
        [SerializeField] private AudioSource uiSource;

        // Volume
        [Header("Volume")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume    = 0.7f;
        [Range(0f, 1f)] public float sfxVolume    = 1f;
        [Range(0f, 1f)] public float uiVolume     = 1f;

        private const string KEY_MASTER = "volume_master";
        private const string KEY_BGM    = "volume_bgm";
        private const string KEY_SFX    = "volume_sfx";
        private const string KEY_UI     = "volume_ui";

        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource _activeBGM;
        private Coroutine   _crossfadeRoutine;


        // Init

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();
            InitBGMSources();
            InitSFXPool();
            database?.Initialize();

            _activeBGM = bgmSourceA;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void InitBGMSources()
        {
            var goA = new GameObject("BGM_A");
            goA.transform.SetParent(transform);
            bgmSourceA             = goA.AddComponent<AudioSource>();
            bgmSourceA.playOnAwake = false;
            bgmSourceA.loop        = true;

            var goB = new GameObject("BGM_B");
            goB.transform.SetParent(transform);
            bgmSourceB             = goB.AddComponent<AudioSource>();
            bgmSourceB.playOnAwake = false;
            bgmSourceB.loop        = true;
        }

        private void InitSFXPool()
        {
            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go  = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool[i] = src;
            }
        }

        // Public API

        /// <summary>Play sound by id registered in AudioDatabase</summary>
        public void Play(string id)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Audio] ▶ Play({id})");
#endif
            if (database == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[AudioManager] Database is not assigned.");
#endif
                return;
            }

            var data = database.Get(id);
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AudioManager] Unknown id: {id}  ← AudioDatabase에 등록 필요");
#endif
                return;
            }

            switch (data.type)
            {
                case AudioType.SFX:
                    if (data.pitchVariance > 0f)
                        PlaySFXWithPitch(data.clip, data.pitchVariance, data.volumeScale);
                    else
                        PlaySFX(data.clip, data.volumeScale);
                    break;
                case AudioType.UI:
                    PlayUI(data.clip);
                    break;
                case AudioType.BGM:
                    PlayBGM(data.clip, data.loop);
                    break;
            }
        }

        /// <summary>Play SFX clip directly</summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            var src    = GetFreeSFXSource();
            src.pitch  = 1f;
            src.volume = sfxVolume * masterVolume * volumeScale;
            src.clip   = clip;
            src.Play();
        }

        /// <summary>Play SFX with random pitch variance</summary>
        public void PlaySFXWithPitch(AudioClip clip, float variance = 0.1f, float volumeScale = 1f)
        {
            if (clip == null) return;
            var src    = GetFreeSFXSource();
            src.pitch  = 1f + Random.Range(-variance, variance);
            src.volume = sfxVolume * masterVolume * volumeScale;
            src.clip   = clip;
            src.Play();
        }

        /// <summary>Play UI sound (separate channel, unaffected by ducking)</summary>
        public void PlayUI(AudioClip clip)
        {
            if (clip == null || uiSource == null) return;
            uiSource.volume = uiVolume * masterVolume;
            uiSource.PlayOneShot(clip);
        }

        /// <summary>Switch BGM with crossfade</summary>
        public void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            if (_activeBGM.clip == clip && _activeBGM.isPlaying) return;

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeBGM(clip, loop));
        }

        // BGM Crossfade

        private IEnumerator CrossfadeBGM(AudioClip newClip, bool loop)
        {
            AudioSource incoming = (_activeBGM == bgmSourceA) ? bgmSourceB : bgmSourceA;
            AudioSource outgoing = _activeBGM;

            incoming.clip   = newClip;
            incoming.loop   = loop;

            // 현재 재생 중인 BGM이 없으면 크로스페이드 없이 바로 재생
            if (!outgoing.isPlaying)
            {
                incoming.volume = bgmVolume * masterVolume;
                incoming.Play();
                _activeBGM = incoming;
                yield break;
            }

            float outgoingStartVol = outgoing.volume;

            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed        += Time.deltaTime;
                float t         = Mathf.Clamp01(elapsed / crossfadeDuration);
                // bgmVolume을 매 프레임 동적으로 읽어 SetBGMVolume 호출이 즉시 반영되도록 함
                incoming.volume = Mathf.Lerp(0f,               bgmVolume * masterVolume, t);
                outgoing.volume = Mathf.Lerp(outgoingStartVol, 0f,                       t);
                yield return null;
            }

            incoming.volume = bgmVolume * masterVolume;
            outgoing.Stop();
            outgoing.clip = null;
            _activeBGM    = incoming;
        }

        // SFX Pool

        private AudioSource GetFreeSFXSource()
        {
            foreach (var src in _sfxPool)
                if (!src.isPlaying) return src;

            var fallback  = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % sfxPoolSize;
            return fallback;
        }

        // Volume

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
            ApplyAllVolumes();
        }

        public void SetBGMVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
            PlayerPrefs.Save();

            // 크로스페이드 중이면 중단하고 incoming 소스를 새 activeBGM으로 확정
            if (_crossfadeRoutine != null)
            {
                StopCoroutine(_crossfadeRoutine);
                _crossfadeRoutine = null;

                AudioSource incomingSource = (_activeBGM == bgmSourceA) ? bgmSourceB : bgmSourceA;
                if (incomingSource != null && incomingSource.isPlaying)
                {
                    _activeBGM.Stop();
                    _activeBGM.clip = null;
                    _activeBGM = incomingSource;
                }
            }

            float vol = bgmVolume * masterVolume;
            if (bgmSourceA != null) bgmSourceA.volume = vol;
            if (bgmSourceB != null) bgmSourceB.volume = vol;
        }

        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            uiVolume  = sfxVolume;
            PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
            if (uiSource != null) uiSource.volume = uiVolume * masterVolume;
        }

        public void SetUIVolume(float value)
        {
            uiVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_UI, uiVolume);
            if (uiSource != null)
                uiSource.volume = uiVolume * masterVolume;
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, masterVolume);
            bgmVolume    = PlayerPrefs.GetFloat(KEY_BGM,    bgmVolume);
            sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX,    sfxVolume);
            uiVolume     = PlayerPrefs.GetFloat(KEY_UI,     uiVolume);
        }

        private void ApplyAllVolumes()
        {
            float vol = bgmVolume * masterVolume;
            if (bgmSourceA != null) bgmSourceA.volume = vol;
            if (bgmSourceB != null) bgmSourceB.volume = vol;
            if (uiSource != null)   uiSource.volume   = uiVolume * masterVolume;
        }

        // Scene

        /// <summary>Stop BGM and play a one-shot sting (victory / defeat)</summary>
        public void PlaySting(string id)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Audio] ★ PlaySting({id})");
#endif
            if (database == null) return;
            var data = database.Get(id);
            if (data == null || data.clip == null) return;

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            bgmSourceA.Stop();
            bgmSourceB.Stop();

            _activeBGM        = bgmSourceA;
            _activeBGM.clip   = data.clip;
            _activeBGM.volume = bgmVolume * masterVolume;
            _activeBGM.loop   = false;
            _activeBGM.Play();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            PlayBGMForScene(scene.name);

        void PlayBGMForScene(string sceneName)
        {
            string id = sceneName switch
            {
                "SampleScene" => "bgm_battle",
                "Gacha"       => "bgm_gacha",
                "DeckBuilder" => "bgm_deckbuilder",
                _             => "bgm_lobby"
            };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Audio] ♪ BGM 전환 — 씬:{sceneName} → id:{id}");
#endif

            if (database != null && database.Get(id) != null)
            {
                Play(id);
                return;
            }

            // Fallback: legacy AudioClip references
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[Audio] BGM '{id}' 미등록 — Fallback 클립으로 재생");
#endif
            AudioClip target = sceneName == "SampleScene" ? gameBGM : lobbyBGM;
            PlayBGM(target);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}