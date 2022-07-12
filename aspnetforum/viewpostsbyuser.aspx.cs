using System;
using System.Collections;
using System.Collections.Generic;
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
	/// Summary description for viewpostsbyuser.
	/// </summary>
	public partial class viewpostsbyuser : ForumPage
	{
		protected int userID;
        protected string pagerString = "";
        protected string userName;
        protected string avatarFileName, firstName, lastName;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			try
			{
				userID = int.Parse( Request.QueryString["UserID"] );
			}
			catch
			{
				Response.Write("Invalid UserID passed");
				Response.End();
				return;
			}

			Cn.Open();

			DbDataReader dr = this.Cn.ExecuteReader("SELECT UserName, AvatarFileName, FirstName, LastName FROM ForumUsers WHERE UserID=" + userID);
			if(dr.Read())
			{
                lblUser.Text = userName = dr["UserName"].ToString();
                avatarFileName = dr["AvatarFileName"].ToString();
                firstName = dr["FirstName"].ToString();
                lastName = dr["LastName"].ToString();
			}
            dr.Close();
			BindRepeater();

			Cn.Close();

			Title = "Posts from \"" + userName + "\"";
			MetaDescription = Settings.ForumTitle + " - viewing all forum posts from user \"" + userName + "\"";
		}

		private void BindRepeater()
		{
			List<object> parameters = new List<object>();

			string sql =
				@"SELECT ForumMessages.Body, ForumMessages.CreationDate, ForumTopics.TopicID, ForumTopics.Subject
                FROM (ForumMessages INNER JOIN ForumTopics ON ForumMessages.TopicID=ForumTopics.TopicID)";
			if (CurrentUserID == 0)
				sql += " INNER JOIN Forums ON ForumTopics.ForumID = Forums.ForumID ";
				
			sql += @" WHERE ForumTopics.ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions) AND ForumMessages.UserID=" + userID;

			if (CurrentUserID == 0)
			{
				sql += " AND Forums.MembersOnly=?";
				parameters.Add(false);
			}

			sql += " ORDER BY ForumMessages.CreationDate";

			DbDataReader dr = Cn.ExecuteReader(sql, parameters.ToArray());
			DataTable dt = new DataTable();
			dt.Load(dr);
			PagedDataSource pagedSrc = new PagedDataSource
			                           	{
			                           		DataSource = dt.DefaultView,
			                           		AllowPaging = true,
			                           		PageSize = this.PageSize
			                           	};

			int curPage = 0;
			if(Request.QueryString["page"]!=null)
                int.TryParse(Request.QueryString["page"], out curPage);
			pagedSrc.CurrentPageIndex = curPage;

            //prepare a string for the "pager" at the bottom
			pagerString = Utils.Various.GetPaginationString(curPage, pagedSrc.PageCount, "viewpostsbyuser.aspx?UserID=" + userID);

			this.rptMessagesList.DataSource = pagedSrc;
			this.rptMessagesList.DataBind();
		}
	}
}
