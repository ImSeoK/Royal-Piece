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

        public void Initialize(GameManager gm)
        {
            gameManager = gm;
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
        }

        void OnSoundClicked()
        {
            if (panel != null) panel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(true);

            if (SettingsManager.Instance != null)
            {
                if (bgmSlider != null) bgmSlider.value = SettingsManager.Instance.BGMVolume;
                if (sfxSlider != null) sfxSlider.value = SettingsManager.Instance.SFXVolume;
            }
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
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetBGM(value);
        }

        void OnSFXChanged(float value)
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetSFX(value);
        }
    }
}