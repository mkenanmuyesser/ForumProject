using System.Configuration;

namespace aspnetforum.Utils.openid.Configuration {
	internal class WhiteBlackListElement : ConfigurationElement {
		const string nameConfigName = "name";
		[ConfigurationProperty(nameConfigName, IsRequired = true)]
		//[StringValidator(MinLength = 1)]
		public string Name {
			get { return (string)this[nameConfigName]; }
			set { this[nameConfigName] = value; }
		}
	}
}
