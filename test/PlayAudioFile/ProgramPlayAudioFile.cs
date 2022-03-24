using System;
using System.Collections.Generic;
using SIPSorceryMedia.SDL2;
using static SDL2.SDL;

namespace PlayAudioFile
{
    internal class Program
    {
        // Path to a valid audio WAV path
        const String WAV_FILE_PATH = "./../../../../../media/file_example_WAV_5MG.wav";
        //const String WAV_FILE_PATH = "./../../../../../media/file_example_WAV_1MG.wav";

        static IntPtr audio_buffer;/* Pointer to wave data - uint8 */
        static uint audio_len;     /* Length of wave data * - uint32 */
        static int audio_pos;      /* Current play position */

        static SDL_AudioSpec audio_spec;
        static SDL_Event sdlEvent;
        static uint deviceId; // SDL Device Id

        static Boolean end_audio_file = false;

        static void Main(string[] args)
        {
            int deviceIndex = 0; // To store the index of the audio playback device selected
            String deviceName; // To store the name of the audio playback device selected

            Console.Clear();
            Console.WriteLine("\nTry to init SDL2 libraries - they must be stored in the same folder than this application");

            // Init SDL Library - Library files must be in the same folder than the application
            SDL2Helper.InitSDL();

            Console.WriteLine("\nInit done");

            // Get list of Audio Playback devices
            List<String> sdlDevices = SIPSorceryMedia.SDL2.SDL2Helper.GetAudioPlaybackDevices();

            // Quit since no Audio playback found
            if( (sdlDevices == null) || (sdlDevices.Count == 0 ) )
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
                        deviceIndex = keyValue;
                        break;
                    }
                }
            }

            // Get name of the device
            deviceName = sdlDevices[deviceIndex-1];
            Console.WriteLine($"\nDevice selected: {deviceName}");

            // Open WAV file:
            if (SDL_LoadWAV(WAV_FILE_PATH, out audio_spec, out audio_buffer, out audio_len) == null)
            {
                Console.WriteLine("\nCannot open audio file - its format is not supported");
                SDL2Helper.QuitSDL();
                return;
            }

            // Check len of of the Wav file
            if(audio_len == 0)
            {
                Console.WriteLine("\nAudio file not found - path is incorrect");
                SDL2Helper.QuitSDL();
                return;
            }

            // Set callback used to play audio (we fill the device using bytes)
            audio_spec.callback = FillWavData;

            // Open audio file and start to play Wav file
            deviceId = OpenAudioDevice(deviceName);

            if(deviceId == 0)
            {
                Console.WriteLine("\nCannot open Audio device ...");
                SDL2Helper.QuitSDL();
                return;
            }

            Console.WriteLine($"\nPlaying file: {WAV_FILE_PATH}");

            SDL_FlushEvents(SDL_EventType.SDL_AUDIODEVICEADDED, SDL_EventType.SDL_AUDIODEVICEREMOVED);
            while (!end_audio_file)
            {

                while (SDL_PollEvent(out sdlEvent) > 0)
                {
                    if (sdlEvent.type == SDL_EventType.SDL_QUIT)
                    {
                        end_audio_file = true;
                    }
                }
                SDL_Delay(100);
            }

            // No more need callback
            audio_spec.callback = null;

            // Free WAV file
            SDL_FreeWAV(audio_buffer);

            // Close audio file
            CloseAudioDevice(deviceId);

            // Quit SDL Library
            SDL2Helper.QuitSDL();
        }

        static void CloseAudioDevice(uint deviceId)
        {
            if (deviceId != 0)
                SDL_CloseAudioDevice(deviceId);
        }

        static uint OpenAudioDevice(String deviceName)
        {
            /* Initialize fillerup() variables */
            deviceId = SDL_OpenAudioDevice(deviceName, SDL_FALSE, ref audio_spec, out SDL_AudioSpec obtainedeAudioSpec, 0);
            if (deviceId != 0)
            {
                /* Let the audio run */
                SDL_PauseAudioDevice(deviceId, SDL_FALSE);
            }
            return deviceId;
        }

        static void FillWavData(IntPtr unused, IntPtr stream, int len)
        {
            if (end_audio_file)
                return;

            IntPtr waveptr; // Uint8
            int waveleft;

            /* Set up the pointers */
            waveptr = audio_buffer + audio_pos;
            waveleft = (int)(audio_len - audio_pos);

            if (waveleft <= len)
            {
                SDL_memcpy(stream, waveptr, new IntPtr(waveleft));
                audio_pos = 0;
                end_audio_file = true;
            }
            else
            {
                SDL_memcpy(stream, waveptr, new IntPtr(len));
                audio_pos += len;
            }
        }
    }
}
