using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using System.Net;
using System.IO;
using FISCA.Data;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;

namespace SMSSendingSystem.World
{
    public partial class SendNow : BaseForm
    {
        /// <summary>
        /// 本畫面開啟之來源
        /// </summary>
        public tool.tag _tag = tool.tag.student;

        /// <summary>
        /// Log記錄
        /// </summary>
        StringBuilder sb_Log = new StringBuilder();

        BackgroundWorker bgwork { get; set; }

        /// <summary>
        /// UDT 儲存物件
        /// </summary>
        List<MessageRecord> MessageList { get; set; }

        /// <summary>
        /// 對象ID清單
        /// </summary>
        List<string> _IDList = new List<string>();

        /// <summary>
        /// 學生清單
        /// </summary>
        List<KeyBoStudent> Students { get; set; }

        string SendStringFromCampus = "簡訊系統_發送簡訊";

        string SendStringCNG = "學生簡訊";

        string StudentSend { get; set; }

        Campus.Configuration.ConfigData cd { get; set; }

        public SendNow(tool.tag tag, List<string> IDList)
        {
            InitializeComponent();
            _tag = tag;
            _IDList = IDList;
        }

        private void SendNow_Load(object sender, EventArgs e)
        {
            bgwork = new BackgroundWorker();
            bgwork.DoWork += new DoWorkEventHandler(bgwork_DoWork);
            bgwork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwork_RunWorkerCompleted);
            bgwork.ProgressChanged += new ProgressChangedEventHandler(bgwork_ProgressChanged);
            bgwork.WorkerReportsProgress = true;

            CheckText();

            if (_tag == tool.tag.student)
            {
                this.Text = string.Format("發送簡訊(共 {0} 名學生)", _IDList.Count);
                //建立Columns
                SetStudentColumns();

                //取得簡訊範本
                cd = Campus.Configuration.Config.User[SendStringFromCampus];
                StudentSend = cd[SendStringCNG];
                if (string.IsNullOrEmpty(StudentSend))
                {
                    tbMessage.Text = "貴家長你好,貴子弟之基本資料如下:班級「{班級}」座號「{座號}」姓名「{姓名}」學號「{學號}」";
                }
                else
                {
                    tbMessage.Text = StudentSend;
                }

                //取得學生資料(含電話資料)
                Students = tool.GetStudent(_IDList);

                //建立畫面上的學生資料
                SetStudentRows();
            }
            else if (_tag == tool.tag.teacher)
            {
                //建立Columns
                SetTeacherColumns();

                //取得老師資料(含電話資料)

            }
        }

        private void btnNowSend_Click(object sender, EventArgs e)
        {
            if (bgwork.IsBusy)
            {
                MsgBox.Show("系統忙碌中,請稍後...");
                return;
            }

            PasswordForm pf = new PasswordForm();

            DialogResult dr2 = pf.ShowDialog();
            if (dr2 == System.Windows.Forms.DialogResult.Yes)
            {
                if (MsgBox.Show(string.Format("發送簡訊將衍生相關費用!!\n您確定對 {0} 名學生發送簡訊?", "" + Students.Count), MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
                {
                    //傳送清單
                    List<HttpAction> actions = new List<HttpAction>();
                    if (_tag == tool.tag.student)
                    {
                        #region 學生
                        //Log
                        sb_Log.AppendLine("學生簡訊傳送：");

                        //開始建立傳送清單
                        foreach (KeyBoStudent stud in Students)
                        {
                            if (tool.CheckPhone(stud.SMS_Phone))
                            {
                                //儲存傳送範本,開始背景模式後再儲存
                                StudentSend = tbMessage.Text;

                                string sb = tbMessage.Text;
                                sb = sb.Replace("\r", ""); //將/r移除,\n為台灣簡訊認可之換行符號
                                sb = sb.Replace("{姓名}", stud.Name);
                                sb = sb.Replace("{座號}", stud.SeatNo);
                                sb = sb.Replace("{班級}", stud.ClassName);
                                sb = sb.Replace("{學號}", stud.StudentNumber);

                                HttpAction ha = new HttpAction();
                                ha.GetStudentMessage(stud.SMS_Phone, sb); //設定訊息與接受電話
                                ha.student = stud;
                                ha.student.State = tool.State.傳送中;
                                actions.Add(ha);

                                //Log
                                sb_Log.AppendLine(string.Format("班級「{0}」座號「{1}」姓名「{2}」", stud.ClassName, stud.SeatNo, stud.Name));
                                sb_Log.AppendLine(string.Format("訊息內容「{0}」", sb));
                                sb_Log.AppendLine("");
                            }
                            else
                            {
                                stud.State = tool.State.不傳送;
                            }
                        }
                        sb_Log.AppendLine(string.Format("共發送 {0} 筆簡訊", actions.Count));
                        #endregion
                    }
                    else if (_tag == tool.tag.teacher)
                    {
                        #region 老師
                        //如果是對老師發訊息



                        #endregion
                    }
                    this.Text = "發送簡訊(訊息發送中請勿關閉本畫面!!)";
                    btnNowSend.Enabled = false;

                    bgwork.RunWorkerAsync(actions);
                }
                else
                {
                    MsgBox.Show("已取消發送!!");
                }
            }
            else
            {
                MsgBox.Show("已取消發送!!");
            }
        }

        /// <summary>
        /// 開始背景模式
        /// </summary>
        void bgwork_DoWork(object sender, DoWorkEventArgs e)
        {
            //記錄傳送範本內容
            cd[SendStringCNG] = StudentSend;
            cd.Save();

            List<HttpAction> actions = e.Argument as List<HttpAction>;
            MessageList = new List<MessageRecord>();
            int successCount = 0; //用於計算進度。

            //平行迴圈，迴圈內部的程式碼有可能同時執行，達到加速效果。
            Parallel.ForEach<HttpAction>(actions, (action) =>
            {
                SendByHttp(action); //依 Action 設定，呼叫 HTTP

                Interlocked.Increment(ref successCount);

                decimal seed = (decimal)successCount / actions.Count;
                bgwork.ReportProgress((int)(seed * 100), action); //把 action 傳出去是用於在畫面顯示資訊。
            });

            //UDT資料儲存
            tool._a.InsertValues(MessageList);

            FISCA.LogAgent.ApplicationLog.Log("簡訊系統", "發送", sb_Log.ToString());
        }

        /// <summary>
        /// 呼叫 Http 的位置。
        /// </summary>
        private void SendByHttp(HttpAction action)
        {
            //Dropbox 本來就慢，所以這裡很慢是正常的。
            string url = string.Format(tool.tr._url, tool.tr._username, tool.tr._password, action._mobile, action._message);
            string resp = tool.REQ(url);

            //將回傳資訊進行檔案儲存
            //tool.textToFile(string.Format(@"C:\Google\{0}.html", action.student.Name + "_" + action._mobile), resp);

            MessageRecord mr = new MessageRecord();
            mr.ResponseXML = resp; ////回傳內容即存(未分老師/學生)

            if (!string.IsNullOrEmpty(resp))
            {
                XmlElement xml = (XmlElement)tool.GetXml(mr.ResponseXML).SelectSingleNode("msgid");
                if (xml != null)
                    mr.ResponseMsgid = xml.InnerText; ////台灣簡訊回傳 - 本ID可向台灣簡訊查詢發送狀態
            }

            if (_tag == tool.tag.student)
            {
                #region 學生

                mr.Ref_ID = action.student.ref_student_id; //系統編號
                mr.Tag = tool.tag.student.ToString(); //TAG
                mr.Phone = action._mobile; //向哪個行動電話發訊息
                mr.Content = action._message; //傳送之訊息內容

                DataTable dtable = tool._q.Select("select now()");
                DateTime dt = DateTime.Now;
                DateTime.TryParse("" + dtable.Rows[0][0], out dt);
                mr.ComputerSendTime = dt.ToString("yyyy/MM/dd HH:mm:ss"); //電腦端的發送時間

                #endregion
            }
            else if (_tag == tool.tag.teacher)
            {
                //老師
            }

            MessageList.Add(mr);
        }

        /// <summary>
        /// 完成
        /// </summary>
        void bgwork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Text = string.Format("發送簡訊(共發送 {0} 筆簡訊)", MessageList.Count);
            //是否取消此操作
            if (!e.Cancelled)
            {
                //是否沒有錯誤訊息
                if (e.Error == null)
                {
                    //完成
                    MessageBox.Show("傳送完成!");
                    FISCA.Presentation.MotherForm.SetStatusBarMessage("傳送完成!");
                }
                else
                {
                    MsgBox.Show("簡訊發送發生錯誤!\n" + e.Error.Message);
                }
            }
            else
            {
                MsgBox.Show("本作業已被取消!");
            }
        }

        /// <summary>
        /// 進度
        /// </summary>
        void bgwork_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            HttpAction ha = e.UserState as HttpAction;

            //傳送成功
            ha.student.State = tool.State.已傳送;

            FISCA.Presentation.MotherForm.SetStatusBarMessage("開始傳送:", e.ProgressPercentage);
        }

        /// <summary>
        /// 設定Row內容
        /// </summary>
        private void SetStudentRows()
        {
            //電話有誤的先填入
            foreach (KeyBoStudent stud in Students)
            {
                if (!tool.CheckPhone(stud.SMS_Phone))
                {
                    listViewEx1.Items.Add(tool.SetListViewColor(tool.SetSubItemValue(stud)));
                }
            }

            //電話正確者
            foreach (KeyBoStudent stud in Students)
            {
                if (tool.CheckPhone(stud.SMS_Phone))
                {
                    listViewEx1.Items.Add(tool.SetSubItemValue(stud));
                }
            }

        }

        /// <summary>
        /// 設定教師欄位
        /// </summary>
        private void SetTeacherColumns()
        {
            listViewEx1.Columns.Add(tool.SetColumn("狀態", 80));
            listViewEx1.Columns.Add(tool.SetColumn("姓名", 80));
            listViewEx1.Columns.Add(tool.SetColumn("暱稱", 80));
            listViewEx1.Columns.Add(tool.SetColumn("性別", 80));
            listViewEx1.Columns.Add(tool.SetColumn("行動電話", 120));
        }

        /// <summary>
        /// 設定學生欄位
        /// </summary>
        private void SetStudentColumns()
        {
            listViewEx1.Columns.Add(tool.SetColumn("狀態", 80));
            listViewEx1.Columns.Add(tool.SetColumn("班級", 80));
            listViewEx1.Columns.Add(tool.SetColumn("座號", 80));
            listViewEx1.Columns.Add(tool.SetColumn("姓名", 80));
            listViewEx1.Columns.Add(tool.SetColumn("行動電話", 120));
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            HelpForm hf = new HelpForm();
            hf.ShowDialog();
        }

        private void tbMessage_TextChanged(object sender, EventArgs e)
        {
            CheckText();
        }

        /// <summary>
        /// 檢查字元數
        /// </summary>
        private void CheckText()
        {
            labelX1.Text = tool.CheckTextCount(tbMessage.Text);
            //if (tool.CheckTextCount(tbMessage.Text) <= 160)
            //{
            //    tbMessage.ForeColor = Color.Black;
            //}
            //else
            //{
            //    tbMessage.ForeColor = Color.Red;
            //}
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_tag == tool.tag.student)
            {
                foreach (KeyBoStudent stud in Students)
                {
                    string sb = tbMessage.Text;
                    sb = sb.Replace("\r", ""); //將/r移除,\n為台灣簡訊認可之換行符號
                    sb = sb.Replace("{姓名}", stud.Name);
                    sb = sb.Replace("{座號}", stud.SeatNo);
                    sb = sb.Replace("{班級}", stud.ClassName);
                    sb = sb.Replace("{學號}", stud.StudentNumber);

                    MsgBox.Show(sb);

                    break;
                }
            }
            else
            {

            }
        }

        private void 預覽簡訊ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_tag == tool.tag.student)
            {
                ListViewItem item = listViewEx1.SelectedItems[0];
                if (item.Tag is KeyBoStudent)
                {
                    KeyBoStudent stud = (KeyBoStudent)item.Tag;

                    string txt = tbMessage.Text;
                    txt = txt.Replace("\r", ""); //將/r移除,\n為台灣簡訊認可之換行符號
                    txt = txt.Replace("{姓名}", stud.Name);
                    txt = txt.Replace("{座號}", stud.SeatNo);
                    txt = txt.Replace("{班級}", stud.ClassName);
                    txt = txt.Replace("{學號}", stud.StudentNumber);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(string.Format("{0}", tool.CheckTextCount(txt)));
                    sb.AppendLine("內容如下：\n");
                    sb.Append(txt);

                    MsgBox.Show(sb.ToString());
                }
            }
        }

        private void listViewEx1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 1)
                預覽簡訊ToolStripMenuItem.Enabled = true;
            else
                預覽簡訊ToolStripMenuItem.Enabled = false;
        }

        private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogResult dr = MsgBox.Show("將簡訊內容儲存為簡訊範本?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2);
            if (dr == System.Windows.Forms.DialogResult.Yes)
            {
                //儲存範本內容
                StudentSend = tbMessage.Text;
                cd[SendStringCNG] = StudentSend;
                cd.Save();
                MsgBox.Show("已儲存簡訊範本!!");
            }
            else
            {
                MsgBox.Show("已取消");
            }
        }
    }
}
