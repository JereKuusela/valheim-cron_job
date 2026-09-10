- v1.14
  - Fixes for the new game version.

- v1.13
  - Adds new fields `globalKeys` and `bannedGlobalKeys` to allow skipping jobs based on global keys.

- v1.12
  - Adds dependency to YamlDotnet package.
  - Fixed to work with the new Discord Connector mod update.

- v1.11
  - Adds a new file "cron_last.yaml" to track the last time job were checked.
  - This prevents running all jobs on server restart.
  - Adds a new field `useGameTime` to run jobs based on the in-game time.
  - Adds a new field `commands` to run multiple commands.
  - Adds new player related parameters to match Expand World Prefabs mod.
  - Fixes join job triggering on player respawn.

- v1.10
  - Fixed for the new update.
