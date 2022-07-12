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
using aspnetforum.Utils;

namespace aspnetforum
{
	/// <summary>
	/// Summary description for users.
	/// </summary>
	public partial class allusers : ForumPage
	{
		protected string pagerString = "";
		private bool _showDisabled;
	
		protected void Page_Load(object sender, System.EventArgs e)
		{
			// if not authenticated - get out
			if (CurrentUserID == 0)
			{
				Response.Redirect("default.aspx", true);
				return;
			}

			_showDisabled = (Request.QueryString["Disabled"] == "1");

			ForumPage.AssignButtonTextboxEnterKey(tbUsername, "btnSearch");

			btnDel.Visible = btnDisable.Visible = lnkAdd.Visible = IsAdministrator;

			spanNonActive.Visible = IsAdministrator && !_showDisabled;
			spanActive.Visible = IsAdministrator && _showDisabled;
			if (_showDisabled)
			{
				btnDisable.ToolTip = "RE-ENABLE selected users";
			}

			if(Request.QueryString["q"]==null ||Request.QueryString["q"].Length==0)
				BindRepeater(null);
			else
				BindRepeater(Server.UrlDecode(Request.QueryString["q"]));
		}

		private void BindRepeater(string username)
		{
			Cn.Open();
			string sql = "SELECT * FROM ForumUsers WHERE Disabled=? ";
			
			if(!IsAdministrator)
				sql += " AND HidePresence=0";


			if (Request.QueryString["Admin"] != null)
				sql += " AND UserID IN (SELECT UserID FROM ForumAdministrators)";

			if (username != null && username.Trim() != "")
			{
				username = username.Replace("'", ""); //injection protection
				sql += string.Format(" AND (UserName LIKE '{0}%' OR Email LIKE '{0}%') ", username);
			}

			string order = Request.QueryString["order"];

			if (order == "regdate")
				sql += " ORDER BY RegistrationDate";
			else if (order == "email")
				sql += " ORDER BY Email";
			else if (order == "posts")
				sql += " ORDER BY PostsCount";
			else if (order == "logondate")
				sql += " ORDER BY LastLogonDate";
			else
				sql += " ORDER BY UserName";
			DataTable dt = new DataTable();
			DbDataReader dr = Cn.ExecuteReader(sql, (Request.QueryString["Disabled"] == "1"));
			dt.Load(dr);
			dr.Close();
			Cn.Close();

			PagedDataSource pagedSrc = new PagedDataSource();
			pagedSrc.DataSource = dt.DefaultView;
			pagedSrc.AllowPaging = true;
			pagedSrc.PageSize = this.PageSize * 5;
			int curPage = 0;
			if (Request.QueryString["page"] != null)
				int.TryParse(Request.QueryString["page"], out curPage);
			pagedSrc.CurrentPageIndex = curPage;
			pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, "allusers.aspx?order=" + order + "&q=" + Server.UrlEncode(username == null ? "" : username));

			this.rptUsersList.DataSource = pagedSrc;
			this.rptUsersList.DataBind();
		}

		protected string ShowEmail(object email)
		{
			return IsAdministrator ? email.ToString() : "******"; 
		}

		protected void btnDel_Click(object sender, EventArgs e)
		{
			/*foreach (RepeaterItem itm in rptUsersList.Items)
			{
				if (itm.ItemType == ListItemType.Item || itm.ItemType == ListItemType.AlternatingItem)
				{
					CheckBox cb = itm.FindControl("cbDel") as CheckBox;
					if (cb != null && cb.Checked)
					{
						Label lblUserID = itm.FindControl("lblUserID") as Label;
						int userid = Convert.ToInt32(lblUserID.Text);
						Utils.User.DeleteUser(userid);
					}
				}
			}*/
			foreach (string key in Request.Form.Keys)
			{
				if (key.StartsWith("cbDel"))
				{
					int userid;
					if(int.TryParse(key.Substring(5), out userid))
						Utils.User.DeleteUser(userid);
				}
			}
			BindRepeater(null);
		}

		protected void btnDisable_Click(object sender, EventArgs e)
		{
			foreach (string key in Request.Form.Keys)
			{
				if (key.StartsWith("cbDel"))
				{
					int userid = Convert.ToInt32(key.Substring(5));
					if(!_showDisabled)
						Utils.User.DisableUser(userid);
					else
						Utils.User.EnableUser(userid, false);
				}
			}
			BindRepeater(null);
		}
	}
}
