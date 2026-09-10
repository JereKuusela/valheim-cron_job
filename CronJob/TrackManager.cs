using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace CronJob;

[HarmonyPatch]
public class TrackManager
{
  public static string FileNameJob = "cron_last.yaml";
  public static string FileNameZone = "cron_track.yaml";
  public static string FilePathJob = Path.Combine(Paths.ConfigPath, FileNameJob);
  public static string FilePathZone = Path.Combine(Paths.ConfigPath, FileNameZone);

  [HarmonyPatch(typeof(Chat), nameof(Chat.Awake)), HarmonyPostfix]
  public static void ChatAwake()
  {
    if (File.Exists(FilePathJob))
    {
      var jobData = Data.Read<Dictionary<string, long>>(FilePathJob);
      if (jobData.TryGetValue("world", out var world))
        CronManager.Previous = new DateTime(world, DateTimeKind.Utc);
      if (jobData.TryGetValue("game", out var game))
        CronManager.PreviousGameTime = new DateTime(game, DateTimeKind.Utc);
    }
    if (File.Exists(FilePathZone))
      ZoneTrackFromFile();
  }
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread)), HarmonyPrefix]
  public static void OnSave()
  {
    var jobData = new Dictionary<string, long>() {
      { "world", CronManager.Previous.Ticks + TimeSpan.TicksPerSecond },
      { "game", CronManager.PreviousGameTime.Ticks + TimeSpan.TicksPerSecond }
    };
    var yaml = Data.Serializer().Serialize(jobData);
    File.WriteAllText(FilePathJob, yaml);
    if (ZoneTimestamps.Count == 0)
    {
      if (File.Exists(FilePathZone))
        File.Delete(FilePathZone);
      return;
    }
    var data = ZoneTimestamps.ToDictionary(kvp => $"{kvp.Key.x},{kvp.Key.y}", kvp => kvp.Value.Ticks);
    yaml = Data.Serializer().Serialize(data);
    File.WriteAllText(FilePathZone, yaml);
  }
  public static void ZoneTrackFromFile()
  {
    ZoneTimestamps = [];
    if (!File.Exists(FilePathZone)) return;
    try
    {
      var data = Data.Read<Dictionary<string, long>>(FilePathZone);
      ZoneTimestamps = data.ToDictionary(
        kvp =>
        {
          var split = kvp.Key.Split(',');
          return new Vector2s(int.Parse(split[0]), int.Parse(split[1]));
        },
        kvp => new DateTime(kvp.Value, DateTimeKind.Utc)
      );
      CronJob.Log.LogInfo($"Reloading {ZoneTimestamps.Count} zone last runs.");
    }
    catch (Exception e)
    {
      CronJob.Log.LogError(e.StackTrace);
    }
  }
  public static Dictionary<Vector2s, DateTime> ZoneTimestamps = [];

  private static readonly HashSet<Vector2s> Zones = [];
  public static void Track()
  {
    if (CronManager.ZoneJobs.Count == 0) return;
    var hasPlayer = GetPlayerZones();
    Zones.Clear();
    if (!ZNet.instance.IsDedicated())
      TrackPeer(Zones, ZNet.instance.GetReferencePosition());
    foreach (var peer in ZNet.instance.GetPeers())
      TrackPeer(Zones, peer.GetRefPos());
    foreach (var zone in Zones)
      Poke(zone, hasPlayer.Contains(zone));
  }

  private static void Poke(Vector2s zone, bool hasPlayer)
  {
    DateTime? previous = ZoneTimestamps.ContainsKey(zone) ? ZoneTimestamps[zone] : null;
    if (CronManager.Execute(zone, hasPlayer, previous))
      ZoneTimestamps[zone] = DateTime.UtcNow;
  }

  private static void TrackPeer(HashSet<Vector2s> zones, Vector3 pos)
  {
    var zs = ZoneSystem.instance;
    var middle = ZoneSystem.GetZone(pos);
    // Was previously using zs.m_activeArea + zs.m_activeDistantArea but these don't exist anymore.
    // 2 would be loaded area, so 3 is probably a good guess even though previous was like 4.
    var num = 3;
    for (var i = middle.y - num; i <= middle.y + num; i++)
    {
      for (var j = middle.x - num; j <= middle.x + num; j++)
      {
        Vector2s zone = new(j, i);
        if (zs.IsZoneGenerated(zone))
          zones.Add(zone);
      }
    }
  }

  private static readonly HashSet<Vector2s> PlayerZones = [];
  private static HashSet<Vector2s> GetPlayerZones()
  {
    PlayerZones.Clear();
    if (!ZNet.instance.IsDedicated())
      PlayerZones.Add(ZoneSystem.GetZone(ZNet.instance.GetReferencePosition()));
    foreach (var peer in ZNet.instance.GetPeers())
      PlayerZones.Add(ZoneSystem.GetZone(peer.GetRefPos()));
    return PlayerZones;
  }
}
