using System;
using System.Collections.Generic;
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
using System.IO;

namespace FutureGO
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public enum Col { 日期, 時間, 開盤價, 最高, 最低, 收盤價, 成交量, MAX };
        static List<string[]> m_kMinK;
        static List<KeyValuePair<int,int>> m_kCheckTime;
        private static string m_pPath;                                              // 存執行檔案路徑變數
        private static string m_pSetting = "\\Setting.ini";                         // 設定檔檔名
        public int MA;
        public static List<string[]> OpenMinK(string filePath)
        {
            //System.Data.DataTable dt = new System.Data.DataTable();
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default);
            //記錄每次讀取的一行記錄
            string strLine = "";
            //記錄每行記錄中的各字段內容
            List<string[]> arrayData = new List<string[]>();

            //逐行讀取檔案中的數據
            while ((strLine = sr.ReadLine()) != null)
            {
                arrayData.Add(strLine.Split(','));
            }
            sr.Close();
            fs.Close();
            return arrayData;
        }

        private bool ParseSetting() // 讀取設定檔
        {
            try
            {
                using (StreamReader sr = new StreamReader(m_pPath + m_pSetting))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] dict = line.Split('=');
                        //bool value = Boolean.Parse(dict[1]);
                        switch (dict[0])
                        {
                            case "MA":
                                MA = int.Parse(dict[1]);
                                break;
                            case "CHECK TIME":
                                string [] times = dict[1].Split(',');
                                foreach(string time in times)
                                {
                                    string [] str = time.Split(':');
                                    m_kCheckTime.Add(new KeyValuePair<int, int>(int.Parse(str[0]), int.Parse(str[1])));
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        public void WriteMessage(string strMsg)
        {
            listInformation.Items.Add(strMsg);

            listInformation.SelectedIndex = listInformation.Items.Count - 1;

            listInformation.ScrollIntoView(listInformation.SelectedItem);
        }
        public MainWindow()
        {
            InitializeComponent();
            m_pPath = System.IO.Directory.GetCurrentDirectory();
            m_kCheckTime = new List<KeyValuePair<int, int>>();
            ParseSetting();
            //取得程式執行路徑
            int mon = -1;
            int year = -1;
            int day = -1;
            int hour = -1;
            int min = -1;
            int sec = -1;
            int nowMA = -1;
            int have = 0;
            int have_price = 0;
            int diffma = 0;
            int now_make_money = 0;
            int lose_max_money = 0;
            int lose_max_money_buffer = 0;
            int total = 0;
            Queue<int> m_kDayClose = new Queue<int>();
            Dictionary<int, int> m_kMA = new Dictionary<int, int>();
            String dir = System.IO.Directory.GetCurrentDirectory() + "\\";
            m_kMinK = OpenMinK(dir + "TXMinKN.txt");
            foreach (var item in m_kMinK)
            {
                string [] date = item[(int)Col.日期].Split('/');
                int dayO = day;
                int yearO = year;
                year = int.Parse(date[0]);
                mon = int.Parse(date[1]);
                day = int.Parse(date[2]);

                string[] time = item[(int)Col.時間].Split(':');
                hour = int.Parse(time[0]);
                min = int.Parse(time[1]);
                int closep = (int)float.Parse(item[(int)Col.收盤價]);
                if (dayO > 0 && day != dayO)
                {
                    int day_close = closep;
                    m_kDayClose.Enqueue(day_close);
                    if(m_kDayClose.Count > MA)
                    {
                        m_kDayClose.Dequeue();
                    }
                    if(m_kDayClose.Count >= MA)
                    {
                        int sum = 0;
                        foreach(var close in m_kDayClose)
                        {
                            sum += close;
                        }
                        int avg = sum / MA;
                        int date_num = (year * 10000) + (mon * 100) + day;
                        m_kMA.Add(date_num, avg);
                        nowMA = avg;
                    }
                }
                if (nowMA > 0)
                {
                    bool checktime_ok = false;
                    foreach (var tt in m_kCheckTime)
                    {
                        checktime_ok = checktime_ok || ((tt.Key == hour) && (tt.Value == min));
                    }
                    if(checktime_ok)
                    {

                        if (diffma >= 0)
                        {
                            if ((closep - nowMA) < 0)
                            {
                                if (have > 0)
                                {

                                    now_make_money += (closep - have_price);
                                    if(now_make_money < lose_max_money)
                                    {
                                        lose_max_money = now_make_money;
                                    }
                                    WriteMessage(year + "年" + mon + "月" + day + "日" + hour + "點" + min + "分，出現訊號 操作(多) 新倉指數(" + have_price + ")" + " 平倉指數(" + closep + ") 損益(" + (closep - have_price) + "點)");
                                    //have_price = 0;
                                    //have = 0;
                                }
                                if (have >= 0)
                                {
                                    
                                    have_price = closep;
                                    have = -1;
                                   
                                }
                            }
                        }
                        else if (diffma <= 0)
                        {
                            if ((closep - nowMA) > 0)
                            {
                                if (have < 0)
                                {
                                    WriteMessage(year + "年" + mon + "月" + day + "日" + hour + "點" + min + "分，出現訊號 操作(空) 新倉指數(" + have_price + ")" + " 平倉指數(" + closep + ") 損益(" + (have_price - closep) + "點)");
                                    now_make_money += (have_price - closep);
                                    if (now_make_money < lose_max_money)
                                    {
                                        lose_max_money = now_make_money;
                                    }
                                    //have_price = 0;
                                    //have = 0;
                                }
                                if (have <= 0)
                                {
                                    
                                    have_price = closep;
                                    have = 1;
                                }
                            }
                        }
                        diffma = closep - nowMA;
                    }
                   
                }
                if (yearO > 0 && year != yearO)
                {
                    WriteMessage(yearO + "年，總共獲利" + now_make_money + "點");
                    WriteMessage(yearO + "年，最大累計虧損為" + lose_max_money + "點");
                    total += now_make_money;
                    now_make_money = 0;
                    lose_max_money = 0;


                }


            }
            WriteMessage("目前總共獲利" + total + "點");
            int jj = 0;
            jj++;
        }


    }
}
