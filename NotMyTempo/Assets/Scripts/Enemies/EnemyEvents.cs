using System;
using UnityEngine;

/// <summary>
/// Game-wide announcements about enemies. Every enemy type reports its death here,
/// and anything that cares (kill counter, HUD, groove meter later) listens.
/// </summary>
public static class EnemyEvents // static because we only need one
{
    public static event Action<GameObject> EnemyKilled; // “something happened, and here’s the GameObject it happened to.”

    public static void ReportKill(GameObject enemy)
    {
        EnemyKilled?.Invoke(enemy); // ? so that if no-one is listening, it does nothin
    }
}