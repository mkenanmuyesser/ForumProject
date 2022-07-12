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
	/// Summary description for privatemessages.
	/// </summary>
	public partial class privateinbox : ForumPage
	{
		protected string pagerString = "";
		protected int _userId;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			if (CurrentUserID == 0)
			{
				lblNotLoggedIn.Visible = true;
				return;
			}
			
			lblNotLoggedIn.Visible = false;
			_userId = Request.QueryString["UserID"] == null ? 0 : int.Parse(Request.QueryString["UserID"]);

			if (_userId != 0)
			{
				rptConversationsList.Visible = false;
				Cn.Open();
				BindBodiesRepeater();
				MarkAllAsRead();
				Cn.Close();
			}
			else
			{
				rptMessagesList.Visible = false;
				Cn.Open();
				BindConversationsRepeater();
				Cn.Close();
			}
		}

		private void MarkAllAsRead()
		{
			Cn.ExecuteNonQuery("UPDATE ForumPersonalMessages SET New=? WHERE FromUserID=? and ToUserID=?", false, _userId, CurrentUserID);
			Session["ForumUnreadMessagesCount"] = null;
		}

		private void BindConversationsRepeater()
		{
			DbDataReader dr = Cn.ExecuteReader(@"
				SELECT m.UserID, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName,
					COUNT(m.MessageID) as Posts, MAX(m.CreationDate) as LastMessageDate, MAX(m.NewFlag) as New,
					ForumUsers.AvatarFileName, ForumUsers.UseGravatar, ForumUsers.Email
				FROM 
					(SELECT MessageID, FromUserID AS UserID, CreationDate, 2 AS NewFlag FROM ForumPersonalMessages WHERE ToUserID=? AND New=? AND HiddenByRecipient<>?
					UNION SELECT MessageID, FromUserID AS UserID, CreationDate, 1 AS NewFlag FROM ForumPersonalMessages WHERE ToUserID=? AND New=? AND HiddenByRecipient<>?
					UNION SELECT MessageID, ToUserID AS UserID, CreationDate, 0 as NewFlag FROM ForumPersonalMessages WHERE FromUserID=? AND HiddenBySender<>?) as m
				INNER JOIN
					ForumUsers ON ForumUsers.UserID = m.UserID
				GROUP BY m.UserID, ForumUsers.UserName, ForumUsers.FirstName, ForumUsers.LastName, ForumUsers.AvatarFileName, ForumUsers.UseGravatar, ForumUsers.Email
				ORDER BY MAX(m.CreationDate) DESC", CurrentUserID, true, true, CurrentUserID, false, true, CurrentUserID, true);
			rptConversationsList.DataSource = dr;
			rptConversationsList.DataBind();
			rptConversationsList.Visible = rptConversationsList.Items.Count > 0;
		}

		private void BindBodiesRepeater()
		{
			DataSet ds = new DataSet();
			ds.Tables.Add("Messages");
			ds.Tables.Add("UploadedFiles");

			DbDataReader dr = Cn.ExecuteReader(@"SELECT ForumPersonalMessages.MessageID, ForumUsers.UserName, ForumUsers.AvatarFileName, ForumUsers.Signature, ForumPersonalMessages.CreationDate, ForumPersonalMessages.Body, ForumUsers.UserID, ForumPersonalMessages.New, ForumUsers.FirstName, ForumUsers.LastName
				FROM ForumPersonalMessages
					INNER JOIN ForumUsers ON ForumPersonalMessages.FromUserID=ForumUsers.UserID
				WHERE (ForumPersonalMessages.ToUserID=? and ForumPersonalMessages.FromUserID=? and ForumPersonalMessages.HiddenByRecipient=?) OR (ForumPersonalMessages.ToUserID=? and ForumPersonalMessages.FromUserID=? and ForumPersonalMessages.HiddenBySender=?)
				ORDER BY ForumPersonalMessages.MessageID", CurrentUserID, _userId, false, _userId, CurrentUserID, false);
			ds.Tables[0].Load(dr);
			dr.Close();

			//now get files uploaded
			dr = Cn.ExecuteReader("SELECT FileID, FileName, MessageID, UserID FROM ForumUploadedPersonalFiles WHERE MessageID IN (SELECT MessageID FROM ForumPersonalMessages WHERE ToUserID=" + CurrentUserID + ")");
			ds.Tables[1].Load(dr);

			ds.Relations.Add(new DataRelation("MessagesFiles", ds.Tables[0].Columns["MessageID"], ds.Tables[1].Columns["MessageID"], false));

			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = ds.Tables[0].DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = this.PageSize;
			int curPage = 0;
			if(Request.QueryString["page"]!=null)
				int.TryParse(Request.QueryString["page"], out curPage);
			else if (Request.QueryString["lastpage"] != null)
				curPage = pagedSrc.PageCount - 1;
			pagedSrc.CurrentPageIndex = curPage;

			//prepare a string for the "pager" at the bottom
			pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, "privateinbox.aspx?UserID=" + _userId);

			this.rptMessagesList.DataSource = pagedSrc;
			this.rptMessagesList.DataBind();
			this.rptMessagesList.Visible = rptMessagesList.Items.Count > 0;
		}

		protected void rptMessagesList_ItemCommand(object source, RepeaterCommandEventArgs e)
		{
			//delete message
			if (e.CommandName == "delete")
			{
				int deletedMessageID = int.Parse(e.CommandArgument.ToString());

				this.Cn.Open();
				Utils.Message.DeletePersonalMessage(CurrentUserID, deletedMessageID, Cn);
				BindBodiesRepeater();
				this.Cn.Close();

				if (rptMessagesList.Items.Count == 0) Response.Redirect("privateinbox.aspx", true);
			}
		}

		protected void rptMessagesList_ItemDataBound(object sender, RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				//show attachments
				Repeater nestedRepeater = e.Item.FindControl("rptFiles") as Repeater;
				DataRowView dv = e.Item.DataItem as DataRowView;
				nestedRepeater.DataSource = dv.CreateChildView("MessagesFiles");
				nestedRepeater.DataBind();
				nestedRepeater.Visible = (nestedRepeater.Items.Count > 0);
			}
		}

		protected void rptConversationsList_ItemCommand(object source, RepeaterCommandEventArgs e)
		{
			//delete message
			if (e.CommandName == "delete")
			{
				int deletedUserId = int.Parse(e.CommandArgument.ToString());

				this.Cn.Open();
				Utils.Message.DeletePersonalConversationWithUser(CurrentUserID, deletedUserId, Cn);
				BindConversationsRepeater();
				this.Cn.Close();
			}
		}
	}
}
