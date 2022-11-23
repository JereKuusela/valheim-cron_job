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
    var data = Poked.ToDictionary(kvp => $"{kvp.Key.x},{kvp.Key.y}", kvp => kvp.Value.Ticks);
    var yaml = Data.Serializer().Serialize(data);
    File.WriteAllText(FilePath, yaml);
  }
  public static void FromFile()
  {
    try
    {
      var data = Data.Read(FilePath, Data.Deserialize<Dictionary<string, long>>);
      Poked = data.ToDictionary(
        kvp =>
        {
          var split = kvp.Key.Split(',');
          return new Vector2i(int.Parse(split[0]), int.Parse(split[1]));
        },
        kvp => new DateTime(kvp.Value)
      );
      CronJob.Log.LogInfo($"Reloading {Poked.Count} zone pokes.");
    }
    catch (Exception e)
    {
      CronJob.Log.LogError(e.StackTrace);
    }
  }
  public static Dictionary<Vector2i, DateTime> Poked = new();

  public static void Poke(Vector2i zone)
  {
    if (CronManager.ZoneJobs.Count == 0) return;
    if (Poked.ContainsKey(zone)) CronManager.Execute(zone, Poked[zone]);
    else CronManager.Execute(zone, null);
    Poked[zone] = DateTime.UtcNow;
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
        var zone = new Vector2i(j, i);
        if (zs.IsZoneGenerated(zone))
          zones.Add(zone);
      }
    }
  }
  public static void Track()
  {
    HashSet<Vector2i> zones = new();
    if (!ZNet.instance.IsDedicated())
      TrackPeer(zones, ZNet.instance.GetReferencePosition());
    foreach (var peer in ZNet.instance.GetPeers())
      TrackPeer(zones, peer.GetRefPos());
    foreach (var zone in zones)
      Poke(zone);
  }
}
