using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using BepInEx;
using Cronos;
using HarmonyLib;

namespace CronJob;

public class CronData
{
  [DefaultValue("UTC")]
  public string timezone = "UTC";
  [DefaultValue(10f)]
  public float interval = 10f;
  public List<CronEntry> jobs = new();
  public List<CronEntry> zone = new();
  public List<CronEntry> join = new();
  [DefaultValue(true)]
  public bool logJobs = true;
  [DefaultValue(true)]
  public bool logZone = true;
  [DefaultValue(true)]
  public bool logJoin = true;
}
public class CronEntry
{
  [DefaultValue("")]
  public string command = "";
  [DefaultValue("")]
  public string schedule = "";
  [DefaultValue(0f)]
  public float inactive = 0f;
  [DefaultValue(1f)]
  public float chance = 1f;
  [DefaultValue(null)]
  public DateTime? next = null;
}

[HarmonyPatch]
public class CronManager
{
  public static string FileName = "cron.yaml";
  public static string FilePath = Path.Combine(Paths.ConfigPath, FileName);

  public static List<CronEntry> Jobs = new();
  public static List<CronEntry> ZoneJobs = new();
  public static List<CronEntry> JoinJobs = new();
  public static float Interval = 10f;
  public static bool LogJobs = true;
  public static bool LogZone = true;
  public static bool LogJoin = true;
  public static TimeZoneInfo TimeZone = TimeZoneInfo.Utc;

  private static DateTime? Parse(string value, DateTime? next = null)
  {
    CronExpression expression = CronExpression.Parse(value);
    return expression.GetNextOccurrence(next ?? DateTime.UtcNow, TimeZone);
  }
  private static System.Random random = new();
  private static bool Roll(float chance)
  {
    if (chance >= 1f || chance == 0f) return true;
    return random.NextDouble() < chance;
  }
  public static void Execute()
  {
    var time = DateTime.UtcNow;
    foreach (var cron in Jobs)
    {
      if (cron.next <= time)
      {
        if (Roll(cron.chance))
        {
          Console.instance.TryRunCommand(cron.command);
          if (LogJobs)
            CronJob.Log.LogInfo($"Executing: {cron.command}");
        }
        else
        {
          if (LogJobs)
            CronJob.Log.LogInfo($"Skipped: {cron.command}");
        }
        cron.next = Parse(cron.schedule);
      }
    }

  }

  public static void Execute(Vector2i zone, DateTime? previous)
  {
    var time = DateTime.UtcNow;
    foreach (var cron in ZoneJobs)
    {
      bool? run = null;
      if (cron.schedule != "")
        run = Parse(cron.schedule, previous) <= time;
      if (run == false) continue;
      if (cron.inactive != 0f && previous.HasValue)
        run = (time - previous.Value).TotalMinutes >= cron.inactive;
      if (run != true) continue;
      var pos = ZoneSystem.instance.GetZonePos(zone);
      var cmd = cron.command
        .Replace("$$i", zone.x.ToString())
        .Replace("$$I", zone.x.ToString())
        .Replace("$$j", zone.y.ToString())
        .Replace("$$J", zone.y.ToString())
        .Replace("$$x", pos.x.ToString())
        .Replace("$$X", pos.x.ToString())
        .Replace("$$y", pos.y.ToString())
        .Replace("$$Y", pos.y.ToString())
        .Replace("$$z", pos.z.ToString())
        .Replace("$$Z", pos.z.ToString());
      if (Roll(cron.chance))
      {
        Console.instance.TryRunCommand(cmd);
        if (LogZone)
          CronJob.Log.LogInfo($"Executing: {cmd}");
      }
      else
      {
        if (LogZone)
          CronJob.Log.LogInfo($"Skipped: {cmd}");
      }
    }
  }


  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_CharacterID)), HarmonyPostfix]
  static void AddPeer(ZNet __instance, ZRpc rpc)
  {
    if (!__instance.IsServer()) return;
    var peer = __instance.GetPeer(rpc);
    foreach (var cron in JoinJobs)
    {
      var cmd = cron.command
        .Replace("$$name", peer.m_playerName)
        .Replace("$$NAME", peer.m_playerName)
        .Replace("$$first", peer.m_playerName.Split(' ')[0])
        .Replace("$$FIRST", peer.m_playerName.Split(' ')[0])
        .Replace("$$id", peer.m_characterID.m_userID.ToString())
        .Replace("$$ID", peer.m_characterID.m_userID.ToString())
        .Replace("$$x", peer.m_refPos.x.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("$$X", peer.m_refPos.x.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("$$y", peer.m_refPos.y.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("$$Y", peer.m_refPos.y.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("$$z", peer.m_refPos.z.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("$$Z", peer.m_refPos.z.ToString("F2", CultureInfo.InvariantCulture));
      if (Roll(cron.chance))
      {
        Console.instance.TryRunCommand(cmd);
        if (LogJoin)
          CronJob.Log.LogInfo($"Executing: {cmd}");
      }
      else
      {
        if (LogJoin)
          CronJob.Log.LogInfo($"Skipped: {cmd}");
      }
    }

  }

  [HarmonyPatch(typeof(Chat), nameof(Chat.Awake)), HarmonyPostfix]
  static void ChatAwake()
  {
    if (File.Exists(FilePath))
      FromFile();
    else
    {
      var yaml = Data.Serializer().Serialize(new CronData());
      File.WriteAllText(FilePath, yaml);
    }
  }
  private static bool ParseTimeZone(string timezone)
  {
    timezone = timezone.ToLower();
    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
    {
      if (tz.Id.ToLower() == timezone || tz.DisplayName.ToLower() == timezone)
      {
        TimeZone = tz;
        return true;
      }
    }
    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
    {
      if (tz.Id.ToLower().Contains(timezone) || tz.DisplayName.ToLower().Contains(timezone))
      {
        TimeZone = tz;
        return true;
      }
    }
    return false;
  }
  public static void FromFile()
  {
    try
    {
      var data = Data.Read(FilePath, Data.Deserialize<CronData>);
      Interval = data.interval;
      if (ParseTimeZone(data.timezone))
        CronJob.Log.LogInfo($"Selected time zone {TimeZone.Id} / {TimeZone.DisplayName}.");
      else
      {
        CronJob.Log.LogWarning($"Time zone {data.timezone} not found, using UTC. Possible time zones are:");
        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
          CronJob.Log.LogWarning($"{tz.Id} / {tz.DisplayName}");
      }
      LogJobs = data.logJobs;
      LogZone = data.logZone;
      LogJoin = data.logJoin;
      Jobs = data.jobs;
      foreach (var cron in Jobs)
        cron.next = Parse(cron.schedule);
      CronJob.Log.LogInfo($"Reloading {Jobs.Count} cron jobs.");
      ZoneJobs = data.zone;
      CronJob.Log.LogInfo($"Reloading {ZoneJobs.Count} zone cron jobs.");
      JoinJobs = data.join;
      CronJob.Log.LogInfo($"Reloading {JoinJobs.Count} join jobs.");
    }
    catch (Exception e)
    {
      CronJob.Log.LogError(e.StackTrace);
    }
  }
  public static void SetupWatcher()
  {
    Data.SetupWatcher(FileName, FromFile);
  }
}
