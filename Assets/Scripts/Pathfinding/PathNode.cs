using System;
using UnityEngine;

public class PathNode
{
    public Vector2Int gridPos;
    public Vector3 worldPos;
    public bool walkable;

    public int gCost; // 시작점 -> 현재 노드 비용
    public int hCost; // 현재 노드 -> 목표 휴리스틱 비용

    public PathNode parent;

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

}