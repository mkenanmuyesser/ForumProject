using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.Common;
using System.Data;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class unapprovedposts : ForumPage
	{
		protected string pagerString = "";

		protected void Page_Load(object sender, EventArgs e)
		{
			this.Cn.Open();
			BindRepeater();
			this.Cn.Close();
		}

		private void BindRepeater()
		{
			DbDataReader dr;
			if (this.IsAdministrator)
			{
				dr = Cn.ExecuteReader(@"SELECT ForumMessages.MessageID, ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
					FROM (ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID
					WHERE ForumMessages.Visible=?
					ORDER BY ForumMessages.MessageID DESC", false);
			}
			else
			{
				dr = Cn.ExecuteReader(@"SELECT ForumMessages.MessageID, ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
					FROM (ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID
					WHERE ForumMessages.Visible=?
					AND ForumTopics.ForumID IN (SELECT DISTINCT ForumID FROM ForumModerators WHERE UserID=" + CurrentUserID + @")
					ORDER BY ForumMessages.MessageID DESC", false);
			}

			DataTable dt = new DataTable();
			dt.Load(dr);
			dr.Close();
			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = dt.DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = this.PageSize;
			int curPage = 0;
			if (Request.QueryString["page"] != null)
				int.TryParse(Request.QueryString["page"], out curPage);
			pagedSrc.CurrentPageIndex = curPage;

			//prepare a string for the "pager" at the bottom
			pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, "unapprovedposts.aspx");

			this.rptMessagesList.DataSource = pagedSrc;
			this.rptMessagesList.DataBind();

			rptMessagesList.Visible = (rptMessagesList.Items.Count > 0);
			divNothingFound.Visible = !rptMessagesList.Visible;
		}

		protected void rptMessagesList_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			//delete message
			if (e.CommandName == "delete")
			{
				int deletedMessageID = int.Parse(e.CommandArgument.ToString());
				this.Cn.Open();
				Utils.Message.DeleteMessage(deletedMessageID, Cn);
				BindRepeater();
				this.Cn.Close();
			}
			//approve message (for premoderated forum)
			if (e.CommandName == "approve")
			{
				int approvedMessageID = int.Parse(e.CommandArgument.ToString());

				this.Cn.Open();
				Utils.Message.ApproveMessage(approvedMessageID, Cn);
				BindRepeater();
				this.Cn.Close();
			}
		}
	}
}
