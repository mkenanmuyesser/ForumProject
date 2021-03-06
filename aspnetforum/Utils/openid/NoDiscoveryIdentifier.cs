using System;
using System.Collections.Generic;
using System.Text;
using aspnetforum.Utils.openid.RelyingParty;

namespace aspnetforum.Utils.openid {
	/// <summary>
	/// Wraps an existing Identifier and prevents it from performing discovery.
	/// </summary>
	internal class NoDiscoveryIdentifier : Identifier {
		Identifier wrappedIdentifier ;
		internal NoDiscoveryIdentifier(Identifier wrappedIdentifier, bool claimSsl)
			: base(claimSsl) {
			if (wrappedIdentifier == null) throw new ArgumentNullException("wrappedIdentifier");

			this.wrappedIdentifier = wrappedIdentifier;
		}

		internal override IEnumerable<ServiceEndpoint> Discover() {
			return new ServiceEndpoint[0];
		}

		internal override Identifier TrimFragment() {
			return new NoDiscoveryIdentifier(wrappedIdentifier.TrimFragment(), IsDiscoverySecureEndToEnd);
		}

		internal override bool TryRequireSsl(out Identifier secureIdentifier) {
			return wrappedIdentifier.TryRequireSsl(out secureIdentifier);
		}

		public override string ToString() {
			return wrappedIdentifier.ToString();
		}

		public override bool Equals(object obj) {
			return wrappedIdentifier.Equals(obj);
		}

		public override int GetHashCode() {
			return wrappedIdentifier.GetHashCode();
		}
	}
}
