# Backup Landing Burn Engines

A BepInEx mod for Starship Factory.

If a center engine fails during ascent, one of the inner-ring engines
automatically takes over for it matching its throttle for the rest of
the flight, through staging, boostback, and the landing burn.

Without this mod, a failed center engine during the final landing burn
means going in on 2 engines instead of 3. That's often the difference
between a clean catch and a hard splashdown, or not making it back at
all.

## What it does

- Detects a failed center engine and assigns it a specific backup engine
  from the inner ring.
- The backup mirrors the failed engine's throttle in real time, including
  spinning down to zero during coast phases and shutting down completely
  when the rest of the engines do.
- Once assigned, the backup stays on the job for the rest of that flight.
- The landing burn correctly lights the full remaining engine set around
  an active backup, instead of thinking the ring is already fully lit.

## Requirements

- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases) (Mono)

## Installation

1. Download `BackupLandingBurnEngines.dll` from the
   [Releases](../../releases) page.
2. Drop it into `StarshipFactory_Data/../BepInEx/plugins/`.
3. Launch the game.

## Notes

- Only affects vehicles using the standard `Center_N` / `Inner_N` engine
  mount naming (i.e. boosters). Other vehicles are unaffected.
- No changes are made to the game's own files — remove the DLL to fully
  revert.
