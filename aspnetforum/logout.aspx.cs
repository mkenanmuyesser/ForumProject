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

namespace aspnetforum
{
	/// <summary>
	/// Summary description for logout.
	/// </summary>
	public partial class logout : ForumPage
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
            Utils.User.Logout();
            Response.Redirect("default.aspx");
		}
	}
}
