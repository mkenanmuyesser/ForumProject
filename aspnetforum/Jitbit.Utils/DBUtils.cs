using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;

namespace Jitbit.Utils
{
	public static class DBUtils
	{
		private static string _strcon;
		public static string _connStr
		{
			get { return _strcon; }
		}

		/// <summary>
		/// static constructor
		/// </summary>
		static DBUtils()
		{
			_strcon = ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
		}

		public static SqlCommand GetNewCommandObject(SqlConnection connection = null)
		{
			if (connection == null) connection = new SqlConnection(_strcon);
			SqlCommand cmd = new SqlCommand();
			cmd.Connection = connection;
			return cmd;
		}

		public static SqlDataReader ExecuteReader(this SqlConnection cn, string commandText)
		{
			SqlCommand cmd = new SqlCommand(commandText, cn);
			return cmd.ExecuteReader();
		}

		public static SqlConnection GetNewConnection()
		{
			return new SqlConnection(_connStr);
		}

		public static SqlConnection GetNewOpenConnection()
		{
			var cn = new SqlConnection(_connStr);
			cn.Open();
			return cn;
		}

		public static object NullToDBNull(object value)
		{
			return (value == null) ? DBNull.Value : value;
		}

		/// <summary>
		/// creates a dictionary of "columns" from a datareader
		/// </summary>
		public static Dictionary<string, object> GetFieldsFromDataReaderCurrentPosition(SqlDataReader dr)
		{
			try
			{
				Dictionary<string, object> retval = new Dictionary<string, object>();
				for (int i = 0; i < dr.FieldCount; i++)
				{
					retval.Add(dr.GetName(i), dr[i]);
				}
				return retval;
			}
			catch { return null; }			
		}

		public static bool? _isFullTextInstalled;
		public static bool IsFullTextInstalled()
		{
			if (!_isFullTextInstalled.HasValue)
				using (var cn = GetNewOpenConnection())
				{
					try
					{
						_isFullTextInstalled = (cn.Query<int>("SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')").First() == 1);
					}
					catch
					{
						_isFullTextInstalled = false; //Azure workaround
					}
				}
		
			return _isFullTextInstalled.Value;
		}


		public static bool? _isFullTextIndexSetUp;
		public static bool IsFullTextIndexSetUp()
		{
			if (!_isFullTextIndexSetUp.HasValue)
			{
				using (var cn = GetNewOpenConnection())
				{
					_isFullTextIndexSetUp = (cn.Query("select * from sys.fulltext_indexes").Count() > 0); //assumming that if index on hdIssues is installed, it also installed on hdComments
				}
			}

			return _isFullTextIndexSetUp.Value;
		}

		public static DataTable SelectTopNRows(this DataTable dtSource, int topRows)
		{
			var topN = dtSource.AsEnumerable().Take(topRows);
			DataTable dtNew = new DataTable();
			dtNew = dtSource.Clone();

			foreach (DataRow drrow in topN.ToArray())
			{
				dtNew.ImportRow(drrow);
			}
			dtSource.Dispose();	
			return dtNew;
		}

		public static IEnumerable<T> GetPage<T>(this IEnumerable<T> source, int pageSize, int pageNumber)
		{
			return source.Skip(pageSize * pageNumber).Take(pageSize);
		}

		public static T FindPreviousItem<T>(this IEnumerable<T> items, Predicate<T> matchFilling)
		{
			using (var iter = items.GetEnumerator())
			{
				T previous = default(T);
				while (iter.MoveNext())
				{
					if (matchFilling(iter.Current)) //found condition
					{
						return previous;
					}
					previous = iter.Current;
				}
			}
			// If we get here nothing has been found so return three default values
			return default(T);
		}

		public static T FindNextItem<T>(this IEnumerable<T> items, Predicate<T> matchFilling)
		{
			using (var iter = items.GetEnumerator())
			{
				while (iter.MoveNext())
				{
					if (matchFilling(iter.Current)) //found condition
					{
						if (iter.MoveNext())
							return iter.Current;
						else
							return default(T);
					}
				}
			}
			// If we get here nothing has been found so return three default values
			return default(T);
		}

		public static string GetFullTextSearchQuery(string query)
		{
			//if it's the whole query surronded with qoutes, leave it
			if (query.StartsWith("\"") && query.EndsWith("\""))
				return query;

			//matches single word or many words in qoutes, split query to matches
			Regex regex = new Regex(@"\w+|""[\w\s]*""");
			var matches = regex.Matches(query).Cast<Match>().Select(m => m.Value).ToList();

			StringBuilder q = new StringBuilder();
			bool previousMatchWasOperator = true;
			foreach (var match in matches)
			{
				bool isOperator;
				if (match.ToLower() != "or" && match.ToLower() != "and")
				{
					if (!previousMatchWasOperator)
						q.Append(" AND "); //if there wasn't AND or OR to combine two matches, let's add AND

					if (match.StartsWith("\""))
						q.Append(match);
					else
						q.AppendFormat("\"{0}\"", match);
					previousMatchWasOperator = false;
				}
				else //this is an operator
				{
					q.AppendFormat(" {0} ", match);
					previousMatchWasOperator = true;
				}
			}

			return q.ToString();
		}
	}
}
