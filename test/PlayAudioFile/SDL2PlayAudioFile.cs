using System;
using System.Collections.Generic;
using System.Text;
using static SDL2.SDL;

namespace SIPSorceryMedia.SDL2
{
    public unsafe class SDL2PlayAudioFile
    {
        SDL_AudioSpec audioSpec;
        SDL_Event sdlEvent;

        uint device;

        //String playbackDeviceNameToChoose = "Realtek HD Audio 2nd output";
        String playbackDeviceNameToChoose = "Speakers (2";

        String? playbackDeviceName;


        IntPtr audio_buffer;/* Pointer to wave data - uint8 */
        uint audio_len;     /* Length of wave data * - uint32 */
        int audio_pos;      /* Current play position */

        public unsafe SDL2PlayAudioFile(string path)
        {
            SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (SDL_Init(SDL_INIT_AUDIO | SDL_INIT_TIMER) < 0)
            {
                throw new ApplicationException($"Cannot initialized SDL for Audio purpose");
            }

            if (SDL_LoadWAV(path, out audioSpec, out audio_buffer, out audio_len) == null)
            {
                throw new ApplicationException($"Cannot get information  SDL for Audio purpose");
            }

            // Set callback
            audioSpec.callback = fillerup;

            // Open audio file
            OpenAudio();


            ///* Show the list of available drivers */
            //for (int i = 0; i < SDL_GetNumAudioDrivers(); ++i)
            //{
            //    String audioDriver = SDL_GetAudioDriver(i);
            //}
            //String currentAudioDriver = SDL_GetCurrentAudioDriver();

            Boolean done = false;


            SDL_FlushEvents(SDL_EventType.SDL_AUDIODEVICEADDED, SDL_EventType.SDL_AUDIODEVICEREMOVED);
            while (!done)
            {

                while (SDL_PollEvent(out sdlEvent) > 0)
                {
                    if (sdlEvent.type == SDL_EventType.SDL_QUIT) {
                        done = true;
                    }
                    if ((sdlEvent.type == SDL_EventType.SDL_AUDIODEVICEADDED && (sdlEvent.adevice.iscapture != SDL_TRUE)) ||
                        (sdlEvent.type == SDL_EventType.SDL_AUDIODEVICEREMOVED && (sdlEvent.adevice.iscapture != SDL_TRUE) && sdlEvent.adevice.which == device))
                    {
                        ReopenAudio();
                    }

                    SDL_Delay(100);
                }
            }

            SDL_FreeWAV(audio_buffer);

            SDL_Quit();
        }


        //fillerup(void *unused, Uint8 * stream, int len)
        void fillerup(IntPtr unused, IntPtr stream, int len)
        {
            IntPtr waveptr; // Uint8
            int waveleft;

            /* Set up the pointers */
            waveptr = audio_buffer + audio_pos;
            waveleft = (int)(audio_len - audio_pos);

            /* Go! */
            while (waveleft <= len)
            {
                SDL_memcpy(stream, waveptr, new IntPtr(waveleft));
                stream += waveleft;
                len -= waveleft;
                waveptr = audio_buffer;
                waveleft = (int)audio_len;
                audio_pos = 0;
            }
            SDL_memcpy(stream, waveptr, new IntPtr(len));
            audio_pos += len;
        }

        void CloseAudio()
        {
            if (device != 0)
            {
                SDL_CloseAudioDevice(device);
                device = 0;
            }
        }

        void OpenAudio()
        {
            playbackDeviceName = SDL2Helper.GetAudioPlaybackDevice(playbackDeviceNameToChoose);

            if(playbackDeviceName == null)
                throw new ApplicationException($"Playback device not found");

            /* Initialize fillerup() variables */
            device = SDL_OpenAudioDevice(playbackDeviceName, SDL_FALSE, ref audioSpec, out SDL_AudioSpec obtainedeAudioSpec, 0);
            if (device == 0)
                throw new ApplicationException($"Cannot open Playback device with audiospec");

            /* Let the audio run */
            SDL_PauseAudioDevice(device, SDL_FALSE);
        }

        void ReopenAudio()
        {
            CloseAudio();
            OpenAudio();
        }

    }
}
