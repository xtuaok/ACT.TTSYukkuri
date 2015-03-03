﻿namespace ACT.TTSYukkuri.SoundPlayer
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using ACT.TTSYukkuri.Config;
    using Advanced_Combat_Tracker;
    using NAudio.CoreAudioApi;
    using NAudio.Wave;

    /// <summary>
    /// WavePlayers
    /// </summary>
    public enum WavePlayers
    {
        WaveOut,
        DirectSound,
        WASAPI,
        ASIO,
    }

    /// <summary>
    /// NAudioプレイヤー
    /// </summary>
    public partial class NAudioPlayer
    {
        /// <summary>
        /// プレイヤのレイテンシ
        /// </summary>
        private const int PlayerLatency = 128;

        /// <summary>
        /// Device Enumrator
        /// </summary>
        private static MMDeviceEnumerator deviceEnumrator = new MMDeviceEnumerator();

        /// <summary>
        /// 再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumlateDevices()
        {
            switch (TTSYukkuriConfig.Default.Player)
            {
                case WavePlayers.WaveOut:
                    return EnumlateDevicesByWaveOut();

                case WavePlayers.DirectSound:
                    return EnumlateDevicesByDirectSoundOut();

                case WavePlayers.WASAPI:
                    return EnumlateDevicesByWasapiOut();

                case WavePlayers.ASIO:
                    return EnumlateDevicesByAsioOut();
            }

            return null;
        }

        /// <summary>
        /// WaveOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumlateDevicesByWaveOut()
        {
            var list = new List<PlayDevice>();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                list.Add(new PlayDevice()
                {
                    ID = i.ToString(),
                    Name = capabilities.ProductName,
                });
            }

            return list;
        }

        /// <summary>
        /// DirectSoundOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumlateDevicesByDirectSoundOut()
        {
            var list = new List<PlayDevice>();

            foreach (var device in DirectSoundOut.Devices)
            {
                list.Add(new PlayDevice()
                {
                    ID = device.Guid.ToString(),
                    Name = device.Description,
                });
            }

            return list;
        }

        /// <summary>
        /// WasapiOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumlateDevicesByWasapiOut()
        {
            var list = new List<PlayDevice>();

            foreach (var device in deviceEnumrator.EnumerateAudioEndPoints(
                DataFlow.Render,
                DeviceState.Active))
            {
                list.Add(new PlayDevice()
                {
                    ID = device.ID,
                    Name = device.FriendlyName,
                });
            }

            return list;
        }


        /// <summary>
        /// WasapiOutから再生デバイスを列挙する
        /// </summary>
        /// <returns>再生デバイスのリスト</returns>
        public static List<PlayDevice> EnumlateDevicesByAsioOut()
        {
            var list = new List<PlayDevice>();

            foreach (var name in AsioOut.GetDriverNames())
            {
                list.Add(new PlayDevice()
                {
                    ID = name,
                    Name = name,
                });
            }

            return list;
        }

        /// <summary>
        /// 再生する
        /// </summary>
        /// <param name="deviceID">再生デバイスID</param>
        /// <param name="waveFile">wavファイル</param>
        /// <param name="isDelete">再生後に削除する</param>
        /// <param name="volume">ボリューム</param>
        public static void Play(
            string deviceID,
            string waveFile,
            bool isDelete,
            int volume)
        {
            try
            {
                IWavePlayer player = null;

                switch (TTSYukkuriConfig.Default.Player)
                {
                    case WavePlayers.WaveOut:
                        player = new WaveOut()
                        {
                            DeviceNumber = int.Parse(deviceID),
                            DesiredLatency = PlayerLatency,
                        };
                        break;

                    case WavePlayers.DirectSound:
                        player = new DirectSoundOut(
                            Guid.Parse(deviceID),
                            PlayerLatency);
                        break;

                    case WavePlayers.WASAPI:
                        player = new WasapiOut(
                            deviceEnumrator.GetDevice(deviceID),
                            AudioClientShareMode.Shared,
                            false,
                            PlayerLatency);
                        break;

                    case WavePlayers.ASIO:
                        player = new AsioOut(deviceID);
                        break;
                }

                if (player == null)
                {
                    return;
                }

                var reader = new AudioFileReader(waveFile)
                {
                    Volume = ((float)volume / 100f)
                };

                player.Init(reader);
                player.PlaybackStopped += (s, e) =>
                {
                    player.Dispose();
                    reader.Dispose();

                    if (isDelete)
                    {
                        File.Delete(waveFile);
                    }
                };

                // 再生する
                player.Play();
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(
                    ex,
                    "サウンドの再生で例外が発生しました。");
            }
        }

        /// <summary>
        /// プレイヤを開放する
        /// </summary>
        public static void DisposePlayers()
        {
            // NO-OP
        }
    }

    /// <summary>
    /// 再生デバイス
    /// </summary>
    public class PlayDevice
    {
        /// <summary>
        /// デバイスのID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// デバイス名
        /// </summary>
        public string Name { get; set; }
    }
}
