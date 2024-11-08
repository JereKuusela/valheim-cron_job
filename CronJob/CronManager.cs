using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using Cronos;
using HarmonyLib;

namespace CronJob;


[HarmonyPatch]
public class CronManager
{
  public static string FileName = "cron.yaml";
  public static string FilePath = Path.Combine(Paths.ConfigPath, FileName);

  public static List<CronGeneralJob> Jobs = [];
  public static List<CronZoneJob> ZoneJobs = [];
  public static List<CronJoinJob> JoinJobs = [];
  public static float Interval = 10f;
  public static bool LogJobs = true;
  public static bool LogZone = true;
  public static bool LogJoin = true;
  public static bool LogSkipped = true;
  public static string DiscordConnector = "CronJob";
  public static DateTime Previous = DateTime.UtcNow;
  public static DateTime PreviousGameTime = new(Year2000, DateTimeKind.Utc);
  public static TimeZoneInfo TimeZone = TimeZoneInfo.Utc;

  private static DateTime? Parse(string value, DateTime? next = null)
  {
    var format = value.Split(' ').Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
    CronExpression expression = CronExpression.Parse(value, format);
    return expression.GetNextOccurrence(next ?? DateTime.UtcNow, TimeZone);
  }
  private static readonly Random random = new();
  private static bool Roll(float? chance)
  {
    if (chance == null || chance >= 1f || chance == 0f) return true;
    return random.NextDouble() < chance;
  }
  private static readonly long Year2000 = new DateTime(2000, 1, 1).Ticks;
  public static void Execute()
  {
    var time = DateTime.UtcNow;
    DateTime gameTime = new(Year2000 + (long)(ZNet.instance.GetTimeSeconds() / EnvMan.instance.m_dayLengthSec * TimeSpan.TicksPerDay), DateTimeKind.Utc);
    foreach (var cron in Jobs)
    {
      var t = cron.UseGameTime ? gameTime : time;
      var p = cron.UseGameTime ? PreviousGameTime : Previous;
      // Not fully sure what happens if current time is before the previous time.
      // This can happen when setting the game time.
      if (t < p) continue;
      if (t < Parse(cron.Schedule, p)) continue;
      RunJob(cron.Commands, cron.Chance, cron.Log);
    }
    Previous = time;
    PreviousGameTime = gameTime;
  }
  private static void RunJob(string[] commands, float? chance, bool? log)
  {
    if (Roll(chance))
    {
      foreach (var cmd in commands)
      {
        Console.instance.TryRunCommand(cmd);
        if (log ?? LogJobs)
          Log($"Executing: {cmd}");
      }

    }
    else if (LogSkipped)
    {
      foreach (var cmd in commands)
      {
        if (log ?? LogJobs)
          Log($"Skipped: {cmd}");
      }
    }
  }
  private static void Log(string message)
  {
    CronJob.Log.LogInfo(message);
    if (DiscordConnector != "")
      DiscordHook.SendMessage(DiscordConnector, message);
  }
  public static bool Execute(Vector2i zone, bool hasPlayer, DateTime? previous)
  {
    var toRun = ZoneJobs.Where(cron => !cron.AvoidPlayers || !hasPlayer).ToList();
    if (toRun.Count == 0) return false;
    var time = DateTime.UtcNow;
    var zs = ZoneSystem.instance;
    var zm = ZDOMan.instance;
    var wg = WorldGenerator.instance;
    foreach (var cron in toRun)
    {
      if (time < Parse(cron.Schedule, previous)) continue;
      var pos = ZoneSystem.GetZonePos(zone);
      if (cron.Biomes != 0)
      {
        if ((wg.GetBiome(pos.x, pos.y) & cron.Biomes) == 0 &&
         (wg.GetBiome(pos.x + 32f, pos.y + 32f) & cron.Biomes) == 0 &&
         (wg.GetBiome(pos.x + 32f, pos.y - 32f) & cron.Biomes) == 0 &&
         (wg.GetBiome(pos.x - 32f, pos.y + 32f) & cron.Biomes) == 0 &&
         (wg.GetBiome(pos.x - 32f, pos.y + 32f) & cron.Biomes) == 0)
          continue;
      }
      if (cron.Locations.Count > 0)
      {
        if (!zs.m_locationInstances.TryGetValue(zone, out var location)) continue;
        if (!cron.Locations.Contains(location.m_location?.m_prefabName ?? "")) continue;
      }
      if (cron.Objects.Count > 0)
      {
        var sector = zm.SectorToIndex(zone);
        if (sector < 0 || sector >= zm.m_objectsBySector.Length) continue;
        var zdos = zm.m_objectsBySector[sector];
        if (zdos == null) continue;
        if (zdos.All(zdo => !cron.Objects.Contains(zdo.m_prefab))) continue;
      }
      if (cron.BannedObjects.Count > 0)
      {
        var sector = zm.SectorToIndex(zone);
        if (sector < 0 || sector >= zm.m_objectsBySector.Length) continue;
        var zdos = zm.m_objectsBySector[sector];
        if (zdos == null) continue;
        if (zdos.Any(zdo => cron.BannedObjects.Contains(zdo.m_prefab))) continue;
      }
      var commands = cron.Commands.Select(cmd => cmd
        .Replace("<i>", zone.x.ToString())
        .Replace("<j>", zone.y.ToString())
        .Replace("<x>", pos.x.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("<y>", pos.y.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("<z>", pos.z.ToString("F2", CultureInfo.InvariantCulture)));
      RunJob(commands.ToArray(), cron.Chance, cron.Log);
    }
    return true;
  }

  private static HashSet<ZNetPeer> HandledPeers = [];
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_CharacterID)), HarmonyPostfix]
  static void AddPeer(ZNet __instance, ZRpc rpc, ZDOID characterID)
  {
    if (!__instance.IsServer()) return;
    if (characterID.IsNone()) return;
    var peer = __instance.GetPeer(rpc);
    if (HandledPeers.Contains(peer)) return;
    HandledPeers.Add(peer);
    foreach (var cron in JoinJobs)
    {
      var cmds = cron.Commands.Select(cmd => cmd
        .Replace("<name>", peer.m_playerName)
        .Replace("<pname>", peer.m_playerName)
        .Replace("<first>", peer.m_playerName.Split(' ')[0])
        .Replace("<id>", peer.m_characterID.UserID.ToString())
        .Replace("<pid>", peer.m_rpc.GetSocket().GetHostName())
        .Replace("<pchar>", peer.m_characterID.UserID.ToString())
        .Replace("<x>", peer.m_refPos.x.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("<y>", peer.m_refPos.y.ToString("F2", CultureInfo.InvariantCulture))
        .Replace("<z>", peer.m_refPos.z.ToString("F2", CultureInfo.InvariantCulture)));
      RunJob(cmds.ToArray(), cron.Chance, cron.Log);
    }

  }


  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect)), HarmonyPostfix]
  static void Disconnect(ZNetPeer peer)
  {
    HandledPeers.Remove(peer);
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
    if (!File.Exists(FilePath))
      File.WriteAllText(FilePath, Data.Serializer().Serialize(new CronData()));

    try
    {
      var data = Data.Read<CronData>(FilePath);
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
      LogSkipped = data.logSkipped;
      data.join.ForEach(ReplaceParameters);
      data.zone.ForEach(ReplaceParameters);
      data.jobs.ForEach(ReplaceParameters);
      data.zone.ForEach(VerifyParameterExists);
      data.zone.ForEach(VerifyInactiveIsNull);
      data.jobs.ForEach(VerifyInactiveIsNull);
      data.jobs = SkipWithoutSchedule(data.jobs);
      data.zone = SkipWithoutSchedule(data.zone);

      DiscordConnector = data.discordConnector;
      if (DiscordConnector == "true")
        DiscordConnector = "cronjob";
      else if (DiscordConnector == "false")
        DiscordConnector = "";
      Jobs = data.jobs.Select(s => new CronGeneralJob(s)).ToList();
      CronJob.Log.LogInfo($"Reloading {Jobs.Count} cron jobs.");
      ZoneJobs = data.zone.Select(s => new CronZoneJob(s)).ToList();
      CronJob.Log.LogInfo($"Reloading {ZoneJobs.Count} zone cron jobs.");
      JoinJobs = data.join.Select(s => new CronJoinJob(s)).ToList();
      CronJob.Log.LogInfo($"Reloading {JoinJobs.Count} join jobs.");
    }
    catch (Exception e)
    {
      CronJob.Log.LogError(e.StackTrace);
    }
  }
  private static void ReplaceParameters(CronEntryData entry)
  {
    if (!entry.command.Contains("$$")) return;
    CronJob.Log.LogWarning($"$$ is deprecated, use <> instead. Command: {entry.command}");
    entry.command = entry.command
      .Replace("$$i", "<i>")
      .Replace("$$I", "<i>")
      .Replace("$$j", "<j>")
      .Replace("$$J", "<j>")
      .Replace("$$x", "<x>")
      .Replace("$$X", "<x>")
      .Replace("$$y", "<y>")
      .Replace("$$Y", "<y>")
      .Replace("$$z", "<z>")
      .Replace("$$Z", "<>>")
      .Replace("$$id", "<id>")
      .Replace("$$ID", "<id>")
      .Replace("$$name", "<name>")
      .Replace("$$NAME", "<name>")
      .Replace("$$first", "<first>")
      .Replace("$$FIRST", "<first>");
  }
  private static void VerifyParameterExists(CronEntryData entry)
  {
    if (entry.command.Contains("<") || entry.command.Contains(">")) return;
    CronJob.Log.LogWarning($"Command {entry.command} does not contain parameters, should this be general job instead?");
  }
  private static void VerifyInactiveIsNull(CronEntryData entry)
  {
    if (entry.inactive == null) return;
    CronJob.Log.LogWarning($"Inactive time is deprecated and can be removed. Command: {entry.command}");
  }
  private static List<CronEntryData> SkipWithoutSchedule(List<CronEntryData> entries) =>
    entries.Where(entry =>
    {
      var valid = entry.schedule != null && entry.schedule != "";
      if (!valid)
        CronJob.Log.LogWarning($"Command {entry.command} does not have a schedule, skipping.");
      return valid;
    }).ToList();

  public static void SetupWatcher()
  {
    Data.SetupWatcher(FileName, FromFile);
  }
}
