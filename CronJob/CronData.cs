
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CronJob;

public class CronData
{
  [DefaultValue("UTC")]
  public string timezone = "UTC";
  [DefaultValue(10f)]
  public float interval = 10f;
  public List<CronEntryData> jobs = [];
  public List<CronEntryData> zone = [];
  public List<CronEntryData> join = [];
  [DefaultValue(true)]
  public bool logJobs = true;
  [DefaultValue(true)]
  public bool logZone = true;
  [DefaultValue(true)]
  public bool logJoin = true;
  [DefaultValue(true)]
  public bool discordConnector = true;
}

public class CronEntryData
{
  [DefaultValue("")]
  public string command = "";
  [DefaultValue("")]
  public string schedule = "";
  [DefaultValue(null)]
  public float? inactive;
  [DefaultValue(null)]
  public float? chance;
  [DefaultValue(false)]
  public bool avoidPlayers = false;
  [DefaultValue(null)]
  public bool? log;
  [DefaultValue("")]
  public string biomes = "";
  [DefaultValue("")]
  public string locations = "";
  [DefaultValue("")]
  public string objects = "";
  [DefaultValue("")]
  public string bannedObjects = "";
}

public class CronGeneralJob(CronEntryData data) : CronBaseJob(data)
{
  public string Schedule = data.schedule;
}
public class CronZoneJob(CronEntryData data) : CronBaseJob(data)
{
  public string Schedule = data.schedule;
  public bool AvoidPlayers = data.avoidPlayers;
  public Heightmap.Biome Biomes = Parse.ToBiomes(data.biomes);
  public HashSet<string> Locations = Parse.ToSet(data.locations);
  public HashSet<int> Objects = Parse.ToHashSet(data.objects);
  public HashSet<int> BannedObjects = Parse.ToHashSet(data.bannedObjects);
}
public class CronJoinJob(CronEntryData data) : CronBaseJob(data)
{
}
public abstract class CronBaseJob(CronEntryData data)
{
  public string Command = data.command;
  public float? Chance = data.chance;
  public bool? Log = data.log;
}
