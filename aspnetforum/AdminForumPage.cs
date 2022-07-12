using System;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for AdminForumPage.
	/// </summary>
	public class AdminForumPage : ForumPage
	{
		protected override void OnLoad(EventArgs e)
		{
			if(!IsAdministrator)
			{
				if (Cn.State == System.Data.ConnectionState.Open)
					Cn.Close();

				Response.Redirect("default.aspx", true);
				Response.End();
				return;
			}

			base.OnLoad (e);
		}
	}
}
