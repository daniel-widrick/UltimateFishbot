using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAudioApi;

namespace UltimateFishBot.Classes.BodyParts
{
    public interface IAsyncEars
    {
        event EventHandler HeardFish;
        bool StartListening();
        void StopListening();
        Task<bool> ListenForFish(TimeSpan timeOut, CancellationToken cancellationToken);
    }

    public class AsyncEars : IAsyncEars
    {
        private const int TickRate = 100;
        private const int MaxVolumeQueueLength = 5;

        private readonly object _lockObj = new object();
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly CircularQueue<int> _volumeQueue = new CircularQueue<int>(MaxVolumeQueueLength);

        public event EventHandler HeardFish;

        public static IAsyncEars Instance { get; } = new AsyncEars();

        private AsyncEars()
        {
        }

        public bool StartListening()
        {
            lock (_lockObj)
            {
                if (_isRunning)
                    return false;

                _isRunning = true;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Listen(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            return true;
        }

        public void StopListening()
        {
            lock (_lockObj)
            {
                if (_isRunning && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        public async Task<bool> ListenForFish(TimeSpan timeOut, CancellationToken cancellationToken)
        {
            try
            {
                var timeOutTokenSource = new CancellationTokenSource(timeOut);
                CancellationTokenSource tokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeOutTokenSource.Token);
                var timeOutCancellationToken = tokenSource.Token;

                bool heardFish = false;

                void HeardFishHandler(object sender, EventArgs e)
                {
                    heardFish = true;
                }

                HeardFish += HeardFishHandler;

                StartListening();

                while (!timeOutCancellationToken.IsCancellationRequested && !heardFish)
                {
                    await Task.Delay(TickRate, timeOutCancellationToken);
                }

                HeardFish -= HeardFishHandler;

                return heardFish;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        private async Task Listen(CancellationToken cancellationToken)
        {
            var sndDevEnum = new MMDeviceEnumerator();
            var sndDevice = Properties.Settings.Default.AudioDevice != ""
                ? sndDevEnum.GetDevice(Properties.Settings.Default.AudioDevice)
                : sndDevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TickRate, cancellationToken);

                if (HaveHeardFish(sndDevice))
                {
                    EventHandler handler = HeardFish;

                    handler?.Invoke(this, EventArgs.Empty);
                }
            }

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                lock (_lockObj)
                {
                    _isRunning = false;
                }
            }
        }

        private bool HaveHeardFish(MMDevice sndDevice)
        {
            return Properties.Settings.Default.AverageSound
                ? ListenTimerTickAvg(sndDevice)
                : ListenTimerTick(sndDevice);
        }

        private bool ListenTimerTick(MMDevice sndDevice)
        {
            // Get the current level
            int currentVolumnLevel = (int)(sndDevice.AudioMeterInformation.MasterPeakValue * 100);

            return currentVolumnLevel >= Properties.Settings.Default.SplashLimit;
        }

        private bool ListenTimerTickAvg(MMDevice sndDevice)
        {
            // Get the current level
            int currentVolumnLevel = (int)(sndDevice.AudioMeterInformation.MasterPeakValue * 100);

            _volumeQueue.Enqueue(currentVolumnLevel);

            // Determine if the current level is high enough to be a fish
            int avgVol = (int)_volumeQueue.Average();

            return currentVolumnLevel - avgVol >= Properties.Settings.Default.SplashLimit;
        }
    }
}