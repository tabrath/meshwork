using System;
using System.Collections.Generic;

namespace FileFind.Stun
{
	public enum MessageAttributeType : ushort
	{
		MappedAddress = 0x0001,
		ResponseAddress = 0x0002,
		ChangeRequest = 0x0003,
		SourceAddress = 0x0004,
		ChangedAddress = 0x0005,
		Username = 0x0006,
		Password = 0x0007,
		MessageIntegrity = 0x0008,
		ErrorCode = 0x0009,
		UnknownAttributes = 0x00a,
		ReflectedFrom = 0x00b
	}

}
