using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Chess.Presentation
{
    public class MainMenuSettingsPanel : MonoBehaviour
    {
        [Header("패널")]
        public GameObject panel;
        public Button backgroundOverlay;
        public Button closeButton;

        [Header("사운드")]
        public Slider bgmSlider;
        public TextMeshProUGUI bgmValueText;
        public Slider sfxSlider;
        public TextMeshProUGUI sfxValueText;

        [Header("계정 - AccountPanel")]
        public GameObject guestState;
        public TextMeshProUGUI guestDescText;
        public Button linkButton;
        public GameObject linkedState;
        public Image providerIcon;
        public Sprite googleIconSprite;
        public Sprite appleIconSprite;
        public TextMeshProUGUI linkedStatusText;
        public TextMeshProUGUI emailText;

        [Header("계정 연동 팝업")]
        public GameObject linkProviderPopup;
        public Button linkGoogleButton;
        public Button linkAppleButton;
        public Button linkCancelButton;

        [Header("하단")]
        public Button logoutButton;
        public TextMeshProUGUI versionText;

        bool _initializing;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);

            if (backgroundOverlay != null) backgroundOverlay.onClick.AddListener(Close);
            if (closeButton       != null) closeButton.onClick.AddListener(Close);
            if (logoutButton      != null) logoutButton.onClick.AddListener(OnLogoutClicked);
            if (linkButton        != null) linkButton.onClick.AddListener(OnLinkButtonClicked);
            if (linkGoogleButton  != null) linkGoogleButton.onClick.AddListener(OnLinkGoogleClicked);
            if (linkAppleButton != null) linkAppleButton.onClick.AddListener(OnLinkAppleClicked);
            if (linkCancelButton  != null) linkCancelButton.onClick.AddListener(CloseLinkPopup);
            if (linkProviderPopup != null) linkProviderPopup.SetActive(false);

            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);

            if (versionText != null) versionText.text = $"Royal Piece  v{Application.version}";
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);

            _initializing = true;
            if (AudioManager.Instance != null)
            {
                if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.bgmVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.sfxVolume;
            }
            _initializing = false;

            RefreshSliderTexts();
            RefreshAccountState();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        // ─── 사운드 ────────────────────────────────────────────────

        void OnBGMChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetBGMVolume(value);
            if (bgmValueText != null) bgmValueText.text = Mathf.RoundToInt(value * 100).ToString();
        }

        void OnSFXChanged(float value)
        {
            if (_initializing) return;
            AudioManager.Instance?.SetSFXVolume(value);
            if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(value * 100).ToString();
        }

        void RefreshSliderTexts()
        {
            if (bgmValueText != null && bgmSlider != null)
                bgmValueText.text = Mathf.RoundToInt(bgmSlider.value * 100).ToString();
            if (sfxValueText != null && sfxSlider != null)
                sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100).ToString();
        }

        // ─── 계정 상태 ─────────────────────────────────────────────

        void RefreshAccountState()
        {
            var fm = Chess.Core.FirebaseManager.Instance;
            bool isAnonymous = fm?.IsAnonymous ?? true;

            if (guestState  != null) guestState.SetActive(isAnonymous);
            if (linkedState != null) linkedState.SetActive(!isAnonymous);

            if (!isAnonymous && fm != null)
            {
                string provider = fm.GetProvider();
                bool isGoogle   = provider == "google.com";

                if (providerIcon     != null) providerIcon.sprite     = isGoogle ? googleIconSprite : appleIconSprite;
                if (linkedStatusText != null) linkedStatusText.text   = isGoogle ? "Google 연결됨" : "Apple 연결됨";
                if (emailText        != null) emailText.text          = fm.Email;
            }
        }

        // ─── 계정 연동 ─────────────────────────────────────────────

        void OnLinkButtonClicked()
        {
            if (linkProviderPopup != null) linkProviderPopup.SetActive(true);
        }

        void CloseLinkPopup()
        {
            if (linkProviderPopup != null) linkProviderPopup.SetActive(false);
        }

        void OnLinkGoogleClicked()
        {
            CloseLinkPopup();
            if (guestDescText != null) guestDescText.text = "연동 중...";

            var fm = Chess.Core.FirebaseManager.Instance;
            if (fm == null) return;
            fm.OnLinkSuccess += OnLinkSuccess;
            fm.OnLinkFailed  += OnLinkFailed;
            _ = fm.LinkWithGoogleAsync();
        }

        void OnLinkAppleClicked()
        {
            CloseLinkPopup();
            if (guestDescText != null) guestDescText.text = "연동 중...";

            var fm = Chess.Core.FirebaseManager.Instance;
            if (fm == null) return;
            fm.OnLinkSuccess += OnLinkSuccess;
            fm.OnLinkFailed  += OnLinkFailed;
            fm.LinkWithAppleAsync();
        }

        void OnLinkSuccess(string uid)
        {
            UnsubscribeLink();
            RefreshAccountState();
        }

        void OnLinkFailed(string error)
        {
            UnsubscribeLink();
            if (guestDescText != null) guestDescText.text = "연동 실패. 다시 시도해주세요.";
            Debug.LogWarning($"[Settings] 계정 연동 실패: {error}");
        }

        void UnsubscribeLink()
        {
            var fm = Chess.Core.FirebaseManager.Instance;
            if (fm == null) return;
            fm.OnLinkSuccess -= OnLinkSuccess;
            fm.OnLinkFailed  -= OnLinkFailed;
        }

        // ─── 로그아웃 ──────────────────────────────────────────────

        void OnLogoutClicked()
        {
            Chess.Core.FirebaseManager.Instance?.SignOut();
            SceneManager.LoadScene("Login");
        }
    }
}