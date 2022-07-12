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
	public partial class forumgroups : AdminForumPage
	{
		private const int DEL_COLUMN_INDEX = 5;

		protected void Page_Load(object sender, EventArgs e)
		{
			if(!IsPostBack)
				BindGroups();
		}

		private void BindGroups()
		{
			this.Cn.Open();
			DbDataReader dr = this.Cn.ExecuteReader("SELECT * FROM ForumGroups ORDER BY OrderByNumber");
			this.gridForumGroups.DataSource = dr;
			this.gridForumGroups.DataBind();
			dr.Close();
			this.Cn.Close();
		}

		protected void gridForumGroups_EditCommand(object source, DataGridCommandEventArgs e)
		{
			gridForumGroups.EditItemIndex = e.Item.ItemIndex;
			BindGroups();
		}

		protected void gridForumGroups_CancelCommand(object source, DataGridCommandEventArgs e)
		{
			gridForumGroups.EditItemIndex = -1;
			BindGroups();
		}

		protected void gridForumGroups_UpdateCommand(object source, DataGridCommandEventArgs e)
		{
			TextBox tbName = e.Item.Cells[1].Controls[0] as TextBox;
			
			string groupid = e.Item.Cells[0].Text;
			this.Cn.Open();
			Cn.ExecuteNonQuery("UPDATE ForumGroups SET GroupName=? WHERE GroupID=?", tbName.Text, groupid);
			this.Cn.Close();
			gridForumGroups.EditItemIndex = -1;
			BindGroups();
		}

		protected void gridForumGroups_ItemDataBound(object sender, DataGridItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				WebControl btn = e.Item.Cells[DEL_COLUMN_INDEX].Controls[0] as WebControl;
				if (btn != null)
					btn.Attributes.Add("onclick", "return confirm('Delete?');");
			}
		}

		protected void gridForumGroups_ItemCommand(object source, DataGridCommandEventArgs e)
		{
			if (e.CommandName == "delete")
			{
				string groupid = e.Item.Cells[0].Text;
				this.Cn.Open();
				Cn.ExecuteNonQuery("DELETE FROM ForumGroups WHERE GroupID=? and GroupID NOT IN (SELECT GroupID FROM Forums)", groupid);
				this.Cn.Close();
				BindGroups();
			}
			else if (e.CommandName == "up" || e.CommandName == "down")
			{
				SaveCurrentOrderOfSectinsCategories(); //save current picture

				string groupId = e.Item.Cells[0].Text;
				if (e.CommandName == "up")
				{
					if (e.Item.ItemIndex > 0)
					{
						DataGridItem previousItem = gridForumGroups.Items[e.Item.ItemIndex - 1];
						if (previousItem.ItemType == ListItemType.Item || previousItem.ItemType == ListItemType.AlternatingItem)
						{
							string previousGroupId = previousItem.Cells[0].Text;

							Cn.Open();
							Cn.ExecuteNonQuery(@"UPDATE ForumGroups SET OrderByNumber = OrderByNumber-1 WHERE GroupID=?", groupId);
							Cn.ExecuteNonQuery(@"UPDATE ForumGroups SET OrderByNumber = OrderByNumber+1 WHERE GroupID=?", previousGroupId);
							Cn.Close();
							BindGroups();
						}
					}
				}
				if (e.CommandName == "down")
				{
					if (e.Item.ItemIndex < gridForumGroups.Items.Count - 1)
					{
						DataGridItem nextItem = gridForumGroups.Items[e.Item.ItemIndex + 1];
						if (nextItem.ItemType == ListItemType.Item || nextItem.ItemType == ListItemType.AlternatingItem)
						{
							string nextGroupId = nextItem.Cells[0].Text;

							Cn.Open();
							Cn.ExecuteNonQuery(@"UPDATE ForumGroups SET OrderByNumber = OrderByNumber+1	WHERE GroupID=?", groupId);
							Cn.ExecuteNonQuery(@"UPDATE ForumGroups SET OrderByNumber = OrderByNumber-1 WHERE GroupID=?", nextGroupId);
							Cn.Close();
							BindGroups();
						}
					}
				}
			}
		}

		/// <summary>
		/// saves the current order of forums in which they ALREADY APPEAR inthe grid
		/// </summary>
		private void SaveCurrentOrderOfSectinsCategories()
		{
			Cn.Open();
			foreach (DataGridItem item in gridForumGroups.Items)
			{
				if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
				{
					Cn.ExecuteNonQuery(@"UPDATE ForumGroups SET OrderByNumber = ? WHERE GroupID=?", item.ItemIndex, item.Cells[0].Text);
				}
			}
			Cn.Close();
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			if (this.tbForumGroup.Text.Trim() != "")
			{
				Cn.Open();
				Cn.ExecuteNonQuery("INSERT INTO ForumGroups (GroupName) VALUES (?)", tbForumGroup.Text);
				Cn.Close();
				BindGroups();
			}
		}
	}
}
