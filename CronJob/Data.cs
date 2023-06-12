
using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CronJob;

public class Data : MonoBehaviour
{
  public static bool SkipWatch = false;
  public static void SetupWatcher(string pattern, Action action)
  {
    var skippableAction = () =>
    {
      if (SkipWatch)
      {
        SkipWatch = false;
        return;
      }
      action();
    };
    FileSystemWatcher watcher = new(Paths.ConfigPath, pattern);
    watcher.Created += (s, e) => skippableAction();
    watcher.Changed += (s, e) => skippableAction();
    watcher.Renamed += (s, e) => skippableAction();
    watcher.Deleted += (s, e) => skippableAction();
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  public static IDeserializer Deserializer() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
  public static ISerializer Serializer() => new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).WithIndentedSequences().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).DisableAliases().Build();

  public static T Deserialize<T>(string raw) where T : new() => Deserializer().Deserialize<T>(raw);
  public static string Serialize<T>(T data) where T : new() => Serializer().Serialize(data ?? new());

  private static void AutoMigrate<T>(string file, string original, T data) where T : new()
  {
    if (data == null) return;
    var serialized = Serialize(data);
    if (serialized == original) return;
    CronJob.Log.LogInfo($"Adding missing fields to {Path.GetFileName(file)}.");
    SkipWatch = true;
    File.WriteAllText(file, serialized);
  }
  public static T Read<T>(string file, bool autoMigrate) where T : new()
  {
    var yaml = File.ReadAllText(file);
    var data = Deserialize<T>(yaml);
    if (autoMigrate)
      AutoMigrate(file, yaml, data);
    return data;
  }
}
