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
using System.Net;
using System.IO;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class admin : AdminForumPage
	{
		private const int DEL_COLUMN_INDEX = 4;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			btnAdd.DataBind(); //stupid fix for code blocks inside server tag

			BindForums();
			if (!IsPostBack)
			{
				BindForumGroups();
			}

			lnkUpgrade.Visible = IsNewVersionAvailable();
		}

		private void BindForums()
		{
			this.Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT * FROM Forums ORDER BY OrderByNumber");
			this.gridForums.DataSource = dr;
			this.gridForums.DataBind();
			dr.Close();
			this.Cn.Close();
			lblNoForums.Visible = (gridForums.Items.Count == 0);
		}

		private void BindForumGroups()
		{
			this.Cn.Open();
			DbDataReader dr = this.Cn.ExecuteReader("SELECT * FROM ForumGroups");
			this.ddlForumGroup.DataSource = dr;
			this.ddlForumGroup.DataBind();
			dr.Close();
			this.Cn.Close();

			//first time launch
			lblSelectGroup.Visible = lnkEditForumGroups.Visible = ddlForumGroup.Visible = lblEnterGroup.Visible = (ddlForumGroup.Items.Count > 0);
		}

		protected void gridForums_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
		{
			if (e.CommandName == "delete")
			{
				string forumid = e.Item.Cells[0].Text;
				Utils.Forum.DeleteForum(int.Parse(forumid));
				BindForums();
			}
			else if (e.CommandName == "up" || e.CommandName == "down")
			{
				SaveCurrentOrderOfSectinsCategories(); //save current picture

				string forumId = e.Item.Cells[0].Text;
				if (e.CommandName == "up")
				{
					if (e.Item.ItemIndex > 0)
					{
						DataGridItem previousItem = gridForums.Items[e.Item.ItemIndex - 1];
						if (previousItem.ItemType == ListItemType.Item || previousItem.ItemType == ListItemType.AlternatingItem)
						{
							string previousForumId = previousItem.Cells[0].Text;

							Cn.Open();
							Cn.ExecuteNonQuery(@"UPDATE Forums SET OrderByNumber = OrderByNumber-1 WHERE ForumID=?", forumId);
							Cn.ExecuteNonQuery(@"UPDATE Forums SET OrderByNumber = OrderByNumber+1 WHERE ForumID=?", previousForumId);
							Cn.Close();
							BindForums();
						}
					}
				}
				if (e.CommandName == "down")
				{
					if (e.Item.ItemIndex < gridForums.Items.Count - 1)
					{
						DataGridItem nextItem = gridForums.Items[e.Item.ItemIndex + 1];
						if (nextItem.ItemType == ListItemType.Item || nextItem.ItemType == ListItemType.AlternatingItem)
						{
							string nextForumId = nextItem.Cells[0].Text;

							Cn.Open();
							Cn.ExecuteNonQuery(@"UPDATE Forums SET OrderByNumber = OrderByNumber+1	WHERE ForumID=?", forumId);
							Cn.ExecuteNonQuery(@"UPDATE Forums SET OrderByNumber = OrderByNumber-1 WHERE ForumID=?", nextForumId);
							Cn.Close();
							BindForums();
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
			foreach (DataGridItem item in gridForums.Items)
			{
				if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
				{
					Cn.ExecuteNonQuery(@"UPDATE Forums SET OrderByNumber = ? WHERE ForumID=?", item.ItemIndex, item.Cells[0].Text);
				}
			}
			Cn.Close();
		}

		protected void btnAdd_Click(object sender, System.EventArgs e)
		{
			lblError.Visible = false;

			if ((tbForumGroup.Text == "" && ddlForumGroup.Items.Count == 0)
				|| tbTitle.Text.Trim() == ""
				|| tbDescr.Text.Trim() == "")
			{
				lblError.Visible = true;
				BindForumGroups(); //to rebind the groups
				return;
			}

			int forumGroup = 0;

			if (this.tbForumGroup.Text.Trim() != "")
			{
				forumGroup = Utils.Forum.AddForumGroup(tbForumGroup.Text);
			}
			else
			{
				forumGroup = int.Parse(ddlForumGroup.SelectedValue);
			}

			//finally - adding
			Utils.Forum.AddForum(tbTitle.Text.Trim(), tbDescr.Text.Trim(), forumGroup);

			BindForums();
			BindForumGroups();

			this.tbDescr.Text = "";
			this.tbTitle.Text = "";
			this.tbForumGroup.Text = "";
		}

		protected void gridForums_ItemDataBound(object sender, DataGridItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				WebControl btn = e.Item.Cells[DEL_COLUMN_INDEX].Controls[0] as WebControl;
				if (btn != null)
					btn.Attributes.Add("onclick", "return confirm('Delete?');");
			}

			//simply to make grid prettier (align header to center)
			if (e.Item.ItemType == ListItemType.Header)
			{
				e.Item.Cells[1].ColumnSpan = 4;
				e.Item.Cells[2].Visible = false;
				e.Item.Cells[3].Visible = false;
				e.Item.Cells[4].Visible = false;
			}
		}

		/// <summary>
		/// Checks for a new version on the server
		/// </summary>
		private bool IsNewVersionAvailable()
		{
			if (Cache["newVer"] == null)
			{
				bool newVer;
				try
				{
					WebClient wc = new WebClient();
					Stream s = wc.OpenRead("http://www.jitbit.com/getversion.ashx?ProductID=5");
					StreamReader sr = new StreamReader(s);
					string serverVersion = sr.ReadToEnd();
					s.Close();
					newVer = !System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().StartsWith(serverVersion);
				}
				catch { newVer = false; }

				//now cache this
				Cache.Add("newVer", newVer, null, DateTime.Now.AddDays(1),
					System.Web.Caching.Cache.NoSlidingExpiration,
					System.Web.Caching.CacheItemPriority.Normal,
					null);
			}

			return (bool)Cache["newVer"];
		}
	}
}
