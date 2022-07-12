using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class users : ForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			// if not authenticated - get out
			if (CurrentUserID == 0)
			{
				Response.Redirect("default.aspx", true);
				return;
			}

			lnkOnlineUsers.Visible = spanNonActive.Visible = spanAddUser.Visible = IsAdministrator;
			this.Cn.Open();
			BindRecentUsers();
			BindActiveUsers();
			BindRecentlyActiveUsers();
			this.Cn.Close();
		}

		private void BindRecentUsers()
		{
			DbDataReader dr = Cn.ExecuteReader(@"SELECT top 15 UserID, UserName, AvatarFileName, FirstName, LastName
				FROM ForumUsers WHERE Disabled=0 AND HidePresence=0 ORDER BY UserID DESC");
			DataTable dt = new DataTable();
			dt.Load(dr);
			dt.DefaultView.Sort = "UserName"; //resort by username
			rptRecent.DataSource = dt.DefaultView;
			rptRecent.DataBind();
		}

		private void BindActiveUsers()
		{
			DbDataReader dr = Cn.ExecuteReader(@"SELECT TOP 15 ForumUsers.UserID, ForumUsers.UserName, COUNT(ForumMessages.MessageID) AS MsgCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
				FROM ForumUsers INNER JOIN ForumMessages ON ForumUsers.UserID=ForumMessages.UserID
				WHERE Disabled=0 AND HidePresence=0
				GROUP BY ForumUsers.UserID, ForumUsers.UserName, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
				ORDER BY COUNT(ForumMessages.MessageID) DESC");
			rptMostActive.DataSource = dr;
			rptMostActive.DataBind();
			dr.Close();
		}

		private void BindRecentlyActiveUsers()
		{
			DbDataReader dr = Cn.ExecuteReader(@"SELECT TOP 15 ForumUsers.UserID, ForumUsers.UserName, COUNT(ForumMessages.MessageID) AS MsgCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
				FROM ForumUsers INNER JOIN ForumMessages ON ForumUsers.UserID=ForumMessages.UserID
				WHERE ForumMessages.CreationDate>?
				AND Disabled=0 AND HidePresence=0
				GROUP BY ForumUsers.UserID, ForumUsers.UserName, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
				ORDER BY COUNT(ForumMessages.MessageID) DESC", Various.GetCurrTime().AddDays(-14));
			rptRecentlyActive.DataSource = dr;
			rptRecentlyActive.DataBind();
			dr.Close();
		}
	}
}
