using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Configuration;
using Dapper;

namespace Jitbit.Utils
{
	public class InstanceNotFoundException : Exception
	{
		public InstanceNotFoundException(string message)
			: base(message)
		{
			ExceptionHandler.RenderErrorPage(message,true);
		}
	}


	/// <summary>
	/// this class is required for hosted version ONLY
	/// not needed when we run a single instace, so basically - do not mind it
	/// </summary>
	public static class Instance
	{
		private static string _assemblyName = null;
		private static string AssemblyName
		{
			get
			{
				if (_assemblyName == null)
				{
					_assemblyName = Assembly.GetExecutingAssembly().GetName().Name.ToLower();
				}
				return _assemblyName;
			}
		}

		//contants
		private static readonly string ConnStr = ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
		private static readonly Dictionary<string, int> ProductIDs = new Dictionary<string, int>
		{
			{"helpdesk", 34},
			{"jitbitcrm", 40},
			{"knowledgebase", 61},
			{"livechat", 62} //todo!
		};
		private static readonly Dictionary<string, string> ProductOrderURLs = new Dictionary<string, string>
		{
			{"helpdesk", "http://www.jitbit.com/hosted-helpdesk/purchase/?CustomerID={0}"},
			{"jitbitcrm", "http://www.jitbit.com/hosted-crm/purchase/?CustomerID={0}"},
			{"knowledgebase", "http://www.jitbit.com/hosted-knowledge-base/purchase/?CustomerID={0}"},
			{"livechat", "http://www.jitbit.com/livechat/purchase/?CustomerID={0}"}
		};
		private static readonly Dictionary<string, string> ProductPageURLs = new Dictionary<string, string>
		{
			{"helpdesk", "http://www.jitbit.com/hosted-helpdesk/"},
			{"jitbitcrm", "http://www.jitbit.com/hosted-crm/"},
			{"knowledgebase", "http://www.jitbit.com/hosted-knowledge-base/"},
			{"livechat", "http://www.jitbit.com/livechat/"}
		};
		private static readonly Dictionary<string, string> InstanceNameURLs = new Dictionary<string, string>
		{
			{"helpdesk", "https://{0}.jitbit.com/helpdesk/"},
			{"jitbitcrm", "https://{0}.jitbit.com/crm/"},
			{"knowledgebase", "https://{0}.jitbit.com/kb/"},
			{"livechat", "https://{0}.jitbit.com/chat/"}
		};
		private static readonly Dictionary<string, string> DomainURLs = new Dictionary<string, string>
		{
			{"helpdesk", "http://{0}/helpdesk/"},
			{"jitbitcrm", "http://{0}/crm/"},
			{"knowledgebase", "http://{0}/kb/"},
			{"livechat", "http://{0}/chat/"}		
		};
		private static readonly Dictionary<string, Regex> InstanceUrlRegexs = new Dictionary<string, Regex>
		{
			{"helpdesk", new Regex(@"https*://([a-z0-9_\-]+)\.jitbit\.com/helpdesk", RegexOptions.Compiled)},
			{"jitbitcrm", new Regex(@"https*://([a-z0-9_\-]+)\.jitbit\.com/crm", RegexOptions.Compiled)},
			{"knowledgebase", new Regex(@"https*://([a-z0-9_\-]+)\.jitbit\.com/kb", RegexOptions.Compiled)},
			{"livechat", new Regex(@"https*://([a-z0-9_\-]+)\.jitbit\.com/chat", RegexOptions.Compiled)}
		};
		private static Regex _urlDomainRegex = new Regex(@"https*://([a-z0-9\.:-]+)", RegexOptions.Compiled);

		public static bool IsMultiInstanceApp
		{
			get { return ConfigurationManager.AppSettings["MultiInstance"] != null; }
		}

		/// <summary>
		/// returns the "current instance" ID. The defaulat value is "0"
		/// "-1" is returned when there are no instances found or wrong regex.
		/// </summary>
		public static int CurrentInstanceID
		{
			get
			{
				//for unit-tests
				if (HttpContext.Current == null) throw new Exception("Do not call this member when there's no http-context, idiot!");

				if (HttpContext.Current.Session != null && HttpContext.Current.Session["InstanceID"] != null)
				{
					return (int)HttpContext.Current.Session["InstanceID"];
				}
				else
				{
					int instanceID = 0;

					//no setting found in webconfig - it's a single-instance setup
					if (!IsMultiInstanceApp)
					{
						instanceID = 0;
					}
					else
					{
						using (SqlConnection cn = DBUtils.GetNewOpenConnection())
						{
							// 1) extracting instance-name or domain-name from URL
							// 2) getting instanceID from database by it's name or domain
							string url = HttpContext.Current.Request.Url.ToString().ToLower();

							Match mName = InstanceUrlRegexs[AssemblyName].Match(url);
							Match mDomain = _urlDomainRegex.Match(url);

							if (!mName.Success && !mDomain.Success) //no name or domain found
							{
								throw new InstanceNotFoundException("Instance not found: wrong URL format.");
							}
							else
							{
								IEnumerable<dynamic> instances;
								if (mName.Success)
								{
									string instanceName = mName.Groups[1].Value;

									instances = cn.Query("SELECT InstanceID, ValidTill FROM Instances WHERE Name=@Name",
										new { Name = instanceName.ToLower() });
								}
								else if (mDomain.Success)
								{
									string instanceDomain = mDomain.Groups[1].Value;

									instances = cn.Query("SELECT InstanceID, ValidTill FROM Instances WHERE CustomDomain=@CustomDomain",
										new { CustomDomain = instanceDomain.ToLower() });
								}
								else //wrong URL format
								{
									throw new InstanceNotFoundException("Instance not found: no application found at this URL.");
								}

								//no instance with this name found in DB
								if (!instances.Any())
								{
									throw new InstanceNotFoundException("Instance not found: no application found at this URL.");
								}

								instanceID = instances.First().InstanceID;
								DateTime validTill = instances.First().ValidTill;

								if (IsInstanceExpired(instanceID, validTill)) //instance found but it's expired
								{
									string orderUrl = string.Format(ProductOrderURLs[AssemblyName], GetInstanceCustomerId(instanceID));
									throw new InstanceNotFoundException(string.Format("This app has expired, please <a href='{0}'>renew</a>", orderUrl));
								}
							}
						} //end "using" sql connection
					}

					if (HttpContext.Current != null && HttpContext.Current.Session != null)
					{
						HttpContext.Current.Session["InstanceID"] = instanceID;
					}
					return instanceID;
				}
			}
		}

		public static int CurrentInstanceCustomerID
		{
			get
			{
				if (CurrentInstanceID == 0) return 0;

				if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["CustomerID"] != null)
				{
					return (int)HttpContext.Current.Session["CustomerID"];
				}
				else
				{
					int custId = GetInstanceCustomerId(CurrentInstanceID);
					HttpContext.Current.Session["CustomerID"] = custId;
					return custId;
				}
			}
		}

		public static int GetInstanceCustomerId(int instanceId)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return cn.Query<int>(
					"SELECT CustomerID FROM Instances WHERE InstanceID=@InstanceID",
					new { InstanceID = instanceId }).FirstOrDefault();
			}
		}

		public static bool IsTrial()
		{
			if (CurrentInstanceID == 0) return false; //on-premise version

			//cache?
			try { return (bool)HttpContext.Current.Session["IsTrialInstance"]; }
			catch { }


			int productId = ProductIDs[AssemblyName];

			//look into the orders DB
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				bool isTrial = cn.Query<bool>(
					@"SELECT IsTrial
					FROM jitbit_web.dbo.AllowedHostedServices
					WHERE ProductID = @ProductID AND CustomerID=@CustomerID",
					new { ProductID = productId, CustomerID = CurrentInstanceCustomerID }).FirstOrDefault();

				//cache
				try { HttpContext.Current.Session["IsTrialInstance"] = isTrial; }
				catch { }

				return isTrial;
			}
		}

		//returns the instance name for URL.
		public static string CurrentInstanceName
		{
			get
			{
				if (HttpContext.Current.Session != null && HttpContext.Current.Session["InstanceName"] != null)
				{
					return HttpContext.Current.Session["InstanceName"].ToString();
				}
				else
				{
					string instanceName = GetInstanceName(CurrentInstanceID);
					if (HttpContext.Current.Session != null)
						HttpContext.Current.Session["InstanceName"] = null;
					return instanceName;
				}
			}
			set
			{
				HttpContext.Current.Session["InstanceName"] = value;
				SetInstanceName(CurrentInstanceID, value);
			}
		}

		public static string GetInstanceName(int instanceId)
		{
			if (instanceId == 0) return "";

			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return cn.Query<string>("SELECT Name FROM Instances WHERE InstanceID=@InstanceID", new { InstanceID = instanceId }).FirstOrDefault();
			}
		}

		public static string GetInstanceCustomDomain(int instanceId)
		{
			if (instanceId == 0) return "";

			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return cn.Query<string>("SELECT CustomDomain FROM Instances WHERE InstanceID=@InstanceID", new { InstanceID = instanceId }).FirstOrDefault();
			}
		}

		public static bool CheckInstanceNameAvailability(string name)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return !cn.Query<int>("SELECT InstanceID FROM Instances WHERE Name=@Name", new { Name = name }).Any();
			}
		}

		public static bool CheckCustomDomainAvailability(string customDomain)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				return !cn.Query<int>("SELECT InstanceID FROM Instances WHERE CustomDomain=@CustomDomain", new { CustomDomain = customDomain }).Any();
			}
		}

		private static void SetInstanceName(int instanceId, string name)
		{
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				cn.Execute("UPDATE Instances SET Name=@Name WHERE InstanceID=@InstanceID", new { Name = name, InstanceID = instanceId });
			}
		}

		public static void SetInstanceCustomDomain(int instanceId, string customDomain)
		{
			customDomain = customDomain.Trim();
			using (var cn = DBUtils.GetNewOpenConnection())
			{
				cn.Execute("UPDATE Instances SET CustomDomain=@CustomDomain WHERE InstanceID=@InstanceID", new { CustomDomain = customDomain, InstanceID = instanceId });
			}
		}

		public static string GetInstanceUrlByName(string instanceName)
		{
			//ok, this is hardcode, I know. Used by Jitbit only, anyway:
			string url = string.Format(InstanceNameURLs[AssemblyName], instanceName);
			return url;
		}

		public static string GetInstanceUrlByCustomDomain(string instanceDomain)
		{
			//ok, this is hardcode, I know. Used by Jitbit only, anyway:
			string url = string.Format(DomainURLs[AssemblyName], instanceDomain);
			if (IsSslCertInstalledForDomain(instanceDomain)) url = url.Replace("http://", "https://");
			return url;
		}

		public static bool IsSslCertInstalledForDomain(string domain) //ssl domains should be listed in webconfig
		{
			string[] sslDomains = ((ConfigurationManager.AppSettings["SslHostedDomains"] as string) ?? "").Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
			if (sslDomains.Contains(domain)) return true;
			return false;
		}

		/// <summary>
		/// get instance name from address like "support@ACME.jitbit.com"
		/// used for hosted instance only
		/// </summary>
		public static int? GetInstanceIdByEmailAddress(IEnumerable<string> emails)
		{
			if (emails == null || !emails.Any()) return null;

			Regex addressRegex1 = new Regex(@"@([a-zA-Z0-9_\-]+)\.jitbit\.com", RegexOptions.IgnoreCase);

			using (SqlConnection cn = new SqlConnection(ConnStr))
			{
				cn.Open();
				foreach (var email in emails)
				{
					if (email == null) continue;
					Match mName = addressRegex1.Match(email);
					if (!mName.Success) continue;

					string instanceName = mName.Groups[1].Value;

					int? instanceId = cn.Query<int?>("SELECT InstanceID FROM Instances WHERE Name=@Name", new {Name = instanceName}).FirstOrDefault();
					if (instanceId.HasValue) return instanceId;
				}
			}
			return null;
		}

		/// <summary>
		/// get instance name from "Received" header like "by mail8-tx2.bigfish.com (Postfix) with ESMTP id CC36D220440	for <support@prodigy.jitbit.com>; Sun, 25 Mar 2012 12:14:44 +0000 ("
		/// used for hosted instance only
		/// </summary>
		public static int? GetInstanceIdByEmailHeaders(Limilabs.Mail.Headers.HeaderCollection headers)
		{
			Regex addressRegex1 = new Regex(@"@([a-zA-Z0-9_\-]+)\.jitbit\.com", RegexOptions.IgnoreCase);
			Match mName = null;

			string receivedHeaders = headers["received"] ?? headers["Received"];
			if (receivedHeaders != null)
				mName = addressRegex1.Match(receivedHeaders); //seeking "***@***.jitbit.com" in the headers string

			if (receivedHeaders == null || !mName.Success) //not found in "received" lets look at "ogirignal to" header
			{
				string xOriginalTo = headers["X-Original-To"] ?? headers["x-original-to"];
				if (xOriginalTo == null) return null;
				mName = addressRegex1.Match(xOriginalTo); //seeking "***@***.jitbit.com" in the headers string AGAIN
				if (!mName.Success) return null;
			}
			

			using (SqlConnection cn = DBUtils.GetNewOpenConnection())
			{
				string instanceName = mName.Groups[1].Value;

				int? instanceId = cn.Query<int?>("SELECT InstanceID FROM Instances WHERE Name=@Name", new {Name = instanceName}).FirstOrDefault();
				if (instanceId.HasValue) return instanceId;
			}
			return null;
		}

		private static void UnitTest()
		{
			string tst = Assembly.GetExecutingAssembly().FullName;
		}

		/// <summary>
		/// returns the list of instances. for default installation returns {0}-array
		/// </summary>
		public static List<int> GetInstanceIDs()
		{
			List<int> retval = new List<int>();
			retval.Add(0); //we always add "0"

			if (!IsMultiInstanceApp)
			{
				return retval;
			}

			SqlConnection cn = new SqlConnection(ConnStr);
			SqlCommand cmd = new SqlCommand();
			cmd.Connection = cn;
			cmd.CommandText = "SELECT InstanceID FROM Instances WHERE ValidTill>getdate()-11";
			cn.Open();
			SqlDataReader dr = cmd.ExecuteReader();

			while (dr.Read())
				if (dr.GetInt32(0) != 0)
					retval.Add(dr.GetInt32(0));

			dr.Close();
			cn.Close();
			return retval;
		}

		public static DateTime GetInstanceExpirationDate(int instanceId)
		{
			//single-instance app
			if (instanceId == 0) return DateTime.Now.AddYears(20);

			using (var cn = new SqlConnection(ConnStr))
			{
				cn.Open();
				var dates = cn.Query<DateTime>("SELECT ValidTill FROM Instances WHERE InstanceID=@InstanceID", new {InstanceID = instanceId});
				cn.Close();

				if (!dates.Any())
				{
					throw new InstanceNotFoundException("Instance not found");
				}
				else
				{
					return dates.First();
				}
			}
		}

		public static DateTime GetCurrentInstanceExpirationDate()
		{
			return GetInstanceExpirationDate(CurrentInstanceID);
		}

		//you can pass "validTill" here to save a db-query
		public static bool IsInstanceExpired(int instanceId, DateTime? validTill = null)
		{
            try
            {
                if (!validTill.HasValue) validTill = GetInstanceExpirationDate(instanceId);
            }
            catch(InstanceNotFoundException ex)
            {
                return true; //if instance is not found, IsExpired should be true
            }

			//add an extra free week even if expired
			return (DateTime.Now.AddDays(-11) > validTill.Value);
		}

		public static bool IsCurrentInstanceExpired()
		{
			bool res = IsInstanceExpired(CurrentInstanceID);

			//if expired - stop execution
			if (res)
			{
				if (HttpContext.Current != null)
				{
					//stop execution and show purchase link
					int customerId = CurrentInstanceCustomerID;
					string orderUrl = string.Format(ProductOrderURLs[AssemblyName], customerId);
					if (IsTrial())
					{
						string retweetUrl = ProductPageURLs[AssemblyName] + "hostedretweet/";
						ExceptionHandler.RenderErrorPage(string.Format("Your trial period has expired. Please <a href=\"{0}\" style='color:red'>renew</a> or <a href='{1}'>extend your free trial</a>", orderUrl, retweetUrl), true);
					}
					else
					{
						ExceptionHandler.RenderErrorPage(string.Format("Your paid period has expired. Please <a href=\"{0}\">renew</a>.", orderUrl), true);
					}
				}
			}

			return res;
		}
	}
}
