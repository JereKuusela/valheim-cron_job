# Cron Job

Allows executing commands automatically on the server.

Install on the server (modding [guide](https://youtu.be/WfvA5a5tNHo)).

## Usage

After starting the server, `cron.yaml` and `cron_track.yaml` files are created in the config folder.

`cron_track.yaml` stores the last time a zone was visited. Editing this file is not recommended.

`cron.yaml` contains the jobs. Jobs are checked every 10 seconds. To understand cron schedules better, check out [crontab.guru](https://crontab.guru/).

- timezone: Time zone of the cron schedules. Default is UTC.
- interval: How often jobs are checked. Default is 10 seconds.
- jobs: General jobs intended for server wide commands. For example, sending a message to all players or server wide location/vegetation/zone resets.
- zone: Zone based jobs intended for zone specific commands. For example, resetting a single zone.
  - This will be run for each zone over time. The command is executed multiple times with different parameters.
  - Affected zones can be filtered by biomes, locations and objects. This is faster than filtering with Upgrade World mod.
- join: Jobs that are executed when a player joins the server.
- logJobs: Whether to log general jobs. Default is true.
- logZone: Whether to log zone based jobs. Default is true.
- logJoin: Whether to log join jobs. Default is true.
- logSkipped: Whether to log skipped jobs. Default is true.
- discordConnector: [Discord Connector](https://discord-connector.valheim.games.nwest.one/config/webhook.events.html) logging.
  - Default value true sets the message type to "cronjob".
  - Other message types can also be used.
  - Value false disables the Discord Connector logging.

## Jobs

- command: Command to execute.
- commands: List of commands to execute.
- schedule: Cron schedule. If schedule starts with *, you must use "" around the it.
- useGameTime: If true, the schedule is based on the in-game time. Default is false.
  - In game time starts from year 2000.
- chance: Chance of executing the command. Default is 1 (100%).
- log: Whether to log the job. If missing, `logJobs` value is used.

## Zone jobs

- command: Command to execute.
- commands: List of commands to execute.
- schedule: Cron schedule. If schedule starts with *, you must use "" around the it.
- chance: Chance of executing the command. Default is 1 (100%).
- log: Whether to log the job. If missing, `logZone` value is used.
- avoidPlayers: If true, the job is skipped if there are players in the zone. Default is false.
  - The job is tried again later after `interval`.
  - However if another job runs on the zone, all pending jobs are removed (technical reasons).
  - To not try again, add Player to `bannedObjects` instead.
- biomes: List of valid biomes separated by `,`.
  - Checked on zone center and corners.
- locations: List of valid locations separated by `,`.
- objects: List of valid objects separated by `,`.
  - If none is found in the zone, the job is skipped.
- bannedObjects: List of banned objects separated by `,`.
  - If any is found in the zone, the job is skipped.

Command parameters:

- `<i>`: X index of the targeted zone. For example 2.
- `<j>`: Z index of the targeted zone. For example -2.
- `<x>`: X coordinate of the targeted zone. For example 2.31.
- `<y>`: Y coordinate of the targeted zone (always 0).
- `<z>`: Z coordinate of the targeted zone. For example -1232.21.

## Join jobs

- command: Command to execute.
- commands: List of commands to execute.
- chance: Chance of executing the command. Default is 1 (100%).
- log: Whether to log the job. If missing, `logJoin` value is used.

Command parameters:

- `<name>`: Name of the player. For example "John Doe".
- `<first>`: First name of the player. For example "John" in "John Doe".
- `<id>`: Player id. For example 12335.
- `<x>`: X coordinate of the player position. For example 2.31.
- `<y>`: Y coordinate of the player position. For example 30.64.
- `<z>`: Z coordinate of the player position. For example -1232.21.

# Examples

Recommended mods:

- [Server Devcommands](https://valheim.thunderstore.io/package/JereKuusela/Server_devcommands/) for `broadcast` and `say` commands.
- [Upgrade World](https://valheim.thunderstore.io/package/JereKuusela/Upgrade_World/) for reseting zones.

## Weekly world reset

```yaml
timezone: UTC
# Jobs checked every minute.
interval: 60
jobs:
  - command: broadcast center <color=orange>WARNING - WORLD RESET IN 5 MINUTES - PLEASE LOG OUT</color>
    # At 02:55 on Monday.
    schedule: "55 2 * * 1"
  - command: broadcast center <color=orange>WARNING - WORLD RESET IN 4 MINUTES - PLEASE LOG OUT</color>
    schedule: "56 2 * * 1"
  - command: broadcast center <color=orange>WARNING - WORLD RESET IN 3 MINUTES - PLEASE LOG OUT</color>
    schedule: "57 2 * * 1"
  - command: broadcast center <color=orange>WARNING - WORLD RESET IN 2 MINUTES - PLEASE LOG OUT</color>
    schedule: "58 2 * * 1"
  - commands:
    - broadcast center <color=orange>WARNING - WORLD RESET IN 1 MINUTE - PLEASE LOG OUT</color>
    - save
    schedule: "59 2 * * 1"
    # Resets the world.
  - command: zones_reset start
    schedule: "0 3 * * 1"
```

## Weekly reboot

Note: This doesn't actually reboot the server. That must be configured from the server host.

```yaml
timezone: UTC
# Jobs checked every minute.
interval: 60
jobs:
  - command: broadcast center <color=orange>WARNING - REBOOT IN 5 MINUTES - PLEASE LOG OUT</color>
    # At 02:55 on Monday.
    schedule: "55 2 * * 1"
  - command: broadcast center <color=orange>WARNING - REBOOT IN 4 MINUTES - PLEASE LOG OUT</color>
    schedule: "56 2 * * 1"
  - command: broadcast center <color=orange>WARNING - REBOOT IN 3 MINUTES - PLEASE LOG OUT</color>
    schedule: "57 2 * * 1"
  - command: broadcast center <color=orange>WARNING - REBOOT IN 2 MINUTES - PLEASE LOG OUT</color>
    schedule: "58 2 * * 1"
  - commands: 
    - broadcast center <color=orange>WARNING - REBOOT IN 1 MINUTE - PLEASE LOG OUT</color>
    - save
    schedule: "59 2 * * 1"
    # Hosting service configured to reboot at 03:00 on Monday.
```

## Gradually resetting dungeons and copper every month

After the 15th day, dungeons and copper reset the first time the zone is visited.

Copper reset also includes terrain reset within 30 meters.

```yaml
zone:
  - command: vegetation_reset rock4_copper terrain=30 zone=<i>,<j> start
    # At minute 0 on every 15th day-of-month.
    schedule: "0 * */15 * *"
    bannedObjects: rock4_copper
    biomes: BlackForest

  - command: locations_reset Crypt2,Crypt3,Crypt4,SunkenCrypt4 zone=<i>,<j> start
    schedule: "0 * */15 * *"
    # Prevents resetting dungeons while players are currently there.
    # Copper job causes this to be tried only once.
    avoidPlayers: true
    locations: Crypt2,Crypt3,Crypt4,SunkenCrypt4
```

## Player greeting

Never logged even if `logJoin` is true.

```yaml
join:
  - command: say Welcome to the server <name>!
    log: false
```

## Player surprise

1% chance to start the "Eikthyr rallies the creatures of the forest" event when a player joins.

```yaml
join:
  - command: event army_eikthyr <x> <z>
    chance: 0.01
```
