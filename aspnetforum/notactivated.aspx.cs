using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace aspnetforum
{
	public partial class notactivated : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			btnResend.Visible = aspnetforum.Utils.Settings.EnableEmailActivation;
		}

		protected void btnResend_Click(object sender, EventArgs e)
		{
			int? userId = Session["InvalidLoginUserId"] as int?;
			if (!userId.HasValue) return;

			Utils.User.SendActivationEmail(userId.Value);

			Session.Remove("InvalidLoginUserId");


			Response.Redirect("default.aspx", true);
		}
	}
}