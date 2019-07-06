using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

namespace FlymeUpdateParser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public string stringRegexMatch = @"(.*)\nサイズ:((.*)\s(G|M)B\s\((.*)\sバイト\))*\nディスク上のサイズ:((.*)\s(G|M)B\s*.\((.*)\sバイト\))*\nMD5:(.*)\nSHA1:(.*)\n(https?.+/((\d{1,2}\.){3}\d{1,2})/([a-zA-Z_]+)/(\d{4})(\d{2})(\d{2})\d{6}/.+)";
        public string stringRegexReplace = @"|Flyme $13$15|$1|$16/$17/$18|$3$4B ($5 Bytes)|$7$8B ($9 Bytes)|$10|$11|<$12>|";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Access_Click(object sender, RoutedEventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TextBox_Url.Text);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream recStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(recStream, Encoding.UTF8);
            string stringItems = Regex.Replace(sr.ReadToEnd(), @"<\/*[^<>]*>", "", RegexOptions.IgnoreCase);
            stringItems = stringItems.Replace("\r\n", "").Replace("\r", "").Replace("&nbsp;", "");
            MatchCollection matches = Regex.Matches(stringItems, stringRegexMatch, RegexOptions.IgnoreCase);
            TextBox_Item.Text = "";
            foreach (Match match in matches)
            {
                TextBox_Item.Text += ProcessItem(match.Value);
                TextBox_Item.Text += "\n";
            }
        }

        private string ProcessItem(string stringOrigin)
        {
            string updateType = Regex.Replace(stringOrigin, stringRegexMatch, @"$15").Replace("cn_beta", " beta").Replace("cn", "A").Replace("intl_beta", "G beta").Replace("intl", "G").Replace("ru", "RU");

            return Regex.Replace(stringOrigin, stringRegexMatch, stringRegexReplace, RegexOptions.IgnoreCase).Replace(@"GB ( Bytes)", "");
        }
    }
}
