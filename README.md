# SIPSorceryMedia.SDL2

This project is an example of developing a C# library that can use features from [SDL2](https://www.libsdl.org/index.php) native libraries and that integrates with the [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) real-time communications library.

The classes in this project provide **Audio End Point** and **Audio Source** features.

So used in correlation with **SIPSorceryMedia.FFMpeg** you have both  **Audio and Video End Point** and **Audio and Video Source** using multi-platform component.

Using both you have these features:

 - **Video Input**:
    - using local file or remote using URI [**`With SIPSorceryMedia.FFMpeg`**] 
    - using camera [**`With SIPSorceryMedia.FFMpeg`**]
    - using screen [**`With SIPSorceryMedia.FFMpeg`**]
    
 - **Audio Input**:
    - using local file or remote using URI [**`With SIPSorceryMedia.FFMpeg`**]
    - using microphone [**`With SIPSorceryMedia.FFMpeg or SIPSorceryMedia.SDL2`**]
    
 - **Audio Ouput**:
    - using a speaker [**`With SIPSorceryMedia.SDL2`**]

**Audio Codecs** supported by this library: (using **AudioEncoder** from **SIPSorcery** )
 - PCMU
 - PCMA
 - G722
 - G729 (not tested in SIP Context but should work)
 - Opus (see **CheckCodec** example which implements Opus Codec using [Concentus (C# implementation)](https://github.com/lostromb/concentus)

# Installing SDL2

## For Windows

No additional steps are required for an x64 build. The nuget package includes the [SDL v2.0.20](https://www.libsdl.org/download-2.0.php) x64 binaries.

## For Mac

Install the DMG file available [here](https://www.libsdl.org/download-2.0.php)

## For Linux

Install the [SDL](https://www.libsdl.org/index.php) binaries using the package manager for the distribution.

`sudo apt-get install libsdl2`


# Testing

Several projects permits to understand how the library can be used:

- [PlayAudioFile](./test/PlayAudioFile) - **Multiplatform application**:
    - Let user select an Audio Playback device
    - Play Audio file
    
- [PlayVideoFile](./test/PlayVideoFile) - **Multiplatform application**:
    - Let user select an Audio Playback device
    - Play Video file (Display it in ASCII in a Console/Terminal Window)
    
- [CheckCodec](./test/CheckCodec) - **Multiplatform application**:
    - Let user select an Audio Playback device
    - Let user select an Audio Recording device OR a Video File
    - Let user select one of the Audio Codecs supported  
    - Play back the sound after Encoding then Decoding the sample using the specified codec 
