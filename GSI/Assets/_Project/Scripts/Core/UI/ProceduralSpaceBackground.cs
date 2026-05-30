using UnityEngine;
using UnityEngine.UI;

namespace ArchE.Game
{
    /// <summary>
    /// G.S.I 공용 우주 배경 렌더러:
    /// 메인화면과 GSI 시설 로비 등에서 사용되는 절차적 우주 배경(성운, 별빛) 텍스처를 단일 메모리 인스턴스로 생성하고 
    /// UI Image 컴포넌트에 안전하게 실시간 바인딩 및 관리합니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ProceduralSpaceBackground : MonoBehaviour
    {
        private static Sprite _proceduralSpaceSprite;

        private void Awake()
        {
            ApplyBackground();
        }

        /// <summary>
        /// 부착된 Image 컴포넌트에 절차적 우주 텍스처를 바인딩합니다.
        /// </summary>
        public void ApplyBackground()
        {
            Image targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = GetOrCreateSpaceSprite();
                targetImage.type = Image.Type.Simple;
                targetImage.preserveAspect = false;
                targetImage.color = Color.white;
                targetImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// 임의의 Image 컴포넌트에 절차적 우주 텍스처를 바인딩합니다.
        /// </summary>
        public static void ApplyToImage(Image image)
        {
            if (image == null) return;
            image.sprite = GetOrCreateSpaceSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static Sprite GetOrCreateSpaceSprite()
        {
            if (_proceduralSpaceSprite == null)
            {
                _proceduralSpaceSprite = CreateProceduralSpaceSprite(1024, 576);
            }
            return _proceduralSpaceSprite;
        }

        private static Sprite CreateProceduralSpaceSprite(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.name = "GSI_CosmicSpaceBackground";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color spaceDark = new Color(0.002f, 0.001f, 0.004f, 1f); // Deep void black/violet
            Color indigoBlack = new Color(0.001f, 0.002f, 0.006f, 1f); // Deep void indigo

            // Define brilliant glowing star coordinates and intensities
            var brightStars = new (float x, float y, float r, float intensity)[]
            {
                (0.15f, 0.72f, 8f, 0.9f),
                (0.32f, 0.24f, 6f, 0.8f),
                (0.55f, 0.85f, 12f, 0.95f), // A bright star in upper middle
                (0.78f, 0.42f, 10f, 0.85f),
                (0.88f, 0.78f, 7f, 0.75f),
                (0.22f, 0.48f, 5f, 0.7f),
                (0.48f, 0.18f, 9f, 0.85f),
                (0.68f, 0.62f, 6f, 0.75f)
            };

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);

                    // Base gradient
                    Color pixelColor = Color.Lerp(spaceDark, indigoBlack, v + u * 0.15f);

                    // Nebula 1: Large soft violet clouds
                    float n1 = Mathf.PerlinNoise(u * 2.2f + 4.5f, v * 1.8f + 1.2f);
                    float n2 = Mathf.PerlinNoise(u * 4.8f - 2.5f, v * 3.6f + 3.8f);
                    float neb1 = Mathf.Max(0f, (n1 * 0.65f + n2 * 0.35f) - 0.35f) * 1.8f;
                    Color nebColor1 = new Color(0.04f, 0.012f, 0.07f, 1f) * neb1;

                    // Nebula 2: Glowing cosmic cyan/teal clouds
                    float n3 = Mathf.PerlinNoise(u * 3.5f - 8.2f, v * 2.8f + 5.5f);
                    float n4 = Mathf.PerlinNoise(u * 6.5f + 1.1f, v * 5.2f - 4.2f);
                    float neb2 = Mathf.Max(0f, (n3 * 0.58f + n4 * 0.42f) - 0.42f) * 1.9f;
                    Color nebColor2 = new Color(0.008f, 0.045f, 0.06f, 1f) * neb2;

                    pixelColor += nebColor1 + nebColor2;

                    // Sparkling background stars (pseudo-random fast hash)
                    float starSeed = Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1.0f);
                    if (starSeed > 0.9975f)
                    {
                        float starBrightness = (starSeed - 0.9975f) / 0.0025f;
                        pixelColor += new Color(starBrightness, starBrightness, starBrightness * 1.08f, 0f) * 0.6f;
                    }

                    // Draw soft glow for the brilliant stars
                    foreach (var star in brightStars)
                    {
                        float sx = star.x * width;
                        float sy = star.y * height;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(sx, sy));
                        if (dist < star.r)
                        {
                            float glow = Mathf.Pow(1.0f - dist / star.r, 2.2f);
                            pixelColor += new Color(star.intensity, star.intensity, star.intensity * 1.05f, 0f) * glow * 0.4f;
                        }
                    }

                    tex.SetPixel(x, y, pixelColor);
                }
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
