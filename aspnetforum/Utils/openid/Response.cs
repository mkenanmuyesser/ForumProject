using System;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Web;
using System.Diagnostics;

namespace aspnetforum.Utils.openid {
	/// <summary>
	/// A response to an OpenID request in terms the host web site can forward to the user agent.
	/// </summary>
	internal class Response : IResponse {
		/// <param name="code">The HTTP status code.</param>
		/// <param name="headers">The collection of any HTTP headers that should be included.  Cannot be null, but can be an empty collection.</param>
		/// <param name="body">The payload of the response, if any.  Cannot be null, but can be an empty array.</param>
		/// <param name="encodableMessage">
		/// Used to assist testing to decipher the field contents of a Response.
		/// </param>
		internal Response(HttpStatusCode code, WebHeaderCollection headers, byte[] body, IEncodable encodableMessage) {
			if (headers == null) throw new ArgumentNullException("headers");
			if (body == null) throw new ArgumentNullException("body");
			Debug.Assert(encodableMessage != null, "For testing, this is useful to have.");
			Code = code;
			Headers = headers ?? new WebHeaderCollection();
			Body = body;
			EncodableMessage = encodableMessage;
		}

		public HttpStatusCode Code { get; private set; }
		public WebHeaderCollection Headers { get; private set; }
		public byte[] Body { get; private set; }
		internal IEncodable EncodableMessage { get; private set; }

		/// <summary>
		/// Sends this response to the user agent or OpenId consumer.
		/// Execution of the current page terminates after this call.
		/// </summary>
		/// <remarks>
		/// This method requires a current ASP.NET HttpContext.
		/// </remarks>
		public void Send() {
			if (HttpContext.Current == null) throw new InvalidOperationException(Strings.CurrentHttpContextRequired);
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.StatusCode = (int)Code;
			Util.ApplyHeadersToResponse(Headers, HttpContext.Current.Response);
			if (Body != null && Body.Length > 0) {
				HttpContext.Current.Response.OutputStream.Write(Body, 0, Body.Length);
				HttpContext.Current.Response.OutputStream.Flush();
			}
			HttpContext.Current.Response.OutputStream.Close();
			HttpContext.Current.Response.End();
		}

		/// <summary>
		/// Gets the indirect message as it would appear as a single URI request.
		/// </summary>
		internal Uri IndirectMessageAsRequestUri {
			get {
				if (EncodableMessage != null && EncodableMessage.RedirectUrl != null && EncodableMessage.EncodingType == EncodingType.IndirectMessage) {
					UriBuilder builder = new UriBuilder(EncodableMessage.RedirectUrl);
					UriUtil.AppendQueryArgs(builder, EncodableMessage.EncodedFields);
					return builder.Uri;
				} else {
					throw new InvalidOperationException();
				}
			}
		}
	}
}
