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
    public partial class adminusergroups : AdminForumPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			//to force creation of "all_users" group
        	Utils.User.GetAllUsersGroupId();

            BindGroups();
        }

        private void BindGroups()
        {
            this.Cn.Open();
            DbDataReader dr = Cn.ExecuteReader("SELECT * FROM ForumUserGroups");
            this.gridGroups.DataSource = dr;
            this.gridGroups.DataBind();
            dr.Close();
            this.Cn.Close();
            lblNoGroups.Visible = (gridGroups.Items.Count == 0);
        }

        protected void btnAddGroup_Click(object sender, System.EventArgs e)
        {
            if (tbGroupTitle.Text == "") return;

            this.Cn.Open();
            this.Cn.ExecuteNonQuery("INSERT INTO ForumUserGroups (Title) VALUES (?)", tbGroupTitle.Text);
            this.Cn.Close();

            BindGroups();

            tbGroupTitle.Text = "";
        }

        protected void gridGroups_ItemCommand(object source, System.Web.UI.WebControls.DataGridCommandEventArgs e)
        {
            if (e.CommandName == "delete")
            {
                string groupid = e.Item.Cells[0].Text;
				if (groupid != Utils.User.GetAllUsersGroupId().ToString())
				{
					this.Cn.Open();
					this.Cn.ExecuteNonQuery("DELETE FROM ForumGroupPermissions WHERE GroupID=" + groupid);
					this.Cn.ExecuteNonQuery("DELETE FROM ForumUsersInGroup WHERE GroupID=" + groupid);
					this.Cn.ExecuteNonQuery("DELETE FROM ForumUserGroups WHERE GroupID=" + groupid);
					this.Cn.Close();
				}

                BindGroups();
            }
        }
    }
}
