# REWORK IN PROGRESS
For the following reasons this project is currently being rewritten from the ground up:
- The original is reliant on WPF and other Windows-Only components
- The project started development with 1 year of experience, leading to an unmaintainable mess
- As a personal challenge to try out things

**If you wish to view the code of the current release version, refer to any tag lower than v1**

## Current Progress
### Initial Release V1
- ❌ **Initial Startup**
  - ✅ Dependency injection
  - ✅ Logging
  - ✅ Config Loading
  - ✅ Displaying startup errors (Currently windows only)
  - ✅ Loading splash screen
  - ❌ Version checking & updating
- ✅ **User Interface**
  - ✅ Recreation of original Hoscy components
  - ✅ Recreation of UI
- ✅ **Features**
  - ✅ OSC
    - ✅ Sending
    - ✅ Receiving
    - ✅ Routing
    - ✅ OscQuery
  - ✅ Media
    - ✅ Control
    - ✅ Display
  - ✅ Translation
  - ✅ Textbox Control
  - ✅ STT
    - ✅ Voice Commands
    - ✅ API Recognition
    - ✅ Azure Recognition
    - ✅ Vosk Recognition
    - ✅ Whisper Recognition
    - ✅ Windows Recognition (V1 & V2)\
  - ✅ TTS
    - ✅ Azure TTS
    - ✅ Windows TTS
### Future Updates
- ❌ UI Themes
- ✅ Updating Whisper
- ✅ Improved TTS with Piper

## AppImage Build Instructions
Locally building an AppImage can be done easiest by installing Nix and using the provided flake.
### Updating Dependencies and Flake
Before generating the AppImage, make sure the NuGet dependencies in `flake-deps-xyz.json` are up to date by running
```
nix flake update
```
to update the flake and then for UI
```
nix build .#default.fetch-deps
./result ./flake-deps-ui.json
```
or for CLI
```
nix build .#cli.fetch-deps
./result ./flake-deps-cli.json
```
to update the NuGet package information file.
### Building the AppImage Itself
To build the AppImage finally run the following for UI
```
nix bundle --bundler github:ralismark/nix-appimage .# -L
```
or for CLI
```
nix bundle --bundler github:ralismark/nix-appimage .#cli -L
```
### Testing in a VM
A very simple VM with Gnome and both Ui and Cli installed can be created by running:
```
nixos-rebuild build-vm --flake .#test-vm
```
Running `HoscyUi` / `HoscyCli` within the VM will start the tool.

# HOSCY (Old version)
HOSCY is a free and Open-Source tool with many utilities for communication and OSC aimed at making communication and use of OSC easier

Need help setting this up? Check the **[Quickstart Guide](https://github.com/PaciStardust/HOSCY/wiki/Quickstart-Guide)**

If you wish to contribute or need support, join the **[Discord](https://discord.gg/pxwGHvfcxs)**

## Features
- **Speech Recognition**
	- Windows Speech Recognition
	- Locally running AI *(Thanks to [WHISPER](https://github.com/Const-me/Whisper) & [VOSK](https://alphacephei.com/vosk/))*
	- Azure Cognitive Services
	- Most external APIs *(Provided they use raw audio data)*
- **Utility for communicating**
	- A manual textbox for input with preset support
	- A customizable system for displaying Text on VRChats chatbox
	- Integrated Text-to-Speech support
- **Translation** of whatever you say using an external API of your choice
- **OSC** ***(Open Sound Control)*** **Support** using [CoreOsc](https://github.com/PaciStardust/CoreOSC-UTF8)
	- Configurable routing of incomming OSC data
	- Sending out OSC data
	- Creation of your own OSC command sequences
	- Support for **[OSCQuery](https://github.com/vrchat-community/vrc-oscquery-lib)**
	- Counters for any parameter and AFK detection
- **Media control** using Voice:
	- Simple and non-intrusive "Now Playing" display 

## Credits
- **[CoreOSC](https://github.com/PaciStardust/CoreOSC-UTF8)** by ValdemarOrn and Dalesjo for sending and receiving OSC Data
- **[OSCQuery](https://github.com/vrchat-community/vrc-oscquery-lib)** by VRChat for OSC-Service communication
- **[VOSK](https://alphacephei.com/vosk/)** by AlphaCephei for local AI speech recognition
- **[Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/)** for API speech recognition
- **[Const-me](https://github.com/Const-me)** for creating a usable C# whisper wrapper
- **[AuroraNemoia](https://github.com/AuroraNemoia)** for branding *(Logo, Name)*
- **[Hyblocker](https://github.com/hyblocker)** for providing assistance when I got stuck
- **[Realmlist](https://linktr.ee/Realmlist)** for providing assistance and API keys for testing
- **[M.O.O.N](https://twitter.com/MOONVRCHAT)** for creating a youtube tutorial and sfx for muting and unmuting
- **[DrBlackRat](https://twitter.com/DrBlackRat)** for creating the Avatar 3.0 menu for VRChat use
