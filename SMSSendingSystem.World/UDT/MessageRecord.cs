using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;
using System.Xml;

namespace SMSSendingSystem.World
{
    [TableName("SMSSendingSystem.MessageRecord.cs")]
    class MessageRecord : FISCA.UDT.ActiveRecord
    {
        /// <summary>
        /// 系統編號
        /// </summary>
        [Field(Field = "ref_id", Indexed = true)]
        public string Ref_ID { get; set; }

        /// <summary>
        /// 對象(學生=student,老師=teacher)
        /// </summary>
        [Field(Field = "tag", Indexed = true)]
        public string Tag { get; set; }

        /// <summary>
        /// 發送之電話號碼
        /// </summary>
        [Field(Field = "phone", Indexed = true)]
        public string Phone { get; set; }

        /// <summary>
        /// 簡訊內容
        /// </summary>
        [Field(Field = "content", Indexed = false)]
        public string Content { get; set; }

        /// <summary>
        /// 使用者電腦之簡訊發送時間
        /// </summary>
        [Field(Field = "computer_send_time", Indexed = false)]
        public string ComputerSendTime { get; set; }

        #region 台灣簡訊

        ///// <summary>
        ///// 台灣簡訊回傳 - 此筆簡訊目前狀態
        ///// </summary>
        //[Field(Field = "response_code", Indexed = false)]
        //public string ResponseCode { get; set; }

        ///// <summary>
        ///// 台灣簡訊回傳 - 發送至台灣簡訊是否成功(Success)
        ///// </summary>
        //[Field(Field = "response_text", Indexed = false)]
        //public string ResponseText { get; set; }

        /// <summary>
        /// 台灣簡訊回傳 - 本ID可向台灣簡訊查詢發送狀態
        /// </summary>
        [Field(Field = "response_msgid", Indexed = false)]
        public string ResponseMsgid { get; set; }

        /// <summary>
        /// 台灣簡訊回傳完整內容
        /// </summary>
        [Field(Field = "response_xml", Indexed = false)]
        public string ResponseXML { get; set; }

        #endregion

        #region 查詢

        ///// <summary>
        ///// 最後狀態
        ///// </summary>
        //[Field(Field = "final_state", Indexed = false)]
        //public string FinalState { get; set; }

        ///// <summary>
        ///// 簡訊接收時間
        ///// </summary>
        //[Field(Field = "final_donetime", Indexed = false)]
        //public string FinalDoneTime { get; set; }

        /// <summary>
        /// 狀態檢查(向"台灣簡訊"檢查回傳之完整內容)
        /// </summary>
        [Field(Field = "final_xml", Indexed = false)]
        public string FinalState_XML { get; set; }



        ///// <summary>
        ///// 家長簡訊回覆內容
        ///// </summary>
        //[Field(Field = "final_reply_content", Indexed = false)]
        //public string FinalReplyContent { get; set; }

        #endregion
    }
}
