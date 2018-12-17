using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml;

// ReSharper disable IdentifierTypo
namespace BiliBiliDanmakuCrawler
{
    /// <summary>
    /// 后台处理逻辑
    /// </summary>
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class BiliBiliDanmakuCrawler
    {
        public static Func<string, string> Api = s => $"https://biliquery.typcn.com/api/user/hash/{s}",
            Trend = s => $"https://api.bilibili.com/x/v1/dm/list.so?oid={s}";

        public static int Size, Current;

        public static string GetDanmakuXml(string aid)
        {
            var header = new Dictionary<string, string>
            {
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.110 Safari/537.36"
                }
            };
            var cid = SendRequest($"https://www.bilibili.com/video/av{aid}", header).DecompressGzipBytes()
                .GetString(Encoding.UTF8)
                .PatternFirst("\"pages\":\\[\\{\"cid\":(\\d+)", 1);
            return SendRequest(Trend(cid), header).DecompressDeflateBytes().GetString(Encoding.UTF8);

        }

        public static string DecryptHash(string hash)
        {
            var header = new Dictionary<string, string>
            {
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.110 Safari/537.36"
                }
            };
            return SendRequest(Api(hash), header).GetString(Encoding.UTF8).PatternFirst("\"id\":(\\d+)", 1);
        }

        private static void SetProgressBarMaxValue(RangeBase progressBar, int value) =>
            progressBar.Dispatcher.Invoke(() => progressBar.Maximum = value);

        private static void UpdateProgressBar(RangeBase progressBar) =>
            progressBar.Dispatcher.Invoke(() => progressBar.Value = Current);

        private static void UpdateTextBlock(TextBlock textBlock) =>
            textBlock.Dispatcher.Invoke(() => { textBlock.Text = $"{Current}/{Size}"; });

        public static byte[] SendRequest(string url, Dictionary<string, string> header = null)
        {
            var clients = new WebClient();
            // ReSharper disable once InvertIf
            if (header != null)
            {
                foreach (var property in header)
                {
                    clients.Headers[property.Key] = property.Value;
                }
            }
            return clients.DownloadData(url);
        }

        public static Dictionary<string, string> AnalyzeDanmaku(string text)
        {
            var document = new XmlDocument();
            var xmlMap = new Dictionary<string, string>();
            ThreadPool.SetMinThreads(32, 32);
            ThreadPool.SetMinThreads(32, 32);
            document.LoadXml(text);
            var nodes = document.ChildNodes;
            SetProgressBarMaxValue(MainWindow._SearchProgressBar, nodes.Count);
            foreach (XmlNode documentNode in nodes)
            {
                new Task(() =>
                {
                    UpdateProgressBar(MainWindow._SearchProgressBar);
                    UpdateTextBlock(MainWindow.ProgressTextBlock);
                    var attr = documentNode.Attributes?["p"].Value;
                    var hash = attr?.Split(',')[6];
                    try
                    {
                        xmlMap[documentNode.InnerText] = DecryptHash(hash);
                    }
                    catch (Exception)
                    {
                        //ignored
                    }
                }).Start();
            }
            return xmlMap;
        }

        public static Dictionary<string, string> GetPair(string aid)
        {
            var xml = GetDanmakuXml(aid);
            var danmaku = AnalyzeDanmaku(xml);
            Console.WriteLine(@"finish");
            return danmaku;
        }
    }
}
