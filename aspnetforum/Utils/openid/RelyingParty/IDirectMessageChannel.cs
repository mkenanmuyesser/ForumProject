using System.Collections.Generic;

namespace aspnetforum.Utils.openid.RelyingParty {
	internal interface IDirectMessageChannel {
		IDictionary<string, string> SendDirectMessageAndGetResponse(ServiceEndpoint provider, IDictionary<string, string> fields);
	}
}
