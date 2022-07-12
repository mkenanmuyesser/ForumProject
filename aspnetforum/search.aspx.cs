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
	/// Summary description for search.
	/// </summary>
	public partial class search : ForumPage
	{
	
		protected void Page_Load(object sender, System.EventArgs e)
		{
			ForumPage.AssignButtonTextboxEnterKey(tbWords, btnSearch);

			if (!IsPostBack)
			{
				Cn.Open();
				ListItem lstEverywhere = new ListItem(Resources.various.Everywhere, "");
				lstEverywhere.Attributes.Add("style", "color:#777777");
				ddlForum.Items.Add(lstEverywhere);
				BindForumsListRecursive(0, 0);
				Cn.Close();
			}
		}

		protected void btnSearch_Click(object sender, System.EventArgs e)
		{
			if (!Page.IsValid) return;

			string searchStr = tbWords.Text.Trim().Replace("'", ""); //injection protection
			string[] words = searchStr.Split(new[] { ' ', ',' });
			string commandText = "";
			List<object> parameters = new List<object>();

#if TRIAL
			commandText = "SELECT DISTINCT TOP 5 ";
#else
			commandText = "SELECT DISTINCT ";
#endif

			commandText += @" ForumTopics.TopicID, ForumTopics.Subject, ForumTopics.LastMessageID, ForumTopics.RepliesCount
								FROM ForumTopics
								INNER JOIN ForumMessages ON ForumMessages.TopicID = ForumTopics.TopicID";

			if (CurrentUserID == 0) //guest user - search in public forums only
			{
				commandText += " WHERE ForumTopics.ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE AllowReading=?) ";
				parameters.Add(true);
			}
			else //search in allowed forums only
			{
				string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID);

				commandText += " WHERE ForumTopics.ForumID IN (" + strSQLAllowedForums + " ) ";
			}

			if (tbDateTo.Text != "")
			{
				commandText += " AND ForumMessages.CreationDate<?";
				parameters.Add(DateTime.Parse(tbDateTo.Text));
			}
			if (tbDateFrom.Text != "")
			{
				commandText += " AND ForumMessages.CreationDate>?";
				parameters.Add(DateTime.Parse(tbDateFrom.Text));
			}

			if (ddlForum.SelectedValue != "")
			{
				commandText += string.Format(" AND (ForumTopics.ForumID = {0} OR ForumTopics.ForumID IN (SELECT SubForumID FROM ForumSubforums WHERE ParentForumID={0})) ",
					int.Parse(ddlForum.SelectedValue));
			}

			if (rbAll.Checked)
			{
				string criteria = "";
				foreach (string word in words)
				{
					criteria += "(ForumTopics.Subject LIKE '%" + word + "%' ";
					if (!cbSearchTtitleOnly.Checked)
						criteria += "OR ForumMessages.Body LIKE '%" + word + "%' ";
					criteria += ") AND ";
				}
				criteria = " AND (" + criteria.Substring(0, criteria.Length - 5) + ")";
				commandText += criteria;
			}
			else if (rbExact.Checked)
			{
				commandText += " AND (ForumTopics.Subject LIKE '%" + searchStr + "%' ";
				if (!cbSearchTtitleOnly.Checked)
					commandText += "OR ForumMessages.Body LIKE '%" + searchStr + "%' ";
				commandText += ")";
			}
			else if (rbAny.Checked)
			{
				string criteria = "";
				foreach (string word in words)
				{
					criteria += "ForumTopics.Subject LIKE '%" + word + "%' ";
					if (!cbSearchTtitleOnly.Checked)
						criteria += "OR ForumMessages.Body LIKE '%" + word + "%' ";
					criteria += " OR ";
				}
				criteria = " AND (" + criteria.Substring(0, criteria.Length - 4) + ")";
				commandText += criteria;
			}

			this.Cn.Open();
			DbDataReader dr = Cn.ExecuteReader(commandText, parameters.ToArray());
			lblNothingFound.Visible = !dr.HasRows;
#if TRIAL
			if (!lblNothingFound.Visible) //something found
			{
				lblNothingFound.Visible = true;
				lblNothingFound.Text = "The free version returns the first 5 results only";
				lblNothingFound.ForeColor = Color.Red;
			}
#endif
			DataTable dt = new DataTable();
			dt.Load(dr);
			dr.Close();
			this.rptTopicsList.DataSource = dt;
			this.rptTopicsList.DataBind();
			this.Cn.Close();
		}

		protected void CustomValidatorDateFrom_ServerValidate(object source, ServerValidateEventArgs args)
		{
			DateTime res;
			args.IsValid = DateTime.TryParse(tbDateFrom.Text, out res);
		}

		protected void CustomValidatorDateTo_ServerValidate(object source, ServerValidateEventArgs args)
		{
			DateTime res;
			args.IsValid = DateTime.TryParse(tbDateTo.Text, out res);
		}

		private void BindForumsListRecursive(int parentId, int lvl)
		{
			List<object> parameters = new List<object>();
			string commandText = "SELECT Forums.ForumID, Forums.Title FROM Forums ";

			if (parentId == 0) //not a subforum
				commandText += "WHERE Forums.ForumID NOT IN (SELECT SubForumID FROM ForumSubforums)"; //not a subforum
			else //a subforum of a specified parent
				commandText +=
					@"INNER JOIN ForumSubforums ON ForumSubforums.SubForumID=Forums.ForumID
					WHERE ForumSubforums.ParentForumID=" + parentId; //subforum for parentID

			if (CurrentUserID == 0)
			{
				//not a restricted forum
				commandText += @" AND Forums.MembersOnly=0
					AND Forums.ForumID NOT IN (SELECT ForumID FROM ForumGroupPermissions WHERE ForumGroupPermissions.AllowReading=?) ";
				parameters.Add(true);
			}
			else
			{
				string strSQLAllowedForums = Utils.Forum.GetReadableForumsForUserString(Utils.User.CurrentUserID);

				//not a restricted forum or - a forum with permissions
				commandText += @" AND Forums.ForumID IN ( " + strSQLAllowedForums + ")";
			}

			DataTable dt = new DataTable();
			DbDataReader dr = Cn.ExecuteReader(commandText, parameters.ToArray());
			if(dr.HasRows) dt.Load(dr);
			dr.Close();

			foreach(DataRow row in dt.Rows)
			{
				string indent = new string('-', lvl);
				ListItem lstItm = new ListItem(indent + row["Title"].ToString(), row["ForumID"].ToString());
				ddlForum.Items.Add(lstItm);
				BindForumsListRecursive(Convert.ToInt32(row["ForumID"]), lvl + 1);
			}
		}
	}
}