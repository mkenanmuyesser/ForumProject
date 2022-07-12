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
using System.IO;
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for editaspnetforum.
	/// </summary>
	public partial class editforum : AdminForumPage
	{
		int _forumId;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			_forumId = int.Parse( Request.QueryString["ForumID"] );

			if(!IsPostBack)
			{
				BindForumProperties();
				BindGroupsList();
			}
			BindModeratorsGrid();
			BindPermissionsGrid();
		}

		private void BindForumProperties()
		{
			Cn.Open();

			DbDataReader dr = Cn.ExecuteReader("SELECT * FROM ForumGroups ORDER BY OrderByNumber");
			ddlForumGroup.DataSource = dr;
			ddlForumGroup.DataBind();
			dr.Close();

			dr = Cn.ExecuteReader("SELECT * FROM Forums WHERE ForumID=" + _forumId);
			if(dr.Read())
			{
				tbTitle.Text = dr["Title"].ToString();
				tbDescr.Text = dr["Description"].ToString();
				cbPremoderated.Checked = Convert.ToBoolean(dr["Premoderated"]);
				cbMembersOnly.Checked = Convert.ToBoolean(dr["MembersOnly"]);
				cbRestrictTopicCreation.Checked = Convert.ToBoolean(dr["RestrictTopicCreation"]);
				ddlForumGroup.SelectedValue = dr["GroupID"].ToString();
				imgForumIcon.ImageUrl = forums.GetForumIcon(dr["IconFile"].ToString());
			}
			dr.Close();

			Cn.Close();
		}

		private void BindModeratorsGrid()
		{
			Cn.Open();
			gridModerators.DataSource = Cn.ExecuteReader(@"SELECT ForumModerators.UserID, ForumUsers.UserName 
				FROM ForumModerators INNER JOIN ForumUsers ON ForumModerators.UserID = ForumUsers.UserID
				WHERE ForumModerators.ForumID=" + _forumId);
			gridModerators.DataBind();
			Cn.Close();
			lblNoModerators.Visible = (gridModerators.Items.Count==0);
		}

		private void BindPermissionsGrid()
		{
			Cn.Open();
			gridGroups.DataSource = Cn.ExecuteReader(@"SELECT ForumUserGroups.GroupID, ForumUserGroups.Title, ForumGroupPermissions.AllowReading, ForumGroupPermissions.AllowPosting
				FROM ForumUserGroups INNER JOIN ForumGroupPermissions ON ForumGroupPermissions.GroupID = ForumUserGroups.GroupID
				WHERE ForumGroupPermissions.ForumID=" + _forumId);
			gridGroups.DataBind();
			Cn.Close();
			lblFFA.Visible = (gridGroups.Items.Count==0);
			gridGroups.Visible = (gridGroups.Items.Count!=0);
		}

		private void BindGroupsList()
		{
			Cn.Open();
			ddlGroups.DataSource = Cn.ExecuteReader("SELECT * FROM ForumUserGroups ORDER BY Title");
			ddlGroups.DataBind();
			Cn.Close();
		}

		protected void gridModerators_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if(e.CommandName == "delete")
			{
				Cn.Open();
				Cn.ExecuteNonQuery("DELETE FROM ForumModerators WHERE UserID=? AND ForumID=?", int.Parse(e.Item.Cells[0].Text), _forumId);
				Cn.Close();

				BindModeratorsGrid();
			}
		}

		protected void btnSave_Click(object sender, System.EventArgs e)
		{
			if (tbForumGroup.Text.Trim() == "" && ddlForumGroup.Items.Count == 0)
			{
				Response.Write("error");
				return;
			}

			Cn.Open();

			int forumGroup = 0;

			if (tbForumGroup.Text.Trim() != "")
			{
				Cn.ExecuteNonQuery("INSERT INTO ForumGroups (GroupName) VALUES (?)", tbForumGroup.Text);
				forumGroup = Convert.ToInt32(Cn.ExecuteScalar("SELECT GroupID FROM ForumGroups WHERE GroupName='" + tbForumGroup.Text + "'"));
			}
			else
			{
				forumGroup = int.Parse(ddlForumGroup.SelectedValue);
			}

			string uploadDir = Attachments.GetIconsDirAbsolutePath();

			string iconFileName = iconUpload.PostedFile.FileName;
			if (iconFileName != "" && !Attachments.IsExtForbidden(iconFileName))
			{
				//deleting old iconfile form disk
				object res = Cn.ExecuteScalar("SELECT IconFile FROM Forums WHERE ForumID=" + _forumId);
				if (res != null && res.ToString() != "")
				{
					File.Delete(uploadDir + "\\" + res);
				}

				iconFileName = Path.GetFileName(iconFileName);
				//rename if the file already exists
				iconFileName = Utils.Attachments.ChangeFileNameIfAlreadyExists(iconFileName, uploadDir);
				iconUpload.PostedFile.SaveAs(uploadDir + "\\" + iconFileName);

				//saving icon to DB
				Cn.ExecuteNonQuery("UPDATE Forums SET IconFile=? WHERE ForumID=?", iconFileName, _forumId);
			}

			Cn.ExecuteNonQuery("UPDATE Forums SET Title=?, Description=?, Premoderated=?, GroupID=?, MembersOnly=?, RestrictTopicCreation=? WHERE ForumID=?",
				tbTitle.Text, tbDescr.Text, cbPremoderated.Checked, forumGroup, cbMembersOnly.Checked, cbRestrictTopicCreation.Checked, _forumId);
			Cn.Close();

			//to update the front-page with new name, icon etc.
			Forum.ClearFrontPageCacheForGuests();

			Response.Redirect("admin.aspx", true);

			//tbForumGroup.Text = "";
			//BindForumProperties();
		}

		protected void btnAdd_Click(object sender, System.EventArgs e)
		{
			int userId = 0;
			if (!string.IsNullOrEmpty(hiddenUserId.Value))
				userId = int.Parse(hiddenUserId.Value);
			else
				userId = Utils.User.GetUserIdByUserName(tbModerator.Text);

			if (userId != 0)
			{
				Utils.User.AddForumModerator(_forumId, userId);
				BindModeratorsGrid();
			}
		}

		protected void btnAddPermission_Click(object sender, System.EventArgs e)
		{
			if(ddlGroups.SelectedValue=="") return;
			Cn.Open();

			//delete just in case
			Cn.ExecuteNonQuery("DELETE FROM ForumGroupPermissions WHERE GroupID=" + ddlGroups.SelectedValue + " AND ForumID=" + _forumId);

			Cn.ExecuteNonQuery("INSERT INTO ForumGroupPermissions (GroupID, ForumID, AllowReading, AllowPosting) VALUES(?, ?, ?, ?)",
				ddlGroups.SelectedValue, _forumId, chkAllowReadingNew.Checked, chkAllowPostingNew.Checked);
			
			Cn.Close();

			Forum.ClearFrontPageCacheForGuests();

			BindPermissionsGrid();
		}

		protected void gridGroups_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if(e.CommandName=="delete")
			{
				string groupid = e.Item.Cells[0].Text;
				Cn.Open();
				Cn.ExecuteNonQuery("DELETE FROM ForumGroupPermissions WHERE GroupID=" + groupid + " AND ForumID=" + _forumId);
				Cn.Close();
				Forum.ClearFrontPageCacheForGuests();
				BindPermissionsGrid();
				return;
			}
			if(e.CommandName=="save")
			{
				string groupid = e.Item.Cells[0].Text;
				CheckBox chkAllowReading = (CheckBox)e.Item.Cells[2].FindControl("chkAllowReading");
				CheckBox chkAllowPosting = (CheckBox)e.Item.Cells[2].FindControl("chkAllowPosting");
				Cn.Open();
				Cn.ExecuteNonQuery("UPDATE ForumGroupPermissions SET AllowReading=?, AllowPosting=? WHERE GroupID="+groupid+" AND ForumID="+_forumId,
					chkAllowReading.Checked, chkAllowPosting.Checked);
				Cn.Close();
				Forum.ClearFrontPageCacheForGuests();
				BindPermissionsGrid();
				return;
			}
		}

		protected void btnReset_Click(object sender, System.EventArgs e)
		{
			string uploadDir = Utils.Attachments.GetIconsDirAbsolutePath();
			Cn.Open();
			//deleting old iconfile form disk
			object res = Cn.ExecuteScalar("SELECT IconFile FROM Forums WHERE ForumID=" + _forumId);
			if (res != null && res.ToString() != "")
			{
				File.Delete(uploadDir + "\\" + res);
			}
			//saving icon to DB
			Cn.ExecuteNonQuery("UPDATE Forums SET IconFile=? WHERE ForumID=?", "", _forumId);
			
			Cn.Close();

			imgForumIcon.ImageUrl = forums.GetForumIcon("");
		}
	}
}
