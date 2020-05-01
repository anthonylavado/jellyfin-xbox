using System;
using System.Timers;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Jellyfin.Core;
using Jellyfin.Models;
using Jellyfin.Services.Interfaces;

namespace Jellyfin.ViewModels
{
    public class MediaPlaybackViewModel : JellyfinViewModelBase
    {
        #region Properties

        public MediaPlayerElement MediaPlayer { get; set; }

        public Timer OSDUpdateTimer { get; set; }

        /// <summary>
        /// Timer for reporting playback status.
        /// </summary>
        public Timer ReportPlaybackStatusTimer { get; set; }

        public bool IsPlaybackConfirmationDisplayedBefore { get; set; }

        #region RemainingTimeLeft

        private TimeSpan _remainingTimeLeft;

        public TimeSpan RemainingTimeLeft
        {
            get { return _remainingTimeLeft; }
            set
            {
                _remainingTimeLeft = value;
                RaisePropertyChanged(nameof(RemainingTimeLeft));
            }
        }

        #endregion

        #region SelectedMediaElement

        private Movie _selectedMediaElement;

        public Movie SelectedMediaElement
        {
            get { return _selectedMediaElement; }
            set
            {
                _selectedMediaElement = value;
                RaisePropertyChanged(nameof(SelectedMediaElement));
            }
        }

        #endregion

        private Timer SeekRequestTimer;

        /// <summary>
        /// Thread locking for seek request timer configuration
        /// </summary>
        private object padlock = new object();

        /// <summary>
        /// Indicates how many seek seconds requested in sum.
        /// </summary>

        #region SeekRequestedSeconds

        private int _seekRequestedSeconds;

        public int SeekRequestedSeconds
        {
            get { return _seekRequestedSeconds; }
            set
            {
                _seekRequestedSeconds = value;

                Globals.Instance.UIDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    RaisePropertyChanged(nameof(SeekRequestedSeconds));
                    RaisePropertyChanged(nameof(FormattedSeekRequestedSeconds));
                });
            }
        }

        #endregion

        public string FormattedSeekRequestedSeconds
        {
            get
            {
                if (SeekRequestedSeconds == 0)
                {
                    return string.Empty;
                }

                if (SeekRequestedSeconds < 0)
                {
                    return $" ({SeekRequestedSeconds}s)"; 
                }

                return $" (+{SeekRequestedSeconds}s)";
            }
        }

        #region IsLoading

        private bool _isLoading;

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged(nameof(IsLoading));
                RaisePropertyChanged(nameof(LoadingText));
            }
        }

        #endregion

        #region LoadingText

        private string _loadingText = "Loading...";

        public string LoadingText
        {
            get { return _loadingText; }
            set
            {
                _loadingText = value;
                RaisePropertyChanged(nameof(LoadingText));
            }
        }

        #endregion

        #region PlaybackMode

        private string _playbackMode;

        /// <summary>
        /// Indicates the playback mode: is it transcoding or direct stream.
        /// </summary>
        public string PlaybackMode
        {
            get { return _playbackMode; }
            set
            {
                _playbackMode = value;
                RaisePropertyChanged(nameof(PlaybackMode));
            }
        }

        #endregion

        /// <summary>
        /// The service for reporting current playback.
        /// </summary>
        private readonly IReportProgressService _reportProgressService;
        
        #endregion

        #region ctor

        public MediaPlaybackViewModel(IReportProgressService reportProgressService)
        {
            OSDUpdateTimer = new Timer();
            OSDUpdateTimer.Interval = 1000;
            OSDUpdateTimer.AutoReset = true;
            OSDUpdateTimer.Elapsed += OsdUpdateTimerOnElapsed;
            OSDUpdateTimer.Start();

            _reportProgressService = reportProgressService ??
                throw new ArgumentNullException(nameof(reportProgressService));

            ReportPlaybackStatusTimer = new Timer();
            ReportPlaybackStatusTimer.Interval = 10000;
            ReportPlaybackStatusTimer.AutoReset = true;
            ReportPlaybackStatusTimer.Elapsed += ReportPlaybackStatusTimer_Elapsed;
            ReportPlaybackStatusTimer.Start();
        }
        
        #endregion

        #region Additional methods

        protected override void Execute(string commandParameter)
        {
            switch (commandParameter)
            {
                case "Play":
                    Play();
                    break;
                case "Pause":
                    Pause();
                    break;
                case "Return":
                    Return();
                    break;
                case "SeekForward":
                    SeekRequest(30);
                    break;
                case "SeekBackward":
                    SeekRequest(-30);
                    break;
                default:
                    base.Execute(commandParameter);
                    break;
            }
        }

        public void Return()
        {
            Pause();
            NavigationService.GoBack();

            // to skip the "resume playback" screen
            if (IsPlaybackConfirmationDisplayedBefore)
            {
                NavigationService.GoBack();
            }
        }

        public void SeekRequest(int seconds)
        {
            lock (padlock)
            {
                if (SeekRequestTimer == null)
                {
                    SeekRequestedSeconds = seconds;

                    SeekRequestTimer = new Timer();
                    SeekRequestTimer.Interval = 1000;
                    SeekRequestTimer.Elapsed += SeekRequestTimerOnElapsed;
                    SeekRequestTimer.AutoReset = false;
                    SeekRequestTimer.Start();
                }
                else
                {
                    SeekRequestedSeconds += seconds;

                    SeekRequestTimer.Interval = 1000;
                    SeekRequestTimer.Start();
                }
            }
        }

        private void SeekRequestTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            SeekRequestTimer.Stop();
            Seek(SeekRequestedSeconds);
            SeekRequestedSeconds = 0;

            SeekRequestTimer.Elapsed -= SeekRequestTimerOnElapsed;
            SeekRequestTimer = null;
        }

        public void Seek(int seconds)
        {
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Globals.Instance.UIDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (MediaPlayer != null)
                {
                    MediaPlayer.MediaPlayer.PlaybackSession.Position =
                        MediaPlayer.MediaPlayer.PlaybackSession.Position + TimeSpan.FromSeconds(seconds);
                }
            });
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void Pause()
        {
            MediaPlayer?.MediaPlayer.Pause();
        }

        public void Play()
        {
            MediaPlayer?.MediaPlayer.Play();
        }

        public ControllerButtonHandledResult HandleKeyPressed(VirtualKey key)
        {
            switch (key)
            {
                case VirtualKey.Escape:
                    Return();
                    return new ControllerButtonHandledResult();
                case VirtualKey.GamepadRightTrigger:
                    Execute("SeekForward");
                    return new ControllerButtonHandledResult
                    {
                        ShouldOsdOpen = true,
                        ShouldStartLoading = true
                    };
                case VirtualKey.GamepadLeftTrigger:
                    Execute("SeekBackward");
                    return new ControllerButtonHandledResult
                    {
                        ShouldOsdOpen = true,
                        ShouldStartLoading = true
                    };
                default:
                    return null;
            }
        }

        private void ReportPlaybackStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (SelectedMediaElement == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(PlaybackMode))
            {
                return;
            }


            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Globals.Instance.UIDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (MediaPlayer?.MediaPlayer?.PlaybackSession?.Position == null)
                {
                    return;
                }

                _reportProgressService.Report(SelectedMediaElement.Id, PlaybackMode,
                    MediaPlayer.MediaPlayer.PlaybackSession.Position);
            });
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        private void OsdUpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
#pragma warning disable CS4014
            // Because this call is not awaited, execution of the current method continues before the call is completed
            Globals.Instance.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (MediaPlayer != null && MediaPlayer.MediaPlayer != null)
                {
                    MediaPlayer mp = MediaPlayer.MediaPlayer;
                    RemainingTimeLeft = mp.NaturalDuration - mp.PlaybackSession.Position;

                    RaisePropertyChanged(nameof(MediaPlayer));
                }
                else
                {
                    RemainingTimeLeft = TimeSpan.Zero;
                }
            });
#pragma warning restore CS4014
            // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        #endregion
    }
}