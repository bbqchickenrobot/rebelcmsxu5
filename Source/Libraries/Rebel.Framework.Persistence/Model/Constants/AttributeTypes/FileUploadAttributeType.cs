﻿using Rebel.Framework.Persistence.Model.Attribution.MetaData;
using Rebel.Framework.Persistence.Model.Constants.SerializationTypes;

namespace Rebel.Framework.Persistence.Model.Constants.AttributeTypes
{
    public class FileUploadAttributeType : AttributeType
    {
        public const string AliasValue = "system-file-upload-type";

        public FileUploadAttributeType()
            : base(
                AliasValue,
                AliasValue,
                "This type represents the internal file upload type",
                new StringSerializationType())
        {
            Id = FixedHiveIds.FileUploadAttributeType;
        }
    }
}