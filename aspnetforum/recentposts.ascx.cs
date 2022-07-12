using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Web.UI.HtmlControls;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// this control is used on "recent posts" page AND on the forum homepage
	/// </summary>
	public partial class recentposts : System.Web.UI.UserControl
	{
		private int _currentUserId;

		protected void Page_Load(object sender, EventArgs e)
		{
			_currentUserId = Utils.User.CurrentUserID;

			if (this.Visible)
			{
				BindRecentPostsRepeater(rptMessagesList, ((ForumPage)Page).PageSize);
			}
		}

		/// <summary>
		/// the method is static cause its called from different locations. Binds a repeater with recent messages
		/// </summary>
		public static void BindRecentPostsRepeater(Repeater rptMessagesList, int pageSize)
		{
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr;

				if (Utils.User.CurrentUserID == 0) //if anonymous user - hide "membersonly" forums
				{
					dr = cn.ExecuteReader("SELECT TOP " + Settings.PageSize + @" ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumMessages.MessageID, ForumUsers.FirstName, ForumUsers.LastName
					FROM (ForumMessages INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
					LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID
					WHERE ForumMessages.Visible=?
					AND ForumTopics.ForumID NOT IN (SELECT DISTINCT ForumID FROM ForumGroupPermissions WHERE AllowReading=?)
					AND ForumTopics.ForumID NOT IN (SELECT ForumID FROM Forums WHERE MembersOnly=?)
					ORDER BY ForumMessages.MessageID DESC", true, true, true);
				}
				else
				{
					string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID); //query select allowed forums

					dr = cn.ExecuteReader(@"SELECT TOP " + Settings.PageSize + @" ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject, ForumUsers.UserName, ForumMessages.UserID, ForumUsers.PostsCount, ForumUsers.AvatarFileName, ForumMessages.MessageID, ForumUsers.FirstName, ForumUsers.LastName
					FROM (ForumMessages INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)
					LEFT JOIN ForumUsers ON ForumMessages.UserID=ForumUsers.UserID
					WHERE ForumMessages.Visible=?
					AND ForumTopics.ForumID IN (" + strSQLAllowedForums + @")
					ORDER BY ForumMessages.MessageID DESC", true);
				}

				rptMessagesList.DataSource = dr;
				rptMessagesList.DataBind();
				dr.Close();
				cn.Close();
			}
		}

		protected void rptMessagesList_ItemDataBound(object sender, RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				IDataRecord record = (IDataRecord)e.Item.DataItem;

				//registered users are allowed to QUOTE msgs
				if (_currentUserId != 0)
				{
					HtmlAnchor lnkQuote = (HtmlAnchor)e.Item.FindControl("lnkQuote");
					lnkQuote.Visible = true;
					lnkQuote.HRef = "addpost.aspx?TopicID=" + record["TopicID"] + "&Quote=" + record["MessageID"];

					//LinkButton btnComplain = (LinkButton)e.Item.FindControl("btnComplain");
					//btnComplain.Visible = true;
				}
			}
		}
	}
}