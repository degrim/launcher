using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;
using System.Security.Cryptography;
using System.Runtime.Remoting.Contexts;

namespace AvariceLauncher
{
    public class Launcher
    {
        public static string curpath = Directory.GetCurrentDirectory();
        public static string verpath = "ver.txt";
        public static string update = "https://avarice.online/client/updatelist.csv";
        public static string updatepath = "https://avarice.online/client/";
        public static string updatefile = "updatelist.csv";
        public static string remote_ver = "https://avarice.online/client/ver.txt";
        public static string md5file = "checksum.md5";

        public static void PlayGame()
        {
            Process.Start("Client\\test.exe");
        }

        public static string ReadVer()
        {
            string localver = "";
            if (!File.Exists(verpath))
            {
                using (StreamWriter verw = File.CreateText(verpath))
                {
                    localver = "1";
                    verw.WriteLine(localver);
                    return localver;
                }
            }
            if (File.Exists(verpath))
            {
                using (StreamReader verr = File.OpenText(verpath))
                {
                    while ((localver = verr.ReadLine()) != null)
                    {
                        return localver;
                    }
                }
            }
            return localver;
        }

        public static string UpdateVer()
        {
            string VersionString;

            WebClient webpage = new WebClient();
            VersionString = webpage.DownloadString(remote_ver);
            if (VersionString == "")
            {
                VersionString = "NULL";
            }

            return VersionString;
        }

        public static string UpdateTitle()
        {
            string title = "Project Avarice - Local Version " + ReadVer() + " Remote Version " + UpdateVer();
            return title;
        }

        public static void MakeMD5List()
        {
            string curpath = Directory.GetCurrentDirectory();
            string[] allfiles = Directory.GetFiles(curpath, "*.*", SearchOption.AllDirectories);
            string md5file = "checksum.md5";
            int filecount = allfiles.Length;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var main = ((MainWindow)System.Windows.Application.Current.MainWindow);
                main.CRCBar.Visibility = System.Windows.Visibility.Visible;
                main.CRCLabel.Visibility = System.Windows.Visibility.Visible;
                main.CRCBar.Maximum = filecount - 1;
                main.CRCBar.Value = 0;
            });

            using (StreamWriter md5sum = File.CreateText(md5file))
            {
                foreach (var files in allfiles)
                {
                    bool loop = true;
                    while (IsFileReady(files) == true && loop == true)
                    {
                        string md5 = CalculateMD5(files);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var main = ((MainWindow)System.Windows.Application.Current.MainWindow);
                            main.CRCBar.Value++;
                            main.CRCLabel.Content = "Checking File " + main.CRCBar.Value + "/" + main.CRCBar.Maximum;
                        });
                        loop = false;
                        md5sum.Write(files + "," + md5 + "\n");
                    }
                }
            }
        }

        public class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
                request.Timeout = 600000; //1 minute timeout
                return request;
            }
        }

        public static async Task NewUpdate()
        {
            WebClient updater = new WebClient();
            updater.DownloadFile(update, @updatefile);
            string[] filelist = File.ReadAllLines(updatefile);
            double linecount = filelist.Length;

            foreach (string files in filelist)
            {
                string[] filepath = files.Split(',');

                string dlpath = string.Concat(updatepath + filepath[0]);
                string curpath = Directory.GetCurrentDirectory();

                TimeoutWebClient dl = new TimeoutWebClient();

                if (!Directory.Exists(filepath[1]))
                {
                    Directory.CreateDirectory(filepath[1]);
                }

                string savpath = string.Concat(filepath[1] + "\\" + filepath[2]);

                if (!File.Exists(savpath))
                {
                    await DLContent(dlpath, savpath, linecount, filepath[2], dl);
                }
                if (File.Exists(savpath))
                {
                    bool loop = true;
                    while (IsFileReady(savpath) == true && loop == true)
                    {
                        if (!filepath[4].Equals(CalculateMD5(savpath)))
                        {
                            await DLContent(dlpath, savpath, linecount, filepath[2], dl);
                        }
                        loop = false;
                    }
                }
            }

            if (File.Exists(verpath))
            {
                File.WriteAllText(verpath, UpdateVer());
            }

            return;
        }

        public static async Task DLContent(string dlpath, string savpath, double linecount, string filename, TimeoutWebClient dl)
        {
            await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                //TimeoutWebClient dl = new TimeoutWebClient();
                var main = ((MainWindow)System.Windows.Application.Current.MainWindow);
                main.DLProgress.Maximum = linecount;
                main.Speed.Content = main.DLProgress.Value + " / " + linecount;
                main.Filename.Content = filename;

                dl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    double bytesIn = double.Parse(e.BytesReceived.ToString());
                    double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                    double percentage = bytesIn / totalBytes * 100;

                    main.Filesize.Content = SizeSuffix(Convert.ToInt64(bytesIn)) + " / " + SizeSuffix(Convert.ToInt64(totalBytes));

                    main.FileProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
                });

                dl.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(delegate (object sender, System.ComponentModel.AsyncCompletedEventArgs e)
                    {
                        if (e.Error == null && !e.Cancelled)
                        {
                            main.DLProgress.Value++;
                        }
                    });

                //main.Filesize.Content = SizeSuffix(GetFileSize(dlpath));

                await dl.DownloadFileTaskAsync(new Uri(dlpath), savpath);
            });
        }

        //private static long GetFileSize(string dlpath)
        //{
        //    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(dlpath);
        //    req.Method = "HEAD";
        //    HttpWebResponse resp = (HttpWebResponse)(req.GetResponse());
        //    long len = resp.ContentLength;
        //    resp.Close();
        //    return len;
        //}

        private static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

        public static void LaunchWebsite(string url)
        {
            Process.Start(url);
        }

        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static bool IsFileReady(String sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}