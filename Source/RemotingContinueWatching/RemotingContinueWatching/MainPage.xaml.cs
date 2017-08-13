using RemotingContinueWatching.ViewModels;
using Windows.UI.Xaml.Controls;
using System;

namespace RemotingContinueWatching
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        public MainPage()
        {
            this.DataContext = ViewModel;
            this.InitializeComponent();
        }
    }
}
