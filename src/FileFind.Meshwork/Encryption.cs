//
// Encryption.cs: Cryptography helper methods
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace FileFind.Meshwork.Security
{
    public static class Encryption 
	{
		public static byte[] Decrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static byte[] Encrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static string PasswordEncrypt(string password, string text, byte[] salt)
		{
            var result = string.Empty;

            using (var passwordBytes = new Rfc2898DeriveBytes(password, salt))
            {
                using (var alg = Rijndael.Create())
                {
                    alg.Key = passwordBytes.GetBytes(32);
                    alg.IV = passwordBytes.GetBytes(16);

                    var buffer = Encoding.UTF8.GetBytes(text);

                    using (var ms = new MemoryStream())
                    {
                        using (var encryptStream = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            encryptStream.Write(buffer, 0, buffer.Length);
                            encryptStream.Flush();
                        }

                        result = Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

            return result;
		}

		public static string PasswordDecrypt(string password, string text, byte[] salt)
		{
            var result = string.Empty;

            using (var bytes = new Rfc2898DeriveBytes(password, salt))
            {
                using (var alg = Rijndael.Create())
                {
                    alg.Key = bytes.GetBytes(32);
                    alg.IV = bytes.GetBytes(16);

                    var buffer = Convert.FromBase64String(text);

                    using (var ms = new MemoryStream())
                    {
                        using (var decryptStream = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            decryptStream.Write(buffer, 0, buffer.Length);
                            decryptStream.Flush();
                        }

                        result = Encoding.UTF8.GetString(ms.ToArray());
    			    }
                }
            }

            return result;
		}
	}
}
