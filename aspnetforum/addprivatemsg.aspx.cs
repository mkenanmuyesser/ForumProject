using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for addprivatemsg.
	/// </summary>
	public partial class addprivatemsg : ForumPage
	{
		protected int toUserID;
		protected bool mailNotificationsEnabled;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (!Utils.Settings.EnablePrivateMessaging)
			{
				Response.End();
				return;
			}

			try
			{
				toUserID = int.Parse(Request.QueryString["ToUserID"]);
				if (CurrentUserID == 0) throw new Exception("not logged in");
			}
			catch
			{
				divMain.Style["display"] = "none";
				lblError.Visible = true;
				return;
			}

			btnSave.DataBind();
			mailNotificationsEnabled = Utils.Settings.MailNotificationsEnabled;
			
			//if quoting
			if(Request.QueryString["Quote"]!=null && !IsPostBack)
			{
				int quotedMsgId = int.Parse(Request.QueryString["Quote"]);
				Cn.Open();
				var dr = Cn.ExecuteReader(
					@"SELECT ForumPersonalMessages.Body, ForumUsers.UserName
					FROM ForumUsers INNER JOIN ForumPersonalMessages ON ForumUsers.UserID=ForumPersonalMessages.FromUserID
					WHERE ForumPersonalMessages.MessageID=?", quotedMsgId);
				if(dr.Read())
				{
					string body = dr["Body"].ToString().Replace("<br>", "\r\n");
					body = System.Text.RegularExpressions.Regex.Replace(body, @"<\S[^>]*>", "");
					tbMsg.Text = "[quote=" + dr["UserName"].ToString() + "]" + body + "[/quote]";
				}
				dr.Close();
				Cn.Close();
			}
		}

		protected void btnSave_Click(object sender, System.EventArgs e)
		{
			if (!Utils.Attachments.CheckAttachmentsSize())
			{
				lblMaxSize.Text = Utils.Settings.MaxUploadFileSize / 1000 + " Kb";
				lblMaxSize.Visible = true;
				lblFileSizeError.Visible = true;
				return;
			}

			Utils.User.SendPM(toUserID, tbMsg.Text);

			Response.Redirect("privateinbox.aspx?UserID=" + toUserID);
		}
	}
}
