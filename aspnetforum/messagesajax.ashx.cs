using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;

namespace aspnetforum
{
    public class messagesajax : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            if (request.Form["Mode"] == "Rate")
            {
                int score = int.Parse(request.Form["Score"]);
                int messageId = int.Parse(request.Form["MessageID"]);
                int userId = Utils.User.CurrentUserID;
                if (userId != 0)
                {
                    int? rating = Utils.Message.RateMessage(messageId, userId, score);
                    response.Write(rating);
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
