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
using Jitbit.Utils;
using System.Collections.Generic;

namespace aspnetforum
{
	public partial class AspNetForumMaster : System.Web.UI.MasterPage
	{
		DbConnection cn;

		public Label LoginErrorLabel { get { return lblLoginErr; } }

		protected int ModeratorCount;
		
		/// <summary>
		/// property to show/hide login window (called from pages)
		/// </summary>
		public bool ShowLoginTable { set; private get; }

		public AspNetForumMaster()
			: base()
		{
			ShowLoginTable = true; //dafult property value
			cn = DB.CreateConnection();
		}

		/// <summary>
		/// property returns contentplaceholder, required for some methods in the ForumPage class (to find controls etc)
		/// </summary>
		internal ContentPlaceHolder MainPlaceHolder { get { return AspNetForumContentPlaceHolder; } }

		protected void Page_PreRender(object sender, EventArgs e)
		{
			if (lblVersion != null)
			{
				string dllversion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
				lblVersion.Text = dllversion;
			}

			if (Utils.User.IsCurrentUserModerator())
			{
				ModeratorCount = ModeratorStats.GetComplaintsCount() + ModeratorStats.GetUnapprovedMsgsCount();
			}

			bool integratedAuthEnabled = Utils.Settings.IntegratedAuthentication;
			
			if (Utils.User.CurrentUserID == 0) //not logged in
			{
				//if (registerLink != null) registerLink.Visible = !integratedAuthEnabled;
				if (logoutLink != null) logoutLink.Visible = false;
				if (viewProfileLink != null) viewProfileLink.Visible = false;
				if (loginTable != null) loginTable.Visible = !integratedAuthEnabled && ShowLoginTable;
				if (curuserTable != null) curuserTable.Visible = false;
				if (usersLink != null) usersLink.Visible = false;
				if (aOpenId != null) aOpenId.Visible = Utils.Settings.EnableOpenId;
				if (aTwitter != null) aTwitter.Visible = !string.IsNullOrEmpty(Utils.Settings.TwitterConsumerKey) && !string.IsNullOrEmpty(Utils.Settings.TwitterConsumerSecret);
				if (aFacebook != null) aFacebook.Visible = !string.IsNullOrEmpty(Utils.Settings.FacebookAppID) && !string.IsNullOrEmpty(Utils.Settings.FacebookAppSecret);
			}
			else //logged in
			{
				//if (registerLink != null) registerLink.Visible = false;
				if (logoutLink != null) logoutLink.Visible = !integratedAuthEnabled;
				if (loginTable != null) loginTable.Visible = false;
				if (curuserTable != null) curuserTable.Visible = ShowLoginTable;
				if (usersLink != null) usersLink.Visible = true;
				//if (editProfileHeaderLink != null) editProfileHeaderLink.Visible = true;

				if (viewProfileLink != null)
				{
					viewProfileLink.Visible = true;
					string username;
					//IF "integrated auth" is enabled
					//AND it is windows-authenctication
					//then lets remove tha domain name from "domain\user" username
					if (Page.User is System.Security.Principal.WindowsPrincipal
					    && Utils.Settings.IntegratedAuthentication)
					{
						username = Session["aspnetforumUserName"].ToString();
						username = username.Substring(username.IndexOf("\\") + 1);
					}
					else
					{
						username = Session["aspnetforumUserName"].ToString();
					}
					viewProfileLink.InnerHtml = username;
					viewProfileLink.HRef = "viewprofile.aspx?UserID=" + Utils.User.CurrentUserID.ToString();
				}

				cn.Open();
				int unreadPrivateMsgs = GetUnreadPersonalMessagesCount();
				string avatarPath = Utils.User.GetCurrUserAvatarImagePath(cn);
				cn.Close();

				if (imgAvatar != null) imgAvatar.Src = avatarPath;
				if (spanNumMsgs != null)
				{
					spanNumMsgs.InnerHtml = unreadPrivateMsgs.ToString();
					if (unreadPrivateMsgs > 0) spanNumMsgs.Style["font-weight"] = "bold";
				}
				if (spanNumUnreadThreads != null)
				{
					spanNumUnreadThreads.InnerHtml = GetUpdatedThreadCount().ToString();
				}
			}

			adminLink.Visible = Utils.User.IsAdministrator(Utils.User.CurrentUserID) && adminLink != null;

			if(!Utils.Settings.DisableAchievements)
				Achievements.RegisterNewAchievements(Page);
		}

		//unread msg count is cached in the Session-object for 5 minutes
		//to save performance
		private int GetUnreadPersonalMessagesCount()
		{
			int count = Session.GetWithTimeout("ForumUnreadMessagesCount") as int? ?? -1;

			if (count == -1)
			{
				count = Convert.ToInt32(
					cn.ExecuteScalar("SELECT COUNT(MessageID) FROM ForumPersonalMessages WHERE HiddenByRecipient<>? AND ToUserID=? AND New=?",
						true, User.CurrentUserID, true));
				Session.AddWithTimeout("ForumUnreadMessagesCount", count, TimeSpan.FromMinutes(5));
			}
			return count;
		}

		private int GetUpdatedThreadCount()
		{
			int count = Session.GetWithTimeout("ForumUpdatedThreadsCount") as int? ?? -1;

			if (count == -1)
			{
				count = Utils.UnreadTracker.GetUpdatedThreads().Rows.Count;
				Session.AddWithTimeout("ForumUpdatedThreadsCount", count, TimeSpan.FromMinutes(5));
			}
			return count;
		}
	}
}
