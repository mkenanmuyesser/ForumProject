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
	public partial class editsubforums : AdminForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				Cn.Open();
				object res = Cn.ExecuteScalar("SELECT ForumID FROM Forums");
				Cn.Close();
				if (res == null)
				{
					lblNoForumsFound.Visible = true;
					divAddSubforum.Visible = false;
					lblNoSubForums.Visible = true;
					return;
				}

				BindSubForums();
				BindDropDownLists();
			}
		}

		private void BindSubForums()
		{
			this.Cn.Open();
			DbDataReader dr = this.Cn.ExecuteReader(
				@"SELECT Forums.Title AS ParentForumTitle, Forums_SubForums.Title AS SubForumTitle, Forums_SubForums.ForumID AS SubForumID, Forums.ForumID AS ParentForumID
				FROM (Forums INNER JOIN ForumSubforums ON Forums.ForumID = ForumSubforums.ParentForumID)
					INNER JOIN Forums AS Forums_SubForums ON ForumSubforums.SubForumID = Forums_SubForums.ForumID");
			this.gridSubForums.DataSource = dr;
			this.gridSubForums.DataBind();
			dr.Close();
			this.Cn.Close();
			lblNoSubForums.Visible = (gridSubForums.Items.Count == 0);
			gridSubForums.Visible = (gridSubForums.Items.Count != 0);
		}

		private void BindDropDownLists()
		{
			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT ForumID, Title FROM Forums");
			ddlParentForum.DataSource = dr;
			ddlParentForum.DataBind();
			dr.Close();

			dr = Cn.ExecuteReader("SELECT ForumID, Title FROM Forums WHERE ForumID NOT IN (SELECT SubForumID FROM ForumSubforums)");
			ddlSubForum.DataSource = dr;
			ddlSubForum.DataBind();
			dr.Close();
			Cn.Close();

			divAddSubforum.Visible = (ddlParentForum.Items.Count != 0);
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			int parentid = 0, subforumid = 0;
			int.TryParse(ddlParentForum.SelectedValue, out parentid);
			int.TryParse(ddlSubForum.SelectedValue, out subforumid);

			Cn.Open();

			//reverse subforum check
			object res = Cn.ExecuteScalar("SELECT ParentForumID FROM ForumSubforums WHERE ParentForumID=" + subforumid + " AND SubForumID=" + parentid);

			if (parentid != 0 && parentid != subforumid && res == null)
			{
				lblError.Visible = false;
				Cn.ExecuteNonQuery("INSERT INTO ForumSubforums (ParentForumID, SubForumID) VALUES (?, ?)", parentid, subforumid);
			}
			else
			{
				lblError.Visible = true;
			}

			Cn.Close();

			BindDropDownLists();
			BindSubForums();
		}

		protected void gridSubForums_ItemCommand(object source, DataGridCommandEventArgs e)
		{
			if (e.CommandName == "delete")
			{
				string parentid = e.Item.Cells[0].Text;
				string subid = e.Item.Cells[1].Text;
				this.Cn.Open();
				this.Cn.ExecuteNonQuery("DELETE FROM ForumSubforums WHERE ParentForumID=" + parentid + " AND SubForumID=" + subid);
				this.Cn.Close();

				BindSubForums();
				BindDropDownLists();
			}
		}
	}
}
