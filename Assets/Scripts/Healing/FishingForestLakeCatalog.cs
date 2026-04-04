using UnityEngine;

/// <summary>
/// 「이름 없는 숲의 호수」에 서식하는 상상 어종 목록. 이후 다른 수역은 별도 카탈로그로 확장합니다.
/// </summary>
public static class FishingForestLakeCatalog
{
    private static readonly FishingFishEntry[] Fish =
    {
        new FishingFishEntry("mist_perch", "안개잉어", "물안개 사이로 은은하게 빛나는 비늘.", 0.85f),
        new FishingFishEntry("moon_dace", "달빛 마자", "밤이면 지느러미 끝이 옅은 달빛을 닮습니다.", 1f),
        new FishingFishEntry("reed_whisper", "갈대 속 송사", "살짝만 건드려도 잔물결이 길게 이어져요.", 0.9f),
        new FishingFishEntry("lily_koi", "연잎 금붕어", "연꽃 그림자 아래서 천천히 도는 작은 빛.", 0.75f),
        new FishingFishEntry("deep_glimmer", "깊은 반딧불", "물속 깊은 곳에서만 보이는 푸른 점.", 1.15f),
        new FishingFishEntry("forest_minnow", "숲줄기 피라미", "숲에서 불어온 바람을 좋아하는 작은 무리.", 0.65f),
        new FishingFishEntry("dew_trout", "이슬 송어", "아침 이슬이 맺힐 때 가장 잘 보입니다.", 1.05f),
    };

    private static readonly float[] Weights =
    {
        1.1f, 1f, 1f, 1.15f, 0.75f, 1.25f, 1f,
    };

    public static FishingFishEntry RollCatch()
    {
        float sum = 0f;
        for (int i = 0; i < Weights.Length; i++)
        {
            sum += Weights[i];
        }

        float r = Random.Range(0f, sum);
        float acc = 0f;
        for (int i = 0; i < Fish.Length; i++)
        {
            acc += Weights[i];
            if (r <= acc)
            {
                return Fish[i];
            }
        }

        return Fish[Fish.Length - 1];
    }
}
