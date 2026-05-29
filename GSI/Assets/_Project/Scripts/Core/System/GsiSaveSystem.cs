using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ArchE.Game
{
    [Serializable]
    public struct GsiSaveEntry
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public class GsiSaveData
    {
        public int FormatVersion = 1;
        public List<GsiSaveEntry> Entries = new List<GsiSaveEntry>();
    }

    /// <summary>
    /// G.S.I 통합 세이브 시스템:
    /// Windows 레지스트리 기반인 PlayerPrefs를 완벽히 대체하여, 스팀 클라우드와 멀티플레이에 적합한 암호화 JSON 백업본으로 저장합니다.
    /// 기존 PlayerPrefs API와 100% 동일한 서명을 지원하여 기존 코드 수정을 최소화합니다.
    /// </summary>
    public static class GsiSaveSystem
    {
        private const string EncryptionKey = "RuneAtelierGSI!";
        private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "gsi_secure_save.json");
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private static bool _isLoaded;

        static GsiSaveSystem()
        {
            EnsureLoaded();
        }

        /// <summary>
        /// 세이브 파일이 로드되었는지 확인하고, 로드되지 않았다면 로딩 및 마이그레이션을 개시합니다.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_isLoaded) return;
            Load();
        }

        private static void Load()
        {
            _cache.Clear();
            _isLoaded = true;

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string cipherText = File.ReadAllText(SaveFilePath, Encoding.UTF8);
                    string plainText = Decrypt(cipherText);
                    
                    GsiSaveData data = JsonUtility.FromJson<GsiSaveData>(plainText);
                    if (data != null && data.Entries != null)
                    {
                        foreach (var entry in data.Entries)
                        {
                            if (!string.IsNullOrEmpty(entry.Key))
                            {
                                _cache[entry.Key] = entry.Value;
                            }
                        }
                        Debug.Log($"[GsiSaveSystem] Successfully loaded {_cache.Count} keys from secure JSON.");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GsiSaveSystem] Failed to load secure JSON: {e.Message}. Attempting legacy fallback.");
            }

            // 파일이 없거나 오류 발생 시 1회성 레거시 PlayerPrefs 마이그레이션 실행
            PerformLegacyMigration();
        }

        /// <summary>
        /// 변경 메모리 데이터를 로컬 JSON 파일로 안전하게 직렬화 및 암호화하여 디스크에 저장합니다.
        /// </summary>
        public static void Save()
        {
            EnsureLoaded();
            try
            {
                var data = new GsiSaveData();
                foreach (var kvp in _cache)
                {
                    data.Entries.Add(new GsiSaveEntry { Key = kvp.Key, Value = kvp.Value });
                }

                string plainText = JsonUtility.ToJson(data, true);
                string cipherText = Encrypt(plainText);

                File.WriteAllText(SaveFilePath, cipherText, Encoding.UTF8);
                Debug.Log($"[GsiSaveSystem] Successfully wrote {_cache.Count} keys to secure JSON file.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GsiSaveSystem] Failed to write secure JSON: {e.Message}");
            }
        }

        #region Legacy Migration
        private static void PerformLegacyMigration()
        {
            Debug.Log("[GsiSaveSystem] Initializing legacy PlayerPrefs data migration...");
            int migratedCount = 0;

            // G.S.I에서 사용하는 모든 표준 마이그레이션 대상 키 리스트
            string[] standardKeys = {
                "UnifiedExamHistory",
                "GSI_UnifiedExamHistoryJson_v1",
                "BestReactionTime",
                "BestAimTime",
                "BestMemorySpan",
                "BestRhythmAccuracy",
                "BestRhythmMeanError",
                "BestMotAccuracy",
                "BestBulletHellTime",
                "BestCpsAverage",
                "BestUnifiedExamTotal",
                "GSI_Tokens_v2",
                "GSI_Tickets_v2",
                "GSI_PracticeGrade_Reaction",
                "GSI_PracticeGrade_Aim",
                "GSI_PracticeGrade_Memory",
                "GSI_PracticeGrade_Rhythm",
                "GSI_PracticeGrade_MOT",
                "GSI_PracticeGrade_BulletHell",
                "GSI_PracticeGrade_Cps",
                "GSI_Settings_MasterVolume",
                "GSI_Settings_SfxVolume",
                "GSI_Settings_MusicVolume",
                "RunicAtelier.PreferredLocale",
                "GSI_UiAppearanceMode",
                "GSI_Cosmetic_EquippedId",
                "GSI_Cosmetic_Owned_skin_ocean",
                "GSI_Cosmetic_Owned_skin_amber",
                "GSI_Cosmetic_Owned_skin_violet",
                "GSI_PracticeGrade",
                "GSI_SteamCloud_LastMergedWrittenUnixV2",
                "GSI_SteamCloud_LastLocalPushUnixV2"
            };

            foreach (var key in standardKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    _cache[key] = PlayerPrefs.GetString(key, string.Empty);
                    if (string.IsNullOrEmpty(_cache[key]))
                    {
                        // GetString fallback 실패 시 float 또는 int 시도
                        float f = PlayerPrefs.GetFloat(key, float.MinValue);
                        if (f != float.MinValue)
                        {
                            _cache[key] = f.ToString();
                        }
                        else
                        {
                            int i = PlayerPrefs.GetInt(key, int.MinValue);
                            if (i != int.MinValue)
                            {
                                _cache[key] = i.ToString();
                            }
                        }
                    }
                    migratedCount++;
                }
            }

            // 성단 내의 별 노드 위치 및 속도 잠금 키 마이그레이션 (0번부터 7번 별까지)
            for (int i = 0; i <= 7; i++)
            {
                string[] starKeys = {
                    $"LobbyStar_{i}_x", $"LobbyStar_{i}_y", $"LobbyStar_{i}_vx", $"LobbyStar_{i}_vy", $"LobbyStar_{i}_static",
                    $"GsiStar_{i}_x", $"GsiStar_{i}_y", $"GsiStar_{i}_vx", $"GsiStar_{i}_vy", $"GsiStar_{i}_static"
                };
                foreach (var sk in starKeys)
                {
                    if (PlayerPrefs.HasKey(sk))
                    {
                        _cache[sk] = PlayerPrefs.GetFloat(sk, 0f).ToString();
                        migratedCount++;
                    }
                }
            }

            Debug.Log($"[GsiSaveSystem] Legacy migration finished. {migratedCount} keys migrated successfully.");
            
            // 캐시에 마이그레이션이 완료되었음을 기록하고 디스크에 즉시 보존
            _cache["GSI_JSON_Save_Initialized"] = "true";
            Save();
        }
        #endregion

        #region PlayerPrefs Parity API
        public static bool HasKey(string key)
        {
            EnsureLoaded();
            return _cache.ContainsKey(key);
        }

        public static void DeleteKey(string key)
        {
            EnsureLoaded();
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
        }

        public static void DeleteAll()
        {
            EnsureLoaded();
            _cache.Clear();
        }

        public static void SetInt(string key, int value)
        {
            EnsureLoaded();
            _cache[key] = value.ToString();
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            EnsureLoaded();
            if (_cache.TryGetValue(key, out string val) && int.TryParse(val, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        public static void SetFloat(string key, float value)
        {
            EnsureLoaded();
            _cache[key] = value.ToString("G");
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            EnsureLoaded();
            if (_cache.TryGetValue(key, out string val) && float.TryParse(val, out float result))
            {
                return result;
            }
            return defaultValue;
        }

        public static void SetString(string key, string value)
        {
            EnsureLoaded();
            _cache[key] = value ?? string.Empty;
        }

        public static string GetString(string key, string defaultValue = "")
        {
            EnsureLoaded();
            if (_cache.TryGetValue(key, out string val))
            {
                return val;
            }
            return defaultValue;
        }
        #endregion

        #region Encryption Helper (XOR + Base64)
        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ EncryptionKey[i % EncryptionKey.Length]);
            }
            return Convert.ToBase64String(bytes);
        }

        private static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                byte[] bytes = Convert.FromBase64String(cipherText);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ EncryptionKey[i % EncryptionKey.Length]);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // 암호화 복호화 실패 시 평문 반환 시도 (안전 장치)
                return cipherText;
            }
        }
        #endregion
    }
}
