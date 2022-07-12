using System;
using System.Collections.Generic;
using System.Web;
using System.Data.Common;
using System.IO;
using System.Web.SessionState;

namespace aspnetforum
{
    public class getattachment : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            int fileId = 0, userId = 0;
            string filename = "";
            int.TryParse(request.QueryString["FileID"], out fileId);
            bool isPrivateMessage = (request.QueryString["personal"] != null);

            try
            {
                Utils.Attachments.GetAttachment(fileId, isPrivateMessage, out userId, out filename);

                string filePath = Utils.Attachments.GetUploadDirAbsolutePath() + userId.ToString() + "\\" + filename;

                //if old version
                if(!File.Exists(filePath))
                    filePath = Utils.Attachments.GetUploadDirAbsolutePathOLDVersion() + userId.ToString() + "\\" + filename;

                if (File.Exists(filePath))
                {
                    FileInfo fi = new FileInfo(filePath);

                    response.Clear();
                    response.ContentType = "application/octet-stream";
                    response.AddHeader("Content-Disposition", "attachment; filename=\"" + filename + "\";");
                    response.AddHeader("Content-Length", fi.Length.ToString());
                    response.TransmitFile(filePath);
                    //context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    response.Clear();
					response.TrySkipIisCustomErrors = true;
                    response.StatusCode = 404;
                }
            }
            catch(AccessViolationException ex)
            {
                response.Write("Access denied. No permission.");
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
