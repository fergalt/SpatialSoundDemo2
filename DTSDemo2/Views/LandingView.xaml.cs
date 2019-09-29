using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Media.MediaProperties;

namespace DTSDemo2.Views
{
    /// <summary>
    /// Interaction logic for LandingView.xaml
    /// </summary>
    public partial class LandingView : UserControl, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public double x;
        public double y;
        public double z;

        public LandingView()
        {
            InitializeComponent();
            this.DataContext = this;
            x = 0;
            y = 0;
            z = 0;
        }

        /// <summary>
        /// X position in metres
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
                OnPropertyChanged("X");
            }
        }

        /// <summary>
        /// Y position in metres
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
                OnPropertyChanged("Y");
            }
        }

        /// <summary>
        /// Z position in metres
        /// </summary>
        public double Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
                OnPropertyChanged("Z");
            }
        }

        /// <summary>
        /// Adds changed property to the event handler
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Handles dragging of listener in view, updates XY metric values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDragElementBehavior_Dragging(object sender, MouseEventArgs e)
        {
            Point PxPosition = Emitter.TranslatePoint(new Point(Emitter.Width / 2 - Listener.Width / 2, Emitter.Height / 2 - Listener.Height / 2), Listener);
            Point MetricPosition = new Point(PxPosition.X / Canvas.Width, PxPosition.Y / Canvas.Height);
            X = MetricPosition.X;
            Y = MetricPosition.Y;
        }

        private AudioGraph audioGraph;

        private async Task BuildAndStartAudioGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.GameEffects);
            settings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 32);
            settings.EncodingProperties.Subtype = MediaEncodingSubtypes.Float;
            settings.DesiredRenderDeviceAudioProcessing = Windows.Media.AudioProcessing.Raw;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status == AudioGraphCreationStatus.Success)
            {
                //_graph = result.Graph;
            }
        }
    }
}
