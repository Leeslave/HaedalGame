using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 이미지가 없을 때 대신 쓸 그림을 코드로 만들어준다.
/// 해달 식당의 바닷가 분위기에 맞춰 위아래 그러데이션 + 은은한 비네트로 칠하고,
/// 스타일별로 한 번 만든 스프라이트는 캐시해두고 재사용한다.
/// </summary>
public static class DialogueBackgroundFactory
{
    // 그러데이션만 그릴 거라 해상도는 낮아도 된다. 늘려서 쓰면 부드럽게 보간된다.
    private const int TextureSize = 64;

    private static readonly Dictionary<DialogueBackgroundStyle, Sprite> _cache =
        new Dictionary<DialogueBackgroundStyle, Sprite>();

    /// <summary>스타일에 해당하는 배경 스프라이트를 얻는다. None이면 null.</summary>
    public static Sprite Get(DialogueBackgroundStyle style)
    {
        if (style == DialogueBackgroundStyle.None)
            return null;

        if (_cache.TryGetValue(style, out Sprite cached) && cached != null)
            return cached;

        Sprite created = Create(style);
        _cache[style] = created;

        return created;
    }

    /// <summary>만들어 둔 배경을 모두 버린다. (스타일 색을 바꿔가며 확인할 때 쓴다)</summary>
    public static void ClearCache()
    {
        foreach (KeyValuePair<DialogueBackgroundStyle, Sprite> pair in _cache)
        {
            if (pair.Value == null)
                continue;

            if (pair.Value.texture != null)
                Object.Destroy(pair.Value.texture);

            Object.Destroy(pair.Value);
        }

        _cache.Clear();
    }

    private static Sprite Create(DialogueBackgroundStyle style)
    {
        GetPalette(style, out Color top, out Color bottom, out float vignette);

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = $"DialogueBG_{style}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            // 0이 아래, 1이 위. 위아래 색을 부드럽게 섞는다.
            float verticalRatio = TextureSize > 1 ? y / (float)(TextureSize - 1) : 0f;
            Color rowColor = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, verticalRatio));

            for (int x = 0; x < TextureSize; x++)
            {
                float horizontalRatio = TextureSize > 1 ? x / (float)(TextureSize - 1) : 0.5f;

                // 화면 중앙에서 멀어질수록 살짝 어둡게 눌러서 글자가 잘 읽히게 한다.
                float dx = horizontalRatio - 0.5f;
                float dy = verticalRatio - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f; // 모서리에서 1이 되도록 정규화
                float shade = 1f - (vignette * distance * distance);

                Color pixel = rowColor * shade;
                pixel.a = rowColor.a;

                pixels[y * TextureSize + x] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        sprite.name = $"DialogueBG_{style}";
        sprite.hideFlags = HideFlags.HideAndDontSave;

        return sprite;
    }

    /// <summary>스타일별 위/아래 색과 비네트 세기. 해달 식당의 바닷가 색감에 맞춘 값이다.</summary>
    private static void GetPalette(DialogueBackgroundStyle style, out Color top, out Color bottom, out float vignette)
    {
        switch (style)
        {
            case DialogueBackgroundStyle.SeasideDay:
                // 맑은 하늘에서 볕 든 모래사장으로
                top = new Color32(0x7E, 0xC8, 0xE3, 0xFF);
                bottom = new Color32(0xF6, 0xE2, 0xB3, 0xFF);
                vignette = 0.20f;
                break;

            case DialogueBackgroundStyle.SeasideDusk:
                // 노을이 물든 하늘에서 어두워지는 바다로
                top = new Color32(0xF3, 0x9C, 0x6B, 0xFF);
                bottom = new Color32(0x2E, 0x4A, 0x7A, 0xFF);
                vignette = 0.28f;
                break;

            case DialogueBackgroundStyle.SeasideNight:
                // 별 뜬 밤하늘에서 달빛 어린 물결로
                top = new Color32(0x1B, 0x24, 0x4A, 0xFF);
                bottom = new Color32(0x4A, 0x3F, 0x6B, 0xFF);
                vignette = 0.34f;
                break;

            case DialogueBackgroundStyle.WarmInterior:
                // 등불 켠 식당 안. 나무와 촛불빛
                top = new Color32(0x6B, 0x47, 0x35, 0xFF);
                bottom = new Color32(0xD9, 0xA3, 0x6A, 0xFF);
                vignette = 0.30f;
                break;

            case DialogueBackgroundStyle.DeepSea:
                // 물속 깊은 곳. 위에서 빛이 스며든다
                top = new Color32(0x3C, 0xA0, 0xA8, 0xFF);
                bottom = new Color32(0x0E, 0x2B, 0x4A, 0xFF);
                vignette = 0.38f;
                break;

            case DialogueBackgroundStyle.Dim:
                // 게임 화면을 덮어 가리는 용도라 반투명 검정
                top = new Color(0f, 0f, 0f, 0.72f);
                bottom = new Color(0f, 0f, 0f, 0.72f);
                vignette = 0f;
                break;

            default:
                top = Color.black;
                bottom = Color.black;
                vignette = 0f;
                break;
        }
    }
}
