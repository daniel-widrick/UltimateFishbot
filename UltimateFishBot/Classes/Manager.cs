using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UltimateFishBot.Classes.BodyParts;

namespace UltimateFishBot.Classes
{
    public interface IManagerEventHandler
    {
        void Started();
        void Stopped();
        void Resumed();
        void Paused();
    }

    public class Manager
    {
        private enum FishingState
        {
            Fishing = 3,
            Paused  = 6,
            Stopped = 7
        }

        public enum NeededAction
        {
            None        = 0x00,
            HearthStone = 0x01,
            Lure        = 0x02,
            Charm       = 0x04,
            Raft        = 0x08,
            Bait        = 0x10,
            AntiAfkMove = 0x20
        }

        private CancellationTokenSource _cancellationTokenSource;
        private readonly System.Windows.Forms.Timer _mLureTimer;
        private readonly System.Windows.Forms.Timer _mHearthStoneTimer;
        private readonly System.Windows.Forms.Timer _mRaftTimer;
        private readonly System.Windows.Forms.Timer _mCharmTimer;
        private readonly System.Windows.Forms.Timer _mBaitTimer;
        private readonly System.Windows.Forms.Timer _mAntiAfkTimer;

        private readonly IManagerEventHandler _mManagerEventHandler;

        private readonly Eyes _mEyes;
        private readonly Hands _mHands;
        private readonly Ears _mEars;
        private readonly Mouth _mMouth;
        private readonly Legs _mLegs;
        private TextToSpeech _textToSpeech;

        private NeededAction _mNeededActions;
        private FishingState _mFishingState;
        private readonly FishingStats _mFishingStats;

        private const int Second = 1000;
        private const int Minute = 60 * Second;

        public Manager(IManagerEventHandler managerEventHandler, IProgress<string> progressHandle)
        {
            _mManagerEventHandler    = managerEventHandler;

            _mEyes                   = new Eyes();
            _mHands                  = new Hands();
            _mEars                   = new Ears();
            _mMouth                  = new Mouth(progressHandle);
            _mLegs                   = new Legs();

            _mFishingState           = FishingState.Stopped;
            _mNeededActions          = NeededAction.None;

            _mFishingStats           = new FishingStats();
            _mFishingStats.Reset();

            _cancellationTokenSource = null;

            InitializeTimer(out _mLureTimer, LureTimerTick);
            InitializeTimer(out _mCharmTimer, CharmTimerTick);
            InitializeTimer(out _mRaftTimer, RaftTimerTick);
            InitializeTimer(out _mBaitTimer, BaitTimerTick);
            InitializeTimer(out _mHearthStoneTimer, HearthStoneTimerTick);
            InitializeTimer(out _mAntiAfkTimer, AntiAfkTimerTick);

            ResetTimers();
        }

        private void InitializeTimer(out System.Windows.Forms.Timer timer, EventHandler handler)
        {
            timer = new System.Windows.Forms.Timer {Enabled = false};
            timer.Tick += handler;
        }

        public async Task StartOrResumeOrPause()
        {
            if (_mFishingState == FishingState.Stopped)
            {
                await RunBotUntilCanceled();
            }
            else if (_mFishingState == FishingState.Paused)
            {
                await Resume();
            }
            else
            {
                Pause();
            }
        }

        private async Task RunBotUntilCanceled()
        {
            ResetTimers();
            EnableTimers();
            _mMouth.Say(Translate.GetTranslate("frmMain", "LABEL_STARTED"));
            _mManagerEventHandler.Started();
            await RunBot();
        }

        private async Task Resume()
        {
            _mMouth.Say(Translate.GetTranslate("frmMain", "LABEL_RESUMED"));
            _mManagerEventHandler.Resumed();
            await RunBot();
        }

        private async Task RunBot()
        {
            _mFishingState = FishingState.Fishing;
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {

                    // We first check if another action is needed, foreach on all NeededAction enum values
                    foreach (NeededAction neededAction in (NeededAction[])Enum.GetValues(typeof(NeededAction)))
                    {
                        if (HasNeededAction(neededAction))
                        {
                            await HandleNeededAction(neededAction, cancellationToken);
                        }
                    }

                    // If no other action required, we can cast !
                    await Fish(cancellationToken);
                }

            }
            catch (TaskCanceledException)
            {
                return;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CancelRun()
        {
            if (!IsStoppedOrPaused())
            {
                Debug.Assert(_cancellationTokenSource != null);
                _cancellationTokenSource.Cancel();
            }
        }

        private void Pause()
        {
            CancelRun();
            _mFishingState = FishingState.Paused;
            _mMouth.Say(Translate.GetTranslate("frmMain", "LABEL_PAUSED"));
            _mManagerEventHandler.Paused();
        }

        public void EnableTimers()
        {
            if (Properties.Settings.Default.AutoLure)
            {
                AddNeededAction(NeededAction.Lure);
                _mLureTimer.Enabled = true;
            }

            if (Properties.Settings.Default.AutoCharm)
            {
                AddNeededAction(NeededAction.Charm);
                _mCharmTimer.Enabled = true;
            }

            if (Properties.Settings.Default.AutoRaft)
            {
                AddNeededAction(NeededAction.Raft);
                _mRaftTimer.Enabled = true;
            }

            if (Properties.Settings.Default.AutoBait)
            {
                AddNeededAction(NeededAction.Bait);
                _mBaitTimer.Enabled = true;
            }

            if (Properties.Settings.Default.AutoHearth)
                _mHearthStoneTimer.Enabled = true;

            if (Properties.Settings.Default.AntiAfk)
                _mAntiAfkTimer.Enabled = true;
        }

        public void Stop()
        {
            CancelRun();
            _mFishingState = FishingState.Stopped;
            _mMouth.Say(Translate.GetTranslate("frmMain", "LABEL_STOPPED"));
            _mManagerEventHandler.Stopped();
            _mLureTimer.Enabled        = false;
            _mRaftTimer.Enabled        = false;
            _mCharmTimer.Enabled       = false;
            _mBaitTimer.Enabled        = false;
            _mHearthStoneTimer.Enabled = false;
        }

        private bool IsStoppedOrPaused()
        {
            return _mFishingState == FishingState.Stopped || _mFishingState == FishingState.Paused;
        }

        public FishingStats GetFishingStats()
        {
            return _mFishingStats;
        }

        public void ResetFishingStats()
        {
            _mFishingStats.Reset();
        }
        
        public async Task StartOrStop()
        {
            if (IsStoppedOrPaused())
                await StartOrResumeOrPause();
            else
                Stop();
        }

        private void ResetTimers()
        {
            _mLureTimer.Interval        = Properties.Settings.Default.LureTime * Minute + 22 * Second;
            _mRaftTimer.Interval        = Properties.Settings.Default.RaftTime * Minute;
            _mCharmTimer.Interval       = Properties.Settings.Default.CharmTime * Minute;
            _mBaitTimer.Interval        = Properties.Settings.Default.BaitTime * Minute;
            _mHearthStoneTimer.Interval = Properties.Settings.Default.HearthTime * Minute;
            _mAntiAfkTimer.Interval     = Properties.Settings.Default.AntiAfkTime * Minute;
        }

        private async Task Fish(CancellationToken cancellationToken)
        {
            _mMouth.Say(Translate.GetTranslate("manager", "LABEL_CASTING"));
            await _mHands.Cast(cancellationToken);

            _mMouth.Say(Translate.GetTranslate("manager", "LABEL_FINDING"));
            bool didFindFish = await _mEyes.LookForBobber(cancellationToken);
            if (!didFindFish)
            {
                _mFishingStats.RecordBobberNotFound();
                return;
            }

            // Update UI with wait status            
            var uiUpdateCancelTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var uiUpdateCancelToken = uiUpdateCancelTokenSource.Token;
            var progress = new Progress<long>(msecs =>
            {
                if (!uiUpdateCancelToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    _mMouth.Say(Translate.GetTranslate(
                        "manager",
                        "LABEL_WAITING",
                        msecs / Second,
                        Properties.Settings.Default.FishWait / Second));
                }
            });
            var uiUpdateTask = Task.Run(
                async () => await UpdateUiWhileWaitingToHearFish(progress, uiUpdateCancelToken),
                uiUpdateCancelToken);

            bool fishHeard = await _mEars.Listen(
                Properties.Settings.Default.FishWait,
                cancellationToken);
            uiUpdateCancelTokenSource.Cancel();
            try
            {
                uiUpdateTask.GetAwaiter().GetResult(); // Wait & Unwrap
                // https://github.com/StephenCleary/AsyncEx/blob/dc54d22b06566c76db23af06afcd0727cac625ef/Source/Nito.AsyncEx%20(NET45%2C%20Win8%2C%20WP8%2C%20WPA81)/Synchronous/TaskExtensions.cs#L18
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                uiUpdateCancelTokenSource.Dispose();
            }

            if (!fishHeard)
            {
                _mFishingStats.RecordNotHeard();
                return;
            }

            _mMouth.Say(Translate.GetTranslate("manager", "LABEL_HEAR_FISH"));
            await _mHands.Loot();
            _mFishingStats.RecordSuccess();
        }

        private async Task UpdateUiWhileWaitingToHearFish(
            IProgress<long> progress, 
            CancellationToken uiUpdateCancelToken)
        {
            // We are waiting a detection from the Ears
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!uiUpdateCancelToken.IsCancellationRequested)
            {
                progress.Report(stopwatch.ElapsedMilliseconds);
                await Task.Delay(Second / 10, uiUpdateCancelToken);
            }
            uiUpdateCancelToken.ThrowIfCancellationRequested();
        }

        private async Task HandleNeededAction(NeededAction action, CancellationToken cancellationToken)
        {
            switch (action)
            {
                case NeededAction.HearthStone:
                    Stop();
                    goto case NeededAction.Lure; // We continue, Hearthstone need m_hands.DoAction
                case NeededAction.Lure:
                case NeededAction.Charm:
                case NeededAction.Raft:
                case NeededAction.Bait:
                    await _mHands.DoAction(action, _mMouth, cancellationToken);
                    break;
                case NeededAction.AntiAfkMove:
                    await _mLegs.DoMovement(_textToSpeech, cancellationToken);
                    break;
            }

            RemoveNeededAction(action);
        }

        private void LureTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.Lure);
        }

        private void RaftTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.Raft);
        }

        private void CharmTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.Charm);
        }

        private void BaitTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.Bait);
        }

        private void HearthStoneTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.HearthStone);
        }

        private void AntiAfkTimerTick(Object myObject, EventArgs myEventArgs)
        {
            AddNeededAction(NeededAction.AntiAfkMove);
        }

        private void AddNeededAction(NeededAction action)
        {
            _mNeededActions |= action;
        }

        private void RemoveNeededAction(NeededAction action)
        {
            _mNeededActions &= ~action;
        }

        private bool HasNeededAction(NeededAction action)
        {
            return (_mNeededActions & action) != NeededAction.None;
        }
    }
}
