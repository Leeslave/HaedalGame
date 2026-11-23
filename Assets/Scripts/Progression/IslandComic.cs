using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>섬 주민 첫 만남과 교류 완료를 오프닝의 만화 재생기로 보여 준다.</summary>
public static class IslandComic
{
    public static readonly Vector2 PageSize = new Vector2(1660, 1040);

    public static ComicIntro TryPlay(string dialoguePath, Action onFinished)
    {
        if (!TryBuild(dialoguePath, out var cuts, out string title)) return null;
        return ComicIntro.TryPlay(cuts, PageSize, title, onFinished);
    }

    /// <summary>기존 대사를 원본으로 사용하므로 만화와 대화 대체 경로의 내용이 일치한다.</summary>
    public static bool TryBuild(string dialoguePath, out ComicIntro.Cut[] cuts, out string title)
    {
        cuts = null; title = null;
        if (string.IsNullOrEmpty(dialoguePath)) return false;
        for (int island = 0; island < IslandStory.Islands.Length; island++)
        {
            bool arrival = dialoguePath == IslandStory.ArrivalDialogue(island);
            if (!arrival && dialoguePath != IslandStory.DoneDialogue(island)) continue;
            var asset = Resources.Load<TextAsset>(dialoguePath);
            if (asset == null) return false;
            var script = JsonUtility.FromJson<DialogueScriptData>(asset.text);
            if (script?.nodes == null || script.nodes.Count < 3) return false;
            string art = "IslandComic/" + IslandStory.Islands[island].DialogueKey;
            if (Resources.Load<Texture2D>("UIDesign/Art/" + art) == null) return false;

            title = IslandStory.Islands[island].Name + (arrival ? " · 낯선 손님의 방문" : " · 새로운 인연");
            cuts = new ComicIntro.Cut[3];
            // 첫 컷은 도입과 첫 대사, 가운데 컷은 다음 대사, 마지막 컷은 나머지 대사.
            int[] boundaries = { 0, script.nodes.Count > 3 ? 2 : 1, script.nodes.Count > 3 ? 3 : 2, script.nodes.Count };
            for (int panel = 0; panel < cuts.Length; panel++)
            {
                var lines = new List<ComicIntro.Line>();
                for (int node = boundaries[panel]; node < boundaries[panel + 1]; node++)
                {
                    var line = script.nodes[node];
                    if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                    if (string.IsNullOrEmpty(line.speaker))
                        lines.Add(new ComicIntro.Line { box = new Rect(20, 530, 460, 140), text = line.text, caption = true });
                    else
                    {
                        float width = line.text.Length <= 24 ? 320 : 450;
                        float height = line.text.Length <= 24 ? 114 : line.text.Length <= 44 ? 150 : 188;
                        float speakerX = SpeakerX(island, arrival, panel, line.speaker);
                        float x = Mathf.Clamp(speakerX - width * .5f, 10, 490 - width);
                        var body = new Rect(x, -height - 22, width, height);
                        bool underwater = island == 1 && !arrival && panel == 1;
                        lines.Add(new ComicIntro.Line
                        {
                            text = line.text, speech = true, bubble = body,
                            tailTip = underwater ? new Vector2(105, 178) : new Vector2(speakerX, 32), tailFromLeft = underwater,
                            box = new Rect(body.x + width * .13f, body.y + height * .19f, width * .74f, height * .62f)
                        });
                    }
                }
                cuts[panel] = new ComicIntro.Cut(art, 36 + panel * 544, 300, lines.ToArray())
                {
                    size = new Vector2(500, 500),
                    // 원본 아틀라스: 위 3칸은 첫 만남, 아래 3칸은 교류 완료.
                    uv = new Rect(panel / 3f, arrival ? .5f : 0, 1f / 3f, .5f)
                };
            }
            return true;
        }
        return false;
    }

    /// <summary>각 그림에서 화자의 가로 위치. 꼬리는 얼굴 위 여백에서 끝나 얼굴을 가리지 않는다.</summary>
    private static float SpeakerX(int island, bool arrival, int panel, string speaker)
    {
        if (speaker == "haedal") return island == 1 && !arrival && panel == 2 ? 250 : 425;
        switch (island)
        {
            case 0: return arrival ? (panel == 0 ? 190 : 270) : 250;
            case 1: return arrival ? 185 : panel == 1 ? 145 : 230;
            case 2: return arrival && panel == 2 ? 300 : 180;
            case 3: return panel == 0 ? 200 : 250;
            default: return !arrival && panel == 1 ? 300 : 250;
        }
    }
}
