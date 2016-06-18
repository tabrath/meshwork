//
// AESTransportEncryptor.cs: Encrypt transport data using AES
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Security.Cryptography;

namespace FileFind.Meshwork.Transport
{
	public class AESTransportEncryptor : ITransportEncryptor
	{
		private RijndaelManaged algorithm;
		private byte[] keyBytes;
		private byte[] ivBytes;

        public int KeySize { get; } = 32;
        public int IvSize { get; } = 16;
        // XXX: return keySize + ivSize;
        public int KeyExchangeLength { get { return 128; } }

        public bool Ready
        {
            get { return this.algorithm != null; }
        }
		
		public AESTransportEncryptor()
		{
		}
		
		public void SetKey(byte[] keyBytes, byte[] ivBytes)
		{
			this.keyBytes = keyBytes;
			this.ivBytes = ivBytes;
			
			this.algorithm = new RijndaelManaged();
		}
		
		public byte[] Encrypt(byte[] buffer)
		{
			if (algorithm == null)
                throw new Exception("No key");

            using (var encryptor = this.algorithm.CreateEncryptor(this.keyBytes, this.ivBytes))
            {
                return encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
            }
		}
		
		public byte[] Decrypt(byte[] buffer)
		{
			if (algorithm == null)
                throw new Exception("No key");

            using (var decryptor = this.algorithm.CreateDecryptor(this.keyBytes, this.ivBytes))
            {
                return decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
            }
		}
	}
}
