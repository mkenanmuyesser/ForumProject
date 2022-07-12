using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace aspnetforum.Utils.openid.RelyingParty {
	[DebuggerDisplay("{Exception.Message}")]
	internal class FailedAuthenticationResponse : IAuthenticationResponse {
		public FailedAuthenticationResponse(Exception exception) {
			Exception = exception;
		}

		#region IAuthenticationResponse Members

		public IDictionary<string, string> GetCallbackArguments() {
			return new Dictionary<string, string>();
		}

		public string GetCallbackArgument(string key) {
			return null;
		}

		public T GetExtension<T>() where T : openid.Extensions.IExtensionResponse, new() {
			return default(T);
		}

		public openid.Extensions.IExtensionResponse GetExtension(Type extensionType) {
			return null;
		}

		public Identifier ClaimedIdentifier {
			get { return null; }
		}

		public string FriendlyIdentifierForDisplay {
			get { return null; }
		}

		public AuthenticationStatus Status {
			get { return AuthenticationStatus.Failed; }
		}

		public Exception Exception { get; private set; }

		#endregion
	}
}
