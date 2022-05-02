using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;

using static SDL2.SDL;


namespace SIPSorceryMedia.SDL2
{
    public class SDL2Helper
    {
        private static Boolean _sdl2Initialised = false;

        static public String ? GetAudioRecordingDevice(String startWithName) => GetAudioDevice(startWithName, true);

        static public  String? GetAudioPlaybackDevice(String startWithName) => GetAudioDevice(startWithName, false);

        static public String? GetAudioRecordingDevice(int index) => GetAudioDevice(index, true);

        static public String? GetAudioPlaybackDevice(int index) => GetAudioDevice(index, false);

        static public List<String> GetAudioPlaybackDevices() => GetAudioDevices(false);

        static public List<String> GetAudioRecordingDevices() => GetAudioDevices(true);

        static public SDL_AudioSpec GetAudioSpec(int clockRate = AudioFormat.DEFAULT_CLOCK_RATE, byte channels = 1, ushort samples = 960)
        {
            SDL_AudioSpec desiredPlaybackSpec = new SDL_AudioSpec();
            desiredPlaybackSpec.freq = clockRate;
            desiredPlaybackSpec.format = AUDIO_S16SYS;
            desiredPlaybackSpec.channels = channels; // Value returned by (byte)ffmpeg.av_get_channel_layout_nb_channels(ffmpeg.AV_CH_LAYOUT_MONO);
            desiredPlaybackSpec.silence = 0;
            desiredPlaybackSpec.samples = samples;
            //desiredPlaybackSpec.userdata = null;

            return desiredPlaybackSpec;
        }

        static public int GetBytesPerSample(SDL_AudioSpec sdlAudioSpec)
        {
            //Calculate per sample bytes
            return sdlAudioSpec.channels * (SDL_AUDIO_BITSIZE(sdlAudioSpec.format) / 8);
        }

        static public int GetBytesPerSecond(SDL_AudioSpec sdlAudioSpec)
        {
            //Calculate bytes per second
            return sdlAudioSpec.freq * GetBytesPerSample(sdlAudioSpec);
        }

        static public uint OpenAudioPlaybackDevice(String deviceName, ref SDL_AudioSpec audioSpec) => SDL_OpenAudioDevice(deviceName, SDL_FALSE, ref audioSpec, out SDL_AudioSpec receivedPlaybackSpec, SDL_FALSE);

        static public uint OpenAudioRecordingDevice(String deviceName, ref SDL_AudioSpec audioSpec) => SDL_OpenAudioDevice(deviceName, SDL_TRUE, ref audioSpec, out SDL_AudioSpec receivedPlaybackSpec, SDL_FALSE);

        static public void CloseAudioPlaybackDevice(uint deviceId) => CloseAudioDevice(deviceId);

        static public void CloseAudioRecordingDevice(uint deviceId) => CloseAudioDevice(deviceId);

        static public void PauseAudioPlaybackDevice(uint deviceId, bool pauseOn = true) => SDL_PauseAudioDevice(deviceId, pauseOn ? SDL_TRUE : SDL_FALSE);

        static public void PauseAudioRecordingDevice(uint deviceId, bool pauseOn = true) => SDL_PauseAudioDevice(deviceId, pauseOn ? SDL_TRUE : SDL_FALSE);

        static unsafe public void QueueAudioPlaybackDevice(uint deviceId, ref byte[] data, uint len)
        {
            fixed (byte* ptr = &data[0])
                SDL_QueueAudio(deviceId, (IntPtr)ptr, len);
        }

        static public void InitSDL(uint flags = SDL_INIT_AUDIO | SDL_INIT_TIMER)
        {
            if (!_sdl2Initialised)
            {
                //SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

                if (SDL_Init(flags) < 0)
                    throw new ApplicationException($"Cannot initialized SDL for Audio purpose");

                _sdl2Initialised = true;
            }
        }

        static public void QuitSDL()
        {
            if (_sdl2Initialised)
            {
                SDL_Quit();
                _sdl2Initialised = false;
            }
        }

#region PRIVATE methods

        static private List<String> GetAudioDevices(Boolean isCapture)
        {
            List<String> result = new List<string>();
            int isCaptureDevice = isCapture ? SDL_TRUE : SDL_FALSE;

            //Get capture device count
            int deviceCount = SDL_GetNumAudioDevices(isCaptureDevice);

            if (deviceCount > 0)
            {
                String name;
                for (int index = 0; index < deviceCount; index++)
                {
                    name = SDL_GetAudioDeviceName(index, isCaptureDevice);
                    if (!String.IsNullOrEmpty(name))
                        result.Add(name);
                }
            }
            return result;
        }

        static private String? GetAudioDevice(String startWithName, Boolean isCapture)
        {
            String ? result = null;
            int isCaptureDevice = isCapture ? SDL_TRUE : SDL_FALSE;

            //Get capture device count
            int deviceCount = SDL_GetNumAudioDevices(isCaptureDevice);

            if (deviceCount > 0)
            {
                result = SDL_GetAudioDeviceName(0, isCaptureDevice);

                if (deviceCount > 1)
                {
                    for (int index = 1; index < deviceCount; index++)
                    {
                        String deviceName = SDL_GetAudioDeviceName(index, isCaptureDevice);
                        if (deviceName.StartsWith(startWithName, StringComparison.InvariantCultureIgnoreCase))
                            return deviceName;
                    }
                }
                
            }
            return result;
        }

        static private String? GetAudioDevice(int index, Boolean isCapture)
        {
            String? result = null;
            int isCaptureDevice = isCapture ? SDL_TRUE : SDL_FALSE;

            //Get capture device count
            int deviceCount = SDL_GetNumAudioDevices(isCaptureDevice);

            if ( (deviceCount > 0) && (index < deviceCount) )
            {
                return SDL_GetAudioDeviceName(index, isCaptureDevice);
            }
            return result;
        }

        static private void CloseAudioDevice(uint deviceId) =>  SDL_CloseAudioDevice(deviceId);

#endregion PRIVATE methods
    }
}
