using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace aspnetforum
{
	/// <summary>
	/// datagrid with THEAD and TBODY (required for correct rounded corners display
	/// </summary>
	public class THDataGrid : System.Web.UI.WebControls.DataGrid
	{
		protected override void OnPreRender(EventArgs e)
		{
			this.UseAccessibleHeader = true;

			if (Controls.Count > 0)
			{
				Table table = Controls[0] as Table;

				if (table != null && table.Rows.Count > 0)
				{
					table.Rows[0].TableSection = TableRowSection.TableHeader;
					table.Rows[table.Rows.Count - 1].TableSection = TableRowSection.TableFooter;
				}
			}

			base.OnPreRender(e);
		}
	}
}