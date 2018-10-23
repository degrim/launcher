using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Net;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AvariceLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string local_ver = Launcher.ReadVer();
        private string remote_ver = Launcher.UpdateVer();

        public MainWindow()
        {
            InitializeComponent();
            this.Title = Launcher.UpdateTitle();

            DLProgress.Visibility = Visibility.Hidden;
            FileProgress.Visibility = Visibility.Hidden;
            Speed.Visibility = Visibility.Hidden;
            Filename.Visibility = Visibility.Hidden;
            Filesize.Visibility = Visibility.Hidden;
            CRCBar.Visibility = Visibility.Hidden;
            CRCLabel.Visibility = Visibility.Hidden;

            if (float.Parse(local_ver) >= float.Parse(remote_ver))
            {
                PlayBtn.Visibility = System.Windows.Visibility.Visible;
                PatchBtn.Visibility = System.Windows.Visibility.Hidden;
                Repair.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                PlayBtn.Visibility = System.Windows.Visibility.Hidden;
                PatchBtn.Visibility = System.Windows.Visibility.Visible;
                Repair.Visibility = System.Windows.Visibility.Hidden;
            }

            //InitButtons();
        }

        private void WebButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchWebsite("https://avarice.online/");
        }

        private void ForumButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchWebsite("https://avarice.online/forum/");
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchWebsite("https://avarice.online/account/");
        }

        public async void PatchBtn_Click(object sender, RoutedEventArgs e)
        {
            PatchBtn.IsEnabled = false;
            Task t1 = Task.Run(() => Launcher.NewUpdate());
            Task t2 = Task.Run(() => toggleOn());
            await Task.WhenAll(t1, t2);
            Task t3 = Task.Run(() => UpdateTitle());
            Task t3b = Task.Run(() => toggleOn());
            await Task.WhenAll(t3, t3b);
            Task t4 = Task.Run(() => Launcher.MakeMD5List());
            await Task.WhenAll(t4);
            Task t5 = Task.Run(() => toggleOff());
            await Task.WhenAll(t5);
        }

        public void toggleOn()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Toggle(DLProgress);
                Toggle(FileProgress);
                Toggle(Speed);
                Toggle(Filename);
                Toggle(Filesize);
                this.UpdateLayout();
            }));
        }

        public void toggleOff()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                CRCBar.Visibility = Visibility.Hidden;
                CRCLabel.Visibility = Visibility.Hidden;
                PatchBtn.Visibility = Visibility.Hidden;
                PlayBtn.Visibility = Visibility.Visible;
            }));
        }

        public static void Toggle(Control toggle)
        {
            if (toggle.Visibility == Visibility.Hidden)
            {
                toggle.Visibility = Visibility.Visible;
            }
            else if (toggle.Visibility == Visibility.Collapsed)
            {
                toggle.Visibility = Visibility.Visible;
            }
            else
            {
                toggle.Visibility = Visibility.Hidden;
            }
        }

        public void UpdateTitle()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.Title = Launcher.UpdateTitle();
            }));
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayBtn.IsEnabled = false;
            if (File.Exists("Client\\test.exe"))
            {
                Launcher.PlayGame();
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                PlayBtn.Content = "Client not found";
            }
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}