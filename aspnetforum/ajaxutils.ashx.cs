using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;

namespace aspnetforum
{
    /// <summary>
    /// this handler is called by jquery from various pages... it's like a webservice
    /// </summary>
    public class ajaxutils : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            string mode = request.Form["mode"];
            if (mode == "CheckUserNameAvailability")
            {
                int userId = 1;
                if (!string.IsNullOrEmpty(request.Form["username"]))
                {
                    userId = Utils.User.GetUserIdByUserName(request.Form["username"]);
                }
                response.Write(userId == 0 ? "0" : "1");
                return;
            }
			if (mode == "HideTrialBar")
			{
				context.Session["HideTrialBar"] = DateTime.Now;
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
