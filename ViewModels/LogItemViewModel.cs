using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;


namespace MFAWPF.ViewModels
{
    /// <summary>
    /// The view model of log item.
    /// </summary>
    public class LogItemViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogItemViewModel"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="color">The font color.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="dateFormat">The Date format string</param>
        public LogItemViewModel(string content, Brush color, string weight = "Regular", string dateFormat = "MM'-'dd'  'HH':'mm':'ss", bool showTime = true)
        {
            // if (Instances.SettingsViewModel.UseLogItemDateFormat)
            // {
            //     dateFormat = Instances.SettingsViewModel.LogItemDateFormatString;
            // }

            Time = DateTime.Now.ToString(dateFormat);
            Content = content;
            Color = color;
            Weight = weight;
            ShowTime = showTime;
        }

        private string _time;

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        public string Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        private bool _showTime = true;

        public bool ShowTime
        {
            get => _showTime;
            set => SetProperty(ref _showTime, value);
        }

        private string _content;

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        private Brush _color;

        /// <summary>
        /// Gets or sets the font color.
        /// </summary>
        public Brush Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private string _weight;

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public string Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }
    }
}
