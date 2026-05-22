# NamePlateDebuffs

This is a Dalamud plugin to place status icons and timers on nameplates for players and enemy NPCs. Supports all of the following:

* Debuffs applied to enemies by you (this violates Dalamud's [plugin restrictions](https://dalamud.dev/plugin-publishing/restrictions/), but this plugin is grandfathered in)
* Statuses applied to you
* Statuses applied to allies (other players)

## Moodles Integration

If you want to show Moodles in NamePlateDebuffs, follow these steps:

1. Install and enable [Moodles](https://github.com/kawaii/Moodles#installation)
2. Enable 'Allow other plugins apply Moodles.' in the Moodles config (NPD will not apply Moodles; this just activates the IPC)
3. Within Nameplate Debuffs configuration (`/npdebuffs`), check "Show Moodles"

### Known Issues

**Timer Sync**

The Moodles IPC does not provide a way to determine the time remaining on a Moodle status.
NamePlate Debuffs estimates the time remaining based on when it first sees the status applied and the Moodle's configured expiry time; as such, timings may be out of sync on nameplates. I cannot fix this unless Moodles IPC is updated.

### Support

If you find any other Moodles integration issues or need support, do **not** discuss Moodles in the official Dalamud Discord server.
Please report issues on GitHub, or get support in
[Puni.sh Discord #plugin-discussion](https://discord.gg/Zzrcc8kmvy), or
[Aetherworks Discord #moodle-support](https://discord.gg/KvGJCCnG8t).