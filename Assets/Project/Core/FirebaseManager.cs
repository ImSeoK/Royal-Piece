using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Google;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;

namespace Chess.Core
{
    /// <summary>Firestore에서 로드/저장하는 플레이어 전체 데이터</summary>
    public class FirestorePlayerData
    {
        public int currency;
        public int gems;
        public List<OwnedUnitEntry> ownedUnits = new List<OwnedUnitEntry>();
        public int rankPoints;
        public string lastDailyBonus    = "";
        public string dailyMissionDate  = "";
        public int dailyMissionWins;
        public bool dailyMissionClaimed;
        public string email             = "";
    }

    public class OwnedUnitEntry
    {
        public string instanceId;
        public string unitName;
        public int    enhanceLevel;
    }

    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        [Header("Google Sign-In")]
        [Tooltip("Firebase Console → Authentication → Google → Web client ID")]
        public string googleWebClientId;

        public bool   IsInitialized { get; private set; }
        public bool   IsLoggedIn    => _auth?.CurrentUser != null || _cloneTestUID != null;
        public bool   IsAnonymous   => _cloneTestUID == null && (_auth?.CurrentUser?.IsAnonymous ?? false);
        public string UID           => _cloneTestUID ?? _auth?.CurrentUser?.UserId;
        public string Email         => (!string.IsNullOrEmpty(_auth?.CurrentUser?.Email))
                                        ? _auth.CurrentUser.Email
                                        : CachedData?.email ?? "";

        /// <summary>현재 로그인 제공자 반환. "google.com" / "apple.com" / "anonymous" / ""</summary>
        public string GetProvider()
        {
            if (_auth?.CurrentUser == null) return "";
            foreach (var info in _auth.CurrentUser.ProviderData)
            {
                if (info.ProviderId == "google.com") return "google.com";
                if (info.ProviderId == "apple.com")  return "apple.com";
            }
            return _auth.CurrentUser.IsAnonymous ? "anonymous" : "";
        }

        string _cloneTestUID;

        /// <summary>로그인 완료 시 (UID 전달)</summary>
        public event Action<string> OnLoginSuccess;
        /// <summary>로그인 실패 시 (에러 메시지 전달)</summary>
        public event Action<string> OnLoginFailed;
        /// <summary>Firebase 초기화 완료 + 기존 세션 없음 → 로그인 버튼 표시 신호</summary>
        public event Action OnFirebaseReady;
        /// <summary>Firestore 데이터 로드 완료 시. null이면 신규 유저.</summary>
        public event Action<FirestorePlayerData> OnDataReady;
        /// <summary>계정 연동 완료 시 (UID 전달)</summary>
        public event Action<string> OnLinkSuccess;
        /// <summary>계정 연동 실패 시 (에러 메시지 전달)</summary>
        public event Action<string> OnLinkFailed;

        /// <summary>가장 최근 로드된 서버 데이터</summary>
        public FirestorePlayerData CachedData { get; private set; }

        FirebaseAuth      _auth;
        FirebaseFirestore _db;
        IAppleAuthManager _appleAuthManager;

        bool _firebaseReady;
        bool _dataLoadAttempted;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (AppleAuthManager.IsCurrentPlatformSupported)
                _appleAuthManager = new AppleAuthManager(new PayloadDeserializer());

#if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                IsInitialized      = true;
                _dataLoadAttempted = true;
                _firebaseReady     = true;
                _cloneTestUID      = "clone-test-uid-player1";
                Debug.Log("[Firebase] ParrelSync 클론 — Firebase 초기화 스킵");
                OnDataReady?.Invoke(null);
                return;
            }
#endif

            _ = InitializeAsync();
        }

        void Update()
        {
            _appleAuthManager?.Update();
        }

        // ─── 초기화 ────────────────────────────────────────────────

        async Task InitializeAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[Firebase] 초기화 실패: {status}");
                OnLoginFailed?.Invoke($"Firebase 초기화 실패: {status}");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _db   = FirebaseFirestore.DefaultInstance;

#if UNITY_EDITOR
            _db.Settings.Host       = "127.0.0.1:8080";
            _db.Settings.SslEnabled = false;
            Debug.Log("[Firebase] Firestore 에뮬레이터 연결 (127.0.0.1:8080)");
#endif

            IsInitialized = true;
            Debug.Log("[Firebase] 초기화 완료");

            // 기존 세션 유지 중 → 자동 로그인
            if (_auth.CurrentUser != null)
            {
                string uid = _auth.CurrentUser.UserId;
                Debug.Log($"[Firebase] 기존 세션 유지: {uid}");
                SaveUID(uid);
                OnLoginSuccess?.Invoke(uid);
                await LoadAndBroadcastAsync(uid);
                return;
            }

            // 세션 없음 → 로그인 화면에 버튼 표시 신호
            _firebaseReady = true;
            OnFirebaseReady?.Invoke();
        }

        // ─── 로그인 방법 ───────────────────────────────────────────

        /// <summary>Google 계정으로 로그인</summary>
        public async Task SignInWithGoogleAsync()
        {
            if (string.IsNullOrEmpty(googleWebClientId))
            {
                OnLoginFailed?.Invoke("googleWebClientId가 설정되지 않았습니다.");
                return;
            }

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId    = googleWebClientId,
                RequestIdToken = true,
                UseGameSignIn  = false,
            };

            try
            {
                GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                await SignInWithFirebaseCredentialAsync(credential);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] Google 로그인 실패: {e.Message}");
                OnLoginFailed?.Invoke(e.Message);
            }
        }

        /// <summary>게스트(익명) 계정에 Google 계정을 연동. 기존 데이터 유지.</summary>
        public async Task LinkWithGoogleAsync()
        {
            if (string.IsNullOrEmpty(googleWebClientId))
            {
                OnLinkFailed?.Invoke("googleWebClientId가 설정되지 않았습니다.");
                return;
            }

            if (_auth?.CurrentUser == null || !_auth.CurrentUser.IsAnonymous)
            {
                OnLinkFailed?.Invoke("게스트 계정이 아닙니다.");
                return;
            }

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId    = googleWebClientId,
                RequestIdToken = true,
                UseGameSignIn  = false,
            };

            try
            {
                GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                var result = await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                string uid = result.User.UserId;
                Debug.Log($"[Firebase] 계정 연동 완료: {uid}");
                SaveUID(uid);
                OnLinkSuccess?.Invoke(uid);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] 계정 연동 실패: {e.Message}");
                OnLinkFailed?.Invoke(e.Message);
            }
        }

        /// <summary>게스트 계정에 Apple ID를 연동 (iOS 전용). 기존 데이터 유지.</summary>
        public void LinkWithAppleAsync()
        {
            if (_appleAuthManager == null)
            {
                OnLinkFailed?.Invoke("이 기기에서는 Apple 로그인을 지원하지 않습니다.");
                return;
            }

            if (_auth?.CurrentUser == null || !_auth.CurrentUser.IsAnonymous)
            {
                OnLinkFailed?.Invoke("게스트 계정이 아닙니다.");
                return;
            }

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            _appleAuthManager.LoginWithAppleId(loginArgs,
                credential =>
                {
                    var appleCredential = credential as IAppleIDCredential;
                    if (appleCredential == null)
                    {
                        OnLinkFailed?.Invoke("Apple 자격증명을 받지 못했습니다.");
                        return;
                    }
                    string identityToken = Encoding.UTF8.GetString(appleCredential.IdentityToken);
                    _ = LinkWithAppleTokenAsync(identityToken);
                },
                error =>
                {
                    Debug.LogError($"[Firebase] Apple 연동 실패: {error}");
                    OnLinkFailed?.Invoke(error.ToString());
                });
        }

        async Task LinkWithAppleTokenAsync(string identityToken)
        {
            try
            {
                Credential credential = OAuthProvider.GetCredential("apple.com", identityToken, null, null);
                var result = await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                string uid   = result.User.UserId;
                string email = result.User.Email ?? "";
                Debug.Log($"[Firebase] Apple 연동 완료: {uid}");
                SaveUID(uid);
                if (!string.IsNullOrEmpty(email) && (CachedData != null) && string.IsNullOrEmpty(CachedData.email))
                    CachedData.email = email;
                OnLinkSuccess?.Invoke(uid);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] Apple 연동 실패: {e.Message}");
                OnLinkFailed?.Invoke(e.Message);
            }
        }

        /// <summary>Apple ID로 로그인 (iOS 전용)</summary>
        public void SignInWithAppleAsync()
        {
            if (_appleAuthManager == null)
            {
                OnLoginFailed?.Invoke("이 기기에서는 Apple 로그인을 지원하지 않습니다.");
                return;
            }

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            _appleAuthManager.LoginWithAppleId(loginArgs,
                credential =>
                {
                    var appleCredential = credential as IAppleIDCredential;
                    if (appleCredential == null)
                    {
                        OnLoginFailed?.Invoke("Apple 자격증명을 받지 못했습니다.");
                        return;
                    }
                    string identityToken = Encoding.UTF8.GetString(appleCredential.IdentityToken);
                    _ = SignInWithAppleTokenAsync(identityToken);
                },
                error =>
                {
                    Debug.LogError($"[Firebase] Apple 로그인 실패: {error}");
                    OnLoginFailed?.Invoke(error.ToString());
                });
        }

        async Task SignInWithAppleTokenAsync(string identityToken)
        {
            try
            {
                Credential credential = OAuthProvider.GetCredential("apple.com", identityToken, null, null);
                await SignInWithFirebaseCredentialAsync(credential);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] Apple Firebase 인증 실패: {e.Message}");
                OnLoginFailed?.Invoke(e.Message);
            }
        }

        /// <summary>게스트(익명)로 로그인</summary>
        public async Task SignInAnonymouslyAsync()
        {
            try
            {
                var result = await _auth.SignInAnonymouslyAsync();
                string uid = result.User.UserId;
                Debug.Log($"[Firebase] 익명 로그인 완료: {uid}");
                SaveUID(uid);
                OnLoginSuccess?.Invoke(uid);
                await LoadAndBroadcastAsync(uid);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] 익명 로그인 실패: {e.Message}");
                OnLoginFailed?.Invoke(e.Message);
            }
        }

        async Task SignInWithFirebaseCredentialAsync(Credential credential)
        {
            var result = await _auth.SignInWithCredentialAsync(credential);
            string uid   = result.UserId;
            string email = result.Email ?? "";
            Debug.Log($"[Firebase] 로그인 완료: {uid}");
            SaveUID(uid);
            OnLoginSuccess?.Invoke(uid);
            await LoadAndBroadcastAsync(uid, email);
        }

        // ─── 데이터 로드 ───────────────────────────────────────────

        async Task LoadAndBroadcastAsync(string uid, string email = "")
        {
            var data = await LoadPlayerDataAsync(uid);
            if (data == null) data = new FirestorePlayerData();

            // 이메일 값이 있으면 캐시에 반영 (Apple 첫 로그인 시 Firestore에 저장)
            if (!string.IsNullOrEmpty(email) && string.IsNullOrEmpty(data.email))
                data.email = email;

            CachedData         = data;
            _dataLoadAttempted = true;
            OnDataReady?.Invoke(data);
        }

        public async Task<FirestorePlayerData> LoadPlayerDataAsync(string uid = null)
        {
            uid = uid ?? UID;
            if (string.IsNullOrEmpty(uid)) return null;

            try
            {
                var snapshot = await _db.Collection("users").Document(uid).GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    Debug.Log("[Firestore] 신규 유저 — 로컬 데이터 사용");
                    return null;
                }

                var data = new FirestorePlayerData();
                var dict = snapshot.ToDictionary();

                if (dict.TryGetValue("currency",           out var cur))  data.currency           = Convert.ToInt32(cur);
                if (dict.TryGetValue("gems",               out var gm))   data.gems               = Convert.ToInt32(gm);
                if (dict.TryGetValue("rankPoints",         out var rp))   data.rankPoints         = Convert.ToInt32(rp);
                if (dict.TryGetValue("lastDailyBonus",     out var ldb))  data.lastDailyBonus     = ldb?.ToString() ?? "";
                if (dict.TryGetValue("dailyMissionDate",   out var dmd))  data.dailyMissionDate   = dmd?.ToString() ?? "";
                if (dict.TryGetValue("dailyMissionWins",   out var dmw))  data.dailyMissionWins   = Convert.ToInt32(dmw);
                if (dict.TryGetValue("dailyMissionClaimed",out var dmc))  data.dailyMissionClaimed = Convert.ToBoolean(dmc);
                if (dict.TryGetValue("email",              out var em))   data.email               = em?.ToString() ?? "";

                if (dict.TryGetValue("ownedUnits", out var rawUnits) && rawUnits is List<object> unitList)
                {
                    foreach (var item in unitList)
                    {
                        if (item is Dictionary<string, object> u)
                        {
                            data.ownedUnits.Add(new OwnedUnitEntry
                            {
                                instanceId   = u.TryGetValue("instanceId",   out var id)  ? id?.ToString()      : Guid.NewGuid().ToString(),
                                unitName     = u.TryGetValue("unitName",     out var nm)  ? nm?.ToString()      : "",
                                enhanceLevel = u.TryGetValue("enhanceLevel", out var el)  ? Convert.ToInt32(el) : 0,
                            });
                        }
                    }
                }

                Debug.Log($"[Firestore] 로드 완료: {data.ownedUnits.Count}개 유닛, {data.currency} 골드, {data.rankPoints}pt");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firestore] 로드 실패: {e.Message}");
                return null;
            }
        }

        // ─── 데이터 저장 ───────────────────────────────────────────

        public void SavePlayerData(FirestorePlayerData data)
        {
            if (!IsLoggedIn || data == null) return;
            _ = SavePlayerDataAsync(data);
        }

        async Task SavePlayerDataAsync(FirestorePlayerData data)
        {
            string uid = UID;
            if (string.IsNullOrEmpty(uid)) return;

            try
            {
                var unitList = new List<Dictionary<string, object>>();
                foreach (var u in data.ownedUnits)
                {
                    unitList.Add(new Dictionary<string, object>
                    {
                        { "instanceId",   u.instanceId },
                        { "unitName",     u.unitName },
                        { "enhanceLevel", u.enhanceLevel },
                    });
                }

                var dict = new Dictionary<string, object>
                {
                    { "ownedUnits",          unitList },
                    { "gems",                data.gems },
                    { "lastDailyBonus",      data.lastDailyBonus ?? "" },
                    { "dailyMissionDate",    data.dailyMissionDate ?? "" },
                    { "dailyMissionWins",    data.dailyMissionWins },
                    { "dailyMissionClaimed", data.dailyMissionClaimed },
                    // rankPoints / currency 는 Cloud Function만 수정
                };

                // 이메일이 있을 때만 저장 (Apple 첫 로그인 시만 값 존재)
                if (!string.IsNullOrEmpty(data.email))
                    dict["email"] = data.email;

                await _db.Collection("users").Document(uid).SetAsync(dict);
                Debug.Log("[Firestore] 저장 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firestore] 저장 실패: {e.Message}");
            }
        }

        public void UpdateFields(Dictionary<string, object> fields)
        {
            if (!IsLoggedIn) return;
            _ = UpdateFieldsAsync(fields);
        }

        async Task UpdateFieldsAsync(Dictionary<string, object> fields)
        {
            string uid = UID;
            if (string.IsNullOrEmpty(uid)) return;
            try
            {
                await _db.Collection("users").Document(uid).UpdateAsync(fields);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firestore] UpdateFields 실패: {e.Message}");
            }
        }

        // ─── 게임 로그 제출 ────────────────────────────────────────

        public async Task SubmitGameLogAsync(string gameId, Dictionary<string, object> logData)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(gameId)) return;

            logData["timestamp"]   = FieldValue.ServerTimestamp;
            logData["submittedBy"] = UID;

            try
            {
                await _db.Collection("gameLogs").Document(gameId).SetAsync(logData, SetOptions.MergeAll);
                Debug.Log($"[Firestore] 게임 로그 제출 완료: {gameId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firestore] 게임 로그 제출 실패: {e.Message}");
            }
        }

        // ─── 구독 헬퍼 ────────────────────────────────────────────

        /// <summary>Firebase 준비 완료(신규 유저) 구독. 이미 준비됐으면 즉시 콜백.</summary>
        public void SubscribeFirebaseReady(Action callback)
        {
            if (_firebaseReady)
                callback();
            else
                OnFirebaseReady += callback;
        }

        /// <summary>Firestore 데이터 로드 완료 구독. 이미 완료됐으면 즉시 콜백.</summary>
        public void SubscribeDataReady(Action<FirestorePlayerData> callback)
        {
            if (CachedData != null || _dataLoadAttempted)
                callback(CachedData);
            else
                OnDataReady += callback;
        }

        // ─── 유틸 ─────────────────────────────────────────────────

        static void SaveUID(string uid)
        {
            PlayerPrefs.SetString("firebase_uid", uid);
            PlayerPrefs.Save();
        }

        public static string GetCachedUID() => PlayerPrefs.GetString("firebase_uid", "");

        /// <summary>현재 세션 로그아웃. 로컬 캐시도 초기화.</summary>
        public void SignOut()
        {
            _auth?.SignOut();
            _cloneTestUID      = null;
            CachedData         = null;
            _dataLoadAttempted = false;
            _firebaseReady     = true; // 이미 초기화된 상태 유지 → 로그인 씬에서 즉시 버튼 표시
            PlayerPrefs.DeleteKey("firebase_uid");
            PlayerPrefs.Save();
            Debug.Log("[Firebase] 로그아웃 완료");
        }
    }
}
