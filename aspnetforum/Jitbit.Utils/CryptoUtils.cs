using System;
using System.Security.Cryptography;
using System.Text;

namespace Jitbit.Utils
{
	public static class CryptoUtils
	{
		// Returns a string of six random digits.
		public static string GenerateRandomNumericCode()
		{
			// For generating random numbers.
			Random random = new Random();
			string s = "";
			for (int i = 0; i < 6; i++)
				s = String.Concat(s, random.Next(10).ToString());
			return s;
		}

		// Returns a string of N random chars.
		public static string GenerateRandomCode(int length)
		{
			Random random = new Random();
			string s = "";
			string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
			int len = allowedChars.Length;
			for (int i = 0; i < length; i++)
				s = String.Concat(s, allowedChars.Substring(random.Next(len), 1));
			return s;
		}

		public static string SHA1Hash(string input)
		{
			SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
			byte[] buffer = Encoding.UTF8.GetBytes(input);
			string hash = BitConverter.ToString(sha1.ComputeHash(buffer)).Replace("-", "");
			return hash;
		}

		//returns MD5 hash of a string
		public static string MD5Hash(string input)
		{
			System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
			bs = x.ComputeHash(bs);
			System.Text.StringBuilder s = new System.Text.StringBuilder();
			foreach (byte b in bs)
			{
				s.Append(b.ToString("x2").ToLower());
			}
			return s.ToString();
		}
	}
}
