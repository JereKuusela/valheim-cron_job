- v1.11
  - Adds a new file "cron_last.yaml" to track the last time job were checked.
  - This prevents running all jobs on server restart.
  - Adds a new field `useGameTime` to run jobs based on the in-game time.
  - Adds a new field `commands` to run multiple commands.
  - Adds new player related parameters to match Expand World Prefabs mod.
  - Fixes join job triggering on player respawn.

- v1.10
  - Fixed for the new update.

- v1.9
  - Adds support for changing the Discord Connector message type.
  - Adds a new setting "logSkipped" to allow disabling logging of skipped jobs.
  - Changes the default Discord Connector mesasge type from "Other" to "cronjob".

- v1.8
  - Fixed for the new update.

- v1.7
  - Fixes parameters not working for join jobs.

- v1.6
  - Adds a warning for zone jobs without any parameters.
  - Adds a new field `avoidPlayers` to not run zone jobs when players are nearby.
  - Adds support for seconds in the cron schedule.
  - Adds new fields `biomes`, `locations`, `objects` and `bannedObjects` to filter zone jobs.
  - Changes the parameter format from `$$` to `<>`. Old commands still work but cause a warning.
  - Removes the field `inactive` because it caused confusion and didn't work with multiple jobs per zone.

- v1.5
  - Fixed for the new update.

- v1.4
  - Adds a new field `log` to the jobs.

- v1.3
  - Updated for the new game version.

- v1.2
  - Adds auto update for missing fields to the cron.yaml.
  - Fixes loading messages being sent to the Discord connector.
  - Fixes errors when deleting yaml files.
  - Fixes emptry cron_track.yaml being created.

- v1.1
  - Adds support for join jobs.
  - Adds support for Discord Connector.
  - Adds support for time zones.
  - Adds new config options (timezone, interval, logJobs, logZone, logJoin, discordConnector).
  - Changes schedule and inactive conditions to be AND instead of OR (so both must apply instead of one of them).
  - Fixes error for some schedules.
