using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;

namespace aspnetforum.Utils.openid.Yadis {
	internal class TypeElement : XrdsNode {
		public TypeElement(XPathNavigator typeElement, ServiceElement parent) :
			base(typeElement, parent) {
		}

		public string Uri {
			get { return Node.Value; }
		}
	}
}
