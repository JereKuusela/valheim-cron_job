using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace CronJob;
public static class DiscordHook
{
  public const string GUID = "games.nwest.valheim.discordconnector";
  private static Assembly? assembly;
  public static void SendMessage(string message)
  {
    if (assembly == null) return;
    var type = assembly.GetType("DiscordConnector.DiscordApi");
    if (type == null) return;
    var method = AccessTools.Method(type, "SendMessage", new Type[] { typeof(string) });
    if (method == null) return;
    method.Invoke(null, new object[] { message });
  }
  public static void Init()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    assembly = info.Instance.GetType().Assembly;
  }
}
