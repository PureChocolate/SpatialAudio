# SpatialAudio

Turn your desktop into a sound stage: audio is positioned in 3D space based on
where its window sits on your (multi-)monitor setup.

A C# / .NET learning project. All code written by the learner (me), guided and
reviewed by an AI mentor. Personal study notes are kept out of the repo.

## Status

| Milestone | What it does | Status |
|---|---|---|
| M0 | Capture desktop audio (WASAPI loopback), play it back with low latency | **in progress** — pieces 1–2 done (device menu + capture→playback), latency measurement next |
| M1 | Track focused window position → azimuth/distance across monitors | not started |
| M2 | Spatializer v1: interaural time delay + level panning | not started |
| M3 | HRTF: true 3D audio via convolution with measured head filters | not started |
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
2. The app lists your render endpoints (FriendlyName, hardware, mix format)
3. Pick a **capture** device (loopback) and a different **output** device
   (same device is refused — feedback loop)
4. Everything playing on the capture device streams through the output
   device until you press **Esc**

Example: music on the Chu2 headphones → capture Headphones, output to the
LG monitor (2460G4) → you hear the mix through the app, ~100ms delayed.
