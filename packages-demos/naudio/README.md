# NAudio

<img width="570" height="763" alt="image" src="https://github.com/user-attachments/assets/135ea2ed-b78d-4c1c-9821-88ea7bb02c2b" />

<img width="573" height="763" alt="image" src="https://github.com/user-attachments/assets/61603bf4-57e3-47b9-9a10-fd57fb725244" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fnaudio%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Audio Processing

This example demonstrates audio processing capabilities using the [NAudio library](https://github.com/naudio/NAudio) integrated with Ivy. NAudio is a popular .NET audio library that provides a wide range of audio processing features.

**What This Application Does:**

This specific implementation creates an **Audio Processing** application that allows users to:

- **Upload Audio Files**: Support multiple formats (WAV, MP3, etc.) with automatic format detection
- **Generate Custom Tones**: Create audio tones by adjusting frequency (50-1000 Hz), duration (0.1-600 seconds), and volume
- **Wave Type Selection**: Choose from different signal generator types (Sine, Square, Triangle, Sawtooth, etc.)
- **Mix Audio Files**: Combine generated tones with uploaded audio files with independent volume controls
- **Audio Playback**: Built-in audio player with controls for testing generated and mixed audio
- **Format Conversion**: Automatic resampling and channel conversion for compatible audio mixing
- **Real-time Processing**: Interactive UI with instant audio generation and mixing

**Technical Implementation:**

- Uses NAudio's `SignalGenerator` for custom tone generation
- Implements `WaveFileReader`, `AudioFileReader`, and `MediaFoundationReader` for multi-format support
- Utilizes `MixingSampleProvider` for combining audio streams
- Handles automatic resampling with `MediaFoundationResampler` for compatibility
- Supports channel conversion (mono to stereo and vice versa)
- Generates WAV files with proper format specifications
- Implements file upload handling with automatic format detection

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd naudio
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd naudio
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your audio processing application with a single command.

## Learn More

- NAudio for .NET overview: [github.com/naudio/NAudio](https://github.com/naudio/NAudio)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
