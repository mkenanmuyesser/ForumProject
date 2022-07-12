using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace aspnetforum
{
    public partial class adminonlineusers : AdminForumPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            /*foreach (Utils.User.SessionParameters sp in Utils.User.OnlineUsersSessions.Values)
            {
                HtmlTableRow row = new HtmlTableRow();
                row.InnerHtml = string.Format("<td>{0}</td><td>{1}</td><td>{2}</td>",
                    sp.UserName,
                    sp.CurrentURL,
                    sp.LastActivity.ToShortTimeString());
                tblUsers.Rows.Add(row);
            }*/
            rptUsers.DataSource = Utils.User.OnlineUsersSessions.Values;
            rptUsers.DataBind();
        }
    }
}
