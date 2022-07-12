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

namespace aspnetforum
{
	/// <summary>
	/// Summary description for forums.
	/// </summary>
	public partial class forums : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
			DataSet ds = Utils.Forum.GetForumsForFrontpage(CurrentUserID);

			rptGroupsList.DataSource = ds.Tables[0];
			rptGroupsList.DataBind();
			
			bool noForums = (rptGroupsList.Items.Count == 0);
			rptGroupsList.Visible = !noForums;
			lblNoForums.Visible = noForums && !IsAdministrator;
			divNoForumsAdmin.Visible = noForums && IsAdministrator;

			divRecent.Visible = recentPosts.Visible = Utils.Settings.ShowRecentPostsOnHomepage;
		}

		protected void rptGroupsList_ItemDataBound(object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e)
		{
			if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
			{
				Repeater nestedRepeater = e.Item.FindControl("rptForumsList") as Repeater;
				DataRowView drv = e.Item.DataItem as DataRowView;
				DataView dv = drv.CreateChildView("ForumGroupsForums");
				if (dv.Count == 0)
				{
					e.Item.Visible = false;
				}
				else
				{
					nestedRepeater.DataSource = dv;
					nestedRepeater.DataBind();
				}
			}
		}

		public static string GetForumIcon(object iconFile)
		{
			if (iconFile == null)
				return "images/forum.png";

			string strIconFile = iconFile.ToString();

			return (strIconFile == "" ? "images/forum.png" : "getforumicon.ashx?icon=" + strIconFile);
		}
	}
}
