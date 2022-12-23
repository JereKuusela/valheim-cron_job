# Cron Job

Allows executing commands automatically on the server.

Install on the server (modding [guide](https://youtu.be/WfvA5a5tNHo)).

# Usage

See [documentation](https://github.com/JereKuusela/valheim-cron_job/blob/main/README.md).

# Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-cron_job)

Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)

# Changelog

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

- v1.0
	- Initial release.
