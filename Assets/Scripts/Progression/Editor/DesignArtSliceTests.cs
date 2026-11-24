using NUnit.Framework;
using UnityEngine;

/// <summary>DesignArt.SliceBorders에 등록한 9-slice 조각이 실제 텍스처와 맞는지 확인한다.</summary>
public class DesignArtSliceTests
{
    [Test]
    public void SliceBorders_FitInsideTheirTextures()
    {
        foreach (var entry in DesignArt.SliceBorders)
        {
            var texture = Resources.Load<Texture2D>("UIDesign/Art/" + entry.Key);
            Assert.IsNotNull(texture, entry.Key + " 텍스처가 Resources/UIDesign/Art에 있어야 한다");
            Vector4 b = entry.Value; // 왼쪽, 아래, 오른쪽, 위
            Assert.LessOrEqual(b.x + b.z, texture.width, entry.Key + " 가로 테두리가 텍스처보다 넓다");
            Assert.LessOrEqual(b.y + b.w, texture.height, entry.Key + " 세로 테두리가 텍스처보다 높다");
        }
    }

    [Test]
    public void Sliced_ReturnsSpriteWithBorder_AndFallsBackToLoad()
    {
        var sign = DesignArt.Sliced("PlayHud/SignPaper");
        Assert.IsNotNull(sign);
        Assert.AreEqual(DesignArt.SliceBorders["PlayHud/SignPaper"], sign.border);
        Assert.AreEqual(200f, sign.pixelsPerUnit);
        Assert.AreSame(sign, DesignArt.Sliced("PlayHud/SignPaper"), "같은 이름은 캐시된 스프라이트를 돌려준다");

        var pin = DesignArt.Sliced("PlayHud/BrassPin"); // 테두리 없는 조각은 Load와 같다
        Assert.IsNotNull(pin);
        Assert.AreEqual(Vector4.zero, pin.border);
    }
}
