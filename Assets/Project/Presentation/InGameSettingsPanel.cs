using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Chess.Presentation
{
    public class InGameSettingsPanel : MonoBehaviour
    {
        [Header("메인 패널")]
        public GameObject panel;
        public Button forfeitButton;
        public Button resumeButton;
        public Button soundButton;

        [Header("사운드 서브패널")]
        public GameObject soundPanel;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Button soundBackButton;

        private GameManager gameManager;
        private bool _initializing;
        private bool _isRankedMode;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);

            if (forfeitButton != null) forfeitButton.onClick.AddListener(OnForfeit);
            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundClicked);
            if (soundBackButton != null) soundBackButton.onClick.AddListener(OnSoundBack);

            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        public void Initialize(GameManager gm, bool isRanked = false)
        {
            gameManager   = gm;
            _isRankedMode = isRanked;
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        void OnForfeit()
        {
            if (panel != null) panel.SetActive(false);

            // 온라인 모드: 상대에게 항복 RPC 전송
            gameManager?.NotifyForfeit();
            // DEFEAT 패널 표시 + 전적/랭크 처리는 GameManager.ShowForfeitDefeat()에서
            gameManager?.ShowForfeitDefeat(_isRankedMode);
        }

        void OnSoundClicked()
        {
            if (panel != null) panel.SetActive(false);

            _initializing = true;
            if (soundPanel != null) soundPanel.SetActive(true);
            if (AudioManager.Instance != null)
            {
                if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.bgmVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.sfxVolume;
            }
            _initializing = false;
        }

        void OnSoundBack()
        {
            if (soundPanel != null) soundPanel.SetActive(false);
            if (panel != null) panel.SetActive(true);
        }

        void OnRematch()
        {
            Chess.Core.PhotonManager.Instance?.Disconnect();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnMainMenu()
        {
            Chess.Core.PhotonManager.Instance?.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }

        void OnBGMChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetBGMVolume(value);
        }

        void OnSFXChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetSFXVolume(value);
        }
    }
}