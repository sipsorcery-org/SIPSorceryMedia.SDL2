using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.SDL2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusCodec
{
    internal class ProgramCheckCodec
    {
        // Path to a valid audio WAV path
        const String WAV_FILE_PATH = "./../../../../../media/file_example_WAV_5MG.wav";
        //const String WAV_FILE_PATH = "./../../../../../media/file_example_WAV_1MG.wav";

        static SDL2AudioEndPoint audioEndPoint;
        static SDL2AudioSource audioSource;

        static OpusAudioEncoder audioEncoder = new OpusAudioEncoder(); // Create AudioEncoder
        static AudioFormat? audioFormat = new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU);

        static void Main(string[] args)
        {
            int audioPlaybackDeviceIndex = 0; // To store the index of the audio playback device selected
            String audioPlaybackDeviceName; // To store the name of the audio playback device selected

            int audioRecordingDeviceIndex = 0; // To store the index of the audio recording device selected
            String audioRecordingDeviceName; // To store the name of the audio recording device selected

            Console.Clear();

            // Init SDL Library - Library files must be in the same folder than the application
            Console.WriteLine("\nTry to init SDL2 libraries - they must be stored in the same folder than this application");
            SDL2Helper.InitSDL();
            Console.WriteLine("\nInit done");

            // Select Recording Device
            audioRecordingDeviceIndex = DeviceSelection(true);
            if (audioRecordingDeviceIndex < 0)
            {
                SDL2Helper.QuitSDL();
                return;
            }

            // Select Playback Device
            audioPlaybackDeviceIndex = DeviceSelection(false);
            if (audioPlaybackDeviceIndex < 0)
            {
                SDL2Helper.QuitSDL();
                return;
            }

            // Select Audio format
            audioFormat = AudioFormatSelection();
            if (audioFormat == null)
            {
                SDL2Helper.QuitSDL();
                return;
            }

            audioRecordingDeviceName = GetDeviceName(audioRecordingDeviceIndex, true);
            audioPlaybackDeviceName = GetDeviceName(audioPlaybackDeviceIndex, false);

            Console.WriteLine($"\nRecording Device selected: [{audioRecordingDeviceName}]");
            Console.WriteLine($"\nPlayback Device selected: [{audioPlaybackDeviceName}]");
            
            audioEndPoint = new SDL2AudioEndPoint(audioPlaybackDeviceName, audioEncoder);
            audioEndPoint.SetAudioSinkFormat(audioFormat.Value);
            audioEndPoint.StartAudioSink();

            int frameSize = audioEncoder.GetFrameSize();
            audioSource = new SDL2AudioSource(audioRecordingDeviceName, audioEncoder, (uint)frameSize);
            audioSource.OnAudioSourceRawSample += AudioSource_OnAudioSourceRawSample;
            audioSource.OnAudioSourceEncodedSample += AudioSource_OnAudioSourceEncodedSample;

            audioSource.SetAudioSourceFormat(audioFormat.Value);
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
            SDL2Helper.QuitSDL();
        }

        private static void AudioSource_OnAudioSourceEncodedSample(uint durationRtpUnits, byte[] sample)
        {
            // Decode sample
            var pcmSample = audioEncoder.DecodeAudio(sample, audioFormat.Value);
            byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            audioEndPoint.GotAudioSample(pcmBytes);
        }

        private static void AudioSource_OnAudioSourceRawSample(SIPSorceryMedia.Abstractions.AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
        {
            //byte[] pcmBytes = sample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            //audioEndPoint.GotAudioSample(pcmBytes);
        }

        static private String GetDeviceName(int index, bool recordingDevice)
        {
            if(recordingDevice)
                return SIPSorceryMedia.SDL2.SDL2Helper.GetAudioRecordingDevice(index);
            else
                return SIPSorceryMedia.SDL2.SDL2Helper.GetAudioPlaybackDevice(index);
        }

        static private int DeviceSelection(bool recordingDevice)
        {
            string outputStr;
            int deviceIndex = -1;

            // Get list of Audio Playback devices
            List<String> sdlDevices;

            if (recordingDevice)
            {
                outputStr = "recording";
                sdlDevices = SIPSorceryMedia.SDL2.SDL2Helper.GetAudioRecordingDevices();
            }
            else
            {
                outputStr = "playback";
                sdlDevices = SIPSorceryMedia.SDL2.SDL2Helper.GetAudioPlaybackDevices();
            }

            // Quit if no Audio devices found
            if ((sdlDevices == null) || (sdlDevices.Count == 0))
            {
                Console.WriteLine($"No Audio {outputStr} devices found ...");
                SDL2Helper.QuitSDL();
                return -1;
            }

            // Allow end user to select Audio device
            if (sdlDevices?.Count > 0)
            {
                while (true)
                {
                    Console.WriteLine($"\nSelect audio {outputStr} device:");
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
                        deviceIndex = keyValue;
                        break;
                    }
                }
            }
            deviceIndex--;
            return deviceIndex;
        }


        static private AudioFormat? AudioFormatSelection()
        {
            AudioFormat? result = null;
            int audioFormatIndex = 0;

            var audioFormatsSupported = audioEncoder.SupportedFormats;

            if(audioFormatsSupported?.Count > 0)
            {
                if (audioFormatsSupported.Count == 1)
                    result = audioFormatsSupported[0];
                else
                {
                    while (true)
                    {
                        Console.WriteLine($"\nSelect audio format:");
                        int index = 1;
                        foreach (var audioFormat in audioFormatsSupported)
                        {
                            Console.Write($"\n [{index}] - {audioFormat.FormatName} ");
                            index++;
                        }
                        Console.WriteLine("\n");
                        Console.Out.Flush();

                        var keyConsole = Console.ReadKey();
                        if (int.TryParse("" + keyConsole.KeyChar, out int keyValue) && keyValue < index && keyValue >= 0)
                        {
                            audioFormatIndex = keyValue;
                            break;
                        }
                    }

                    if (audioFormatIndex != 0)
                        result = audioFormatsSupported[audioFormatIndex - 1];
                }
            }
            else
                Console.WriteLine($"No Audio Format available ...");

            return result;
        }

    }
}
