using System;
using System.Collections.Generic;
using System.Web;
using System.Data.Common;
using aspnetforum.Utils;

namespace aspnetforum
{
	public class ajaxuserautocomplete : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			string[] g_invalidSQL = { ";", "--", "/*", "*\\", "xp_" };

			HttpRequest request = context.Request;
			HttpResponse response = context.Response;

			response.Expires = -1;

			string q = request.QueryString["q"].Replace("'", "");
			if (string.IsNullOrEmpty(q)) return;

			q = q.Trim();
			if (q.Length < 1) return;

			//sql injections check
			foreach (string invalid in g_invalidSQL)
			{
				if (request.QueryString["q"].Contains(invalid))
				{
					response.TrySkipIisCustomErrors = true;
					response.StatusCode = 400;
					response.Write("Bad request");
					response.End();
					return;
				}
			}
			using (DbConnection cn = DB.CreateOpenConnection())
			{
				DbDataReader dr = cn.ExecuteReader(
					@"SELECT TOP 20 UserName, UserID FROM ForumUsers
					WHERE UserName LIKE '" + q + "%' AND Disabled=? ORDER BY UserName", false);

				while (dr.Read())
				{
					response.Write(dr["UserName"] + "|" + dr["UserID"] + "\r\n");
				}
				dr.Close();
			}
		}

		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

	}
}
