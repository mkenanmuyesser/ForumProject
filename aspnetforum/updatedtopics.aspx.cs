using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.Common;
using System.Data;

namespace aspnetforum
{
    public partial class updatedtopics : ForumPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUserID == 0) //if anonymous user - hide "membersonly" forums
            {
                Response.End();
                return;
            }

            BindRepeater();
        }

        public void BindRepeater()
        {
            DataTable dt = Utils.UnreadTracker.GetUpdatedThreads();
            rptTopicsList.DataSource = dt;
            rptTopicsList.DataBind();
			rptTopicsList.Visible = rptTopicsList.Items.Count > 0;
        }
    }
}
