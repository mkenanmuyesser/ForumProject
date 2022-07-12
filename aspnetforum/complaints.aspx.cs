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
	public partial class complaints : ForumPage
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
			DataSet ds = new DataSet();
			ds.Tables.Add("Messages");
			ds.Tables.Add("CompainUsers");
			string sql;

			if (this.IsAdministrator)
			{
				sql = @"SELECT ForumComplaints.UserID AS ComplainUserID, ForumMessages.MessageID, ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
					FROM ((ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
					INNER JOIN ForumComplaints ON ForumMessages.MessageID=ForumComplaints.MessageID
					ORDER BY ForumMessages.MessageID DESC";
			}
			else
			{
				sql = @"SELECT ForumComplaints.UserID AS ComplainUserID, ForumMessages.MessageID, ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumUsers.FirstName, ForumUsers.LastName
					FROM ((ForumMessages LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID)
					INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
					INNER JOIN ForumComplaints ON ForumMessages.MessageID=ForumComplaints.MessageID
					WHERE ForumTopics.ForumID IN (SELECT DISTINCT ForumID FROM ForumModerators WHERE UserID=" + CurrentUserID + @")
					ORDER BY ForumMessages.MessageID DESC";
			}

			DbDataReader dr = Cn.ExecuteReader(sql, false);
			ds.Tables[0].Load(dr);
			dr.Close();

			//now get complainers
			dr = Cn.ExecuteReader("SELECT UserID, UserName FROM ForumUsers WHERE UserID IN (SELECT UserID FROM ForumComplaints)");
			ds.Tables[1].Load(dr);
			dr.Close();

			ds.Relations.Add(new DataRelation("MessagesUsers", ds.Tables[0].Columns["ComplainUserID"], ds.Tables[1].Columns["UserID"], false));

			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = ds.Tables[0].DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = this.PageSize;
			int curPage = 0;
			if (Request.QueryString["page"] != null)
				int.TryParse(Request.QueryString["page"], out curPage);
			pagedSrc.CurrentPageIndex = curPage;

			//prepare a string for the "pager" at the bottom
			pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, "complaints.aspx");

			this.rptMessagesList.DataSource = pagedSrc;
			this.rptMessagesList.DataBind();

			rptMessagesList.Visible = (rptMessagesList.Items.Count > 0);
			divNothingFound.Visible = !rptMessagesList.Visible;
		}

		protected void rptMessagesList_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			//delete message
			if (e.CommandName == "remove")
			{
				int deletedMessageID = int.Parse(e.CommandArgument.ToString());
				this.Cn.Open();
				Cn.ExecuteNonQuery("DELETE FROM ForumComplaints WHERE MessageID=" + deletedMessageID);
				BindRepeater();
				this.Cn.Close();

				ModeratorStats.ResetComplaintsCountCache();
			}
		}

		protected void rptMessagesList_ItemDataBound(object sender, RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				//show users
				Repeater nestedRepeater = e.Item.FindControl("rptComplainUsers") as Repeater;
				DataRowView dv = e.Item.DataItem as DataRowView;
				nestedRepeater.DataSource = dv.CreateChildView("MessagesUsers");
				nestedRepeater.DataBind();
			}
		}
	}
}
