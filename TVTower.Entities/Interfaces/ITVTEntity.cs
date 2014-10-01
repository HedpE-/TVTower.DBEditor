﻿using System;
using CodeKnight.Core;

namespace TVTower.Entities
{
    public interface ITVTEntity : IIdEntity
	{
		Guid Id { get; set; }
		string OldId { get; set; }

		TVTDataType DataType { get; set; }
		TVTDataStatus DataStatus { get; set; }

		object Tag { get; set; }
		string AdditionalInfo { get; set; }

		string CreatorId { get; set; }
		string EditorId { get; set; }
		DateTime LastModified { get; set; }

		void GenerateGuid();
	}
}
