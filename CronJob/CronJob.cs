using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CronJob;
[BepInPlugin(GUID, NAME, VERSION)]
public class CronJob : BaseUnityPlugin
{
  public const string GUID = "cron_job";
  public const string NAME = "Cron Job";
  public const string VERSION = "1.0";
  private static ManualLogSource? Logs;
  public static ManualLogSource Log => Logs!;
  public void Awake()
  {
    Logs = Logger;
    new Harmony(GUID).PatchAll();
  }
  public void Start()
  {
    CronManager.SetupWatcher();
  }
  private float timer = 0f;
  public void LateUpdate()
  {
    if (ZNet.instance && ZNet.instance.IsServer())
    {
      timer -= Time.deltaTime;
      if (timer <= 0f)
      {
        timer = 10f;
        CronManager.Execute();
        TrackManager.Track();
      }
    }
  }
}
