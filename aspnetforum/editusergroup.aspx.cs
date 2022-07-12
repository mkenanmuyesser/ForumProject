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
using aspnetforum.Utils;
using System.Collections.Generic;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for editusergroup.
	/// </summary>
	public partial class editusergroup : AdminForumPage
	{
		private int _groupID;
		
		protected void Page_Load(object sender, System.EventArgs e)
		{
			try { _groupID = int.Parse(Request.QueryString["GroupID"]); }
			catch 
			{
				Response.End();
				return;
			}

			BindRepeaters();
		}

		private void BindRepeaters()
		{
			var usersInGroup = Utils.User.GetUserIdsInGroup(_groupID);

			Cn.Open();

			//bind allowed users
			if (usersInGroup.Any())
			{
				rptAllowed.DataSource = Cn.ExecuteOrm<UserInfo>(@"SELECT ForumUsers.UserID, ForumUsers.UserName
				FROM ForumUsers
				WHERE UserID IN (" + usersInGroup.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + ") ORDER BY UserName");
			}
			else
				rptAllowed.DataSource = null;

			rptAllowed.DataBind();

			//bind denied users
			rptDenied.DataSource = Cn.ExecuteOrm<UserInfo>(@"SELECT ForumUsers.UserID, ForumUsers.UserName FROM ForumUsers
				WHERE Disabled=0 
				" + (usersInGroup.Any() ? "AND UserID NOT IN (" + usersInGroup.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y) + ")" : "") + " ORDER BY UserName");
			rptDenied.DataBind();

			Cn.Close();

			lblNoUsersInGroup.Visible = (rptAllowed.Items.Count==0);
		}

		protected void rptAllowed_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			if(e.CommandName=="remove")
			{
				//deny access
				Utils.User.RemoveUserFromGroup(int.Parse(e.CommandArgument.ToString()), _groupID);
			}
			BindRepeaters();
		}

		protected void rptDenied_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
		{
			if(e.CommandName=="add")
			{
				//grant access
				Utils.User.AddUserToGroup(int.Parse(e.CommandArgument.ToString()), _groupID);
			}
			BindRepeaters();
		}
	}
}
