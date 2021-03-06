using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace aspnetforum.Utils.openid {
	internal static class UriUtil {
		/// <summary>
		/// Concatenates a list of name-value pairs as key=value&amp;key=value,
		/// taking care to properly encode each key and value for URL
		/// transmission.  No ? is prefixed to the string.
		/// </summary>
		public static string CreateQueryString(IDictionary<string, string> args) {
			if (args == null) throw new ArgumentNullException("args");
			if (args.Count == 0) return string.Empty;
			StringBuilder sb = new StringBuilder(args.Count * 10);

			foreach (var p in args) {
				sb.Append(HttpUtility.UrlEncode(p.Key));
				sb.Append('=');
				sb.Append(HttpUtility.UrlEncode(p.Value));
				sb.Append('&');
			}
			sb.Length--; // remove trailing &

			return sb.ToString();
		}
		/// <summary>
		/// Concatenates a list of name-value pairs as key=value&amp;key=value,
		/// taking care to properly encode each key and value for URL
		/// transmission.  No ? is prefixed to the string.
		/// </summary>
		public static string CreateQueryString(NameValueCollection args) {
			return CreateQueryString(Util.NameValueCollectionToDictionary(args));
		}

		/// <summary>
		/// Adds a set of name-value pairs to the end of a given URL
		/// as part of the querystring piece.  Prefixes a ? or &amp; before
		/// first element as necessary.
		/// </summary>
		/// <param name="builder">The UriBuilder to add arguments to.</param>
		/// <param name="args">
		/// The arguments to add to the query.  
		/// If null, <paramref name="builder"/> is not changed.
		/// </param>
		public static void AppendQueryArgs(UriBuilder builder, IDictionary<string, string> args) {
			if (builder == null) {
				throw new ArgumentNullException("builder");
			}

			if (args != null && args.Count > 0) {
				StringBuilder sb = new StringBuilder(50 + args.Count * 10);
				if (!string.IsNullOrEmpty(builder.Query)) {
					sb.Append(builder.Query.Substring(1));
					sb.Append('&');
				}
				sb.Append(CreateQueryString(args));

				builder.Query = sb.ToString();
			}
		}

		/// <summary>
		/// Adds parameters to a query string, replacing parameters that
		/// match ones that already exist in the query string.
		/// </summary>
		/// <param name="builder">The UriBuilder to add arguments to.</param>
		/// <param name="args">
		/// The arguments to add to the query.  
		/// If null, <paramref name="builder"/> is not changed.
		/// </param>
		internal static void AppendAndReplaceQueryArgs(UriBuilder builder, IDictionary<string, string> args) {
			if (builder == null) {
				throw new ArgumentNullException("builder");
			}

			if (args != null && args.Count > 0) {
				NameValueCollection aggregatedArgs = HttpUtility.ParseQueryString(builder.Query);
				foreach (var pair in args) {
					aggregatedArgs[pair.Key] = pair.Value;
				}

				builder.Query = CreateQueryString(aggregatedArgs);
			}
		}

		/// <summary>
		/// Equivalent to UriBuilder.ToString() but omits port # if it may be implied.
		/// Equivalent to UriBuilder.Uri.ToString(), but doesn't throw an exception if the Host has a wildcard.
		/// </summary>
		public static string UriBuilderToStringWithImpliedPorts(UriBuilder builder) {
			Debug.Assert(builder != null);
			// We only check for implied ports on HTTP and HTTPS schemes since those
			// are the only ones supported by OpenID anyway.
			if ((builder.Port == 80 && string.Equals(builder.Scheme, "http", StringComparison.OrdinalIgnoreCase)) ||
				(builder.Port == 443 && string.Equals(builder.Scheme, "https", StringComparison.OrdinalIgnoreCase))) {
				// An implied port may be removed.
				string url = builder.ToString();
				// Be really careful to only remove the first :80 or :443 so we are guaranteed
				// we're removing only the port (and not something in the query string that 
				// looks like a port.
				return Regex.Replace(url, @"^(https?://[^:]+):\d+", m => m.Groups[1].Value, RegexOptions.IgnoreCase);
			} else {
				// The port must be explicitly given anyway.
				return builder.ToString();
			}
		}
	}

	internal static class Util {
		internal const string DefaultNamespace = "DotNetOpenId";

		public static string DotNetOpenIdVersion {
			get {
				string assemblyFullName = System.Reflection.Assembly.GetExecutingAssembly().GetName().FullName;
				bool official = assemblyFullName.Contains("PublicKeyToken=2780ccd10d57b246");
				// We use InvariantCulture since this is used for logging.
				return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", assemblyFullName, official ? "official" : "private");
			}
		}

		public static IDictionary<string, string> NameValueCollectionToDictionary(NameValueCollection nvc) {
			if (nvc == null) return null;
			var dict = new Dictionary<string, string>(nvc.Count);
			for (int i = 0; i < nvc.Count; i++) {
				string key = nvc.GetKey(i);
				string value = nvc.Get(i);
				// NameValueCollections allow for a null key.  Dictionary<TKey, TValue> does not.
				// We just skip a null key member.  It probably came from a query string that
				// started with "?&".  See Google Code Issue 81.
				if (key != null) {
					dict.Add(key, value);
				}
			}
			return dict;
		}
		public static NameValueCollection DictionaryToNameValueCollection(IDictionary<string, string> dict) {
			if (dict == null) return null;
			NameValueCollection nvc = new NameValueCollection(dict.Count);
			foreach (var pair in dict) {
				nvc.Add(pair.Key, pair.Value);
			}
			return nvc;
		}

		/// <summary>
		/// Gets the query data from the original request (before any URL rewriting has occurred.)
		/// </summary>
		public static NameValueCollection GetQueryFromContextNVC() {
			if (HttpContext.Current == null) throw new InvalidOperationException(Strings.CurrentHttpContextRequired);
			HttpRequest request = HttpContext.Current.Request;
			// This request URL may have been rewritten by the host site.
			// For openid protocol purposes, we really need to look at 
			// the original query parameters before any rewriting took place.
			if (request.Url.PathAndQuery == request.RawUrl) {
				// No rewriting has taken place.
				return request.QueryString;
			} else {
				// Rewriting detected!  Recover the original request URI.
				return HttpUtility.ParseQueryString(GetRequestUrlFromContext().Query);
			}
		}
		/// <summary>
		/// Gets the query or form data from the original request (before any URL rewriting has occurred.)
		/// </summary>
		public static NameValueCollection GetQueryOrFormFromContextNVC() {
			if (HttpContext.Current == null) throw new InvalidOperationException(Strings.CurrentHttpContextRequired);
			HttpRequest request = HttpContext.Current.Request;
			NameValueCollection query;
			if (request.RequestType == "GET") {
				query = GetQueryFromContextNVC();
			} else {
				query = request.Form;
			}
			return query;
		}
		/// <summary>
		/// Gets the querystring or form data from the original request (before any URL rewriting has occurred.)
		/// </summary>
		public static IDictionary<string, string> GetQueryOrFormFromContext() {
			return NameValueCollectionToDictionary(GetQueryOrFormFromContextNVC());
		}
		/// <summary>
		/// Gets the original request URL, as seen from the browser before any URL rewrites on the server if any.
		/// Cookieless session directory (if applicable) is also included.
		/// </summary>
		internal static Uri GetRequestUrlFromContext() {
			HttpContext context = HttpContext.Current;
			if (context == null) throw new InvalidOperationException(Strings.CurrentHttpContextRequired);
			// We use Request.Url for the full path to the server, and modify it
			// with Request.RawUrl to capture both the cookieless session "directory" if it exists
			// and the original path in case URL rewriting is going on.  We don't want to be
			// fooled by URL rewriting because we're comparing the actual URL with what's in
			// the return_to parameter in some cases.
			return new Uri(context.Request.Url, context.Request.RawUrl);
			// Response.ApplyAppPathModifier(builder.Path) would have worked for the cookieless
			// session, but not the URL rewriting problem.
		}

		public static void ApplyHeadersToResponse(WebHeaderCollection headers, HttpResponse response) {
			if (headers == null) throw new ArgumentNullException("headers");
			if (response == null) throw new ArgumentNullException("response");
			foreach (string headerName in headers) {
				switch (headerName) {
					case "Content-Type":
						response.ContentType = headers[HttpResponseHeader.ContentType];
						break;
					// Add more special cases here as necessary.
					default:
						response.AddHeader(headerName, headers[headerName]);
						break;
				}
			}
		}

		public static string GetRequiredArg(IDictionary<string, string> query, string key) {
			if (query == null) throw new ArgumentNullException("query");
			if (key == null) throw new ArgumentNullException("key");
			string value;
			if (!query.TryGetValue(key, out value) || value.Length == 0)
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.MissingOpenIdQueryParameter, key), query);
			return value;
		}
		public static string GetRequiredArgAllowEmptyValue(IDictionary<string, string> query, string key) {
			if (query == null) throw new ArgumentNullException("query");
			if (key == null) throw new ArgumentNullException("key");
			string value;
			if (!query.TryGetValue(key, out value))
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.MissingOpenIdQueryParameter, key), query);
			return value;
		}
		public static string GetOptionalArg(IDictionary<string, string> query, string key) {
			if (query == null) throw new ArgumentNullException("query");
			if (key == null) throw new ArgumentNullException("key");
			string value;
			query.TryGetValue(key, out value);
			return value;
		}
		public static byte[] GetRequiredBase64Arg(IDictionary<string, string> query, string key) {
			string base64string = GetRequiredArg(query, key);
			try {
				return Convert.FromBase64String(base64string);
			} catch (FormatException) {
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.InvalidOpenIdQueryParameterValueBadBase64,
					key, base64string), query);
			}
		}
		public static byte[] GetOptionalBase64Arg(IDictionary<string, string> query, string key) {
			string base64string = GetOptionalArg(query, key);
			if (base64string == null) return null;
			try {
				return Convert.FromBase64String(base64string);
			} catch (FormatException) {
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.InvalidOpenIdQueryParameterValueBadBase64,
					key, base64string), query);
			}
		}
		public static Identifier GetRequiredIdentifierArg(IDictionary<string, string> query, string key) {
			try {
				return Util.GetRequiredArg(query, key);
			} catch (UriFormatException) {
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.InvalidOpenIdQueryParameterValue, key,
					Util.GetRequiredArg(query, key), query));
			}
		}
		public static Uri GetRequiredUriArg(IDictionary<string, string> query, string key) {
			try {
				return new Uri(Util.GetRequiredArg(query, key));
			} catch (UriFormatException) {
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.InvalidOpenIdQueryParameterValue, key,
					Util.GetRequiredArg(query, key), query));
			}
		}
		public static Realm GetOptionalRealmArg(IDictionary<string, string> query, string key) {
			try {
				string realm = Util.GetOptionalArg(query, key);
				// Take care to not return the empty string in case the RP
				// sent us realm= but didn't provide a value.
				return realm.Length > 0 ? realm : null;
			} catch (UriFormatException ex) {
				throw new OpenIdException(string.Format(CultureInfo.CurrentCulture,
					Strings.InvalidOpenIdQueryParameterValue, key,
					Util.GetOptionalArg(query, key)), null, query, ex);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "DotNetOpenId.Realm")]
		internal static UriBuilder GetResolvedRealm(Page page, string realm) {
			Debug.Assert(page != null, "Current HttpContext required to resolve URLs.");
			// Allow for *. realm notation, as well as ASP.NET ~/ shortcuts.

			// We have to temporarily remove the *. notation if it's there so that
			// the rest of our URL manipulation will succeed.
			bool foundWildcard = false;
			// Note: we don't just use string.Replace because poorly written URLs
			// could potentially have multiple :// sequences in them.
			string realmNoWildcard = Regex.Replace(realm, @"^(\w+://)\*\.",
				delegate(Match m) {
					foundWildcard = true;
					return m.Groups[1].Value;
				});

			UriBuilder fullyQualifiedRealm = new UriBuilder(
				new Uri(Util.GetRequestUrlFromContext(), page.ResolveUrl(realmNoWildcard)));

			if (foundWildcard) {
				fullyQualifiedRealm.Host = "*." + fullyQualifiedRealm.Host;
			}

			// Is it valid?
			new Realm(fullyQualifiedRealm); // throws if not valid

			return fullyQualifiedRealm;
		}

		public static bool ArrayEquals<T>(T[] first, T[] second) {
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");
			if (first.Length != second.Length) return false;
			for (int i = 0; i < first.Length; i++)
				if (!first[i].Equals(second[i])) return false;
			return true;
		}

		// The characters to escape here are inspired by 
		// http://code.google.com/p/doctype/wiki/ArticleXSSInJavaScript
		static readonly Dictionary<string, string> javascriptStaticStringEscaping = new Dictionary<string, string> {
			{"\\", @"\\" }, // this WAS just above the & substitution but we moved it here to prevent double-escaping
			{"\t", @"\t" },
			{"\n", @"\n" },
			{"\r", @"\r" },
			{"\u0085", @"\u0085" },
			{"\u2028", @"\u2028" },
			{"\u2029", @"\u2029" },
			{"'", @"\x27" },
			{"\"", @"\x22" },
			{"&", @"\x26" },
			{"<", @"\x3c" },
			{">", @"\x3e" },
			{"=", @"\x3d" },
		};

		/// <summary>
		/// Prepares what SHOULD be simply a string value for safe injection into Javascript
		/// by using appropriate character escaping.
		/// </summary>
		/// <param name="value">The untrusted string value to be escaped to protected against XSS attacks.</param>
		/// <returns>The escaped string.</returns>
		public static string GetSafeJavascriptValue(string value) {
			if (value == null) return "null";
			// We use a StringBuilder because we have potentially many replacements to do,
			// and we don't want to create a new string for every intermediate replacement step.
			StringBuilder builder = new StringBuilder(value);
			foreach (var pair in javascriptStaticStringEscaping) {
				builder.Replace(pair.Key, pair.Value);
			}
			builder.Insert(0, '\'');
			builder.Append('\'');
			return builder.ToString();
		}

		internal delegate R Func<T, R>(T t);
		/// <summary>
		/// Scans a list for matches with some element of the OpenID protocol,
		/// searching from newest to oldest protocol for the first and best match.
		/// </summary>
		/// <typeparam name="T">The type of element retrieved from the <see cref="Protocol"/> instance.</typeparam>
		/// <param name="elementOf">Takes a <see cref="Protocol"/> instance and returns an element of it.</param>
		/// <param name="list">The list to scan for matches.</param>
		/// <returns>The protocol with the element that matches some item in the list.</returns>
		internal static Protocol FindBestVersion<T>(Func<Protocol, T> elementOf, IEnumerable<T> list) {
			foreach (var protocol in Protocol.AllVersions) {
				foreach (var item in list) {
					if (item != null && item.Equals(elementOf(protocol)))
						return protocol;
				}
			}
			return null;
		}

		internal static T FirstOrDefault<T>(IEnumerable<T> sequence, Func<T, bool> predicate) {
			IEnumerator<T> iterator = Where(sequence, predicate).GetEnumerator();
			return iterator.MoveNext() ? iterator.Current : default(T);
		}
		internal static IEnumerable<T> Where<T>(IEnumerable<T> sequence, Func<T, bool> predicate) {
			foreach (T item in sequence) {
				if (predicate(item)) {
					yield return item;
				}
			}
		}
		internal static bool Contains<T>(IEnumerable<T> sequence, Func<T, bool> predicate) {
			foreach (T item in sequence) {
				if (predicate(item)) {
					return true;
				}
			}

			return false;
		}
		internal static IEnumerable<T> Cast<T>(System.Collections.IEnumerable sequence) {
			foreach (object item in sequence) {
				yield return (T)item;
			}
		}
		internal static int Count(System.Collections.IEnumerable sequence) {
			int count = 0;
			foreach (object item in sequence) {
				count++;
			}

			return count;
		}

		/// <summary>
		/// Tests two sequences for same contents and ordering.
		/// </summary>
		internal static bool AreSequencesEquivalent<T>(IEnumerable<T> sequence1, IEnumerable<T> sequence2) {
			if (sequence1 == null && sequence2 == null) return true;
			if (sequence1 == null) throw new ArgumentNullException("sequence1");
			if (sequence2 == null) throw new ArgumentNullException("sequence2");

			IEnumerator<T> iterator1 = sequence1.GetEnumerator();
			IEnumerator<T> iterator2 = sequence2.GetEnumerator();
			bool movenext1, movenext2;
			while (true) {
				movenext1 = iterator1.MoveNext();
				movenext2 = iterator2.MoveNext();
				if (!movenext1 || !movenext2) break; // if we've reached the end of at least one sequence
				object obj1 = iterator1.Current;
				object obj2 = iterator2.Current;
				if (obj1 == null && obj2 == null) continue; // both null is ok
				if (obj1 == null ^ obj2 == null) return false; // exactly one null is different
				if (!obj1.Equals(obj2)) return false; // if they're not equal to each other
			}

			return movenext1 == movenext2; // did they both reach the end together?
		}

		/// <summary>
		/// Prepares a dictionary for printing as a string.
		/// </summary>
		/// <remarks>
		/// The work isn't done until (and if) the 
		/// <see cref="Object.ToString"/> method is actually called, which makes it great
		/// for logging complex objects without being in a conditional block.
		/// </remarks>
		internal static object ToString<K, V>(IEnumerable<KeyValuePair<K, V>> pairs) {
			return new DelayedToString<IEnumerable<KeyValuePair<K, V>>>(pairs, p => {
				var dictionary = pairs as IDictionary<K, V>;
				StringBuilder sb = new StringBuilder(dictionary != null ? dictionary.Count * 40 : 200);
				foreach (var pair in pairs) {
					sb.AppendFormat("\t{0}: {1}{2}", pair.Key, pair.Value, Environment.NewLine);
				}
				return sb.ToString();
			});
		}
		internal static object ToString<T>(IEnumerable<T> list) {
			return ToString<T>(list, false);
		}
		internal static object ToString<T>(IEnumerable<T> list, bool multiLineElements) {
			return new DelayedToString<IEnumerable<T>>(list, l => {
				StringBuilder sb = new StringBuilder();
				if (multiLineElements) {
					sb.AppendLine("[{");
					foreach (T obj in l) {
						// Prepare the string repersentation of the object
						string objString = obj != null ? obj.ToString() : "<NULL>";

						// Indent every line printed
						objString = objString.Replace(Environment.NewLine, Environment.NewLine + "\t");
						sb.Append("\t");
						sb.Append(objString);

						if (!objString.EndsWith(Environment.NewLine)) {
							sb.AppendLine();
						}
						sb.AppendLine("}, {");
					}
					if (sb.Length > 2) { // if anything was in the enumeration
						sb.Length -= 2 + Environment.NewLine.Length; // trim off the last ", {\r\n"
					} else {
						sb.Length -= 1; // trim off the opening {
					}
					sb.Append("]");
					return sb.ToString();
				} else {
					sb.Append("{");
					foreach (T obj in l) {
						sb.Append(obj != null ? obj.ToString() : "<NULL>");
						sb.AppendLine(",");
					}
					if (sb.Length > 1) {
						sb.Length -= 1;
					}
					sb.Append("}");
					return sb.ToString();
				}
			});
		}

		private class DelayedToString<T> {
			public DelayedToString(T obj, Func<T, string> toString) {
				this.obj = obj;
				this.toString = toString;
			}
			T obj;
			Func<T, string> toString;
			public override string ToString() {
				return toString(obj);
			}
		}
	}
}
