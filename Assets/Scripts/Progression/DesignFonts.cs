using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>시안의 폰트를 Resources에서 직접 로드한다. 씬과 정적 SDF의 글리프 포함 여부에 의존하지 않는다.</summary>
public static class DesignFonts
{
    private static TMP_FontAsset heading, body;
    public static TMP_FontAsset Heading
    {
        get
        {
            if (heading != null) return heading;
            heading = Create("Jua-Regular");
            heading.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { Body };
            return heading;
        }
    }
    public static TMP_FontAsset Body => body != null ? body : body = Create("NanumGothic-Bold");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { heading = null; body = null; }

    private static TMP_FontAsset Create(string name)
    {
        var source = Resources.Load<Font>("UIDesign/Fonts/" + name);
        if (source == null) throw new System.InvalidOperationException("UI 폰트 파일을 찾지 못했습니다: " + name);
        var font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = "Design " + name;
        font.hideFlags = HideFlags.DontSave;
        font.isMultiAtlasTexturesEnabled = true;

        font.TryAddCharacters("해달식당이어하기새로설정게임종료아침영업밤준비관리메뉴재료직원가구상점연구탐험집결산확인취소저장일차골드평점품절좌석도구0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ★·/+-:%!", out _);
        return font;
    }

    public static void Apply(TMP_Text text, bool title = false)
    {
        var font = title ? Heading : Body;
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontStyle = FontStyles.Normal;
        text.ForceMeshUpdate(true, true);
    }
}
