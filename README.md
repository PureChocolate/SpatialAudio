# SpatialAudio

Turn your desktop into a sound stage: audio is positioned in 3D space based on
where its window sits on your (multi-)monitor setup.

A C# / .NET learning project. All code written by the learner (me), guided and
reviewed by an AI mentor. Personal study notes are kept out of the repo.

## Status

| Milestone | What it does | Status |
|---|---|---|
| M0 | Capture desktop audio (WASAPI loopback), play it back with low latency | **done** — device menu, capture→playback, latency measurement (2.8/29.4/55.3 ms min/avg/max) |
| M1 | Track focused window position → azimuth/distance across monitors | **done** — Win32 window/monitor tracking, live azimuth readout (front-arc model, ±70° stage) |
| M2 | Spatializer v1: interaural time delay + level panning | **done** — ITD ring-buffer delay + equal-power ILD; audio follows the focused window; direction verified by ear |
| M3 | HRTF: true 3D audio via convolution with measured head filters | **in progress** — Piece 1 (KEMAR dataset + HRIR loader) done: 1420 raw big-endian IRs parsed, verified against the published dataset values |
| M4 | Polish: smoothing, config, standalone .exe | not started |
| M5 (stretch) | Per-app audio routing via virtual cable | not started |
| M6 (stretch) | GUI: window map + speaker positions | not started |

## Requirements

- Windows 10/11
- Visual Studio (Community is fine) with .NET 8 SDK
- **Headphones** (HRTF/binaural only works on headphones)
- Speakers or a second audio device (capture from one, output to the other,
  to avoid the audio feedback loop)

## How to run

1. Open `SpatialAudio.slnx` in Visual Studio, build, Ctrl+F5
2. Choose a run mode:
   - **1. Standard audio spatializer** — lists your render endpoints, pick a
     **capture** device (loopback) and a different **output** device (same
     device is refused — feedback loop); the captured mix streams through the
     spatializer to the output until you press **Esc**
   - **2. HRTF testing** — loads the KEMAR dataset (see `data/` note below)
     and runs a quick verification of the loader
3. The HRTF dataset (MIT KEMAR, `data/full/full`, ~1.4 MB) is not in the repo
   — fetch it from https://sound.media.mit.edu/resources/KEMAR/ (full set)
   if it's missing; the loader verifies itself against the published values

Example: music on the Chu2 headphones → capture Headphones, output to the
LG monitor (2460G4) → you hear the mix through the app, ~100ms delayed.
