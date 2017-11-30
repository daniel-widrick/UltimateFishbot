using CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UltimateFishBot.Classes.BodyParts
{
    public class Ears
    {
        private MMDevice _sndDevice;
        private readonly Queue<int> _volumeQueue = new Queue<int>();
        private const int Tickrate = 100; // ms pause between sound checks

        private const int MaxVolumeQueueLength = 5;
        
        public async Task<bool> Listen(int millisecondsToListen, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var sndDevEnum = new MMDeviceEnumerator();
            _sndDevice = Properties.Settings.Default.AudioDevice != ""
                ? sndDevEnum.GetDevice(Properties.Settings.Default.AudioDevice)
                : sndDevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            Func<bool> heardFish;
            if (Properties.Settings.Default.AverageSound)
                heardFish = ListenTimerTickAvg;
            else
                heardFish = ListenTimerTick;

            while (stopwatch.ElapsedMilliseconds <= millisecondsToListen)
            {
                await Task.Delay(Tickrate, cancellationToken);
                if (heardFish())
                {
                    return true;
                }
            }
            return false;
        }

        private bool ListenTimerTick()
        {
            // Get the current level
            int currentVolumnLevel = (int)(_sndDevice.AudioMeterInformation.MasterPeakValue * 100);

            return currentVolumnLevel >= Properties.Settings.Default.SplashLimit;
        }

        private bool ListenTimerTickAvg()
        {
            // Get the current level
            int currentVolumnLevel = (int)(_sndDevice.AudioMeterInformation.MasterPeakValue * 100);

            _volumeQueue.Enqueue(currentVolumnLevel);

            // Keep a running queue of the last X sounds as a reference point
            if (_volumeQueue.Count >= MaxVolumeQueueLength)
                _volumeQueue.Dequeue();

            // Determine if the current level is high enough to be a fish
            int avgVol = (int)_volumeQueue.Average();

            return currentVolumnLevel - avgVol >= Properties.Settings.Default.SplashLimit;
        }
    }
}
