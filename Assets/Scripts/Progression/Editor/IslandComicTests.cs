using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class IslandComicTests
{
    private static int finishedCallbacks;
    private static void CountFinished() => finishedCallbacks++;

    [UnityTest]
    public IEnumerator Playback_FastClickFinishesFade_AndSkipCompletesOnlyOnce()
    {
        yield return new EnterPlayMode();
        // 도메인 재시작 전의 반복자에 재생 상태를 보관하지 않는다.
        yield return ExercisePlayback();
        yield return new ExitPlayMode();
    }

    private static IEnumerator ExercisePlayback()
    {
        finishedCallbacks = 0;
        var comic = IslandComic.TryPlay(IslandStory.ArrivalDialogue(0), CountFinished);
        try
        {
            Assert.IsNotNull(comic);
            yield return null;
            comic.Next(); // 등장 애니메이션 도중 눌러도 컷이 반투명하게 남으면 안 된다.
            var panel = comic.GetComponentsInChildren<CanvasGroup>().First(g => g.gameObject != comic.gameObject);
            Assert.AreEqual(1f, panel.alpha);
            Assert.AreEqual(Vector3.one, panel.transform.localScale);
            Canvas.ForceUpdateCanvases();
            var bubble = comic.GetComponentInChildren<ComicSpeechBubble>();
            Assert.IsNotNull(bubble);
            Assert.IsNotNull(bubble.canvasRenderer.GetMesh(), "실제 재생 중에도 말풍선 도형이 있어야 한다");
            Assert.Greater(bubble.canvasRenderer.GetMesh().vertexCount, 0);
            comic.Next();
            comic.Skip();
            comic.Skip();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(1, finishedCallbacks, "스킵 중복 입력으로 완료 처리가 중복되면 안 된다");
            Assert.IsNull(ComicIntro.Active);
        }
        finally { if (comic != null) Object.Destroy(comic.gameObject); }
    }

    [TestCase(0, true)] [TestCase(0, false)]
    [TestCase(1, true)] [TestCase(1, false)]
    [TestCase(2, true)] [TestCase(2, false)]
    [TestCase(3, true)] [TestCase(3, false)]
    [TestCase(4, true)] [TestCase(4, false)]
    public void ResidentComic_PreservesEveryDialogueLine_AndHasUsableArt(int island, bool arrival)
    {
        string path = arrival ? IslandStory.ArrivalDialogue(island) : IslandStory.DoneDialogue(island);
        Assert.IsTrue(IslandComic.TryBuild(path, out var cuts, out var title), path);
        Assert.IsNotEmpty(title);
        string shown = string.Join("\n", cuts.SelectMany(c => c.lines).Select(l => l.text));
        foreach (var node in DialogueManager.LoadFromResources(path).nodes)
            StringAssert.Contains(node.text, shown, "대사 누락: " + path);
        CollectionAssert.AreEquivalent(DialogueManager.LoadFromResources(path).nodes.Select(n => n.text),
            cuts.SelectMany(c => c.lines).Select(l => l.text), "모든 대사와 서술을 한 번씩 표시해야 한다");
        foreach (var cut in cuts)
        {
            var texture = Resources.Load<Texture2D>("UIDesign/Art/" + cut.art);
            Assert.IsNotNull(texture);
            Assert.GreaterOrEqual(texture.width * cut.uv.width, 400, "컷 해상도가 부족하다");
            Assert.GreaterOrEqual(texture.height * cut.uv.height, 400, "컷 해상도가 부족하다");
            Assert.LessOrEqual(cut.uv.xMax, 1.001f);
            Assert.LessOrEqual(cut.uv.yMax, 1.001f);
        }
    }

    [TestCase(null)] [TestCase("")] [TestCase("Dialogue/intro_day1")]
    [TestCase("Dialogue/Island/climax_wood")]
    public void OtherStories_KeepTheirExistingPlayback(string path)
    {
        Assert.IsFalse(IslandComic.TryBuild(path, out _, out _));
    }
}
