using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Data.Common;
using Jitbit.Utils;
using aspnetforum.Utils;

namespace aspnetforum
{
	public partial class adminsettings : AdminForumPage
	{
		protected void Page_Load( object sender, EventArgs e )
		{
			if ( !IsPostBack )
			{
				PrepopulateSettings();
				BindSettings();
			}
		}

		void PrepopulateSettings()
		{
			// pre-generate all missing settings from web.config to not confuse 
			//   the administrator when he opens this for the first time
			DbAwareSettings.Current.Preload();
		}

		private void BindSettings()
		{
			Cn.Open();
			DbDataReader dr = Cn.ExecuteReader("SELECT * FROM ForumConfig ORDER BY CfgKey");
			gridSettings.DataSource = dr;
			gridSettings.DataBind();
			dr.Close();
			Cn.Close();
		}

		protected void gridSettings_EditCommand(object source, DataGridCommandEventArgs e)
		{
			gridSettings.EditItemIndex = e.Item.ItemIndex;
			BindSettings();
		}

		protected void gridSettings_CancelCommand(object source, DataGridCommandEventArgs e)
		{
			gridSettings.EditItemIndex = -1;
			BindSettings();
		}

		protected void gridSettings_UpdateCommand(object source, DataGridCommandEventArgs e)
		{
			string oldKey = e.Item.Cells[0].Text;

			string newValue = "";
			TableCell valueCell = e.Item.Cells[2];
			TextBox tbValue = ( valueCell.FindControl("tbValue") as TextBox );
			CheckBox cbValue = ( valueCell.FindControl( "cbValue" ) as CheckBox );
			if ( tbValue.Visible )
			{
				newValue = tbValue.Text;
			}
			else if ( cbValue.Visible )
			{
				// tolower here is not really necessary, merely for uniformity
				newValue = cbValue.Checked.ToString().ToLower(); 
			}
			else
			{
				throw new Exception( "Unexpected condition while editing" );
			}

			DbAwareSettings.Current[oldKey] = newValue;

			gridSettings.EditItemIndex = -1;
			BindSettings();
		}

		protected void gridSettings_ItemDataBound(object source, DataGridItemEventArgs e)
		{
			if ( e.Item.ItemType == ListItemType.EditItem )
			{
				// determine if we'll use checkbox or textbox
				object valObj = DataBinder.Eval( e.Item.DataItem, "CfgValue" );
				string value = (null == valObj) ? "" : valObj.ToString();
				TableCell valueCell = e.Item.Cells[2];
				TextBox tb = valueCell.FindControl( "tbValue" ) as TextBox;
				CheckBox cb = valueCell.FindControl( "cbValue" ) as CheckBox;
				tb.Text = value;
				bool tmp = false;
				cb.Visible = bool.TryParse( value, out tmp );
				cb.Checked = tmp;
				tb.Visible = !cb.Visible;
			}
		}

		/// <summary>
		/// returns "YES/NO" for boolean values and value.ToString() for the rest
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string GetNiceValue(object value)
		{
			bool tmp = false;

			//is it a boolean value?
			if (bool.TryParse(value.ToString(), out tmp))
			{
				return tmp ? "YES" : "NO";
			}
			else
				if (value.ToString() != "")
					return value.ToString();
				else
					return "<i class='gray'>not set</i>";
		}

		protected void btnTestEmail_Click(object sender, EventArgs e)
		{
			divTestEmail.Visible = true;
			divTestEmail.InnerHtml = "Sending test email...<br>";

			try
			{
				//mailsender.SendSynchronously();

				AsyncSendMail mailer = new AsyncSendMail(
					Settings.MailServer,
					!string.IsNullOrEmpty(Settings.MailServerLogin),
					Settings.MailServerLogin,
					Settings.MailServerPassword,
					Settings.MailServerPort,
					Settings.MailUseSSL,
					new string[] { "test@jitbit.com" } ,
					Settings.MailFromAddress,
					null,
					"test",
					"test",
					null,
					false);
				mailer.SendSynchronously();

				divTestEmail.InnerHtml += "Email sent successfully!";
				divTestEmail.Style["color"] = "Green";
			}
			catch (Exception ex)
			{
				divTestEmail.InnerHtml += "<b>ERROR sending email</b>:<br><br>";
				divTestEmail.InnerHtml += ex.ToString();
				divTestEmail.Style["color"] = "Red";
			}
		}
	}
}