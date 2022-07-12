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
	public partial class activate : ForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			string username = Request.QueryString["user"];
			string code = Request.QueryString["code"];
			if (username == null || code == null)
			{
				Response.End();
				return;
			}

			Cn.Open();
			object res = Cn.ExecuteScalar(
				"select UserID from ForumUsers WHERE UserName=? AND ActivationCode=?",
				username,
				code);
			Cn.Close();

			if (res != null)
			{
				Utils.User.EnableUser(Convert.ToInt32(res), false);
				lblSuccess.Visible = true;
				lblError.Visible = false;
			}
			else
			{
				lblError.Visible = true;
				lblSuccess.Visible = false;
			}
		}
	}
}
