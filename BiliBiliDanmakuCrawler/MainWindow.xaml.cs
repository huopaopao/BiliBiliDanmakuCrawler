using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace BiliBiliDanmakuCrawler
{
    /// <inheritdoc cref="MainWindow" />
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InvertIf")]
    public partial class MainWindow : Window
    {
        public static ProgressBar _SearchProgressBar;
        public static TextBlock ProgressTextBlock;
        private readonly List<User> _danmakuList = new List<User>();

        private readonly Dictionary<string, string> _danmakuXml = new Dictionary<string, string>();
        private BackgroundWorker _danmakuWorker;
        private bool _flag;

        public MainWindow()
        {
            _flag = false;
            InitializeComponent();
            _SearchProgressBar = SearchProgressBar;
            ProgressTextBlock = ProgressBlock;
        }

        private void Worker_RunWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            var userList = new List<User>();
            _flag = true;
            if (e.Error != null)
            {
                MessageBox.Show("该aid对应的视频不存在，请确认视频是否被删除及aid是否正确");
                GetVideoButton.IsEnabled = true;
                return;
            }

            DanmakuList.Items.Clear();
            foreach (var pair in _danmakuXml)
            {
                var user = new User
                {
                    DanmakuContent = pair.Key.Trim(),
                    Id = pair.Value
                };
                userList.Add(user);
                _danmakuList.Add(user);
            }

            DanmakuList.ItemsSource = userList;
            GetVideoButton.IsEnabled = true;
        }

        private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SearchProgressBar.Value = e.ProgressPercentage;
            ProgressBlock.Text = e.UserState.ToString();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var xml = BiliBiliDanmakuCrawler.GetDanmakuXml(e.Argument.ToString());

            var i = 0;
            var document = new XmlDocument();
            ThreadPool.SetMinThreads(32, 32);
            ThreadPool.SetMinThreads(32, 32);
            document.LoadXml(xml);
            var nodes = document.GetElementsByTagName("d");
            var size = nodes.Count;

            foreach (XmlNode documentNode in nodes)
            {
                var attr = documentNode.Attributes?["p"].Value;
                var hash = attr?.Split(',')[6];
                try
                {
                    _danmakuXml[documentNode.InnerText] = hash;
                }
                catch (Exception)
                {
                    //ignored
                }

                _danmakuWorker.ReportProgress((int) Math.Round((double) ++i / size * 100), $"{i}/{size}");
            }
        }

        public BackgroundWorker MakeInstance()
        {
            var danmakuWorker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
            danmakuWorker.DoWork += Worker_DoWork;
            danmakuWorker.ProgressChanged += Worker_OnProgressChanged;
            danmakuWorker.RunWorkerCompleted += Worker_RunWorkerComplete;
            return danmakuWorker;
        }

        private void GetVideoButton_Click(object sender, RoutedEventArgs e)
        {
            SearchProgressBar.Value = 0;
            SearchProgressBar.Visibility = Visibility.Visible;
            ProgressBlock.Visibility = Visibility.Visible;
            GetVideoButton.IsEnabled = false;
            _danmakuWorker = MakeInstance();
            _danmakuWorker.RunWorkerAsync(AidBox.Text);

            DanmakuList.Visibility = Visibility.Visible;
            DanmakuContentBox.Visibility = Visibility.Visible;
            SearchDanmakuButton.Visibility = Visibility.Visible;
        }

        private async void DanmakuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!UserNameBox.FontFamily.Equals(new FontFamily("Corbel Light")) || !DanmakuContentBox.FontFamily.Equals(new FontFamily("Corbel Light")))
            {
                OnGotFocusFontChange(UserNameBox);
                OnGotFocusFontChange(DanmakuContentBox);
            }
            // ReSharper disable once AssignNullToNotNullAttribute
            if (DanmakuList.SelectedItem != null)
            { 
                var user = DanmakuList.SelectedItem as User;
                var id = "";
                await Task.Run(() => id = BiliBiliDanmakuCrawler.DecryptHash(user.Id));
                var content = $"https://space.bilibili.com/{id}";
                UserNameBox.Text = content;
                DanmakuContentBox.Text = user.DanmakuContent;
            }
        }

        private void ViewInExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserNameBox.Text.StartsWith("http") && !UserNameBox.Text.IsNumber())
            {
                MessageBox.Show("请输入正确的用户空间链接或用户id");
                return;
            }

            Process.Start(UserNameBox.Text.StartsWith("http")
                ? UserNameBox.Text
                : $"http://space.bilibili.com/{UserNameBox.Text}");
        }

        private async void SearchDanmakuButton_Click(object sender, RoutedEventArgs e)
        {
            var flag = _danmakuList.Select(t => t.DanmakuContent).Contains(DanmakuContentBox.Text);

            if (flag)
            {
                foreach (var t in _danmakuList)
                {
                    if (t.DanmakuContent == DanmakuContentBox.Text)
                    {
                        var id = "";
                        await Task.Run(() => id = BiliBiliDanmakuCrawler.DecryptHash(t.Id));
                        UserNameBox.Text = $"http://space.bilibili.com/{id}";
                    }
                }

                return;
            }

            MessageBox.Show("该弹幕不存在");
        }

        private void MainGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            var rand = new Random();
            var files = new DirectoryInfo("../../backgrounds").GetFiles();
            var bg = files[rand.Next(files.Length)].FullName;
            BgImage.Source = new BitmapImage(new Uri(bg));
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("mailto: decem0730@gmail.com");
        }

        private void ViewGithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Rinacm");
        }

        private static void OnGotFocusFontChange(TextBox box)
        {
            box.Text = "";
            box.Foreground = Brushes.Black;
            box.FontFamily = new FontFamily("Corbel Light");
        }

        private void AidBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            OnGotFocusFontChange(AidBox);
        }

        private void UserNameBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            OnGotFocusFontChange(UserNameBox);
        }
        private void DanmakuContentBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            OnGotFocusFontChange(DanmakuContentBox);
        }
    }
}