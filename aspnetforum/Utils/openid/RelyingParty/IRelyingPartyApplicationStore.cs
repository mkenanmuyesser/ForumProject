using System;
using System.Collections.Generic;
using System.Text;

namespace aspnetforum.Utils.openid.RelyingParty {
	/// <summary>
	/// The contract for implementing a custom store for a relying party web site.
	/// </summary>
	internal interface IRelyingPartyApplicationStore : IAssociationStore<Uri>, INonceStore {
	}
}
