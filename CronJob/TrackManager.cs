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
  public static string FileName = "cron_track.yaml";
  public static string FilePath = Path.Combine(Paths.ConfigPath, FileName);

  [HarmonyPatch(typeof(Chat), nameof(Chat.Awake)), HarmonyPostfix]
  public static void ChatAwake()
  {
    if (File.Exists(FilePath))
      FromFile();
  }
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread)), HarmonyPostfix]
  public static void OnSave()
  {
    if (ZoneTimestamps.Count == 0)
    {
      if (File.Exists(FilePath))
        File.Delete(FilePath);
      return;
    }
    var data = ZoneTimestamps.ToDictionary(kvp => $"{kvp.Key.x},{kvp.Key.y}", kvp => kvp.Value.Ticks);
    var yaml = Data.Serializer().Serialize(data);
    File.WriteAllText(FilePath, yaml);
  }
  public static void FromFile()
  {
    ZoneTimestamps = [];
    if (!File.Exists(FilePath)) return;
    try
    {
      var data = Data.Read<Dictionary<string, long>>(FilePath);
      ZoneTimestamps = data.ToDictionary(
        kvp =>
        {
          var split = kvp.Key.Split(',');
          return new Vector2i(int.Parse(split[0]), int.Parse(split[1]));
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
  public static Dictionary<Vector2i, DateTime> ZoneTimestamps = [];

  public static void Track()
  {
    if (CronManager.ZoneJobs.Count == 0) return;
    var hasPlayer = GetPlayerZones();
    HashSet<Vector2i> zones = [];
    if (!ZNet.instance.IsDedicated())
      TrackPeer(zones, ZNet.instance.GetReferencePosition());
    foreach (var peer in ZNet.instance.GetPeers())
      TrackPeer(zones, peer.GetRefPos());
    foreach (var zone in zones)
      Poke(zone, hasPlayer.Contains(zone));
  }

  private static void Poke(Vector2i zone, bool hasPlayer)
  {
    DateTime? previous = ZoneTimestamps.ContainsKey(zone) ? ZoneTimestamps[zone] : null;
    if (CronManager.Execute(zone, hasPlayer, previous))
      ZoneTimestamps[zone] = DateTime.UtcNow;
  }

  private static void TrackPeer(HashSet<Vector2i> zones, Vector3 pos)
  {
    var zs = ZoneSystem.instance;
    var middle = zs.GetZone(pos);
    var num = zs.m_activeArea + zs.m_activeDistantArea;
    for (var i = middle.y - num; i <= middle.y + num; i++)
    {
      for (var j = middle.x - num; j <= middle.x + num; j++)
      {
        Vector2i zone = new(j, i);
        if (zs.IsZoneGenerated(zone))
          zones.Add(zone);
      }
    }
  }
  private static HashSet<Vector2i> GetActiveZones()
  {
    HashSet<Vector2i> zones = [];
    if (!ZNet.instance.IsDedicated())
      TrackPeer(zones, ZNet.instance.GetReferencePosition());
    foreach (var peer in ZNet.instance.GetPeers())
      TrackPeer(zones, peer.GetRefPos());
    return zones;
  }
  private static HashSet<Vector2i> GetPlayerZones()
  {
    HashSet<Vector2i> zones = [];
    if (!ZNet.instance.IsDedicated())
      zones.Add(ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition()));
    foreach (var peer in ZNet.instance.GetPeers())
      zones.Add(ZoneSystem.instance.GetZone(peer.GetRefPos()));
    return zones;
  }
}
