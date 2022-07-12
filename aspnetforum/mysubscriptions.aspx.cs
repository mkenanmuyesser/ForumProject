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
	/// Summary description for mysubscriptions.
	/// </summary>
	public partial class mysubscriptions : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0)
			{
				lblNotLoggedIn.Visible = true;
				divMain.Visible = false;
			}
			else
			{
				lblNotLoggedIn.Visible = false;
				divMain.Visible = true;
				BindGrid();
			}
		}

		private void BindGrid()
		{
			this.Cn.Open();

			//topics
			DbDataReader dr = this.Cn.ExecuteReader(@"SELECT ForumTopics.TopicID, ForumTopics.Subject
				FROM ForumSubscriptions INNER JOIN ForumTopics ON ForumSubscriptions.TopicID = ForumTopics.TopicID
				WHERE ForumSubscriptions.UserID = " + CurrentUserID);
			this.grid.DataSource = dr;
			this.grid.DataBind();
			dr.Close();

			//forums
			dr = this.Cn.ExecuteReader(
				@"SELECT Forums.ForumID, Forums.Title
				FROM ForumNewTopicSubscriptions INNER JOIN Forums ON ForumNewTopicSubscriptions.ForumID = Forums.ForumID
				WHERE ForumNewTopicSubscriptions.UserID = " + CurrentUserID);
			this.gridForums.DataSource = dr;
			this.gridForums.DataBind();
			dr.Close();

			//forums
			dr = this.Cn.ExecuteReader(@"SELECT Forums.ForumID, Forums.Title
				FROM ForumNewForumMsgSubscriptions INNER JOIN Forums ON ForumNewForumMsgSubscriptions.ForumID = Forums.ForumID
				WHERE ForumNewForumMsgSubscriptions.UserID = " + CurrentUserID);
			this.girdForumPosts.DataSource = dr;
			this.girdForumPosts.DataBind();
			dr.Close();

			this.Cn.Close();
		}

		protected void grid_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if(e.CommandName == "delete")
			{
				string topicid = e.Item.Cells[0].Text;
				this.Cn.Open();
				this.Cn.ExecuteNonQuery("DELETE FROM ForumSubscriptions WHERE TopicID=" + topicid + " AND UserID = " + CurrentUserID);
				this.Cn.Close();
				BindGrid();
			}
		}

		protected void gridForums_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if(e.CommandName == "delete")
			{
				string forumid = e.Item.Cells[0].Text;
				this.Cn.Open();
				this.Cn.ExecuteNonQuery("DELETE FROM ForumNewTopicSubscriptions WHERE ForumID=" + forumid + " AND UserID = " + CurrentUserID);
				this.Cn.Close();
				BindGrid();
			}
		}

		protected void girdForumPosts_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if(e.CommandName == "delete")
			{
				string forumid = e.Item.Cells[0].Text;
				this.Cn.Open();
				this.Cn.ExecuteNonQuery("DELETE FROM ForumNewForumMsgSubscriptions WHERE ForumID=" + forumid + " AND UserID = " + CurrentUserID);
				this.Cn.Close();
				BindGrid();
			}
		}
	}
}
