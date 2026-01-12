using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.util
{
    public class Utils
    {

        public static DateTime ToDateTime(string timestamp)
        {
            var tz = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            return timestamp.Length == 13
                ? tz.AddMilliseconds(Convert.ToInt64(timestamp))
                : tz.AddSeconds(Convert.ToInt64(timestamp));
        }

    }
}
