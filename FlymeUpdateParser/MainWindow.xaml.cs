using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace FlymeUpdateParser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public string filePath = "";
        public string stringRegexMatch = @"(.*)\nサイズ:((.*)\s(G|M)B\s\((.*)\sバイト\))*\nディスク上のサイズ:((.*)\s(G|M)B\s*.\((.*)\sバイト\))*\nMD5:(.*)\nSHA1:(.*)\n(https?.+/((\d{1,2}\.){3}\d{1,2})/([a-zA-Z_]+)/(\d{4})(\d{2})(\d{2})\d{6}/.+)";
        public string stringRegexReplace = @"|Flyme $13$15|$1|$16/$17/$18|$3$4B ($5 Bytes)|$7$8B ($9 Bytes)|$10|$11|<$12>|";
        MatchCollection releaseDate;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Access_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TextBox_Url.Text);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream recStream = response.GetResponseStream();
                StreamReader sr = new StreamReader(recStream, Encoding.UTF8);
                string stringItems = Regex.Replace(sr.ReadToEnd(), @"<\/*[^<>]*>", "", RegexOptions.IgnoreCase);
                stringItems = stringItems.Replace("\r\n", "").Replace("\r", "").Replace("&nbsp;", "");
                MatchCollection matches = Regex.Matches(stringItems, stringRegexMatch, RegexOptions.IgnoreCase);
                releaseDate = Regex.Matches(stringItems, @"リリース日は([0-9]{4})年([0-9]{1,2})月([0-9]{1,2})日。");
                foreach (Match match in matches)
                {
                    TextBox_Item.Text += ProcessItem(match.Value);
                    TextBox_Item.Text += "\n";
                }
            }
            catch (Exception ex)
            {
                TextBox_Item.Text = ex.Message;
            }

        }

        private string ProcessItem(string stringOrigin)
        {
            string updateVersion = Regex.Replace(stringOrigin, stringRegexMatch, @"$13");
            string updateType = Regex.Replace(stringOrigin, stringRegexMatch, @"$15");
            string stringUpdateType;
            if (updateType == "cn" || updateType == "intl" || updateType == "RU")
            {
                updateType = Regex.Replace(updateType, @"cn", @"A").Replace(@"intl", @"G").Replace(@"ru", @"RU");
                stringUpdateType = "稳定版";
            }
            else if (updateType == "cn_beta" || updateType == "intl_beta")
            {
                updateType = Regex.Replace(updateType, @"cn_beta", @" beta").Replace(@"intl_beta", @"G beta");
                stringUpdateType = "体验版";
            }
            else if (updateType == "daily")
            {
                updateType = Regex.Replace(updateType, @"daily", @" daily");
                stringUpdateType = "内测版";
            }
            else
            {
                stringUpdateType = "";
            }
            string model = Regex.Replace(stringOrigin, stringRegexMatch, @"$1");
            string stringCountry = "";
            if (updateType == "G" || updateType == "G beta")
            {
                model += " 国际版";
                stringCountry = "海外版";
            }
            else if (updateType == "RU")
            {
                model += " 俄罗斯版";
                stringCountry = "海外版";
            }
            else
            {
                model = Regex.Replace(model, @"Meizu", "").Trim();
                model = Regex.Replace(model, @"PRO 7-H", @"PRO 7 高配版").Replace(@"PRO 7-S", @"PRO 7 标准版");
                model = Regex.Replace(model, @"V8", @"V8 标配版").Replace(@"V8 标配版 Pro", @"V8 高配版");
                model = Regex.Replace(model, @"M1 E/M3E", "魅蓝 E").Replace(@"M2 E", @"魅蓝 E2").Replace(@"E3", @"魅蓝 E3");
                model = Regex.Replace(model, @"M3X", @"魅蓝 X");
                model = Regex.Replace(model, @"(U[12]0)", @"魅蓝 $1");
                model = Regex.Replace(model, @"M3 Max", @"魅蓝 Max");
                model = Regex.Replace(model, @"S6/M6s", @"魅蓝 S6");
                model = Regex.Replace(model, @"m1\smetal", @"魅蓝 metal");
                model = Regex.Replace(model, @"Note([89])", @"魅族 note$1");
                model = Regex.Replace(model, @"[mM]([12356]) [nN]ote", @"魅蓝 note$1");
                model = Regex.Replace(model, @"[mM]([123568][scT]*)$", @"魅蓝 $1");
                model = Regex.Replace(model, @"15 Lite", @"M15");
                stringCountry = "国内版";
            }
            string updateDate;
            if (releaseDate.Count == 0)
            {
                updateDate = Regex.Replace(stringOrigin, stringRegexMatch, @"$16/$17/$18");
            }
            else
            {
                updateDate = Regex.Replace(releaseDate[0].Value, @"リリース日は([0-9]{4})年([0-9]{1,2})月([0-9]{1,2})日。", @"$1/$2/$3");
            }
            updateDate = Regex.Replace(updateDate, @"/0([0-9])", @"/$1");
            string updateComment = "<!--" + model + "|" + stringCountry + "|" + stringUpdateType + "-->";
            string returnString = updateComment + "|Flyme " + updateVersion + updateType + "|" + model + "|" + updateDate + Regex.Replace(stringOrigin, stringRegexMatch, @"|$3$4B ($5 Bytes)|$7$8B ($9 Bytes)|$10|$11|<$12>|").Replace(@"B ( Bytes)", "");
            return returnString;
        }

        private void Button_LoadWiki_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "Markdown 源文件 (*.md)|*.md"
                };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    filePath = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private void Button_InsertItems_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8);
                string stringFullWiki = streamReader.ReadToEnd();
                MatchCollection items = Regex.Matches(TextBox_Item.Text, @"(<!--.*-->)(.*)");
                foreach (Match item in items)
                {
                    string stringComment = Regex.Replace(item.Value, @"(<!--.*-->)(.*)", "$1");
                    //string stringMatch = (@"(" + stringComment + @"(\n.*){2}\n)").Replace(@"|", @"\|");
                    //string stringReplace = @"$1" + Regex.Replace(item.Value, @"(<!--.*-->)(.*)", @"$2\n");
                    string stringNewItem = Regex.Replace(item.Value, @"(<!--.*-->)(.*)", "\n$2");
                    int intIndex = stringFullWiki.IndexOf(@"|----|----|----|----|----|----|----|----|", stringFullWiki.LastIndexOf(stringComment));
                    stringFullWiki = stringFullWiki.Insert(intIndex + (@"|----|----|----|----|----|----|----|----|").Length, stringNewItem);

                    //stringFullWiki = Regex.Replace(stringFullWiki, stringMatch, stringReplace, RegexOptions.IgnoreCase);
                }
                stringFullWiki = Regex.Replace(stringFullWiki, @"update\.zip>\|\n\n\|Flyme", @"update.zip>|\n|Flyme");
                TextBox_Item.Text = stringFullWiki;
            //}
            /*catch (Exception ex)
            {

                throw;
            }*/
        }
    }
}