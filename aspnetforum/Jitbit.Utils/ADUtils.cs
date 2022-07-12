using System;
using System.DirectoryServices;
using System.Web;
using System.Web.Configuration;

namespace Jitbit.Utils
{
	//pulling user info from Active Directory
	public static class ADUtils
	{
		private static DirectoryEntry GetDirectoryEntry(string domainName)
		{
			DirectoryEntry de = new DirectoryEntry();
			de.Path = "LDAP://" + domainName;

			return de;
		}

		private static string _domainName; //we'll save the domain name here, 

		//we need this overload for this method to work in CRM and Forum, where the department field is not needed
		public static bool GetUserPropertiesFromAD(string userAccount, out string email, out string firstName, out string lastName, out string phone, out string office, out string adLanguage, out string company, out byte[] jpegPhoto)
		{
			string department; //we will never use this variable
			return GetUserPropertiesFromAD(userAccount, out email, out firstName, out lastName, out phone, out office, out adLanguage,
			                        out company, out jpegPhoto, out department);
		}

		public static bool GetUserPropertiesFromAD(string userAccount, out string email, out string firstName, out string lastName, out string phone, out string office, out string adLanguage, out string company, out byte[] jpegPhoto, out string department)
		{
			email = ""; firstName = ""; lastName = ""; phone = ""; office = ""; adLanguage = ""; company = ""; department = ""; jpegPhoto = null;

			int pos = userAccount.IndexOf("\\");
			if (pos < 0) return false; //not a domain user, has no domain-name in it

			string userNameWithoutDomain = userAccount.Substring(pos + 1);
			_domainName = userAccount.Substring(0, pos);

			string ldapSearchString = "(&(objectCategory=person)(objectClass=user)(SAMAccountName=" + userNameWithoutDomain + "))";
			
			string dumbusername;
			
			return GetUserPropertiesFromAD(ldapSearchString, out dumbusername, out email, out firstName, out lastName, out phone, out office, out adLanguage, out company, out jpegPhoto, out department);
		}

		/// <summary>
		/// seached a user in AD by email
		/// if no domain-name specified - the module will use a domain name from previous call to ADUtils
		/// </summary>
		public static bool GetUserPropertiesFromADByEmail(
			string email,
			out string username,
			out string firstName,
			out string lastName,
			out string phone,
			out string office,
			out string adLanguage,
			out string company,
			out byte[] jpegPhoto,
			out string department,
			string domainName = null)
		{
			string ldapSearchString = "(&(objectCategory=person)(objectClass=user)(mail=" + email + "))";
			string dumbemail;

			if (domainName != null)
				_domainName = domainName;

			return GetUserPropertiesFromAD(ldapSearchString, out username, out dumbemail, out firstName, out lastName, out phone, out office, out adLanguage, out company, out jpegPhoto, out department);
		}

		private static bool GetUserPropertiesFromAD(string ldapSearchString, out string username, out string email, out string firstName, out string lastName, out string phone, out string office, out string adLanguage, out string company, out byte[] jpegPhoto, out string department)
		{
			// language is currently ignored
			username = ""; email = ""; firstName = ""; lastName = ""; phone = ""; office = ""; adLanguage = ""; company = ""; jpegPhoto = null; department = "";

			if (!IsWindowsAuthentication()) return false;

			SearchResult result;
			try
			{
				DirectorySearcher search = GetADSearcherObject(ldapSearchString);
				result = search.FindOne();
			}
			catch (System.Runtime.InteropServices.COMException ex)
			{
				//maybe thats a "secondary token" issue, lets try again like this
				//impersonated by the app identity
				using (System.Web.Hosting.HostingEnvironment.Impersonate())
				{
					try
					{
						DirectorySearcher search = GetADSearcherObject(ldapSearchString);
						result = search.FindOne();
					}
					catch (Exception ex2)
					{
						ExceptionHandler.LogException(ex2);
						return false;
					}
				}
			}

			if (result != null)
			{
				Exception adEx = null;

				try { username = result.Properties["sAMAccountName"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { email = result.Properties["mail"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { firstName = result.Properties["givenName"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { lastName = result.Properties["sn"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { phone = result.Properties["telephoneNumber"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { office = result.Properties["physicalDeliveryOfficeName"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { adLanguage = result.Properties["preferredLanguage"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { company = result.Properties["company"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				try { jpegPhoto = result.Properties["thumbnailPhoto"][0] as byte[]; }
				catch (Exception ex) { adEx = ex; }

				try { department = result.Properties["department"][0].ToString(); }
				catch (Exception ex) { adEx = ex; }

				if (jpegPhoto == null) //thumbnailPhoto" field is empty. Let's try "jpegPhoto"
				{
					try { jpegPhoto = result.Properties["jpegPhoto"][0] as byte[]; }
					catch (Exception ex) { adEx = ex; }
				}

				if (adEx != null)
				{
					ExceptionHandler.LogException(adEx);
				}
				return true;
			}
			else
				return false;
		}

		private static DirectorySearcher GetADSearcherObject(string ldapFilterString)
		{
			DirectoryEntry entry;
			
			if (_domainName != null)
				entry = GetDirectoryEntry(_domainName); //sometimes it needs domain name, so in case we already have it - supply it
			else
				entry = new DirectoryEntry(); //in case we don't - ok, sorry, we don't know the current domain

			DirectorySearcher search = new DirectorySearcher(entry);
			search.ClientTimeout = new TimeSpan(0, 0, 2);
			search.Filter = ldapFilterString;
			search.PropertiesToLoad.Add("mail");
			search.PropertiesToLoad.Add("givenName");
			search.PropertiesToLoad.Add("sn");
			search.PropertiesToLoad.Add("telephoneNumber");
			search.PropertiesToLoad.Add("physicalDeliveryOfficeName");
			search.PropertiesToLoad.Add("preferredLanguage");
			search.PropertiesToLoad.Add("company");
			search.PropertiesToLoad.Add("department");
			search.PropertiesToLoad.Add("sAMAccountName");
			search.PropertiesToLoad.Add("thumbnailPhoto");
			search.PropertiesToLoad.Add("jpegPhoto");
			return search;
		}

		/// <summary>
		/// returns if the web-application uses Windows-authentication
		/// </summary>
		public static bool IsWindowsAuthentication()
		{
			if (_isWindowsAuth.HasValue) return _isWindowsAuth.Value;

			//if Context is avail.
			try
			{
				var configuration = WebConfigurationManager.OpenWebConfiguration(VirtualPathUtility.ToAbsolute("~"));
				var authSection = (AuthenticationSection)configuration.GetSection("system.web/authentication");
				_isWindowsAuth = (authSection.Mode == AuthenticationMode.Windows);
			}
			catch
			{
				_isWindowsAuth = false;
			}

			return _isWindowsAuth.Value;
		}
		public static bool? _isWindowsAuth = null; //cache result in this var to prevent re-querying. ITS PUBLIC FOR UNITESTS ONLY!!
	}
}
