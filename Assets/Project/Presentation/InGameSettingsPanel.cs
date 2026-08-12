using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

        [Header("항복 결과 패널")]
        public GameObject forfeitResultPanel;
        public TextMeshProUGUI forfeitResultText;
        public Button rematchButton;
        public Button mainMenuButton;

        private GameManager gameManager;
        private bool _initializing;
        private bool _isRankedMode;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);
            if (forfeitResultPanel != null) forfeitResultPanel.SetActive(false);

            if (forfeitButton != null) forfeitButton.onClick.AddListener(OnForfeit);
            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundClicked);
            if (soundBackButton != null) soundBackButton.onClick.AddListener(OnSoundBack);
            if (rematchButton != null) rematchButton.onClick.AddListener(OnRematch);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);

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
            if (forfeitResultPanel != null) forfeitResultPanel.SetActive(false);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        void OnForfeit()
        {
            if (panel != null) panel.SetActive(false);
            if (forfeitResultPanel != null) forfeitResultPanel.SetActive(true);

            if (forfeitResultText != null)
                forfeitResultText.text = "항복하여 패배했습니다.";

            if (gameManager != null)
                gameManager.enabled = false;

            // 항복 = 패배 전적 저장
            PlayerPrefs.SetInt("stat_losses", PlayerPrefs.GetInt("stat_losses", 0) + 1);
            PlayerPrefs.SetInt("stat_total",  PlayerPrefs.GetInt("stat_total",  0) + 1);
            PlayerPrefs.Save();

            // 골드 보상 및 랭크 포인트 (패배)
            Chess.Core.PlayerInventory.Instance?.AddCurrency(15);
            if (_isRankedMode) Chess.Core.RankSystem.AddLoss();
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnMainMenu()
        {
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