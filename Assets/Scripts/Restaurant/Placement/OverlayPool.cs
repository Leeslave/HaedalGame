using System.Collections.Generic;
using UnityEngine;

// Ghost 미리보기 타일 Overlay를 오브젝트 풀링으로 관리
public class OverlayPool : MonoBehaviour
{
    [SerializeField] private GameObject overlayPrefab;
    [SerializeField] private int initSize = 20;

    private static readonly Color ValidColor = new Color(0f, 1f, 0f, 0.4f);
    private static readonly Color InvalidColor = new Color(1f, 0f, 0f, 0.4f);

    private List<GameObject> pool = new List<GameObject>();
    private List<GameObject> active = new List<GameObject>();

    void Awake()
    {
        for (int i = 0; i < initSize; i++) { pool.Add(CreatNew()); }
    }

    private GameObject CreatNew()
    {
        GameObject obj = Instantiate(overlayPrefab, transform);
        obj.SetActive(false);
        return obj;
    }

    public void ShowOverlay(Vector3 worldPos, bool isValid)
    {
        GameObject obj;

        if (pool.Count > 0)
        {
            int last = pool.Count - 1;
            obj = pool[last];
            pool.RemoveAt(last);
        }
        else { obj = CreatNew(); }

        obj.transform.position = worldPos;
        obj.GetComponent<SpriteRenderer>().color = isValid ? ValidColor : InvalidColor;
        obj.SetActive(true);
        active.Add(obj);
    }

    public void ReturnAll()
    {
        foreach (GameObject obj in active)
        {
            obj.SetActive(false);
            pool.Add(obj);
        }
        active.Clear();
    }

}