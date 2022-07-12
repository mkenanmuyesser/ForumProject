using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Common;
using System.IO;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for viewprofile.
	/// </summary>
	public partial class viewprofile : ForumPage
	{
		protected int _userId;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			try
			{
				_userId = int.Parse( Request.QueryString["UserID"] );
			}
			catch
			{
				Response.TrySkipIisCustomErrors = true;
				Response.StatusCode = 400;
				Response.Write("Invalid UserID passed");
				Response.End();
				return;
			}

			lblUserName.Visible = !Utils.Settings.ShowFullNamesInsteadOfUsernames;

			if (IsAdministrator) BindUserGroups();

			divAchievements.Visible = !Settings.DisableAchievements;
			if (!Settings.DisableAchievements)
			{
				rptAchievements.DataSource = Achievements.GetAchievementsForUser(_userId);
				rptAchievements.DataBind();
			}
			
			trRating.Visible = Utils.Settings.EnableRating;
			divGroups.Visible = IsAdministrator && gridGroups.Rows.Count > 0;
			btnEditUser.Visible = IsAdministrator || (_userId == CurrentUserID);
			btnDelUser.Visible = IsAdministrator;
			btnActivateUser.Visible = IsAdministrator;
			btnDisableUser.Visible = IsAdministrator;
			btnDeleteAllPostsAndTopics.Visible = IsAdministrator;
			btnDeleteAllPostsAndDelete.Visible = IsAdministrator;
			//lnkEdit.Visible = IsAdministrator || (_userId == CurrentUserID);

			//lnkEdit.HRef = (_userId == CurrentUserID) ? "editprofile.aspx" : "editprofile.aspx?userid=" + _userId;
			btnEditUser.OnClientClick = "document.location.href='editprofile.aspx"
				+ (_userId == CurrentUserID ? "" : "?userid=" + _userId)
				+ "';return false;";

			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT * FROM ForumUsers WHERE UserID=" + _userId);
			if(dr.Read())
			{
				lblUser.Text = dr["UserName"].ToString();
				Title = dr["UserName"].ToString();
				lblUserName.Text = dr["UserName"].ToString();
				lblFullName.Text = dr["FirstName"] + " " + dr["LastName"];
				lblInterests.Text = dr["Interests"].ToString();
				
				string homepageUrl = dr["Homepage"].ToString();
				if (!homepageUrl.StartsWith("http://") && !homepageUrl.StartsWith("https://"))
					homepageUrl = "http://" + homepageUrl;
				
				homepage.NavigateUrl = homepageUrl;
				homepage.Text = dr["Homepage"].ToString();
				lnkViewPosts.InnerText = dr["PostsCount"].ToString();
				lnkViewPosts.HRef = "viewpostsbyuser.aspx?UserID=" + _userId;
				lblRegistrationDate.Text = Convert.ToDateTime(dr["RegistrationDate"]).ToShortDateString();
				lblLastLogonDateValue.Text = dr["LastLogonDate"].ToString();

				bool isDisabled = Convert.ToBoolean(dr["Disabled"]);
				btnActivateUser.Visible = isDisabled && IsAdministrator;
				btnDisableUser.Visible = !isDisabled && IsAdministrator;
				btnResendActivaton.Visible = isDisabled && IsAdministrator;

				lblRatingValue.Text = dr["ReputationCache"].ToString();
				if (!(dr["ReputationCache"] is DBNull))
				{
					Color clr;
					if (Convert.ToInt32(dr["ReputationCache"]) < 0)
						clr = Color.Red;
					else
						clr = Color.Green;
					lblRatingValue.ForeColor = clr;
				}

				imgAvatar.Src = Utils.User.GetAvatarFileName(dr["AvatarFileName"], dr["UseGravatar"], dr["Email"]);
			}
			dr.Close();

			MetaDescription = "AspNetForum - viewing " + lblUser.Text + "'s user profile";

			bool isUserAdministrator = Utils.User.IsAdministrator(_userId);

			btnMakeAdmin.Visible = (!isUserAdministrator) && IsAdministrator;
			btnRevokeAdmin.Visible = isUserAdministrator && IsAdministrator;

			Cn.Close();
		}

		protected void btnDelUser_Click(object sender, System.EventArgs e)
		{
			if(IsAdministrator)
			{
				Utils.User.DeleteUser(_userId);

				Response.Redirect("users.aspx");
			}
		}

		protected void btnActivateUser_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.EnableUser(_userId, false);

				btnActivateUser.Visible = false;
				btnDisableUser.Visible = true;
			}
		}

		protected void btnDeleteAllPostsAndTopics_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.DeleteAllPosts(_userId);

				btnActivateUser.Visible = false;
				btnDisableUser.Visible = true;
			}
		}

		protected void btnDeleteAllPostsAndDelete_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.DeleteAllPosts(_userId);
				Utils.User.DeleteUser(_userId);

				Response.Redirect("users.aspx");
			}
		}

		protected void btnDisableUser_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.DisableUser(_userId);

				btnActivateUser.Visible = true;
				btnDisableUser.Visible = false;
			}
		}

		protected void btnMakeAdmin_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.MakeAdmin(_userId);
				btnMakeAdmin.Visible = false;
				btnRevokeAdmin.Visible = true;
			}
		}

		protected void btnRevokeAdmin_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.RevokeAdmin(_userId);
				btnRevokeAdmin.Visible = false;
				btnMakeAdmin.Visible = true;
			}
		}

		protected void btnResendActivaton_Click(object sender, EventArgs e)
		{
			if (IsAdministrator)
			{
				Utils.User.SendActivationEmail(_userId);
			}
		}

		private void BindUserGroups()
		{
			var groups = Utils.User.GetGroupIdsForUser(_userId);

			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader(@"SELECT ForumUserGroups.GroupID, ForumUserGroups.Title
				FROM ForumUserGroups
				WHERE GroupID IN (" + groups.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + @")");
			gridGroups.DataSource = dr;
			gridGroups.DataBind();
			dr.Close();
			Cn.Close();
		}
	}
}
