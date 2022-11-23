using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Cronos;
using HarmonyLib;

namespace CronJob;

public class CronData
{
  public List<CronEntry> jobs = new();
  public List<CronEntry> zone = new();
}
public class CronEntry
{
  public string command = "";
  public string schedule = "";
  public float inactive = 0f;
  public float chance = 1f;
  public DateTime? next = null;
}

[HarmonyPatch]
public class CronManager
{
  public static string FileName = "cron.yaml";
  public static string FilePath = Path.Combine(Paths.ConfigPath, FileName);

  public static List<CronEntry> Jobs = new();
  public static List<CronEntry> ZoneJobs = new();

  private static DateTime? Parse(string value, DateTime? next = null)
  {
    CronExpression expression = CronExpression.Parse(value);
    return expression.GetNextOccurrence(next ?? DateTime.UtcNow);
  }
  private static System.Random random = new();
  private static bool Roll(float chance)
  {
    if (chance >= 1f) return true;
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
          CronJob.Log.LogInfo($"Executing: {cron.command}");
        }
        else
        {
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
      var run = false;
      if (cron.schedule != "")
        run = Parse(cron.schedule, previous) <= time;
      if (cron.inactive != 0f && previous.HasValue)
        run = (time - previous.Value).TotalMinutes >= cron.inactive;
      if (!run) continue;
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
        CronJob.Log.LogInfo($"Executing: {cmd}");
      }
      else
      {
        CronJob.Log.LogInfo($"Skipped: {cmd}");
      }
    }
  }

  [HarmonyPatch(typeof(Chat), nameof(Chat.Awake)), HarmonyPostfix]
  public static void ChatAwake()
  {
    if (File.Exists(FilePath))
      FromFile();
    else
    {
      var yaml = Data.Serializer().Serialize(new CronData());
      File.WriteAllText(FilePath, yaml);
    }
  }
  public static void FromFile()
  {
    try
    {
      var data = Data.Read(FilePath, Data.Deserialize<CronData>);
      Jobs = data.jobs;
      foreach (var cron in Jobs)
        cron.next = Parse(cron.schedule);
      CronJob.Log.LogInfo($"Reloading {Jobs.Count} cron jobs.");
      ZoneJobs = data.zone;
      CronJob.Log.LogInfo($"Reloading {ZoneJobs.Count} zone cron jobs.");
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
