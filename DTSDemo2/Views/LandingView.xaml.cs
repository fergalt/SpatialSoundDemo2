// Copyright (c) 2019 Fergal Toohey

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
using System.Numerics;
using Windows.Storage;
using Windows.Storage.Pickers;
using DTSDemo2.Interfaces;

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

        public bool startButtonEnabled;
        public bool stopButtonEnabled;

        public string errorMessage;
        public string fileName;


        private AudioDeviceOutputNode deviceOutput;
        private AudioGraph graph;
        private AudioFileInputNode currentAudioFileInputNode = null;
        private StorageFile soundFile;
        private AudioNodeEmitter emitter;

        /// <summary>
        /// Constructor
        /// </summary>
        public LandingView()
        {
            InitializeComponent();
            this.DataContext = this;
            Loaded += LandingView_Loaded;
        }

        void LandingView_Loaded(object sender, RoutedEventArgs e)
        {
            StartButtonEnabled = false;
            StopButtonEnabled = false;
            FileName = "";
            UpdateXYPositions();
            ClearErrorMessage();
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
        /// Error Message
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
            set
            {
                errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        /// <summary>
        /// Name of the audio file
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        /// <summary>
        /// Start Button Enabled flag
        /// </summary>
        public bool StartButtonEnabled
        {
            get
            {
                return startButtonEnabled;
            }
            set
            {
                startButtonEnabled = value;
                OnPropertyChanged("StartButtonEnabled");
            }
        }

        /// <summary>
        /// Stop Button Enabled flag
        /// </summary>
        public bool StopButtonEnabled
        {
            get
            {
                return stopButtonEnabled;
            }
            set
            {
                stopButtonEnabled = value;
                OnPropertyChanged("StopButtonEnabled");
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
                if (emitter != null)
                {
                    emitter.Position = new Vector3((float)X, (float)Z, (float)-Y);
                }
            }
        }

        /// <summary>
        /// Handles dragging of listener in view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDragElementBehavior_Dragging(object sender, MouseEventArgs e)
        {
            UpdateXYPositions();
        }

        /// <summary>
        /// Updates the XY metric values based on listener position in view
        /// </summary>
        private void UpdateXYPositions()
        {
            Point PxPosition = Emitter.TranslatePoint(new Point(Emitter.Width / 2 - Listener.Width / 2, Emitter.Height / 2 - Listener.Height / 2), Listener);
            Point MetricPosition = new Point(PxPosition.X / Canvas.Width, PxPosition.Y / Canvas.Height);
            X = MetricPosition.X * 3;
            Y = -MetricPosition.Y * 3;
        }


        /// <summary>
        /// Builds audio graph and assignes output device
        /// </summary>
        /// <returns></returns>
        private async Task BuildAudioGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.GameEffects);
            settings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 32);
            settings.EncodingProperties.Subtype = MediaEncodingSubtypes.Float;
            settings.DesiredRenderDeviceAudioProcessing = Windows.Media.AudioProcessing.Raw;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw (new AudioGraphCreationException(String.Format("AudioGraph creation error: {0}", result.Status.ToString())));
            }
            graph = result.Graph;
            CreateAudioDeviceOutputNodeResult deviceResult = await graph.CreateDeviceOutputNodeAsync();

            if (deviceResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw (new AudioGraphCreationException(String.Format("Audio device error: {0}", result.Status.ToString())));
            }
            deviceOutput = deviceResult.DeviceOutputNode;
            graph.UnrecoverableErrorOccurred += Graph_UnrecoverableErrorOccurred;
        }

        /// <summary>
        /// Handles AudioGraph errors (e.g. device change) and attempts to restart playback from same position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Graph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            Console.WriteLine(String.Format("Unrecoverable Error Occurred, restaring: {0}", args.Error.ToString()));
            TimeSpan CurrentPlayPosition = currentAudioFileInputNode != null ? currentAudioFileInputNode.Position : TimeSpan.Zero;
            sender.Dispose();
            StartButtonEnabled = false;
            StopButtonEnabled = false;
            await LoadAudioFile(CurrentPlayPosition);
        }

        /// <summary>
        /// Handles competion of audio file playback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentAudioFileInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            Stop();
        }
        /// <summary>
        /// Handles Stop button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Handles Start button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        /// <summary>
        /// Handles Load button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".wav");

            // Link file picker to current window thread.
            ((IInitializeWithWindow)(object)openPicker).Initialize(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
            soundFile = await openPicker.PickSingleFileAsync();
            await LoadAudioFile(TimeSpan.Zero);
        }

        /// <summary>
        /// Stops playback and resets position to zero.
        /// </summary>
        private void Stop()
        {
            graph.Stop();
            currentAudioFileInputNode.Seek(TimeSpan.Zero);
            StartButtonEnabled = true;
            StopButtonEnabled = false;
        }


        /// <summary>
        /// Attempts to start playback. Reloads file and audiograph on failure.
        /// </summary>
        private async void Start()
        {
            try
            {
                graph.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Playback failed, restarting: {0}", ex.ToString()));
                await LoadAudioFile(TimeSpan.Zero);
                try
                {
                    graph.Start();
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(String.Format("Playback failed, aborting: {0}", ex2.ToString()));
                    ShowErrorMessage(String.Format("Playback failed: {0}", ex2.ToString()));
                    return;
                }
            }
            StartButtonEnabled = false;
            StopButtonEnabled = true;
        }

        /// <summary>
        /// Loads audio file.
        /// </summary>
        /// <param name="PlayPosition"></param>
        /// <returns></returns>
        private async Task LoadAudioFile(TimeSpan PlayPosition)
        {
            if (soundFile != null)
            {
                ClearErrorMessage();
                StartButtonEnabled = false;
                StopButtonEnabled = false;
                FileName = "";

                try
                {
                    await BuildAudioGraph();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex.Message);
                    return;
                }
                emitter = new AudioNodeEmitter(AudioNodeEmitterShape.CreateOmnidirectional(), AudioNodeEmitterDecayModel.CreateCustom(0.1, 1), AudioNodeEmitterSettings.None);
                emitter.Position = new Vector3((float)X, (float)Z, (float)-Y);

                CreateAudioFileInputNodeResult inCreateResult = await graph.CreateFileInputNodeAsync(soundFile, emitter);
                
                switch (inCreateResult.Status)
                {
                    case AudioFileNodeCreationStatus.FormatNotSupported:
                    case AudioFileNodeCreationStatus.InvalidFileType:
                        ShowErrorMessage(String.Format("Could not load {0}. \n\nPlease choose a different file - For Windows Spatial Sound processing, audio files must be Mono 48kHz PCM Wav.", soundFile.Name));
                        return;
                    case AudioFileNodeCreationStatus.UnknownFailure:
                    case AudioFileNodeCreationStatus.FileNotFound:
                        ShowErrorMessage(String.Format("Audio file load error: {0}", inCreateResult.Status.ToString()));
                        return;
                }

           
                //Dispose of previous input node.
                if (currentAudioFileInputNode != null)
                {
                    try
                    {
                        currentAudioFileInputNode.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        //Do nothing
                    }
                }
                currentAudioFileInputNode = inCreateResult.FileInputNode;
                currentAudioFileInputNode.AddOutgoingConnection(deviceOutput);
                currentAudioFileInputNode.FileCompleted += CurrentAudioFileInputNode_FileCompleted;
                FileName = currentAudioFileInputNode.SourceFile.Name;

                if (PlayPosition != TimeSpan.Zero)
                {
                    currentAudioFileInputNode.Seek(PlayPosition);
                    graph.Start();
                    StopButtonEnabled = true;
                }
                else
                {
                    StartButtonEnabled = true;
                }
            }
        }

        /// <summary>
        /// Sets error message to appear red textblock
        /// </summary>
        /// <param name="message"></param>
        private void ShowErrorMessage(string message)
        {
            ErrorMessage = message;
        }

        /// <summary>
        /// Clears the error message 
        /// </summary>
        private void ClearErrorMessage()
        {
            ErrorMessage = "";
        }

        /// <summary>
        /// Exception thrown if audiograph fails to build
        /// </summary>
        public class AudioGraphCreationException : Exception
        {
            public AudioGraphCreationException(string message) : base(message)
            {
            }
        }
    }
}
