using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class purgeoldtopics : AdminForumPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void btnPurge_Click(object sender, EventArgs e)
		{
			List<int> topicIds = new List<int>();

			Cn.Open();
			var dr = Cn.ExecuteReader(
				@"SELECT ForumTopics.TopicID
				FROM ForumTopics
				INNER JOIN ForumMessages ON ForumTopics.LastMessageID=ForumMessages.MessageID
				WHERE ForumMessages.CreationDate<?", DateTime.Parse(tbDateFrom.Text));
			while (dr.Read())
			{
				topicIds.Add(Convert.ToInt32(dr[0]));
			}
			dr.Close();

			foreach (int topicId in topicIds)
			{
				Topic.DeleteTopic(topicId, Cn);
			}

			Cn.Close();
			lblRes.Text = "OK!";
		}
	}
}