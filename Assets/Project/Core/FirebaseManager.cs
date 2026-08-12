using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;

namespace Chess.Core
{
    /// <summary>Firestore에서 로드/저장하는 플레이어 전체 데이터</summary>
    public class FirestorePlayerData
    {
        public int currency;
        public List<OwnedUnitEntry> ownedUnits = new List<OwnedUnitEntry>();
        public int rankPoints;
        public string lastDailyBonus    = "";
        public string dailyMissionDate  = "";
        public int dailyMissionWins;
        public bool dailyMissionClaimed;
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

        public bool   IsInitialized { get; private set; }
        public bool   IsLoggedIn    => _auth?.CurrentUser != null;
        public string UID           => _auth?.CurrentUser?.UserId;

        /// <summary>로그인 완료 시 (UID 전달)</summary>
        public event Action<string> OnLoginSuccess;
        /// <summary>로그인 실패 시 (에러 메시지 전달)</summary>
        public event Action<string> OnLoginFailed;
        /// <summary>Firestore 데이터 로드 완료 시. null이면 신규 유저.</summary>
        public event Action<FirestorePlayerData> OnDataReady;

        /// <summary>가장 최근 로드된 서버 데이터 (null = 아직 로드 전 또는 신규 유저)</summary>
        public FirestorePlayerData CachedData { get; private set; }

        FirebaseAuth      _auth;
        FirebaseFirestore  _db;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _ = InitializeAsync();
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
            IsInitialized = true;
            Debug.Log("[Firebase] 초기화 완료");

            if (_auth.CurrentUser != null)
            {
                string uid = _auth.CurrentUser.UserId;
                Debug.Log($"[Firebase] 기존 세션 유지: {uid}");
                SaveUID(uid);
                OnLoginSuccess?.Invoke(uid);
                await LoadAndBroadcastAsync(uid);
                return;
            }

            await SignInAnonymouslyAsync();
        }

        async Task SignInAnonymouslyAsync()
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
                Debug.LogError($"[Firebase] 로그인 실패: {e.Message}");
                OnLoginFailed?.Invoke(e.Message);
            }
        }

        // ─── 데이터 로드 ───────────────────────────────────────────

        async Task LoadAndBroadcastAsync(string uid)
        {
            var data       = await LoadPlayerDataAsync(uid);
            CachedData     = data;
            _dataLoadAttempted = true;
            OnDataReady?.Invoke(data);
        }

        /// <summary>
        /// Firestore에서 플레이어 데이터 로드.
        /// 신규 유저(문서 없음)이면 null 반환.
        /// </summary>
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
                if (dict.TryGetValue("rankPoints",         out var rp))   data.rankPoints         = Convert.ToInt32(rp);
                if (dict.TryGetValue("lastDailyBonus",     out var ldb))  data.lastDailyBonus     = ldb?.ToString() ?? "";
                if (dict.TryGetValue("dailyMissionDate",   out var dmd))  data.dailyMissionDate   = dmd?.ToString() ?? "";
                if (dict.TryGetValue("dailyMissionWins",   out var dmw))  data.dailyMissionWins   = Convert.ToInt32(dmw);
                if (dict.TryGetValue("dailyMissionClaimed",out var dmc))  data.dailyMissionClaimed = Convert.ToBoolean(dmc);

                if (dict.TryGetValue("ownedUnits", out var rawUnits) && rawUnits is List<object> unitList)
                {
                    foreach (var item in unitList)
                    {
                        if (item is Dictionary<string, object> u)
                        {
                            data.ownedUnits.Add(new OwnedUnitEntry
                            {
                                instanceId   = u.TryGetValue("instanceId",   out var id)  ? id?.ToString()             : Guid.NewGuid().ToString(),
                                unitName     = u.TryGetValue("unitName",     out var nm)  ? nm?.ToString()             : "",
                                enhanceLevel = u.TryGetValue("enhanceLevel", out var el)  ? Convert.ToInt32(el)        : 0,
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

        /// <summary>플레이어 전체 데이터를 Firestore에 저장 (덮어쓰기).</summary>
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
                    { "currency",            data.currency },
                    { "ownedUnits",          unitList },
                    { "rankPoints",          data.rankPoints },
                    { "lastDailyBonus",      data.lastDailyBonus ?? "" },
                    { "dailyMissionDate",    data.dailyMissionDate ?? "" },
                    { "dailyMissionWins",    data.dailyMissionWins },
                    { "dailyMissionClaimed", data.dailyMissionClaimed },
                };

                await _db.Collection("users").Document(uid).SetAsync(dict);
                Debug.Log("[Firestore] 저장 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firestore] 저장 실패: {e.Message}");
            }
        }

        /// <summary>특정 필드만 부분 업데이트 (문서가 없으면 실패 → SavePlayerData 먼저 호출 필요).</summary>
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
                // 문서 없을 경우 등 실패는 조용히 로그만
                Debug.LogWarning($"[Firestore] UpdateFields 실패: {e.Message}");
            }
        }

        // ─── 구독 헬퍼 ────────────────────────────────────────────

        /// <summary>
        /// OnDataReady 구독. 이미 데이터 로드가 완료된 경우 즉시 콜백 호출.
        /// Start()에서 호출하면 Awake 순서와 무관하게 안전하게 작동.
        /// </summary>
        public void SubscribeDataReady(Action<FirestorePlayerData> callback)
        {
            if (CachedData != null || _dataLoadAttempted)
                callback(CachedData);
            else
                OnDataReady += callback;
        }

        bool _dataLoadAttempted;

        // ─── 유틸 ─────────────────────────────────────────────────

        static void SaveUID(string uid)
        {
            PlayerPrefs.SetString("firebase_uid", uid);
            PlayerPrefs.Save();
        }

        /// <summary>앱 재시작 전 캐싱된 UID (Firebase 초기화 전에도 사용 가능)</summary>
        public static string GetCachedUID() => PlayerPrefs.GetString("firebase_uid", "");
    }
}
