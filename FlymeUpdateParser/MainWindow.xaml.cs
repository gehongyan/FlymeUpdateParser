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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TextBox_Url.Text);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream recStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(recStream, Encoding.UTF8);
            MatchCollection pageMatches = Regex.Matches(sr.ReadToEnd(), @"<a\shref=""(https://reameizu.com/flyme-update.*/)"".*>", RegexOptions.IgnoreCase);
            for (int countIndex = pageMatches.Count - 1; countIndex > 0; countIndex--)
            {
                ProcessPage(Regex.Replace(pageMatches[countIndex].Value, @"<a\shref=""(https://reameizu.com/flyme-update.*/)"".*>", "$1"));
            }

        }

        private void ProcessPage(string link)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
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
            string updateLinkOrigin = Regex.Replace(stringOrigin, @".*(\n.*)+http(s)*:\/\/(.*)\/Firmware\/.*", @"$3");
            string updateModelIdentifier = Regex.Replace(stringOrigin, @".*(\n.*)+\/([a-zA-Z0-9_]+)\/((\d{1,2}\.){2,3}\d{1,2})\/([a-zA-Z_]+)\/.*", @"$2");
            string updateVersion = Regex.Replace(stringOrigin, @".*(\n.*)+\/([a-zA-Z0-9_]+)\/((\d{1,2}\.){2,3}\d{1,2})\/([a-zA-Z_]+)\/.*", @"$3");
            string updateType = Regex.Replace(stringOrigin, @".*(\n.*)+\/([a-zA-Z0-9_]+)\/((\d{1,2}\.){2,3}\d{1,2})\/([a-zA-Z_]+)\/.*", @"$5");

            // 所属类别标识注释
            string commentUpdateOverseas = "";  // 区分国内版/海外版
            string commentUpdateChannel = "";   // 区分更新通道，稳定版/体验版/内测版
            string commentUpdateModel = "";     // 区分机型

            string suffixUpdateCountry = "";    // 区分海外版的具体版本，国际版/俄罗斯版/欧盟版，机型后缀
            string suffixUpdateChannel = "";    // 更新通道后缀
            string stringUpdateModel = "";      // 区分机型，外显

            // 处理国内海外注释信息
            switch (updateLinkOrigin)
            {
                case "firmware.meizu.com":
                    commentUpdateOverseas = "国内版";
                    break;
                case "dl-res.flymeos.com":
                    commentUpdateOverseas = "海外版";
                    switch (updateType)     // 处理海外版的机型后缀
                    {
                        case "intl":
                        case "intl_beta":
                            suffixUpdateCountry = " 国际版";
                            break;
                        case "RU":
                            suffixUpdateCountry = " 俄罗斯版";
                            break;
                        case "cn":
                            suffixUpdateCountry = " 欧盟版";
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            // 处理更新通道注释信息
            switch (updateType)
            {
                case "cn":
                case "RU":
                case "intl":
                    commentUpdateChannel = "稳定版";
                    break;
                case "cn_beta":
                case "intl_beta":
                    commentUpdateChannel = "体验版";
                    break;
                case "daily":
                    commentUpdateChannel = "内测版";
                    break;
                default:
                    break;
            }

            // 处理更新通道后缀
            switch (updateType)
            {
                case "cn":
                    switch (commentUpdateOverseas)
                    {
                        case "国内版":
                            suffixUpdateChannel = "A";
                            break;
                        case "海外版":
                            suffixUpdateChannel = "EU";
                            break;
                        default:
                            break;
                    }
                    break;
                case "RU":
                    suffixUpdateChannel = "RU";
                    break;
                case "intl":
                    suffixUpdateChannel = "G";
                    break;
                case "ali":
                    suffixUpdateChannel = "Y";
                    break;
                case "pl":
                    suffixUpdateChannel = "C";
                    break;
                case "wo":
                    suffixUpdateChannel = "U";
                    break;
                case "cn_beta":
                    suffixUpdateChannel = " beta";
                    break;
                case "intl_beta":
                    suffixUpdateChannel = "G beta";
                    break;
                case "daily":
                    suffixUpdateChannel = " daily";
                    break;
                default:
                    break;
            }

            // 处理机型注释信息
            switch (updateModelIdentifier)
            {
                case "16s": commentUpdateModel = "16s"; break;
                case "16th_Plus": commentUpdateModel = "16th Plus"; break;
                case "16th": commentUpdateModel = "16th"; break;
                case "m1926": commentUpdateModel = "16Xs"; break;
                case "16": commentUpdateModel = "16 X"; break;
                case "15_Plus": commentUpdateModel = "15 Plus"; break;
                case "15": commentUpdateModel = "15"; break;
                case "M15": commentUpdateModel = "M15"; break;
                case "15Lite": commentUpdateModel = "M15"; break;
                case "PRO7_Plus": commentUpdateModel = "PRO 7 Plus"; break;
                case "PRO7H": commentUpdateModel = "PRO 7 高配版"; break;
                case "PRO7S": commentUpdateModel = "PRO 7 标准版"; break;
                case "PRO6_Plus": commentUpdateModel = "PRO 6 Plus"; break;
                case "PRO_6": commentUpdateModel = "PRO 6/PRO 6s"; break;
                case "PRO_5": commentUpdateModel = "PRO 5"; break;
                case "MX6": commentUpdateModel = "MX6"; break;
                case "MX5": commentUpdateModel = "MX5"; break;
                case "MX4_Pro": commentUpdateModel = "MX4 Pro"; break;
                case "MX4": commentUpdateModel = "MX4"; break;
                case "MX3": commentUpdateModel = "MX3"; break;
                case "MX2": commentUpdateModel = "MX2"; break;
                case "MX": commentUpdateModel = "MX"; break;
                case "M9": commentUpdateModel = "M9"; break;
                case "M8": commentUpdateModel = "M8"; break;
                case "M1816": commentUpdateModel = "V8 标配版"; break;
                case "M1813": commentUpdateModel = "V8 高配版"; break;
                case "note9": commentUpdateModel = "Note9"; break;
                case "m1923": commentUpdateModel = "Note9"; break;
                case "note8": commentUpdateModel = "Note8"; break;
                case "M1852": commentUpdateModel = "X8"; break;
                case "M3X": commentUpdateModel = "魅蓝 X"; break;
                case "M3E": commentUpdateModel = "魅蓝 E3"; break;
                case "M2E": commentUpdateModel = "魅蓝 E2"; break;
                case "M1_E": commentUpdateModel = "魅蓝 E"; break;
                case "MAX": commentUpdateModel = "魅蓝 Max"; break;
                case "U20": commentUpdateModel = "魅蓝 U20"; break;
                case "U10": commentUpdateModel = "魅蓝 U10"; break;
                case "m1_metal": commentUpdateModel = "魅蓝 metal"; break;
                case "m6_note": commentUpdateModel = "魅蓝 Note6"; break;
                case "m5_note": commentUpdateModel = "魅蓝 Note5"; break;
                case "m3_note": commentUpdateModel = "魅蓝 Note3"; break;
                case "m2_note": commentUpdateModel = "魅蓝 note2"; break;
                case "m2c_note": commentUpdateModel = "魅蓝 note2 电信版"; break;
                case "m1_note": commentUpdateModel = "魅蓝 note"; break;
                case "m1c_note": commentUpdateModel = "魅蓝 note 电信版"; break;
                case "M6c": commentUpdateModel = "魅蓝 8c"; break;
                case "s6": commentUpdateModel = "魅蓝 S6"; break;
                case "M6T": commentUpdateModel = "魅蓝 6T"; break;
                case "m6": commentUpdateModel = "魅蓝 6"; break;
                case "m5s": commentUpdateModel = "魅蓝 5s"; break;
                case "M5c": commentUpdateModel = "魅蓝 5c"; break;
                case "m5": commentUpdateModel = "魅蓝 5"; break;
                case "m3s": commentUpdateModel = "魅蓝 3s"; break;
                case "m3": commentUpdateModel = "魅蓝 3"; break;
                case "m2": commentUpdateModel = "魅蓝 2"; break;
                case "m1": commentUpdateModel = "魅蓝"; break;
                default: break;
            }

            // 处理机型外显信息
            switch (commentUpdateOverseas)
            {
                case "国内版":
                    stringUpdateModel = commentUpdateModel;
                    break;
                case "海外版":
                    switch (updateModelIdentifier)
                    {
                        case "16s": stringUpdateModel = "Meizu 16s"; break;
                        case "16th_Plus": stringUpdateModel = "Meizu 16th Plus"; break;
                        case "16th": stringUpdateModel = "Meizu 16th"; break;
                        case "m1926": stringUpdateModel = "Meizu 16Xs"; break;
                        case "16": stringUpdateModel = "Meizu 16"; break;
                        case "15_Plus": stringUpdateModel = "Meizu 15 Plus"; break;
                        case "15": stringUpdateModel = "Meizu 15"; break;
                        case "M15": stringUpdateModel = "Meizu 15 Lite"; break;
                        case "15Lite": stringUpdateModel = "Meizu 15 Lite"; break;
                        case "PRO7_Plus": stringUpdateModel = "Meizu PRO 7 Plus"; break;
                        case "PRO7H": stringUpdateModel = "Meizu PRO 7-H"; break;
                        case "PRO7S": stringUpdateModel = "Meizu PRO 7-S"; break;
                        case "PRO6_Plus": stringUpdateModel = "Meizu PRO 6 Plus"; break;
                        case "PRO_6": stringUpdateModel = "Meizu PRO 6/PRO 6s"; break;
                        case "PRO_5": stringUpdateModel = "Meizu PRO 5"; break;
                        case "MX6": stringUpdateModel = "Meizu MX6"; break;
                        case "MX5": stringUpdateModel = "Meizu MX5"; break;
                        case "MX4_Pro": stringUpdateModel = "Meizu MX4 Pro"; break;
                        case "MX4": stringUpdateModel = "Meizu MX4"; break;
                        case "MX3": stringUpdateModel = "Meizu MX3"; break;
                        case "MX2": stringUpdateModel = "Meizu MX2"; break;
                        case "MX": stringUpdateModel = "Meizu MX"; break;
                        case "M9": stringUpdateModel = "Meizu M9"; break;
                        case "M8": stringUpdateModel = "Meizu M8"; break;
                        case "M1816": stringUpdateModel = "Meizu M8 Lite"; break;
                        case "M1813": stringUpdateModel = "Meizu M8"; break;
                        case "note9": stringUpdateModel = "Meizu Note9"; break;
                        case "m1923": stringUpdateModel = "Meizu Note9"; break;
                        case "note8": stringUpdateModel = "Meizu Note8"; break;
                        case "M1852": stringUpdateModel = "Meizu X8"; break;
                        case "M3X": stringUpdateModel = "Meizu M3x"; break;
                        case "M3E": stringUpdateModel = "Meizu E3"; break;
                        case "M2E": stringUpdateModel = "Meizu E2"; break;
                        case "M1_E": stringUpdateModel = "Meizu M3E"; break;
                        case "MAX": stringUpdateModel = "Meizu M3 Max"; break;
                        case "U20": stringUpdateModel = "Meizu U20"; break;
                        case "U10": stringUpdateModel = "Meizu U10"; break;
                        case "m1_metal": stringUpdateModel = "Meizu M1 Metal"; break;
                        case "m6_note": stringUpdateModel = "Meizu M6 Note"; break;
                        case "m5_note": stringUpdateModel = "Meizu M5 Note"; break;
                        case "m3_note": stringUpdateModel = "Meizu M3 Note"; break;
                        case "m2_note": stringUpdateModel = "Meizu M2 Note"; break;
                        case "m2c_note": stringUpdateModel = "Meizu M2 Note"; break;
                        case "m1_note": stringUpdateModel = "Meizu M1 Note"; break;
                        case "m1c_note": stringUpdateModel = "Meizu M1 Note"; break;
                        case "M6c": stringUpdateModel = "Meizu M8c"; break;
                        case "s6": stringUpdateModel = "Meizu M6s"; break;
                        case "M6T": stringUpdateModel = "Meizu M6T"; break;
                        case "m6": stringUpdateModel = "Meizu M6"; break;
                        case "m5s": stringUpdateModel = "Meizu M5s"; break;
                        case "M5c": stringUpdateModel = "Meizu M5c"; break;
                        case "m5": stringUpdateModel = "Meizu M5"; break;
                        case "m3s": stringUpdateModel = "Meizu M3s"; break;
                        case "m3": stringUpdateModel = "Meizu M3"; break;
                        case "m2": stringUpdateModel = "Meizu M2"; break;
                        case "m1": stringUpdateModel = "Meizu M1"; break;
                        default: break;
                    }
                    break;
                default: break;

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
            string updateComment = "<!--" + commentUpdateModel + "|" + commentUpdateOverseas + "|" + commentUpdateChannel + "-->";
            string returnString = updateComment + "|Flyme " + updateVersion + suffixUpdateChannel + "|" + stringUpdateModel + suffixUpdateCountry + "|" + updateDate + Regex.Replace(stringOrigin, stringRegexMatch, @"|$3$4B ($5 Bytes)|$7$8B ($9 Bytes)|$10|$11|<$12>|").Replace(@"B ( Bytes)", "");
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
            stringFullWiki = Regex.Replace(stringFullWiki, @"update\.bin>\|\n\n\|Flyme", @"update.bin>|\n|Flyme");
            TextBox_Item.Text = stringFullWiki;
            //}
            /*catch (Exception ex)
            {

                throw;
            }*/
        }
    }
}