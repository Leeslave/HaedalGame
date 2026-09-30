using System;
using UnityEngine;

public class PathNode : IHeapItem<PathNode>
{
    public Vector2Int gridPos;
    public Vector3 worldPos;
    public bool walkable;

    public int gCost; // 시작점 -> 현재 노드 비용
    public int hCost; // 현재 노드 -> 목표 휴리스틱 비용

    public PathNode parent;

    public int HeapIndex { get; set; }

    public int FCost()
    {
        return gCost + hCost;
    }

    public PathNode(Vector2Int _gridPos, Vector3 _worldPos, bool _walkable)
    {
        gridPos = _gridPos;
        worldPos = _worldPos;
        walkable = _walkable;
    }

    // 힙에서 우선순위가 높을수록(CompareTo 결과가 클수록) 먼저 꺼내진다.
    // FCost가 낮을수록, 동률이면 hCost가 낮을수록 우선순위가 높아야 하므로 부호를 뒤집는다.
    public int CompareTo(PathNode other)
    {
        int compare = FCost().CompareTo(other.FCost());
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);
        }
        return -compare;
    }
}