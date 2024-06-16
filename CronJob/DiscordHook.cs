using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace CronJob;
public static class DiscordHook
{
  public const string GUID = "games.nwest.valheim.discordconnector";
  private static Assembly? assembly;
  private static MethodInfo? sendMessage;
  private static Type? eventType;
  private static Dictionary<string, object> messageTypes = [];
  public static void SendMessage(string type, string message)
  {
    if (sendMessage == null || eventType == null) return;
    if (!messageTypes.ContainsKey(type))
    {
      try
      {
        var messageType = Enum.Parse(eventType, type, true);
        messageTypes[type] = messageType;
      }
      catch
      {
        CronJob.Log.LogError($"Invalid message type {type}");
        return;
      }
    }
    sendMessage.Invoke(null, [messageTypes[type], message]);
  }
  public static void Init()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    assembly = info.Instance.GetType().Assembly;

    eventType = assembly.GetType("DiscordConnector.Webhook+Event");
    if (eventType == null)
    {
      CronJob.Log.LogError("Failed to get DiscordConnector.WebHook+Event");
      return;
    }
    var type = assembly.GetType("DiscordConnector.DiscordApi");
    if (type == null)
    {
      CronJob.Log.LogError("Failed to get DiscordConnector.DiscordApi");
      return;
    }
    sendMessage = AccessTools.Method(type, "SendMessage", [eventType, typeof(string)]);
    if (sendMessage == null)
    {
      CronJob.Log.LogError("Failed to get DiscordConnector.DiscordApi.SendMessage");
      return;
    }
    CronJob.Log.LogInfo("DiscordConnector initialized");
  }
}
