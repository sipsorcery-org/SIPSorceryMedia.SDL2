using SIPSorceryMedia.SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static SDL2.SDL;

namespace SIPSorceryMedia.FFmpeg
{
    public unsafe class SDL2RecordThenPlayback
    {
        enum RecordingState
        {
            //SELECTING_DEVICE,
            STOPPED,
            RECORDING,
            RECORDED,
            PLAYBACK,
            ERROR
        };

        //Maximum number of supported recording devices
        const int MAX_RECORDING_DEVICES = 10;

		//Maximum recording time
		const int MAX_RECORDING_SECONDS = 5;

		//Maximum recording time plus padding
		const int RECORDING_BUFFER_SECONDS = MAX_RECORDING_SECONDS + 1;

        //Recieved audio spec
        SDL_AudioSpec gReceivedRecordingSpec;
        SDL_AudioSpec gReceivedPlaybackSpec;

        //Recording data buffer
        byte[] gRecordingBuffer; // Uint8

        //Size of data buffer
        uint gBufferByteSize = 0; // Uint32

        //Position in data buffer
        uint gBufferBytePosition = 0; // Uint32

        //Maximum position in data buffer for recording
        uint gBufferByteMaxPosition = 0; //Uint32

        //Number of available devices
        uint recordingDeviceId = 0;
        uint playbackDeviceId = 0;

        //String recordingDeviceNameToChoose = "External Mic";
        String recordingDeviceNameToChoose = "Microphone (2";

        //String playbackDeviceNameToChoose = "Realtek HD Audio 2nd output";
        String playbackDeviceNameToChoose = "Speakers (2";

        RecordingState currentState = RecordingState.STOPPED;


        public unsafe SDL2RecordThenPlayback()
        {
            SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (SDL_Init(SDL_INIT_AUDIO ) < 0)
                throw new ApplicationException($"Cannot initialized SDL for Audio purpose");

            String ? recordingDeviceName = SDL2Helper.GetAudioRecordingDevice(recordingDeviceNameToChoose);
            if(recordingDeviceName == null)
                throw new ApplicationException($"Recording device not found");

            String? playbackDeviceName = SDL2Helper.GetAudioPlaybackDevice(playbackDeviceNameToChoose);
            if (playbackDeviceName == null)
                throw new ApplicationException($"Playback device not found");

            //Default audio spec - recording
            SDL_AudioSpec desiredRecordingSpec = new SDL_AudioSpec();
            desiredRecordingSpec.freq = 44100;
            desiredRecordingSpec.format = AUDIO_F32;
            desiredRecordingSpec.channels = 2;
            desiredRecordingSpec.samples = 4096;
            desiredRecordingSpec.callback = audioRecordingCallback;

            //Open recording device
            recordingDeviceId = SDL_OpenAudioDevice(recordingDeviceName, SDL_TRUE, ref desiredRecordingSpec, out gReceivedRecordingSpec, (int)SDL_AUDIO_ALLOW_FORMAT_CHANGE);

            //Device failed to open
            if (recordingDeviceId <= 0)
                throw new ApplicationException($"Cannot open recording device");


            //Default audio spec - playback
            SDL_AudioSpec desiredPlaybackSpec = new SDL_AudioSpec();
            desiredPlaybackSpec.freq = 44100;
            desiredPlaybackSpec.format = AUDIO_F32;
            desiredPlaybackSpec.channels = 2;
            desiredPlaybackSpec.samples = 4096;
            desiredPlaybackSpec.callback = audioPlaybackCallback;

            //Open playback device
            playbackDeviceId = SDL_OpenAudioDevice(playbackDeviceName, SDL_FALSE, ref desiredPlaybackSpec, out gReceivedPlaybackSpec, (int)SDL_AUDIO_ALLOW_FORMAT_CHANGE);

            //Device failed to open
            if (playbackDeviceId <= 0)
                throw new ApplicationException($"Cannot open playback device");


            //Calculate per sample bytes
            int bytesPerSample = gReceivedRecordingSpec.channels * (SDL_AUDIO_BITSIZE(gReceivedRecordingSpec.format) / 8);

            //Calculate bytes per second
            int bytesPerSecond = SDL2Helper.GetBytesPerSecond(gReceivedRecordingSpec);  //gReceivedRecordingSpec.freq * bytesPerSample;

            //Calculate buffer size
            gBufferByteSize = (uint) (RECORDING_BUFFER_SECONDS * bytesPerSecond);

            //Calculate max buffer use
            gBufferByteMaxPosition = (uint) (MAX_RECORDING_SECONDS * bytesPerSecond);

            //Allocate and initialize byte buffer
            gRecordingBuffer = Enumerable.Repeat((byte)0, (int)gBufferByteSize).ToArray();

            Boolean quit = false;

            currentState = RecordingState.STOPPED;

            while (!quit)
            {
                switch (currentState)
                {
                    case RecordingState.STOPPED:
                        //Go back to beginning of buffer
                        gBufferBytePosition = 0;

                        //Start recording
                        SDL_PauseAudioDevice(recordingDeviceId, SDL_FALSE);

                        //Go on to next state
                        currentState = RecordingState.RECORDING;
                        break;

                    case RecordingState.RECORDING:
                        //Lock callback
                        SDL_LockAudioDevice(recordingDeviceId);

                        //Finished recording
                        if (gBufferBytePosition > gBufferByteMaxPosition)
                        {
                            //Stop recording audio
                            SDL_PauseAudioDevice(recordingDeviceId, SDL_TRUE);

                            //Go back to beginning of buffer
                            gBufferBytePosition = 0;

                            //Start playback
                            SDL_PauseAudioDevice(playbackDeviceId, SDL_FALSE);

                            //Go on to next state
                            currentState = RecordingState.PLAYBACK;
                        }

                        //Unlock callback
                        SDL_UnlockAudioDevice(recordingDeviceId);
                        break;

                    case RecordingState.PLAYBACK:
                        //Lock callback
                        SDL_LockAudioDevice(playbackDeviceId);

                        //Finished playback
                        if (gBufferBytePosition > gBufferByteMaxPosition)
                        {
                            //Stop playing audio
                            SDL_PauseAudioDevice(playbackDeviceId, SDL_TRUE);

                            //Go on to next state
                            currentState = RecordingState.STOPPED;
                            quit = true;
                        }

                        //Unlock callback
                        SDL_UnlockAudioDevice(playbackDeviceId);
                        break;
                }
            }

            SDL_Quit();
        }


        //void audioRecordingCallback(void* userdata, Uint8* stream, int len)
        void audioRecordingCallback(IntPtr userdata, IntPtr stream, int len)
        {
            //Copy audio from stream
            fixed (byte* ptr = &gRecordingBuffer[gBufferBytePosition])
            {
                Buffer.MemoryCopy((byte*)stream, ptr, len, len);
            }
            //memcpy(&gRecordingBuffer[gBufferBytePosition], stream, len); //void* memcpy(void* dest, const void* src, size_t n)                              

            //Move along buffer
            gBufferBytePosition += (uint)len;
        }

        //void audioPlaybackCallback(void* userdata, Uint8* stream, int len)
        void audioPlaybackCallback(IntPtr userdata, IntPtr stream, int len)
        {
            //Copy audio from stream
            fixed (byte* ptr = &gRecordingBuffer[gBufferBytePosition])
            {
                Buffer.MemoryCopy(ptr, (byte*)stream, len, len);
            }
            //memcpy(stream, &gRecordingBuffer[gBufferBytePosition], len); //void* memcpy(void* dest, const void* src, size_t n)

            //Move along buffer
            gBufferBytePosition += (uint)len;
        }


        //public unsafe SDL2AudioPlayBack(String path)
        //      {
        //SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

        //if (SDL_Init(SDL_INIT_AUDIO | SDL_INIT_TIMER) < 0)
        //         {
        //             throw new ApplicationException($"Cannot initialized SDL for Audio purpose");
        //         }


        //// Get microphones
        //int nbMicrophones = SDL_GetNumAudioDevices((int)SDL_bool.SDL_TRUE);
        //for (int index = 0; index < nbMicrophones; index++)
        //{
        //	String microphone = SDL_GetAudioDeviceName(index, (int)SDL_bool.SDL_TRUE);
        //}

        //// Get playback
        //int nbAudioPlaybacks = SDL_GetNumAudioDevices((int)SDL_bool.SDL_FALSE);
        //for (int index = 0; index < nbAudioPlaybacks; index++)
        //{
        //	String audioPlayback = SDL_GetAudioDeviceName(index, (int)SDL_bool.SDL_FALSE);
        //}


        ////SDL_AudioSpec
        //SDL_AudioSpec wanted_spec = new SDL_AudioSpec();
        //wanted_spec.freq = 44100;
        //wanted_spec.format = AUDIO_S16SYS;
        //wanted_spec.channels = 2;
        //wanted_spec.silence = 0;
        //wanted_spec.samples = 1024;
        //wanted_spec.callback = fill_audio;

        //SDL_AudioSpec obtained_spec;

        //String defaultAudioPlayback = SDL_GetAudioDeviceName(0, (int)SDL_bool.SDL_FALSE);
        //GCHandle handle = GCHandle.Alloc(defaultAudioPlayback);
        //IntPtr devicePlayBack = (IntPtr)handle;
        //if (SDL_OpenAudioDevice(devicePlayBack, (int)SDL_bool.SDL_FALSE, ref wanted_spec, out obtained_spec, (int)SDL_AUDIO_ALLOW_FORMAT_CHANGE) < 0)
        //{
        //	handle.Free();
        //	throw new ApplicationException($"Cannot open audio");
        //}
        //handle.Free();

        //using (FileStream fs = File.OpenRead(path))
        //{
        //	fs.Position = 0;

        //	int pcm_buffer_size = 4096;
        //	byte [] pcm_buffer = new byte[pcm_buffer_size];
        //	int data_count = 0;

        //	//Play
        //	SDL_PauseAudio(0);

        //	while (true)
        //	{
        //		if( fs.Read(pcm_buffer, data_count, pcm_buffer_size) != pcm_buffer_size)
        //                 {
        //			break;
        //			//fs.Position = 0;
        //			//fs.Read(pcm_buffer, pcm_buffer_size, 1);
        //			//data_count = 0;
        //		}

        //		data_count += pcm_buffer_size;

        //		//Audio buffer length
        //		audio_len = (uint)pcm_buffer_size;
        //		audio_pos = pcm_buffer;

        //		while (audio_len > 0)//Wait until finish
        //			SDL_Delay(1);
        //	}
        //	SDL_Quit();
        //}
        //}

        //public unsafe void fill_audio(IntPtr userdata, IntPtr stream, int len)
        //{
        //	//SDL 2.0
        //	ZeroMemory(stream, new IntPtr(len));

        //	if (audio_len == 0)     /*  Only  play  if  we  have  data  left  */
        //		return;
        //	len = (len > audio_len ? (int)audio_len : len);  /*  Mix  as  much  data  as  possible  */


        //	byte[] dst = new byte[Marshal.SizeOf(stream)];
        //	Marshal.Copy(stream, dst, 0, Marshal.SizeOf(stream));

        //	SDL_MixAudio(dst, audio_pos, (uint)len, SDL_MIX_MAXVOLUME);
        //	audio_len -= (uint)len;
        //}
    }
}
