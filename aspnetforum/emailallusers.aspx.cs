using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class emailallusers : AdminForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				BindGroupsList();
			}
		}

		protected void btnSend_Click(object sender, EventArgs e)
		{
			if (tbBody.Text.Length == 0 || tbSubj.Text.Length == 0) return;

			//send emails
			int groupId = int.Parse(ddlGroups.SelectedValue);
			SendNotifications.SendEmailToUserGroup(groupId, tbSubj.Text, tbBody.Text, rbPM.Checked);

			lblOK.Visible = true;
		}

		private void BindGroupsList()
		{
			Cn.Open();
			ddlGroups.DataSource = Cn.ExecuteReader("SELECT * FROM ForumUserGroups ORDER BY Title");
			ddlGroups.DataBind();
			Cn.Close();
		}
	}
}
