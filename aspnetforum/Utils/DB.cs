using System;
using System.Data;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Data.OleDb;
using System.Collections.Generic;

namespace aspnetforum.Utils
{
	//Cross-Datasource engine.
	public static class DB
	{
		private static readonly string _connStr = ConfigurationManager.ConnectionStrings["AspNetForumConnectionString"].ConnectionString;
		private static readonly string _providerName = ConfigurationManager.ConnectionStrings["AspNetForumConnectionString"].ProviderName;

		public static void FillCommandParamaters(DbCommand cmd, params object[] valuesList)
		{
			//clear params
			cmd.Parameters.Clear();

			AddCommandParamaters(cmd, valuesList);
		}

		private static bool IsMySqlDriver()
		{
			//wither providername is "system.data.mysql" or connection string is as follows:
			//"DRIVER={MySQL ODBC 3.51 Driver};SERVER=localhost;DATABASE=uktherapy;PORT=3306;USER=uktherapy;PASSWORD=xxxxxx;OPTION=3;"

			return (_providerName.ToLower().IndexOf("mysql") > -1) || (_connStr.ToLower().IndexOf("mysql")>-1);
		}

		private static bool IsOdbcDriver()
		{
			//wither providername is "system.data.mysql" or connection string is as follows:
			//"DRIVER={MySQL ODBC 3.51 Driver};SERVER=localhost;DATABASE=uktherapy;PORT=3306;USER=uktherapy;PASSWORD=xxxxxx;OPTION=3;"

			return (_providerName.ToLower().IndexOf("odbc") > -1);
		}

		//adds SEVELRAL parameters to a dbcommand
		//includes OleDb fix for MS Access:
		//(when "DbType.DateTime" causes error, and "DbType.Date" truncates time - so we have to use "OleDbType.Date" explicitly)
		private static void AddCommandParamaters(DbCommand cmd, params object[] valuesList)
		{
			if (valuesList.Length == 0) return;

			DbProviderFactory providerFactory = DbProviderFactories.GetFactory(_providerName);

			//if we are using SqlClient OR MySqlClient -
			//then replace the "?" signs in the command text with "@Param1", "@Param2" etc.
			//OR with "?Param1", "?Param2" in case of MySQL)
			//because ".NET native SQL ADO.NET driver" and "MySQL Connector.NET"
			//do not support unnamed parameters and "?" signs
			bool sqlDriver = (cmd is SqlCommand);
			bool mysqlConnectorDriver = IsMySqlDriver() && !IsOdbcDriver();
			if (sqlDriver || mysqlConnectorDriver)
			{
				int i = 1; //used for param naming
				int j = 0; //used for itarating thru values array

				string newCmd = cmd.CommandText;

				//to simplify the search
				newCmd = newCmd.Replace("?Param", "@Param");

				//if this parameter name is already used...
				while (newCmd.IndexOf("@Param" + i) > -1)
					i++;

				Regex rx = new Regex(@"\?");
				MatchCollection matches = rx.Matches(newCmd);
				foreach (Match mtch in matches)
				{
					string paramName = "@Param" + i;

					//replace the "?" signs in the command text with "@Param1", "@Param2" etc
					newCmd = rx.Replace(newCmd, paramName, 1, mtch.Index);

					if (mysqlConnectorDriver) paramName = "?Param" + i;

					//now create and add parameter
					DbParameter dbParam = providerFactory.CreateParameter();
					dbParam.ParameterName = paramName;
					dbParam.Value = valuesList[j];
					cmd.Parameters.Add(dbParam);

					i++;
					j++;
				}
				if(mysqlConnectorDriver)
				{
					newCmd = newCmd.Replace("@Param", "?Param");
				}
				cmd.CommandText = newCmd;
			}
			else //it is not mssql or mysql - we're simply adding params, without any names
			{
				foreach (object value in valuesList)
				{
					DbParameter dbParam = providerFactory.CreateParameter();
					//if parameter is datetime - MS Access oledb datetime fix
					if (value is DateTime && cmd is OleDbCommand)
					{
						((OleDbParameter)dbParam).OleDbType = OleDbType.Date;
					}
					dbParam.Value = value;
					cmd.Parameters.Add(dbParam);
				}
			}
		}

		/// <summary>
		/// returns a NEW instance of data connection
		/// </summary>
		public static DbConnection CreateConnection()
		{
			DbConnection cn = CreateDBProviderFactory().CreateConnection();
			cn.ConnectionString = _connStr;
			return cn;
		}

		public static DbConnection CreateOpenConnection()
		{
			var cn = CreateConnection();
			cn.Open();
			return cn;
		}

		/// <summary>
		/// returns a new instance of data c0mmand, with a connection object !!already assigned!!
		/// </summary>
		public static DbCommand CreateCommand(string commandText = null, DbConnection cn = null, params object[] valuesList)
		{
			if(cn ==null) cn = CreateConnection();
			DbProviderFactory providerFactory = CreateDBProviderFactory();
			DbCommand cmd = providerFactory.CreateCommand();
			cmd.Connection = cn;

			if (commandText != null)
			{
				cmd.CommandText = commandText;
				PrepareCommandTextForProvider(cmd);
			}

			FillCommandParamaters(cmd, valuesList);

			return cmd;
		}

		/// <summary>
		/// returns a NEW instance of DB Provider Factory
		/// </summary>
		private static DbProviderFactory CreateDBProviderFactory()
		{
			DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(_providerName);
			return dbProviderFactory;
		}

		/// <summary>
		/// prepares command text - removes disallowed symbols and words
		/// always call this method IF command-text contains reserved words, surrounded with "[]"
		/// because MySQL uses "``" instead of "[]"
		/// 
		/// ALSO replaces "TOP XX" instruction with "LIMIT XX" (for MySQL)
		/// </summary>
		/// <param name="cmd"></param>
		private static void PrepareCommandTextForProvider(DbCommand cmd)
		{
			//if mysql-command - then replace "[]" with "`"
			//cause MySQL does not support "[" symbols
			if (IsMySqlDriver())
			{
				string newCmd = cmd.CommandText;

				//special chars [] ``
				newCmd = newCmd.Replace("[", "`");
				newCmd = newCmd.Replace("]", "`");

				//replace "TOP X" to "LIMIT N"
				if (newCmd.ToUpper().StartsWith("SELECT TOP "))
				{
					//find first space after 11 symbol
					int spacePos = newCmd.IndexOf(" ", 11);
					string limitNumber = " LIMIT " + newCmd.Substring(11, spacePos - 11);
					newCmd = "SELECT " + newCmd.Substring(spacePos) + limitNumber;
				}
				else if (newCmd.ToUpper().StartsWith("SELECT DISTINCT TOP "))
				{
					//find first space after 20 symbol
					int spacePos = newCmd.IndexOf(" ", 20);
					string limitNumber = " LIMIT " + newCmd.Substring(20, spacePos - 20);
					newCmd = "SELECT DISTINCT " + newCmd.Substring(spacePos) + limitNumber;
				}

				cmd.CommandText = newCmd;
			}
		}

		//checks if a field exists in a table - just a simple try-catch, required for DB-upgrade engine
		private static bool FieldExists(string tableName, string fieldName)
		{
			bool retval;
			DbCommand cmd = CreateCommand();
			cmd.CommandText = "SELECT " + fieldName + " FROM " + tableName;
			cmd.Connection.Open();
			try
			{
				cmd.ExecuteScalar();
				retval = true;
			}
			catch
			{
				retval = false;
			}
			cmd.Connection.Close();
			return retval;
		}

		public static DbDataReader ExecuteReader(this DbConnection cn, string commandText, params object[] parameters)
		{
			DbCommand cmd = CreateCommand(commandText, cn, parameters);
			return cmd.ExecuteReader();
		}

		/// <summary>
		/// returns a list (rows) of dictionaries(column names/values)
		/// </summary>
		public static List<IDictionary<string, object>> ExecuteData(this DbConnection cn, string commandText, params object[] parameters)
		{
			DbCommand cmd = CreateCommand(commandText, cn, parameters);
			List<IDictionary<string, object>> list = new List<IDictionary<string, object>>();
			var dr = cmd.ExecuteReader();
			while (dr.Read())
			{
				var dict = new Dictionary<string, object>();
				for (int i = 0; i < dr.FieldCount; i++)
				{
					dict[dr.GetName(i)] = dr[i];
				}
				list.Add(dict);
			}
			dr.Close();
			return list;
		}

		public static object ExecuteScalar(this DbConnection cn, string commandText, params object[] parameters)
		{
			DbCommand cmd = CreateCommand(commandText, cn, parameters);
			return cmd.ExecuteScalar();
		}

		public static int ExecuteNonQuery(this DbConnection cn, string commandText, params object[] parameters)
		{
			DbCommand cmd = CreateCommand(commandText, cn, parameters);
			return cmd.ExecuteNonQuery();
		}

		//checks if a table exists in the database table - just a simple try-catch
		private static bool TableExists(string tableName)
		{
			bool retval;
			DbCommand cmd = CreateCommand();
			cmd.CommandText = "SELECT * FROM " + tableName;
			cmd.Connection.Open();
			try
			{
				cmd.ExecuteScalar();
				retval = true;
			}
			catch
			{
				retval = false;
			}
			cmd.Connection.Close();
			return retval;
		}

		/// <summary>
		/// returns a list of strongly=typed object (micro-orm)
		/// </summary>
		public static IEnumerable<T> ExecuteOrm<T>(this DbConnection cn, string commandText, params object[] parameters)
		{
			List<T> retval = new List<T>();
			DbCommand cmd = CreateCommand(commandText, cn, parameters);
			IEnumerable<PropInfo<T>> props = null;
			Type t = typeof (T);
			bool isPrimitiveType = t.IsPrimitive || t.IsValueType || (t ==typeof(string));
			if (!isPrimitiveType) //generate properties array
			{
				props = GetSettableProps<T>();
			}

			var dr = cmd.ExecuteReader();
			if (!dr.HasRows)
			{
				dr.Close();
				return retval;
			}

			if (isPrimitiveType) //if it's a primitive type or valueType or string = just assign the 1st col, like in "SELECT UserID FROM Users"
			{
				while (dr.Read())
					retval.Add((T) Convert.ChangeType(dr[0], typeof (T)));
			}
			else
			{
				//get field names
				var names = new List<string>();
				for (int i = 0; i < dr.FieldCount; i++)
				{
					names.Add(dr.GetName(i));
				}

				var setters = (
				              	from n in names
				              	let prop = props.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.Ordinal)) // property case sensitive first
				              	           ?? props.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)) // property case insensitive second
				              	select new {Name = n, Property = prop}
				              ).ToList();

				while (dr.Read())
				{
					T res = Activator.CreateInstance<T>();
					foreach (var str in setters)
					{
						str.Property.Setter(res, Convert.ChangeType(dr[str.Name], str.Property.Type));
					}
					retval.Add(res);
				}
			}

			dr.Close();

			return retval;
		}

		class PropInfo<T>
		{
			public string Name { get; set; }
			public Action<T, object> Setter { get; set; }
			public Type Type { get; set; }
		}

		static Action<T, object> GetPropertySetter<T>(PropertyInfo property)
		{
			var target = Expression.Parameter(typeof(T));
			var value = Expression.Parameter(typeof(object));
			var assignment = Expression.Assign(Expression.MakeMemberAccess(target, property), Expression.Convert(value, property.PropertyType));
			var propertyGetterExpression = Expression.Lambda<Action<T, object>>(assignment, target, value);
			return propertyGetterExpression.Compile();
		}

		static List<PropInfo<T>> GetSettableProps<T>()
		{
			Type t = typeof(T);
			if (!_propCache.ContainsKey(t)) //look in the cache first
			{
				var props = t
					.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.Select(p => new PropInfo<T>
					{
						Name = p.Name,
						//Setter = p.DeclaringType == t ? p.GetSetMethod(true) : p.DeclaringType.GetProperty(p.Name).GetSetMethod(true),
						Setter = GetPropertySetter<T>(p),
						Type = p.PropertyType
					})
					.Where(info => info.Setter != null)
					.ToList();

				_propCache.Add(t, props);
			}

			return (List<PropInfo<T>>) _propCache[t]; //get from cache
		}

		private static Dictionary<Type, object> _propCache = new Dictionary<Type, object>();

		/*
		/// <summary>
		/// TODO we'll add this in our next versions. SNITZ converter
		/// </summary>
		public static void ConvertFromSnitz(string snitzDbConnectionString, string provideName)
		{
			DbProviderFactory providerFactory = DbProviderFactories.GetFactory(provideName);
			DbConnection cnSnits = providerFactory.CreateConnection();
			cnSnits.ConnectionString = snitzDbConnectionString;
			DbCommand cmdSnitz = providerFactory.CreateCommand();
			cmdSnitz.Connection = cnSnits;

			DbCommand cmdJitbit = CreateCommand();

			cnSnits.Open();
			cmdJitbit.Connection.Open();

			//transferring categories
			cmdSnitz.CommandText = "SELECT CAT_NAME FROM FORUM_CATEGORY";
			DbDataReader dr = cmdSnitz.ExecuteReader();
			while (dr.Read())
			{
				Forum.AddForumGroup(dr[0].ToString());
			}
			dr.Close();

			//transferring forums
			cmdSnitz.CommandText =
				@"SELECT F_SUBJECT, F_DESCRIPTION, CAT_NAME
				FROM FORUM_FORUM
				INNER JOIN FORUM_CATEGORY ON FORUM_CATEGORY.CAT_ID = FORUM_FORUM.CAT_ID";
			dr = cmdSnitz.ExecuteReader();
			while (dr.Read())
			{
				int? groupId = Forum.GetForumGroupIdByName(dr["CAT_NAME"].ToString());
				if(groupId.HasValue)
					Forum.AddForum(dr["F_SUBJECT"].ToString(), dr["F_DESCRIPTION"].ToString(), groupId.Value);
			}
			dr.Close();

			cmdJitbit.Connection.Close();
			cnSnits.Close();
		}*/

		private const string REGEX_GO_PATTERN = @"\b(g|G)(o|O)\b";

		//executes of the DDL commands depending on the database type
		//(DDL-commands are commands like "create table" etc)
		private static void ExecuteDDLCommand(string scriptMsAccess, string scriptMSSQL, string scriptMYSQL)
		{
			string[] commands = new string[] { "" };

			DbCommand cmd = CreateCommand();
			cmd.CommandTimeout = 120;

			if (IsMySqlDriver()) //its MySQL
			{
				commands[0] = scriptMYSQL;
			}
			else if (_connStr.IndexOf(".Jet") > -1) //its ms access
			{
				commands[0] = scriptMsAccess;
			}
			else if (_providerName.ToLower().IndexOf("sqlclient") > -1)
			{
				Regex regex = new Regex(REGEX_GO_PATTERN, RegexOptions.ExplicitCapture);
				commands = regex.Split(scriptMSSQL); //split with "GO" separators
			}

			cmd.Connection.Open();
			try
			{
				foreach (string command in commands)
				{
					if (command.Trim().Length > 0)
					{
						cmd.CommandText = command;
						cmd.ExecuteNonQuery();
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error while executing: " + cmd.CommandText + "\n\nProvider name:" + _providerName + "\n\nException: " + ex.Message, ex);
			}
			finally
			{
				cmd.Connection.Close();
			}
		}

		public static void UpdateDBToLatestVersion()
		{
			string sqlAccess, sqlMSSQL, sqlMYSQL;

			/* starting from version 3.3.0 SubForums feature is added */
			if (!TableExists("ForumSubforums"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "CREATE TABLE ForumSubforums(ParentForumID int NOT NULL, SubForumID int NOT NULL);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumSubforums ADD PRIMARY KEY (ParentForumID,SubForumID);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from version 4.0 a new nullable field is added
			to ForumUsers table - "AvatarFileName"*/
			if (!FieldExists("ForumUsers", "AvatarFileName"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumUsers ADD AvatarFileName varchar(50) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from version 4.0 a new nullable field is added to ForumUsers table - "AvatarFileName"*/
			if (!FieldExists("ForumTopics", "IsSticky"))
			{
				sqlMYSQL = "ALTER TABLE ForumTopics ADD	IsSticky int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumTopics ADD IsSticky int NULL;";
				sqlMSSQL = "ALTER TABLE ForumTopics ADD	IsSticky int NOT NULL DEFAULT 0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from version 4.2.0 file uploading feature added so a new table - ForumUploadedFiles - is being added */
			if (!TableExists("ForumUploadedFiles"))
			{
				sqlMYSQL = @"CREATE TABLE ForumUploadedFiles (
							FileID int auto_increment NOT NULL PRIMARY KEY,
							FileName varchar (255) NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						);";
				sqlAccess = @"CREATE TABLE ForumUploadedFiles (
							FileID autoincrement NOT NULL PRIMARY KEY,
							FileName varchar (255) NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						);";
				sqlMSSQL = @"CREATE TABLE ForumUploadedFiles (
							FileID int IDENTITY (1, 1) NOT NULL PRIMARY KEY,
							FileName nvarchar (255) NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			}

			/* starting from version 4.3.5 you can CLOSE topics, so "IsClosed" column added */
			if (!FieldExists("ForumTopics", "IsClosed"))
			{
				sqlMYSQL = "ALTER TABLE ForumTopics ADD IsClosed bool NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumTopics ADD IsClosed bit;";
				sqlMSSQL = "ALTER TABLE ForumTopics ADD IsClosed bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from version 4.3.8 "ViewsCount" column added - to count how many usere have read the topic*/
			if (!FieldExists("ForumTopics", "ViewsCount"))
			{
				sqlMYSQL = "ALTER TABLE ForumTopics ADD	ViewsCount int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumTopics ADD ViewsCount int;";
				sqlMSSQL = "ALTER TABLE ForumTopics ADD	ViewsCount int NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* IMPORTANT!!!!!!!!
			starting from version 4.4.4 "Password" column has been changed to varchar(50) from varchar(20) to hold large MD5 hashes */
			sqlMYSQL = "ALTER TABLE ForumUsers MODIFY COLUMN Password varchar(50);";
			sqlAccess = "ALTER TABLE ForumUsers ALTER COLUMN [Password] varchar(50);";
			sqlMSSQL = "ALTER TABLE ForumUsers ALTER COLUMN [Password] nvarchar(50)";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			/*adding Signature column to ForumUsers*/
			if (!FieldExists("ForumUsers", "Signature"))
			{
				sqlMYSQL = "ALTER TABLE ForumUsers ADD Signature varchar(1000) NULL;";
				sqlAccess = "ALTER TABLE ForumUsers ADD Signature Memo NULL;";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD Signature nvarchar(1000) NULL";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 4.6.11 forum has a new columm - "MembersOnly" */
			if (!FieldExists("Forums", "MembersOnly"))
			{
				sqlMYSQL = "ALTER TABLE Forums ADD MembersOnly bit NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE Forums ADD MembersOnly bit;";
				sqlMSSQL = "ALTER TABLE Forums ADD MembersOnly bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 4.7.0 forum has a new columm - "OrderByNumber" */
			if (!FieldExists("Forums", "OrderByNumber"))
			{
				sqlMYSQL = "ALTER TABLE Forums ADD OrderByNumber int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE Forums ADD OrderByNumber int;";
				sqlMSSQL = "ALTER TABLE Forums ADD OrderByNumber int NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 4.7.5 POLLS are added */
			if (!TableExists("ForumPolls"))
			{
				sqlMYSQL = @"CREATE TABLE ForumPolls (
							PollID int auto_increment NOT NULL PRIMARY KEY,
							TopicID int NOT NULL,
							Question varchar(255) NOT NULL
						);";
				sqlAccess = @"CREATE TABLE ForumPolls (
							PollID autoincrement NOT NULL PRIMARY KEY,
							TopicID int NOT NULL,
							Question varchar(255) NOT NULL
						);";
				sqlMSSQL = @"CREATE TABLE ForumPolls (
							PollID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
							TopicID int NOT NULL,
							Question nvarchar(255) NOT NULL
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);


				sqlMYSQL = @"CREATE TABLE ForumPollOptions (
							OptionID int auto_increment NOT NULL PRIMARY KEY,
							PollID int NOT NULL,
							OptionText varchar(50) NOT NULL
						);";
				sqlAccess = @"CREATE TABLE ForumPollOptions (
							OptionID autoincrement NOT NULL PRIMARY KEY,
							PollID int NOT NULL,
							OptionText varchar(50) NOT NULL
						);";
				sqlMSSQL = @"CREATE TABLE ForumPollOptions (
							OptionID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
							PollID int NOT NULL,
							OptionText nvarchar(50) NOT NULL
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);


				sqlMYSQL = sqlAccess = sqlMSSQL = @"CREATE TABLE ForumPollAnswers (
							UserID int NOT NULL,
							OptionID int NOT NULL
						);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				/*adding complex keys*/
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumPollAnswers ADD PRIMARY KEY (UserID,OptionID);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 4.8.3 RepliesCount column is added to the ForumTopics table, to prevent joins and speed up things */
			if (!FieldExists("ForumTopics", "RepliesCount"))
			{
				sqlMYSQL = "ALTER TABLE ForumTopics ADD RepliesCount int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumTopics ADD RepliesCount int;";
				sqlMSSQL = "ALTER TABLE ForumTopics ADD RepliesCount int NOT NULL DEFAULT 0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* also the Body column in forummessages table is changed to text from varchar */
			sqlMYSQL = "ALTER TABLE ForumMessages MODIFY COLUMN Body text NOT NULL;";
			sqlAccess = "ALTER TABLE ForumMessages ALTER COLUMN Body Memo NOT NULL;";
			sqlMSSQL = "ALTER TABLE ForumMessages ALTER COLUMN Body ntext NOT NULL;";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			sqlMYSQL = "ALTER TABLE ForumPersonalMessages MODIFY COLUMN Body text NOT NULL;";
			sqlAccess = "ALTER TABLE ForumPersonalMessages ALTER COLUMN Body Memo NOT NULL;";
			sqlMSSQL = "ALTER TABLE ForumPersonalMessages ALTER COLUMN Body ntext NOT NULL;";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			/*starting from 5.1.0 ForumComplains table added */
			if (!TableExists("ForumComplaints"))
			{
				sqlMYSQL = sqlAccess = @"CREATE TABLE ForumComplaints (
							UserID int NOT NULL,
							MessageID int NOT NULL,
							ComplainText text NOT NULL
						);";
				sqlMSSQL = @"CREATE TABLE ForumComplaints (
							UserID int NOT NULL,
							MessageID int NOT NULL,
							ComplainText ntext NOT NULL
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 5.1.2 IPAddress column added */
			if (!FieldExists("ForumMessages", "IPAddress"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumMessages ADD IPAddress varchar(50);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 5.1.4 RestrictTopicCreation column added */
			if (!FieldExists("Forums", "RestrictTopicCreation"))
			{
				sqlMYSQL = "ALTER TABLE Forums ADD RestrictTopicCreation bool NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE Forums ADD RestrictTopicCreation bit;";
				sqlMSSQL = "ALTER TABLE Forums ADD RestrictTopicCreation bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* new table - ForumUploadedPersonalFiles  - for private message attachments */
			if (!TableExists("ForumUploadedPersonalFiles"))
			{
				sqlMYSQL = @"CREATE TABLE ForumUploadedPersonalFiles (
							FileID int auto_increment NOT NULL PRIMARY KEY,
							FileName varchar (255) NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						);";
				sqlAccess = @"CREATE TABLE ForumUploadedPersonalFiles (
							FileID autoincrement NOT NULL PRIMARY KEY,
							FileName varchar (255) NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						);";
				sqlMSSQL = @"CREATE TABLE ForumUploadedPersonalFiles (
							FileID int IDENTITY (1, 1) NOT NULL PRIMARY KEY,
							FileName nvarchar (255)  NOT NULL ,
							MessageID int NOT NULL ,
							UserID int NOT NULL 
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 5.4.5 LastLogonDate column added */
			if (!FieldExists("ForumUsers", "LastLogonDate"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumUsers ADD LastLogonDate datetime NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 6.0.3 IconFile column added to "Forums" */
			if (!FieldExists("Forums", "IconFile"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE Forums ADD IconFile varchar(50) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 6.2.0 theres a new table "ForumRating"
			and a new column in "ForumUsers" - "ReputationCache"            */
			if (!TableExists("ForumMessageRating"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = @"CREATE TABLE ForumMessageRating (
							MessageID int NOT NULL,
							VoterUserID int NOT NULL,
							Score int NOT NULL
						);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumMessageRating ADD PRIMARY KEY (MessageID,VoterUserID);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = "ALTER TABLE ForumUsers ADD ReputationCache int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumUsers ADD ReputationCache int";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD ReputationCache int NOT NULL DEFAULT 0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumUsers ADD OpenIdUserName varchar(255) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = "ALTER TABLE ForumMessages ADD Rating int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumMessages ADD Rating int;";
				sqlMSSQL = "ALTER TABLE ForumMessages ADD Rating int NOT NULL DEFAULT 0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/*starting from 6.6.0 theres two new columns - FirstName and LastName in ForumUsers             */
			if (!FieldExists("ForumUsers", "FirstName"))
			{
				sqlMYSQL = "ALTER TABLE ForumUsers ADD FirstName varchar(100) NULL;";
				sqlAccess = "ALTER TABLE ForumUsers ADD FirstName varchar(100) NULL;";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD FirstName nvarchar(100) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}
			if (!FieldExists("ForumUsers", "LastName"))
			{
				sqlMYSQL = "ALTER TABLE ForumUsers ADD LastName varchar(100) NULL;";
				sqlAccess = "ALTER TABLE ForumUsers ADD LastName varchar(100) NULL;";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD LastName nvarchar(100) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from 6.6.2 a new column - OrderByNumber - added to the ForumGroups table */
			if (!FieldExists("ForumGroups", "OrderByNumber"))
			{
				sqlMYSQL = "ALTER TABLE ForumGroups ADD OrderByNumber int NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumGroups ADD OrderByNumber int;";
				sqlMSSQL = "ALTER TABLE ForumGroups ADD OrderByNumber int NOT NULL DEFAULT 0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* starting from 6.6.7 a new column - HidePresence - added to the ForumUsers table */
			if (!FieldExists("ForumUsers", "HidePresence"))
			{
				sqlMYSQL = "ALTER TABLE ForumUsers ADD HidePresence bit NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumUsers ADD HidePresence bit;";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD HidePresence bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			// new type of subscription: "messages in a FORUM" (not topic)
			if (!TableExists("ForumNewForumMsgSubscriptions"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = @"CREATE TABLE ForumNewForumMsgSubscriptions (
								UserID int NOT NULL ,
								ForumID int NOT NULL 
							);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumNewForumMsgSubscriptions ADD PRIMARY KEY (UserID,ForumID);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			//"use gravatar" setting
			if (!FieldExists("ForumUsers", "UseGravatar"))
			{
				sqlMYSQL = "ALTER TABLE ForumUsers ADD UseGravatar bit NOT NULL DEFAULT 1;";
				sqlAccess = "ALTER TABLE ForumUsers ADD UseGravatar bit;";
				sqlMSSQL = "ALTER TABLE ForumUsers ADD UseGravatar bit NOT NULL DEFAULT 1";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = sqlAccess = sqlMSSQL = "UPDATE ForumUsers SET UseGravatar=0;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			/* new table - ForumSettings  - for storing settings */
			if (!TableExists("ForumSettings"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = @"CREATE TABLE ForumSettings (
							LastDigestSentDate datetime NULL
						);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			//new column "twitter username" in ForumUsers
			if (!FieldExists("ForumUsers", "TwitterUserName"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumUsers ADD TwitterUserName varchar(255) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			//enlarge the avatar column to 255 symbols
			sqlMYSQL = "ALTER TABLE ForumUsers MODIFY COLUMN AvatarFileName varchar(255) NULL;";
			sqlAccess = "ALTER TABLE ForumUsers ALTER COLUMN AvatarFileName varchar(255) NULL;";
			sqlMSSQL = "ALTER TABLE ForumUsers ALTER COLUMN AvatarFileName varchar(255) NULL;";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			//enlarge the homepage column to 255 symbols
			sqlMYSQL = "ALTER TABLE ForumUsers MODIFY COLUMN Homepage varchar(255) NULL;";
			sqlAccess = "ALTER TABLE ForumUsers ALTER COLUMN Homepage varchar(255) NULL;";
			sqlMSSQL = "ALTER TABLE ForumUsers ALTER COLUMN Homepage varchar(255) NULL;";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			//new column "FacebookID" in ForumUsers
			if (!FieldExists("ForumUsers", "FacebookID"))
			{
				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumUsers ADD FacebookID varchar(255) NULL;";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			// settings are now stored in database
			if (!TableExists("ForumConfig"))
			{
				// Access doesn't like "Key" and "Value" names, so use Cfg... prefix
				sqlMYSQL = @"CREATE TABLE ForumConfig (
							CfgKey varchar(255) NOT NULL PRIMARY KEY,
							CfgValue text);";
				sqlAccess = @"CREATE TABLE ForumConfig (
							CfgKey varchar(255) NOT NULL PRIMARY KEY,
							CfgValue memo NOT NULL)";
				sqlMSSQL = @"CREATE TABLE ForumConfig (
							CfgKey nvarchar(255) NOT NULL PRIMARY KEY,
							CfgValue ntext NOT NULL DEFAULT '')";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			//new column "AcceptedAnswer" in ForumMessages
			if (!FieldExists("ForumMessages", "AcceptedAnswer"))
			{
				sqlMYSQL = "ALTER TABLE ForumMessages ADD AcceptedAnswer bool NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumMessages ADD AcceptedAnswer bit;";
				sqlMSSQL = "ALTER TABLE ForumMessages ADD AcceptedAnswer bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			if (!TableExists("ForumAchievements"))
			{
				sqlMYSQL = @"CREATE TABLE ForumAchievements (
							AchievementID int NOT NULL,
							UserID int NOT NULL ,
							DateCreated DateTime NOT NULL,
							TimesAchieved int NOT NULL
						);";
				sqlAccess = @"CREATE TABLE ForumAchievements (
							AchievementID int NOT NULL,
							UserID int NOT NULL ,
							DateCreated DateTime NOT NULL,
							TimesAchieved int NOT NULL
						);";
				sqlMSSQL = @"CREATE TABLE ForumAchievements (
							AchievementID int NOT NULL,
							UserID int NOT NULL ,
							DateCreated DateTime NOT NULL,
							TimesAchieved int NOT NULL
						)";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = sqlAccess = sqlMSSQL = "ALTER TABLE ForumAchievements ADD PRIMARY KEY (AchievementID, UserID);";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}

			//enlarge the poll-option column to 255 symbols
			sqlMYSQL = "ALTER TABLE ForumPollOptions MODIFY COLUMN OptionText varchar(255) NULL;";
			sqlAccess = "ALTER TABLE ForumPollOptions ALTER COLUMN OptionText varchar(255) NULL;";
			sqlMSSQL = "ALTER TABLE ForumPollOptions ALTER COLUMN OptionText nvarchar(255) NULL;";
			ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

			//new column "HiddenByRecipient" and "HiddenBySender" in ForumPersonalMessages
			if (!FieldExists("ForumPersonalMessages", "HiddenByRecipient"))
			{
				sqlMYSQL = "ALTER TABLE ForumPersonalMessages ADD HiddenByRecipient bool NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumPersonalMessages ADD HiddenByRecipient bit;";
				sqlMSSQL = "ALTER TABLE ForumPersonalMessages ADD HiddenByRecipient bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);

				sqlMYSQL = "ALTER TABLE ForumPersonalMessages ADD HiddenBySender bool NOT NULL DEFAULT 0;";
				sqlAccess = "ALTER TABLE ForumPersonalMessages ADD HiddenBySender bit;";
				sqlMSSQL = "ALTER TABLE ForumPersonalMessages ADD HiddenBySender bit NOT NULL DEFAULT 0";
				ExecuteDDLCommand(sqlAccess, sqlMSSQL, sqlMYSQL);
			}
		}
	}
}
