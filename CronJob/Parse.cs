using System;
using System.Collections.Generic;
using System.Linq;

namespace CronJob;

public class Parse
{
  public static string[] Split(string arg, bool removeEmpty = true, char split = ',') => arg.Split(split).Select(s => s.Trim()).Where(s => !removeEmpty || s != "").ToArray();

  public static HashSet<int> ToHashSet(string arg) => new(Split(arg).Select(s => s.GetStableHashCode()));
  public static HashSet<string> ToSet(string arg) => new(Split(arg));
  public static Heightmap.Biome ToBiomes(string biomeStr)
  {
    Heightmap.Biome result = 0;
    var biomes = Split(biomeStr);
    foreach (var biome in biomes)
    {
      if (Enum.TryParse(biome, true, out Heightmap.Biome number))
        result |= number;
      else
      {
        if (int.TryParse(biome, out var value)) result += value;
        else throw new InvalidOperationException($"Invalid biome {biome}.");
      }
    }
    return result;
  }

}