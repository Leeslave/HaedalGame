using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 식당 바닥(배치도). 손님·직원이 오가는 길과 주요 지점을 알려 준다.
// 씬에 배치도가 깔리면(RestaurantNav.Floor) 모든 이동이 그 길을 따르고, 없으면 목적지까지 곧장 헤엄친다.
public interface IRestaurantFloor
{
    List<Vector3> Route(Vector2 from, Vector2 to);      // from에서 to까지 거쳐 갈 지점들 (마지막이 to)
    Vector2 ServePoint(Vector2 seatPoint);              // 직원이 그 좌석 손님을 응대할 때 설 곳
    Vector2 KitchenPickup { get; }                      // 서빙 직원이 음식을 받는 곳
    Vector2 KitchenDropoff { get; }                     // 주방 직원이 완성 요리를 내놓는 곳
    Vector2 StationPoint(CookingType type);             // 조리도구 앞
    Vector2 IdlePoint(PartTimerRole role, int index);   // 직원 대기 위치 (index는 0부터)
}

public static class RestaurantNav
{
    public static IRestaurantFloor Floor { get; set; }

    public static List<Vector3> Route(Vector2 from, Vector2 to)
    {
        return Floor != null ? Floor.Route(from, to) : new List<Vector3> { to };
    }

    public static Vector2 ServePoint(Vector2 seatPoint)
    {
        return Floor != null ? Floor.ServePoint(seatPoint) : seatPoint + Vector2.down * 0.8f;
    }

    // 경로를 따라 헤엄친다. speed는 매 프레임 다시 읽으므로 도중에 배속이 바뀌어도 바로 반영된다.
    public static IEnumerator Swim(Transform mover, List<Vector3> path, Func<float> speed, Action<Vector2> face, float threshold = 0.05f)
    {
        foreach (Vector3 waypoint in path)
        {
            while (Vector2.Distance(mover.position, waypoint) > threshold)
            {
                Vector2 dir = ((Vector2)waypoint - (Vector2)mover.position).normalized;
                mover.position = Vector2.MoveTowards(mover.position, waypoint, speed() * Time.deltaTime);
                face?.Invoke(dir);
                yield return null;
            }
        }
    }
}
