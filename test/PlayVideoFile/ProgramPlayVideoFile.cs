using FFmpeg.AutoGen;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorceryMedia.SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayVideoFile
{
    internal class ProgramPlayVideoFile
    {
        // Path to a valid Video file
        const String VIDEO_FILE_PATH = "./../../../../../media/big_buck_bunny.mp4";

        // Path to FFmpeg library
        private const string FFMPEG_LIB_PATH = @"C:\ffmpeg-4.4.1-full_build-shared\bin"; // On Windows
        //private const string LIB_PATH = @"/usr/local/Cellar/ffmpeg/4.4.1_5/lib"; // On MacBookPro
        //private const string LIB_PATH = @"..\..\..\..\..\lib\x64";

        static AsciiFrame ? asciiFrame = null;
        static SDL2AudioEndPoint ? audioEndPoint = null;

        static void Main(string[] args)
        {
            int audioPlaybackDeviceIndex = 0; // To store the index of the audio playback device selected
            String audioPlaybackDeviceName; // To store the name of the audio playback device selected

            VideoCodecsEnum VideoCodec = VideoCodecsEnum.H264;
            IVideoSource videoSource;
            IAudioSource audioSource;

            AudioEncoder audioEncoder;

            Console.Clear();

            // Initialise FFmpeg librairies
            Console.WriteLine("\nTry to init FFmpeg libraries");
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, FFMPEG_LIB_PATH);


            // Init SDL Library - Library files must be in the same folder than the application
            Console.WriteLine("\nTry to init SDL2 libraries - they must be stored in the same folder than this application");
            SDL2Helper.InitSDL();

            Console.WriteLine("\nInit done");

            // Get list of Audio Playback devices
            List<String> sdlDevices = SIPSorceryMedia.SDL2.SDL2Helper.GetAudioPlaybackDevices();

            // Quit since no Audio playback found
            if ((sdlDevices == null) || (sdlDevices.Count == 0))
            {
                Console.WriteLine("No Audio playbqck devices found ...");
                SDL2Helper.QuitSDL();
                return;
            }

            // Allow end user to select Audio playback device
            if (sdlDevices?.Count > 0)
            {
                while (true)
                {
                    Console.WriteLine("\nSelect audio playback device:");
                    int index = 1;
                    foreach (String device in sdlDevices)
                    {
                        Console.Write($"\n [{index}] - {device} ");
                        index++;
                    }
                    Console.WriteLine("\n");
                    Console.Out.Flush();

                    var keyConsole = Console.ReadKey();
                    if (int.TryParse("" + keyConsole.KeyChar, out int keyValue) && keyValue < index && keyValue >= 0)
                    {
                        audioPlaybackDeviceIndex = keyValue;
                        break;
                    }
                }
            }

            // Get name of the device
            audioPlaybackDeviceName = sdlDevices[audioPlaybackDeviceIndex - 1];
            Console.WriteLine($"\nDevice selected: {audioPlaybackDeviceName}");

            //Create AudioEncoder: Genereic object used to Encode or Decode Audio Sample
            audioEncoder = new AudioEncoder();

            // Create audio end point: it will be used to play back tuhe audio from the video file
            audioEndPoint = new SDL2AudioEndPoint(audioPlaybackDeviceName, audioEncoder);
            audioEndPoint.SetAudioSinkFormat(new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU));
            audioEndPoint.StartAudioSink();

            // Create object used to display video in Ascii
            asciiFrame = new AsciiFrame();

            // Create VideoSource Interface using video file
            SIPSorceryMedia.FFmpeg.FFmpegFileSource fileSource = new SIPSorceryMedia.FFmpeg.FFmpegFileSource(VIDEO_FILE_PATH, false, audioEncoder, true);
            videoSource = fileSource as IVideoSource;
            videoSource.RestrictFormats(x => x.Codec == VideoCodec);
            videoSource.SetVideoSourceFormat(videoSource.GetVideoSourceFormats().Find(x => x.Codec == VideoCodec));
            videoSource.OnVideoSourceRawSample += FileSource_OnVideoSourceRawSample;

            audioSource = fileSource as IAudioSource;
            audioSource.SetAudioSourceFormat(audioSource.GetAudioSourceFormats().Find(x => x.Codec == AudioCodecsEnum.PCMU));
            audioSource.OnAudioSourceRawSample += FileSource_OnAudioSourceRawSample;

            videoSource.StartVideo();
            audioSource.StartAudio();

            for (var loop = true; loop;)
            {
                var cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        loop = false;
                        break;
                }
            }

            videoSource.CloseVideo();

            // Quit SDL Library
            SDL2Helper.QuitSDL();
        }

        private static void FileSource_OnAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
        {
            byte[] pcmBytes = sample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();

            audioEndPoint?.GotAudioSample(pcmBytes);
        }

        private static void FileSource_OnVideoSourceRawSample(uint durationMilliseconds, RawImage rawImage)
        {
            asciiFrame.GotRawImage(ref rawImage);
        }
    }
}
