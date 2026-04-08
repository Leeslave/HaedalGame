using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
    public string directory;
    public float fps = 12f;

    [SerializeField] private Sprite[] frames;
    [SerializeField] private Image image;

    void Start()
    {
        frames = Resources.LoadAll<Sprite>(directory).OrderBy(s => s.name)
        .ToArray();

        if (frames != null && frames.Length > 0)
            StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        float interval = 1f / fps;
        Debug.Log("애니메이션 실행");
        while (true)
        {
            yield return new WaitForSeconds(3f);
            for (int i = 0; i < frames.Length; i++)
            {
                image.sprite = frames[i];
                yield return new WaitForSeconds(interval);
            }
            Debug.Log("실행 끝");
        }
    }

    public void StartAnimation()
    {
        StartCoroutine(PlayAnimation());
    }

    public void StopAnimation()
    {
        StopCoroutine(PlayAnimation());
    }
}
