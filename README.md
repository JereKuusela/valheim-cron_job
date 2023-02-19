# Cron Job

Allows executing commands automatically on the server.

Install on the server (modding [guide](https://youtu.be/WfvA5a5tNHo)).

# Usage

After starting the server, `cron.yaml` and `cron_track.yaml` files are created in the config folder.

`cron_track.yaml` stores the last time a zone was visited. Editing this file is not recommended.

`cron.yaml` contains the jobs. Jobs are checked every 10 seconds. To understand cron schedules better, check out [crontab.guru](https://crontab.guru/).

- timezone: Time zone of the cron schedules. Default is UTC.
- interval: How often jobs are checked. Default is 10 seconds.
- jobs: General jobs intended for server wide commands. For example, sending a message to all players.
- zone: Zone based jobs intended for zone specific commands. For example, resetting a zone.
- join: Jobs that are executed when a player joins the server.
- logJobs: Whether to log general jobs. Default is true.
- logZone: Whether to log zone based jobs. Default is true.
- logJoin: Whether to log join jobs. Default is true.
- discordConnector: Whether to send data to [Discord Connector](https://valheim.thunderstore.io/package/nwesterhausen/DiscordConnector/). Default is true.

## Jobs

- command: Command to execute.
- chance: Chance of executing the command. Default is 1 (100%).
- schedule: Cron schedule. Default is never.

## Zone jobs

- command: Command to execute.
- chance: Chance of executing the command. Default is 1 (100%).
- schedule: Cron schedule. Default is never.
- inactive: How many minutes the zone must have been inactive. Default is 0 (always).

Command parameters: 
- $$i: X index of the targeted zone. For example 2.
- $$j: Z index of the targeted zone. For example -2.
- $$x: X coordinate of the targeted zone. For example 2.31.
- $$y: Y coordinate of the targeted zone (always 0).
- $$z: Z coordinate of the targeted zone. For example -1232.21.

## Join jobs

- command: Command to execute.
- chance: Chance of executing the command. Default is 1 (100%).

Command parameters: 
- $$name: Name of the player. For example "John Doe".
- $$first: First name of the player. For example "John" in "John Doe".
- $$id: Player id. For example 12335.
- $$x: X coordinate of the player position. For example 2.31.
- $$y: Y coordinate of the player position. For example 30.64.
- $$z: Z coordinate of the player position. For example -1232.21.

# Examples

Recommended mods:
- [Server Devcommands](https://valheim.thunderstore.io/package/JereKuusela/Server_devcommands/) for `broadcast` and `say` commands.
- [Upgrade World](https://valheim.thunderstore.io/package/JereKuusela/Upgrade_World/) for reseting zones.


## Weekly world reset / reboot announcement

Note: This doesn't actually reboot the server. That must be configured from the server host.

```
timezone: UTC
# Jobs checked every minute.
interval: 60
jobs: 
  - command: broadcast center <color=orange>WARNING - AUTOMATIC RESTART IN 5 MINUTES - PLEASE LOG OUT</color>
    # At 02:55 on Monday.
    schedule: "55 2 * * 1"
  - command: broadcast center <color=orange>WARNING - AUTOMATIC RESTART IN 4 MINUTES - PLEASE LOG OUT</color>
    schedule: "56 2 * * 1"
  - command: broadcast center <color=orange>WARNING - AUTOMATIC RESTART IN 3 MINUTES - PLEASE LOG OUT</color>
    schedule: "57 2 * * 1"
  - command: broadcast center <color=orange>WARNING - AUTOMATIC RESTART IN 2 MINUTES - PLEASE LOG OUT</color>
    schedule: "58 2 * * 1"
  - command: save
    schedule: "58 2 * * 1"
    # Resets the world. Remove this if you only need a warning for the reboot.
  - command: zones_reset start
    schedule: "59 2 * * 1"
  - command: broadcast center <color=orange>WARNING - AUTOMATIC RESTART IN 1 MINUTE - PLEASE LOG OUT</color>
    schedule: "59 2 * * 1"
```

## Gradually resetting dungeons and copper after 12 hours of inactivity

Dungeons reset after the zone hasn't been visited for 12 hours.

Copper reset also includes terrain reset within 30 meters.

```
zone:
  - command: locations_reset Crypt2,Crypt3,Crypt4,SunkenCrypt4 zone=$$i,$$j start
    inactive: 720
  - command: vegetation_reset rock4_copper terrain=30 zone=$$i,$$j start
    inactive: 720
```

## Gradually resetting dungeons every month

After the 15th day, dungeons reset the first time the zone is visited.

```
zone:
  - command: locations_reset Crypt2,Crypt3,Crypt4,SunkenCrypt4 zone=$$i,$$j start
    # At minute 0 on every 15th day-of-month.
    schedule: "0 * */15 * *"
    # Prevents resetting dungeons while players are currently there.
    inactive: 10
```

## Player greeting

```
join:
  - command: say Welcome to the server $$name!
```

## Player surprise

1% chance to start the "Eikthyr rallies the creatures of the forest" event when a player joins.

```
join:
  - command: event army_eikthyr $$x $$z
    chance: 0.01
```

