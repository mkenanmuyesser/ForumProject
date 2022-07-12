using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace aspnetforum
{
    public partial class dbupgrade : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Page_PreInit(object sender, EventArgs e)
        {
            HttpContext.Current.Items["IgnoreDbSettingErrors"] = true;
        }        
        
        protected void btnGo_Click(object sender, EventArgs e)
        {
            try
            {
                Utils.DB.UpdateDBToLatestVersion();
                lblResult.Text = "Success!!! <a href='default.aspx'>Return to homepage...</a>";
            }
            catch (Exception ex)
            {
                lblResult.Text = ex.ToString();
            }
        }
    }
}
