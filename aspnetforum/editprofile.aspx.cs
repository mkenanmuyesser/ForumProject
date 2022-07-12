using System;
using System.Collections;
using System.Configuration;
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
	/// Summary description for editprofile.
	/// </summary>
	public partial class editprofile : ForumPage
	{
		int _maxAvatarFileSize;
		bool _bAvatarsEnabled;
		int _maxAvatarPictureSize;
		int _editedUserID = 0;
		protected bool UseGravatar { get; private set; }

		protected void Page_Load(object sender, System.EventArgs e)
		{
			lblResult.Text = ""; //reset the result text

			//if "integrated auth" is on then lets HIDE the "change password" area
			if (Utils.Settings.IntegratedAuthentication)
			{
				tblChangePsw.Visible = false;
				tbUsername.ReadOnly = true;
				lblUsername.Enabled = false;
			}

			//enable Gravatar
			trGravatar.Visible = Utils.Settings.EnableGravatar;

			//if we're editing someone else's profile
			if (Request.QueryString["userid"] != null)
			{
				//btnChangePsw.Enabled = false;
				lblOldPsw.Enabled = false;
				tbOldPsw.Enabled = false;
				lblInbox.Enabled = false;
				lblMySubs.Enabled = false;
				if (IsAdministrator)
					_editedUserID = int.Parse(Request.QueryString["userid"]);
				else
					_editedUserID = 0;
			}
			else
			{
				_editedUserID = CurrentUserID;
			}

			if (_editedUserID == 0) //no user to edit
			{
				lblNotLoggedIn.Visible = true;
				divMain.Visible = false;
			}
			else
			{
				lblNotLoggedIn.Visible = false;
				divMain.Visible = true;
				_bAvatarsEnabled = Utils.Settings.EnableAvatars;
				tblAvatar.Visible = _bAvatarsEnabled;

				lblInbox.Visible = Utils.Settings.EnablePrivateMessaging;

				if (_bAvatarsEnabled)
				{
					_maxAvatarFileSize = Utils.Settings.MaxAvatarFileSizeInBytes;
					_maxAvatarPictureSize = Utils.Settings.MaxAvatarWidthHeight;
					lblMaxSize.Text = _maxAvatarFileSize.ToString();
					lblMaxDimenstions.Text = string.Format("{0}x{1}", _maxAvatarPictureSize, _maxAvatarPictureSize);
				}

				ShowDefaultAvatars();
				lblAvatarsNote.Visible = IsAdministrator;

				tblGroups.Visible = IsAdministrator;

				if (!IsPostBack)
					ShowUserInfo();

				if (IsAdministrator)
				{
					BindMemberGroups();
				}
			}
		}

		private void BindMemberGroups()
		{
			var groups = Utils.User.GetGroupIdsForUser(_editedUserID);
				
			Cn.Open();
			if (groups.Any())
			{
				var drMember = Cn.ExecuteReader(
						@"SELECT ForumUserGroups.GroupID, ForumUserGroups.Title
					FROM ForumUserGroups
					WHERE GroupID IN (" + groups.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + @")
					ORDER BY Title");
				rptMember.DataSource = drMember;
				rptMember.DataBind();
				drMember.Close();
			}

			var drNotmember = Cn.ExecuteReader(
				@"SELECT ForumUserGroups.GroupID, ForumUserGroups.Title
				FROM ForumUserGroups " +
				(groups.Any() ? @"WHERE GroupID NOT IN (" + groups.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + ") " : "") +
				"ORDER BY Title");
			rptNotMember.DataSource = drNotmember;
			rptNotMember.DataBind();
			Cn.Close();
		}

		protected void rptMember_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			if (e.CommandName == "remove")
			{
				//deny access
				Utils.User.RemoveUserFromGroup(_editedUserID, int.Parse(e.CommandArgument.ToString()));
			}
			BindMemberGroups();
		}

		protected void rptNotMember_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			if (e.CommandName == "add")
			{
				//grant access
				Utils.User.AddUserToGroup(_editedUserID, int.Parse(e.CommandArgument.ToString()));
			}
			BindMemberGroups();
		}

		private void ShowUserInfo()
		{
			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT * FROM ForumUsers WHERE UserID=" + _editedUserID);
			if(dr.Read())
			{
				UseGravatar = Convert.ToBoolean(dr["UseGravatar"]);
				tbUsername.Text = dr["Username"].ToString();
				string email = dr["Email"].ToString();
				tbEmail.Text = email;
				tbHomepage.Text = dr["Homepage"].ToString();
				tbInterests.Text = dr["Interests"].ToString();
				tbSignature.Text = dr["Signature"].ToString();
				tbFirstName.Text = dr["FirstName"].ToString();
				tbLastName.Text = dr["LastName"].ToString();
				cbHidePresence.Checked = dr["HidePresence"] is DBNull ? false : Convert.ToBoolean(dr["HidePresence"]);

				//avatar
				string avatarPic = dr["AvatarFileName"].ToString();
				imgAvatar.Visible = _bAvatarsEnabled;
				imgAvatar.Src = Utils.User.GetAvatarFileName(avatarPic, UseGravatar, email);
				if (avatarPic=="http://")
					tbAvatarURL.Text = ""; //old version just saved "http://" to db as the deaful value
				else if (avatarPic.StartsWith("http://") || avatarPic.StartsWith("https://"))
					tbAvatarURL.Text = avatarPic;
				else
					tbAvatarURL.Text = ""; //default empty value
			}
			dr.Close();
			Cn.Close();
		}

		protected void btnSave_Click(object sender, System.EventArgs e)
		{
			//reset avatar cache for current user (BECAUSE email can change!!!!)
			if (Utils.User.CurrentUserID == _editedUserID)
				Session["AvatarPath"] = null;

			string username = tbUsername.Text.Replace("<", "&lt;").Replace(">", "&gt;");
			string email = tbEmail.Text.Replace("<", "&lt;").Replace(">", "&gt;");
			string interests = tbInterests.Text.Replace("<", "&lt;").Replace(">", "&gt;");
			string homepage = tbHomepage.Text.Replace("<", "&lt;").Replace(">", "&gt;");
			string firstName = tbFirstName.Text.Trim().Replace("<", "&lt;").Replace(">", "&gt;");
			string lastName = tbLastName.Text.Trim().Replace("<", "&lt;").Replace(">", "&gt;");
			string signature = tbSignature.Text.Trim().Replace("<", "&lt;").Replace(">", "&gt;");
			signature = (signature.Length > 1000 ? signature.Substring(0, 1000) : signature);

			//check is a user tries to change his username but IntegratedAuth is ON
			if (Utils.Settings.IntegratedAuthentication
				&& _editedUserID == CurrentUserID
				&& tbUsername.Text.ToLower() != Session["aspnetforumUserName"].ToString().ToLower())
			{
				lblResult.Text = Resources.various.ErrorIntegratedUserName;
				return;
			}

			//check username uniqueness
			Cn.Open();
			var res = Cn.ExecuteScalar("SELECT UserID FROM ForumUsers WHERE UserName=? AND UserID<>?", username, _editedUserID);
			if(res!=null)
			{
				Cn.Close();
				lblResult.Text = string.Format(Resources.various.ErrorUserExists, username);
				return;
			}

			//update settings
			Cn.ExecuteNonQuery("UPDATE ForumUsers SET UserName=?, Email=?, Homepage=?, Interests=?, Signature=?, FirstName=?, LastName=?, HidePresence=? WHERE UserID=?",
				username, email, homepage, interests, signature, firstName, lastName, cbHidePresence.Checked, _editedUserID);
			Cn.Close();

			if(_editedUserID==CurrentUserID) Session["aspnetforumUserName"] = username;
			lblResult.Text = Resources.various.ProfileSaved;

			//to show avatar img
			ShowUserInfo();
		}

		protected void btnChangePsw_Click(object sender, System.EventArgs e)
		{
			if (tbNewPsw1.Text == "" || tbNewPsw2.Text == "" || tbNewPsw1.Text != tbNewPsw2.Text)
			{
				lblResult.Text = Resources.various.ErrorPasswordsDoNotMatch;
				return;
			}

			if (tbNewPsw1.Text.Length < Utils.Settings.MinPasswordLength)
			{
				lblResult.Text = string.Format("Password is too short, {0} characters minimum", Utils.Settings.MinPasswordLength);
				return;
			}

			Cn.Open();
			var res = Cn.ExecuteScalar("SELECT UserID FROM ForumUsers WHERE (Password=?) AND UserID=?",
				Utils.Password.CalculateHash(tbOldPsw.Text), _editedUserID);
			if (IsAdministrator || res != null)
			{
				Cn.ExecuteNonQuery("UPDATE ForumUsers SET [Password]=? WHERE UserID=?", Utils.Password.CalculateHash(tbNewPsw1.Text), _editedUserID);
				lblResult.Text = Resources.various.PasswordChanged;
			}
			else
			{
				lblResult.Text = Resources.various.ErrorWrongOldPassword;
			}
			Cn.Close();
		}

		protected void btnSaveAvatar_Click(object sender, EventArgs e)
		{
			string uploadDir = Utils.Attachments.GetAvatarsDirAbsolutePath();

			//now adding a new avatar
			string shortname = avatarUpload.PostedFile.FileName;
			if (shortname != "") //saving POSTED FILE
			{
				if (avatarUpload.PostedFile.ContentLength > _maxAvatarFileSize)
				{
					//file is too big
					lblResult.Text = Resources.various.ErrorBigAvatar;
					return;
				}
				shortname = Path.GetFileName(shortname);

				//rename if the file already exists
				shortname = Utils.Attachments.ChangeFileNameIfAlreadyExists(shortname, uploadDir);

				//check picture
				Bitmap bmp;
				try
				{
					if (Attachments.IsExtForbidden(shortname)) throw new Exception("NOOOO");
					bmp = new Bitmap(avatarUpload.PostedFile.InputStream);
				}
				catch
				{
					//is't not a picture
					lblResult.Text = Resources.various.ErrorNotPictureFile;
					return;
				}
				if (bmp.Width > _maxAvatarPictureSize || bmp.Height > _maxAvatarPictureSize)
				{
					//the picture is too big
					lblResult.Text = Resources.various.ErrorBigPictureDimensions;
					return;
				}

				avatarUpload.PostedFile.SaveAs(uploadDir + "\\" + shortname);
				Utils.User.SetAvatarUrl(_editedUserID, shortname, false);
			}
			else
			{
				//no file posted - lets look at radio-buttons
				if (Request.Form["DefaultAvatarInput"] != null) //default avatar or GRAVATAR option selected
				{
					string value = Request.Form["DefaultAvatarInput"];
					bool useGRAVATAR = (value == "GRAVATAR");
					Utils.User.SetAvatarUrl(_editedUserID, useGRAVATAR ? "" : value, useGRAVATAR);
				}
				else if ((tbAvatarURL.Text.StartsWith("http://") || tbAvatarURL.Text.StartsWith("https://")) && tbAvatarURL.Text.Length > 10) //url of the avatar
				{
					Utils.User.SetAvatarUrl(_editedUserID, tbAvatarURL.Text, false);
				}
				else //no avatar - kill it
				{
					Utils.User.SetAvatarUrl(_editedUserID, "", false);
				}
			}

			//reset avatar cache for current user
			if (Utils.User.CurrentUserID == _editedUserID)
				Session["AvatarPath"] = null;

			//to refresh avatar pic
			ShowUserInfo();
		}

		private void ShowDefaultAvatars()
		{
			string[] files = Directory.GetFiles(MapPath("images"), "AspNetForumAvatar*");

			rptDefaultAvatars.Visible = files.Length > 0;

			for (int i = 0; i < files.Length; i++)
			{
				files[i] = Path.GetFileName(files[i]);
			}

			rptDefaultAvatars.DataSource = files;
			rptDefaultAvatars.DataBind();
		}

		protected string GetGravatarUrl()
		{
			return Utils.User.GetGravatarUrl(Utils.User.GetUserEmail(_editedUserID));
		}
	}
}
