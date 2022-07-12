using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;

namespace aspnetforum
{
    public class addpostajax : IHttpHandler, IReadOnlySessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            if (request.Form["mode"] == "preview")
            {
                string msg = request.Form["messagetext"];
                msg = msg.Replace("<", "&lt;").Replace(">", "&gt;");
                response.Write(Utils.Formatting.FormatMessageHTML(msg));
                response.End();
            }

            if (request.Form["mode"] == "delfile")
            {
                int fileId = 0;
                if (int.TryParse(request.Form["FileID"], out fileId))
                    Utils.Attachments.DeleteMessageAttachmentById(fileId);
                response.End();
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
