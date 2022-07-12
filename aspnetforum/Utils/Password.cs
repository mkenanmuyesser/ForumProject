using System;
using System.Security.Cryptography;
using System.Text;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	//backward compatibilty class. Everything is moved to CryptoUtils
	public class Password
	{
		public static string CalculateHash(string input)
		{
			if (Settings.UseSHA1InsteadOfMD5)
				return CryptoUtils.SHA1Hash(input);
			else
				return CryptoUtils.MD5Hash(input).ToUpper();
		}

		public static string CalculateMD5Hash(string input)
		{
			return CryptoUtils.MD5Hash(input).ToUpper();
		}
	}
}
