//-----------------------------------------------------------------------------
// Filename: SDLAudioEnpoint.cs
//
// Description: Example of an AudioEnpoint using SDL2 to playback audio stream
//
// Author(s):
// Christophe Irles (christophe.irles@al-enterprise.com)
//
// History:
// 10 Dec 2021  Christophe Irles  Created
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;

namespace SIPSorceryMedia.SDL2
{
    public class SDL2AudioEndPoint : IAudioSink
    {
        private ILogger log = SIPSorcery.LogFactory.CreateLogger<SDL2AudioEndPoint>();


        private IAudioEncoder _audioEncoder;
        private MediaFormatManager<AudioFormat> _audioFormatManager;

        private String _audioOutDeviceName;
        private uint _audioOutDeviceId = 0;

        protected bool _isStarted = false;
        protected bool _isPaused = true;
        protected bool _isClosed = true;

        public event SourceErrorDelegate ? OnAudioSinkError = null;

        /// <summary>
        /// Creates a new basic RTP session that captures and renders audio to/from the default system devices.
        /// </summary>
        /// <param name="audioEncoder">An audio encoder that can be used to encode and decode
        /// specific audio codecs.</param>
        /// <param name="audioOutDeviceName">String. Name of the audio playback to use.</param>
        public SDL2AudioEndPoint(string audioOutDeviceName, IAudioEncoder audioEncoder)
        {
            _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
            _audioEncoder = audioEncoder;

            _audioOutDeviceName = audioOutDeviceName;
        }

        private void RaiseAudioSinkError(String err)
        {
            CloseAudioSink();
            OnAudioSinkError?.Invoke(err);
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter) => _audioFormatManager.RestrictFormats(filter);

        public void SetAudioSinkFormat(AudioFormat audioFormat)
        {
            if (_audioFormatManager != null)
            {
                _audioFormatManager.SetSelectedFormat(audioFormat);
                InitPlaybackDevice();
                StartAudioSink();
            }
        }

        public List<AudioFormat> GetAudioSinkFormats() => _audioFormatManager.GetSourceFormats();

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                AudioSink = this,
            };
        }

        private void InitPlaybackDevice()
        {
            try
            {
                // Stop previous playback device
                CloseAudioSink();

                // Init Playback device.
                AudioFormat audioFormat = _audioFormatManager.SelectedFormat;
                var audioSpec = SDL2Helper.GetAudioSpec(audioFormat.ClockRate, 1);

                _audioOutDeviceId = SDL2Helper.OpenAudioPlaybackDevice(_audioOutDeviceName, ref audioSpec);
                if(_audioOutDeviceId > 0)
                    log.LogDebug($"[InitPlaybackDevice] Id:[{_audioOutDeviceId}] - DeviceName:[{_audioOutDeviceName}]");
                else
                {
                    log.LogError($"[InitPlaybackDevice] SDLAudioEndPoint failed to initialise device. No audio device found - Id:[{_audioOutDeviceId}] - DeviceName:[{_audioOutDeviceName}]");
                    RaiseAudioSinkError($"SDLAudioEndPoint failed to initialise device. No audio device found - Id:[{_audioOutDeviceId}] - DeviceName:[{_audioOutDeviceName}]");
                }
            }
            catch (Exception excp)
            {
                log.LogError(excp, $"[InitPlaybackDevice] SDLAudioEndPoint failed to initialise device - Id:[{_audioOutDeviceId}] - DeviceName:[{_audioOutDeviceName}]");
                RaiseAudioSinkError($"SDLAudioEndPoint failed to initialise device. No audio device found - Id:[{_audioOutDeviceId}] - DeviceName:[{_audioOutDeviceName}] - Exception:[{excp.Message}]");
            }
        }

        /// <summary>
        /// Event handler for playing audio samples received from the remote call party.
        /// </summary>
        /// <param name="pcmSample">Raw PCM sample from remote party.</param>
        public void GotAudioSample(byte[] pcmSample)
        {
            if (_audioOutDeviceId > 0)
            {
                // Check if device is not stopped
                if (SDL2Helper.IsDeviceStopped(_audioOutDeviceId))
                {
                    RaiseAudioSinkError($"SDLAudioSource [{_audioOutDeviceName}] stoppped.");
                    return;
                }
                SDL2Helper.QueueAudioPlaybackDevice(_audioOutDeviceId, ref pcmSample, (uint)pcmSample.Length);
            }
        }

        public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
            if ( (_audioEncoder != null) && (_audioOutDeviceId > 0) )
            {
                // Decode sample
                var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
                byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
                GotAudioSample(pcmBytes);
            }
        }

        public Task PauseAudioSink()
        {
            if (_isStarted && !_isPaused)
            {
                SDL2Helper.PauseAudioPlaybackDevice(_audioOutDeviceId, true);
                _isPaused = true;

                log.LogDebug($"[PauseAudioSink] Audio output - Id:[{_audioOutDeviceId}]");
            }

            return Task.CompletedTask;
        }

        public Task ResumeAudioSink()
        {
            if (_isStarted && _isPaused)
            {
                SDL2Helper.PauseAudioPlaybackDevice(_audioOutDeviceId, false);
                _isPaused = false;

                log.LogDebug($"[ResumeAudioSink] Audio output - Id:[{_audioOutDeviceId}]");
            }

            return Task.CompletedTask;
        }

        public Task StartAudioSink()
        {
            if(!_isStarted)
            {
                if (_audioOutDeviceId > 0)
                {
                    _isStarted = true;
                    _isClosed = false;
                    _isPaused = true;

                    ResumeAudioSink();
                }
            }

            return Task.CompletedTask;
        }

        public Task CloseAudioSink()
        {
            if (_isStarted && (_audioOutDeviceId > 0))
            {
                PauseAudioSink().Wait();
                SDL2Helper.CloseAudioPlaybackDevice(_audioOutDeviceId);

                _isClosed = true;
                _isStarted = false;

                log.LogDebug($"[CloseAudioSink] Audio output - Id:[{_audioOutDeviceId}]");

                _audioOutDeviceId = 0;
            }

            return Task.CompletedTask;
        }
    }
}

