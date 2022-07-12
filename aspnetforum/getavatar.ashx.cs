using System;
using System.Collections.Generic;
using System.Web;
using System.IO;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for getavatar
	/// </summary>
	public class getavatar : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
			string avatarFile =  Utils.Various.GetSafeFileNameFromQueryStirng(HttpUtility.UrlDecode(context.Request["avatar"]));
			if (string.IsNullOrEmpty(avatarFile)) return;

			string avatarPath = Utils.Attachments.GetAvatarsDirAbsolutePath() + avatarFile;
			if (!File.Exists(avatarPath)) avatarPath = Utils.Attachments.GetUploadDirAbsolutePathOLDVersion() + avatarFile;
			if (File.Exists(avatarPath))
			{
				HttpResponse response = context.Response;
				response.Clear();
				response.ContentType = Utils.Attachments.GetContentType(avatarPath);
				response.AddHeader("Content-Disposition", "attachment; filename=\"" + avatarFile + "\";");
				response.AddHeader("Content-Length", (new FileInfo(avatarPath)).Length.ToString());
				response.TransmitFile(avatarPath);
			}
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}