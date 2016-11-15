﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using Lynicon.Utility;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Indicate the required crop sizes for all its uses on an image content item property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RequiredCropsAttribute : Attribute, IMetadataAware
    {
        public const string CropsKey = "_CropList";
        /// <summary>
        /// Required crop sizes
        /// format: cropspec [',' cropspec]*
        /// cropspec = widthpx 'x' heightpx ['Q' jpegqualitypercent]
        /// </summary>
        public string CropList { get; set; }

        public RequiredCropsAttribute(string cropList)
        {
            CropList = cropList;
        }

        #region IMetadataAware Members

        public void OnMetadataCreated(ModelMetadata metadata)
        {
            metadata.AdditionalValues.Add(CropsKey, CropList);
        }

        #endregion
    }
}
