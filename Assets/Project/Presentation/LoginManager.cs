using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using AppleAuth;
using Chess.Core;

namespace Chess.Presentation
{
    public class LoginManager : MonoBehaviour
    {
        [Header("UI")]
        public Button googleButton;
        public Button appleButton;
        public Button guestButton;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI touchToStartText;
        public GameObject divider;

        [Header("전환 효과")]
        public Image fadeOverlay;

        bool _waitingForTouch;
        bool _buttonFlowStarted;

        void Start()
        {
            // Apple 버튼은 iOS에서만 표시
            if (appleButton != null)
                appleButton.gameObject.SetActive(AppleAuthManager.IsCurrentPlatformSupported);

            SetButtonsActive(false);
            SetStatus("연결 중...");

            var fm = FirebaseManager.Instance;
            if (fm == null)
            {
                SetStatus("");
                SetButtonsActive(true);
                return;
            }

            if (fm.IsLoggedIn)
            {
                ShowTouchToStart();
                return;
            }

            // 신규 유저 — Firebase 준비되면 버튼 표시
            fm.OnLoginSuccess += OnLoginSuccess;
            fm.OnLoginFailed  += OnLoginFailed;
            fm.SubscribeFirebaseReady(ShowButtons);
        }

        void Update()
        {
            if (!_waitingForTouch) return;
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                _waitingForTouch = false;
                ProceedToMain();
            }
        }

        void ShowTouchToStart()
        {
            SetStatus("");
            if (googleButton != null) googleButton.gameObject.SetActive(false);
            if (appleButton  != null) appleButton.gameObject.SetActive(false);
            if (guestButton  != null) guestButton.gameObject.SetActive(false);
            if (divider      != null) divider.SetActive(false);
            _waitingForTouch = true;
            if (touchToStartText != null)
            {
                touchToStartText.gameObject.SetActive(true);
                StartCoroutine(BlinkCoroutine());
            }
        }

        IEnumerator BlinkCoroutine()
        {
            var color = touchToStartText.color;
            while (_waitingForTouch)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
                {
                    color.a = Mathf.Lerp(0.8f, 0.1f, t);
                    touchToStartText.color = color;
                    yield return null;
                }
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
                {
                    color.a = Mathf.Lerp(0.1f, 0.8f, t);
                    touchToStartText.color = color;
                    yield return null;
                }
            }
            color.a = 0.8f;
            touchToStartText.color = color;
            touchToStartText.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            var fm = FirebaseManager.Instance;
            if (fm == null) return;
            fm.OnLoginSuccess -= OnLoginSuccess;
            fm.OnLoginFailed  -= OnLoginFailed;
        }

        void ShowButtons()
        {
            _buttonFlowStarted = true;
            SetStatus("");
            SetButtonsActive(true);

            if (googleButton != null) googleButton.onClick.AddListener(OnGoogleClicked);
            if (appleButton  != null) appleButton .onClick.AddListener(OnAppleClicked);
            if (guestButton  != null) guestButton .onClick.AddListener(OnGuestClicked);
        }

        // ─── 버튼 핸들러 ──────────────────────────────────────────

        void OnGoogleClicked()
        {
            StartLogin();
            _ = FirebaseManager.Instance.SignInWithGoogleAsync();
        }

        void OnAppleClicked()
        {
            StartLogin();
            FirebaseManager.Instance.SignInWithAppleAsync();
        }

        void OnGuestClicked()
        {
            StartLogin();
            _ = FirebaseManager.Instance.SignInAnonymouslyAsync();
        }

        void StartLogin()
        {
            SetButtonsActive(false);
            SetStatus("로그인 중...");
        }

        // ─── Firebase 콜백 ────────────────────────────────────────

        void OnLoginSuccess(string uid)
        {
            var fm = FirebaseManager.Instance;
            if (fm != null)
            {
                fm.OnLoginSuccess -= OnLoginSuccess;
                fm.OnLoginFailed  -= OnLoginFailed;
            }

            StartCoroutine(OnLoginSuccessMain());
        }

        IEnumerator OnLoginSuccessMain()
        {
            yield return null; // 메인 스레드 다음 프레임 보장
            if (_buttonFlowStarted)
                ProceedToMain();
            else
                ShowTouchToStart();
        }

        void OnLoginFailed(string error)
        {
            Debug.LogWarning($"[Login] 로그인 실패: {error}");
            SetStatus("로그인 실패. 다시 시도해주세요.");
            SetButtonsActive(true);
        }

        // ─── 유틸 ─────────────────────────────────────────────────

        void ProceedToMain()
        {
            _waitingForTouch = false;
            StartCoroutine(FadeAndLoad());
        }

        IEnumerator FadeAndLoad()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;

                float duration = 0.6f;
                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    c.a = t / duration;
                    fadeOverlay.color = c;
                    yield return null;
                }
                c.a = 1f;
                fadeOverlay.color = c;
            }
            SceneManager.LoadScene("MainMenu");
        }

        void SetButtonsActive(bool active)
        {
            if (googleButton != null) googleButton.interactable = active;
            if (appleButton  != null && AppleAuthManager.IsCurrentPlatformSupported)
                appleButton.interactable = active;
            if (guestButton  != null) guestButton.interactable = active;
        }

        void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }
    }
}
