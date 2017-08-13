using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Core;

namespace RemotingContinueWatching.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        RemoteSystemWatcher remoteSystemWatcher;
        public ObservableCollection<RemoteSystem> RemoteSystems { get; set; } = new ObservableCollection<RemoteSystem>();

        public RemoteSystem SelectedSystem { get; set; }

        public List<StreamViewModel> Videos => new List<StreamViewModel>
        {
            new StreamViewModel
            {
                DisplayName = "Big Buck Bunny",
                Name = "bigbugbunny",
                Source = new Uri("http://amssamples.streaming.mediaservices.windows.net/683f7e47-bd83-4427-b0a3-26a6c4547782/BigBuckBunny.ism/manifest")
            },
            new StreamViewModel
            {
                DisplayName = "To The Limit",
                Name = "tothelimit",
                Source = new Uri("http://playready.directtaps.net/smoothstreaming/TTLSS720VC1/To_The_Limit_720.ism/Manifest")
            },
            new StreamViewModel
            {
                DisplayName = "Super Speedway",
                Name = "superspeedway",
                Source = new Uri("http://playready.directtaps.net/smoothstreaming/SSWSS720H264/SuperSpeedway_720.ism/Manifest")

            }
        };

        private StreamViewModel _SelectedVideo;

        public StreamViewModel SelectedVideo
        {
            get
            {
                return _SelectedVideo;
            }
            set
            {
                StartPosition = null;
                _SelectedVideo = value;
                Source = value?.Source;
                OnPropertyChanged();
            }
        }


        private TimeSpan? _StartPosition;

        public TimeSpan? StartPosition
        {
            get { return _StartPosition;; }
            set
            {
                _StartPosition = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan CurrentPosition { get; set; }

        private Uri _Source;

        public Uri Source
        {
            get { return _Source; }
            set
            {
                _Source = value;
                OnPropertyChanged();
            }
        }

        public MainPageViewModel()
        {
            StartWatcherAsync();
        }

        private async Task StartWatcherAsync()
        {
            RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();
            if (accessStatus == RemoteSystemAccessStatus.Allowed)
            {
                remoteSystemWatcher = RemoteSystem.CreateWatcher();

                remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;

                remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;

                remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;

                remoteSystemWatcher.Start();

            }
            else
            {
                Debug.WriteLine("Access to Remote Systems is " + accessStatus.ToString());
                Debug.WriteLine("Make sure you have set the Remote System capability");
            }
        }

        private async void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                int index = RemoteSystems.IndexOf(RemoteSystems.FirstOrDefault(rs => rs.Id == args.RemoteSystem.Id));
                if (index > -1)
                    RemoteSystems[index] = args.RemoteSystem;
            });

        }

        private async void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RemoteSystems.Remove(RemoteSystems.FirstOrDefault(rs => rs.Id == args.RemoteSystemId));
            });
        }

        private async void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RemoteSystems.Add(args.RemoteSystem);
            });
        }

        public void Init(Uri uri)
        { 
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            try
            {
                //naive uri parsing :-)
                //good enough for demo-purposes
                parameters = uri.Query.Trim('?').Split('&').Select(s => s.Split('=')).ToDictionary(k => k[0], v => v[1]);
            }
            catch (Exception)
            {
                //
            }

            string video = parameters["video"];
            TimeSpan? startPosition = null;
            if(parameters.ContainsKey("position"))
            {
                if (int.TryParse(parameters["position"], out int p))
                {
                    //subtract 3 seconds from the starttime so the user doesn't miss anything
                    startPosition = TimeSpan.FromSeconds(Math.Max(0, p - 3));
                }
            }
            Load(video, startPosition);
        }

        private void Load(string name, TimeSpan? startPosition = null)
        {
            SelectedVideo = Videos.FirstOrDefault(v => v.Name == name);
            StartPosition = startPosition;
        }

        public async void OnContinueWatching()
        {
            if(SelectedSystem != null)
            {
                await RemoteLauncher.LaunchUriAsync(
                new RemoteSystemConnectionRequest(SelectedSystem),
                new Uri($"myvideoapp://?video={SelectedVideo.Name}&position={CurrentPosition.TotalSeconds}"));
            }
        }

        public void OnPropertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
