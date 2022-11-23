# Cron Job

Allows executing commands automatically on the server.

Install on the server (modding [guide](https://youtu.be/WfvA5a5tNHo)).

# Usage

After starting the server, `cron.yaml` and `cron_track.yaml` files are created in the config folder.

`cron_track.yaml` stores the last time a zone was visited. Editing this file is not recommended.

`cron.yaml` contains the jobs. Jobs are checked every 10 seconds. To understand cron schedules better, check out [crontab.guru](https://crontab.guru/).

Field `jobs` includes general jobs. For example:

```
jobs:
  - command: broadcast center Hello!
    chance: 0.5
    schedule: "0 9 * * *" 
```
would have a 50% chance of sending a message every day at 9:00 (requires Server Devcommands mod). Times are always in UTC so convert your time zone to that.

Field `zone` includes zone based jobs. For example:

```
zone:
  - command: locations_reset Crypt2 zone=$$i,$$j start
    inactive: 60
  - command: vegetation_reset rock4_copper zone=$$i,$$j start
    schedule: "0 9 * * *" 
```
would reset Burial Grounds if the zone hasn't been visited for 1 hour and reset Copper deposits every day at 9:00 (requires Upgrade World mod).

Following parameters are available:
- $$i: X index of the targeted zone.
- $$j: Z index of the targeted zone.
- $$x: X coordinate of the targeted zone.
- $$y: Y coordinate of the targeted zone (always 0).
- $$z: Z coordinate of the targeted zone.

# Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-cron-job)

Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)

# Changelog

- v1.0
	- Initial release.
