using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMSSendingSystem.World
{
    public class TwsmsRecord
    {
        /// <summary>
        /// 台灣簡訊 - 使用者名稱
        /// </summary>
        public string _username = "intellischool";

        /// <summary>
        /// 台灣簡訊 - 密碼
        /// </summary>
        public string _password = "5351316";

        /// <summary>
        /// 台灣簡訊 - 網址
        /// </summary>
        public string _url = "http://api.twsms.com/smsSend.php?username={0}&password={1}&mobile={2}&message={3}";

        /// <summary>
        /// 台灣簡訊 - 取得簡訊狀態網址
        /// </summary>
        public string _GetStateUrl = "http://api.twsms.com/smsQuery.php?username={0}&password={1}&mobile={2}&msgid={3}";
    }
}
