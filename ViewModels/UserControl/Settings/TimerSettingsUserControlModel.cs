using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using System.Windows.Threading;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class TimerSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _customConfig = Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.CustomConfig, bool.FalseString));
    [ObservableProperty] private bool _forceScheduledStart = Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.ForceScheduledStart, bool.FalseString));

    public TimerModel TimerModels { get; set; } = new();

    partial void OnCustomConfigChanged(bool value)
    {
        GlobalConfiguration.SetValue(ConfigurationKeys.CustomConfig, value.ToString());
    }

    partial void OnForceScheduledStartChanged(bool value)
    {
        GlobalConfiguration.SetValue(ConfigurationKeys.ForceScheduledStart, value.ToString());
    }

    public partial class TimerModel
    {
        public partial class TimerProperties : ObservableObject
        {
            public TimerProperties(int timeId, bool isOn, int hour, int min, string? timerConfig)
            {
                TimerId = timeId;
                _isOn = isOn;
                _hour = hour;
                _min = min;
                TimerName = $"{"Timer".ToLocalization()} {TimerId + 1}";
                if (timerConfig == null || !ConfigurationHelper.Configs.Any(c => c.Name.Equals(timerConfig)))
                {
                    _timerConfig = ConfigurationHelper.GetCurrentConfiguration();
                }
                else
                {
                    _timerConfig = timerConfig;
                }
                LanguageHelper.LanguageChanged += OnLanguageChanged;
            }

            public int TimerId { get; set; }

            [ObservableProperty] private string _timerName;

            private void OnLanguageChanged(object sender, EventArgs e)
            {
                TimerName = $"{"Timer".ToLocalization()} {TimerId + 1}";
            }

            private bool _isOn;

            /// <summary>
            /// Gets or sets a value indicating whether the timer is set.
            /// </summary>
            public bool IsOn
            {
                get => _isOn;
                set
                {
                    SetProperty(ref _isOn, value);
                    GlobalConfiguration.SetTimer(TimerId, value.ToString());
                }
            }

            private int _hour;

            /// <summary>
            /// Gets or sets the hour of the timer.
            /// </summary>
            public int Hour
            {
                get => _hour;
                set
                {
                    value = Math.Clamp(value, 0, 23);
                    SetProperty(ref _hour, value);
                    GlobalConfiguration.SetTimerHour(TimerId, _hour.ToString());
                }
            }

            private int _min;

            /// <summary>
            /// Gets or sets the minute of the timer.
            /// </summary>
            public int Min
            {
                get => _min;
                set
                {
                    value = Math.Clamp(value, 0, 59);
                    SetProperty(ref _min, value);
                    GlobalConfiguration.SetTimerMin(TimerId, _min.ToString());
                }
            }

            private string? _timerConfig;

            /// <summary>
            /// Gets or sets the config of the timer.
            /// </summary>
            public string? TimerConfig
            {
                get => _timerConfig;
                set
                {
                    SetProperty(ref _timerConfig, value ?? ConfigurationHelper.GetCurrentConfiguration());
                    GlobalConfiguration.SetTimerConfig(TimerId, _timerConfig);
                }
            }
        }

        public TimerProperties[] Timers { get; set; } = new TimerProperties[8];
        private readonly DispatcherTimer _dispatcherTimer;
        public TimerModel()
        {
            for (var i = 0; i < 8; i++)
            {
                Timers[i] = new TimerProperties(
                    i,
                    GlobalConfiguration.GetTimer(i, bool.FalseString) == bool.TrueString,
                    int.Parse(GlobalConfiguration.GetTimerHour(i, $"{i * 3}")),
                    int.Parse(GlobalConfiguration.GetTimerMin(i, "0")), GlobalConfiguration.GetTimerConfig(i, ConfigurationHelper.GetCurrentConfiguration()));
            }
            _dispatcherTimer = new();
            _dispatcherTimer.Interval = TimeSpan.FromMinutes(1);
            _dispatcherTimer.Tick += CheckTimerElapsed;
            _dispatcherTimer.Start();
        }

        private void CheckTimerElapsed(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            foreach (var timer in Timers)
            {
                if (timer.IsOn && timer.Hour == currentTime.Hour && timer.Min == currentTime.Minute)
                {
                    ExecuteTimerTask(timer.TimerId);
                }
                if (timer.IsOn && timer.Hour == currentTime.Hour && timer.Min == currentTime.Minute + 2)
                {
                    SwitchConfiguration(timer.TimerId);
                }
            }
        }

        private void SwitchConfiguration(int timerId)
        {
            var timer = Timers.FirstOrDefault(t => t.TimerId == timerId, null);
            if (timer != null)
            {
                var config = timer.TimerConfig ?? ConfigurationHelper.GetCurrentConfiguration();
                if (config != ConfigurationHelper.GetCurrentConfiguration())
                {
                    ConfigurationHelper.SetDefaultConfig(config);
                    MaaProcessor.RestartMFA(true);
                }
            }
        }

        private void ExecuteTimerTask(int timerId)
        {
            var timer = Timers.FirstOrDefault(t => t.TimerId == timerId, null);
            if (timer != null)
            {
                if (Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.ForceScheduledStart, bool.FalseString)) && Instances.RootViewModel.IsRunning)
                    Instances.TaskQueueView.Stop();
                Instances.TaskQueueView.Start();
            }
        }
    }
}
