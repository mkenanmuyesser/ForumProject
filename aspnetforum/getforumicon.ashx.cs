using System;
using System.Collections.Generic;
using System.Web;
using System.IO;

namespace aspnetforum
{
    /// <summary>
    /// Summary description for getforumicon
    /// </summary>
    public class getforumicon : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string iconFile = Utils.Various.GetSafeFileNameFromQueryStirng(context.Request["icon"]);
            if (string.IsNullOrEmpty(iconFile)) return;

            iconFile = Utils.Attachments.GetIconsDirAbsolutePath() + iconFile;
            if (!File.Exists(iconFile)) iconFile = Utils.Attachments.GetUploadDirAbsolutePathOLDVersion() + iconFile;
            if (File.Exists(iconFile))
            {
                HttpResponse response = context.Response;
                response.Clear();
                response.ContentType = Utils.Attachments.GetContentType(iconFile);
                response.AddHeader("Content-Disposition", "attachment; filename=\"" + iconFile + "\";");
                response.AddHeader("Content-Length", (new FileInfo(iconFile)).Length.ToString());
                response.TransmitFile(iconFile);
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