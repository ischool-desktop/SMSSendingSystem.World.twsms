using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using FISCA.Data;
using System.IO;
using FISCA.UDT;
using System.Net;
using System.Xml;

namespace SMSSendingSystem.World
{
    public class tool
    {
        public enum tag { student, teacher }
        public enum State { 不傳送, 傳送中, 已傳送 };

        /// <summary>
        /// Query
        /// </summary>
        static public QueryHelper _q = new QueryHelper();

        /// <summary>
        /// UDT
        /// </summary>
        static public AccessHelper _a = new AccessHelper();

        /// <summary>
        /// 台灣簡訊基本設定
        /// </summary>
        static public TwsmsRecord tr = new TwsmsRecord();

        /// <summary>
        /// 建立HttpWebRequest
        /// 向網路端 送出 或 取得 資料
        /// </summary>
        /// <param name="url">網路位置與參數組合</param>
        /// <returns>回傳之資料</returns>
        static public string REQ(string url)
        {
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            HttpWebResponse rsp = req.GetResponse() as HttpWebResponse;
            //從 Server 回傳的資料。
            Stream data = rsp.GetResponseStream();
            StreamReader sr = new StreamReader(data);
            string resp = sr.ReadToEnd();
            sr.Close();
            data.Close();

            return resp;
        }
        /// <summary>
        /// 取得學生資料
        /// </summary>
        static public List<KeyBoStudent> GetStudent(List<string> _IDList)
        {
            StringBuilder sb = new StringBuilder();

            List<KeyBoStudent> Students = new List<KeyBoStudent>();
            sb.Append("select student.id,student.name,student.student_number,student.seat_no,student.ref_class_id,class.class_name,sms_phone from student ");
            sb.Append("left join class on student.ref_class_id=class.id ");
            sb.Append(string.Format("where student.id in ('{0}') ", string.Join("','", _IDList)));
            sb.Append("order by class.class_name,student.seat_no");

            DataTable dt = tool._q.Select(sb.ToString());

            foreach (DataRow row in dt.Rows)
            {
                KeyBoStudent stud = new KeyBoStudent(row);
                Students.Add(stud);
            }
            return Students;
        }

        /// <summary>
        /// 設定一個Column
        /// </summary>
        static public ColumnHeader SetColumn(string text, int width)
        {
            ColumnHeader column = new ColumnHeader();
            column.Text = text;
            column.Width = width;
            return column;
        }

        /// <summary>
        /// ~學生~
        /// 設定 ListViewItem 的文字內容
        /// </summary>
        static public ListViewItem SetSubItemValue(KeyBoStudent stud)
        {
            ListViewItem item = new ListViewItem("");
            item.SubItems.Add(stud.ClassName);
            item.SubItems.Add(stud.SeatNo);
            item.SubItems.Add(stud.Name);
            item.SubItems.Add(stud.SMS_Phone);

            stud.ListViewItem = item;

            return item;
        }

        /// <summary>
        /// 將 ListViewItem 文字顏色設定為紅色
        /// </summary>
        static public ListViewItem SetListViewColor(ListViewItem item)
        {
            foreach (ListViewItem.ListViewSubItem subitem in item.SubItems)
            {
                subitem.ForeColor = Color.Red;
            }
            return item;
        }

        /// <summary>
        /// 檔案儲存
        /// </summary>
        /// <param name="filePath">檔案位置路徑</param>
        /// <param name="text">檔案內容</param>
        static public void textToFile(String filePath, String text)
        {
            StreamWriter file = new StreamWriter(filePath);
            file.Write(text);
            file.Close();
        }

        static public XmlElement GetXml(string _xml)
        {
            if (string.IsNullOrEmpty(_xml))
            {
                FISCA.DSAUtil.DSXmlHelper dsx = new FISCA.DSAUtil.DSXmlHelper("smsResp");
                return dsx.BaseElement;
            }
            else
            {
                FISCA.DSAUtil.DSXmlHelper dsx = new FISCA.DSAUtil.DSXmlHelper();
                dsx.Load(_xml);
                return dsx.BaseElement;
            }
        }

        /// <summary>
        /// 檢查傳入之字串,共幾字元
        /// 範圍為 (a~z A~Z) = (65~122)
        /// </summary>
        static public string CheckTextCount(string _tbMessage)
        {
            _tbMessage = _tbMessage.Replace("\r", "");
            char[] a = _tbMessage.ToArray();
            int countA = 0;
            int countB = 0;
            foreach (char b in a)
            {
                //(a~z A~Z) = (65~122)
                int intValue = Convert.ToInt32(b);
                if (intValue >= 65 && intValue <= 122)
                {
                    countA++;
                }
                else
                {
                    countB++;
                }
            }

            return string.Format("簡訊內容：(英文:{0} 中文與各類符號:{1})", countA, countB);
        }

        /// <summary>
        /// 檢查錯誤狀態的電話號碼
        /// 沒有電話 / 電話不是10碼者 / 電話前2碼不為09之電話
        /// </summary>
        static public bool CheckPhone(string phone)
        {
            //沒有電話
            if (string.IsNullOrEmpty(phone))
            {
                return false;
            }

            //電話不是10碼者
            if (phone.Length != 10)
            {
                return false;
            }

            //電話前2碼不為09之電話
            if (phone.Length > 2)
            {
                if (phone.Substring(0, 2) != "09")
                    return false;
                else
                    return true;
            }

            return true;
        }

        /// <summary>
        /// 台灣簡訊->家長收機端訊息
        /// </summary>
        static public string ReturnStatustext(string code)
        {
            string name = "";
            switch (code)
            {
                case "DELIVRD":
                    name = "訊息已發送到接收手機";
                    break;
                case "EXPIRED":
                    name = "訊息已過了發送的有效時間";
                    break;
                case "DELETED":
                    name = "訊息已被刪除";
                    break;
                case "UNDELIV":
                    name = "訊息無法送達";
                    break;
                case "ACCEPTD":
                    name = "訊息正在接收狀態";
                    break;
                case "UNKNOWN":
                    name = "訊息為無效狀態";
                    break;
                case "REJECTD":
                    name = "訊息發送被拒絕";
                    break;
                case "SYNTAXE":
                    name = "語法錯誤";
                    break;
                case "MOBERROR":
                    name = "電話號碼錯誤";
                    break;
                case "MSGERROR":
                    name = "訊息內容錯誤";
                    break;
                case "OTHERROR":
                    name = "系統錯誤";
                    break;
                case "REJERROR":
                    name = "被關鍵字過濾系統擋掉的簡訊";
                    break;
                case "REJMOBIL":
                    name = "門號使用者有申請檔廣告簡訊服務";
                    break;
                default:
                    name = "";
                    break;
            }
            return name;
        }

        /// <summary>
        /// 澔學->台灣簡訊端訊息
        /// </summary>
        static public string ReturnCode(string code)
        {
            string name = "";
            switch (code)
            {
                case "00000":
                    name = "完成";
                    break;
                case "00001":
                    name = "狀態尚未回復";
                    break;
                case "00010":
                    name = "帳號或密碼錯誤";
                    break;
                case "00020":
                    name = "通數不足";
                    break;
                case "00030":
                    name = "IP 無使用權限";
                    break;
                case "00040":
                    name = "帳號已停用";
                    break;
                case "00050":
                    name = "sendtime 格式錯誤";
                    break;
                case "00060":
                    name = "expirytime 格式錯誤";
                    break;
                case "00070":
                    name = "popup 格式錯誤";
                    break;
                case "00080":
                    name = "mo 格式錯誤";
                    break;
                case "00090":
                    name = "longsms 格式錯誤";
                    break;
                case "00100":
                    name = "手機號碼格式錯誤";
                    break;
                case "00110":
                    name = "沒有簡訊內容";
                    break;
                case "00120":
                    name = "長簡訊不支援國際門號";
                    break;
                case "00130":
                    name = "簡訊內容超過長度";
                    break;
                case "00140":
                    name = "drurl 格式錯誤";
                    break;
                case "00150":
                    name = "sendtime 預約的時間已經超過";
                    break;
                case "00300":
                    name = "找不到msgid";
                    break;
                case "00310":
                    name = "預約尚未送出";
                    break;
                case "00400":
                    name = "找不到 snumber 辨識碼";
                    break;
                case "00410":
                    name = "沒有任何 mo 資料";
                    break;
                case "99998":
                    name = "資料處理異常，請重新發送";
                    break;
                case "99999":
                    name = "系統錯誤，請通知系統廠商";
                    break;
                default:
                    name = "";
                    break;
            }
            return name;
        }
    }
}
