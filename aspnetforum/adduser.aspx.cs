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
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class adduser : AdminForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			btnAdd.DataBind();
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			if (!IsValid) return;

			string username = txUserName.Text.Trim();
			Cn.Open();
			var res = Cn.ExecuteScalar("select UserID from ForumUsers WHERE UserName=?", username);
			if (res == null)
			{
				res = Cn.ExecuteScalar("select UserID from ForumUsers WHERE Email=?", txEmail.Text);
				if (res == null)
				{
					int userId = Utils.User.CreateUser(username, txEmail.Text, Utils.Password.CalculateHash(txPsw.Text), txHomepage.Text, string.Empty, false);
					lblError.Visible = false;
					lblSuccess.Visible = true;
					Response.Redirect("viewprofile.aspx?UserID="+ userId);
				}
				else
				{
					lblError.Text = "Email address already exists!";
					lblError.Visible = true;
					lblSuccess.Visible = false;
				}
			}
			else
			{
				lblError.Text = "User already exists!";
				lblError.Visible = true;
				lblSuccess.Visible = false;
			}
			Cn.Close();
		}
	}
}
