using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 오픈월드 마지막 플레이어 위치·회전을 저장합니다. (persistentDataPath JSON)
/// </summary>
public static class OpenWorldSpawnPersistence
{
    private const string FileName = "Arche_OpenWorld_last_spawn.json";

    [Serializable]
    private struct SpawnData
    {
        public float px;

        public float py;

        public float pz;

        public float qx;

        public float qy;

        public float qz;

        public float qw;
    }

    public static bool TryLoad(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        try
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path))
            {
                return false;
            }

            string json = File.ReadAllText(path);
            SpawnData d = JsonUtility.FromJson<SpawnData>(json);
            position = new Vector3(d.px, d.py, d.pz);
            rotation = new Quaternion(d.qx, d.qy, d.qz, d.qw);
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OpenWorldSpawnPersistence] Load failed: {e.Message}");
            return false;
        }
    }

    public static void Save(Vector3 position, Quaternion rotation)
    {
        try
        {
            var d = new SpawnData
            {
                px = position.x,
                py = position.y,
                pz = position.z,
                qx = rotation.x,
                qy = rotation.y,
                qz = rotation.z,
                qw = rotation.w,
            };

            string path = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(path, JsonUtility.ToJson(d, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OpenWorldSpawnPersistence] Save failed: {e.Message}");
        }
    }

    public static void Clear()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OpenWorldSpawnPersistence] Clear failed: {e.Message}");
        }
    }
}
