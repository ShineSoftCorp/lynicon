﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Lynicon.Extensibility;
using Newtonsoft.Json;

namespace Lynicon.Controllers
{
    public class VersionController : Controller
    {
        /// <summary>
        /// Change the current UI version where this is permitted
        /// </summary>
        /// <param name="version">The new version</param>
        /// <returns>Status of operation</returns>
        public ActionResult ChangeVersion(Dictionary<string, string[]> version)
        {
            ItemVersion iv = new ItemVersion(version.ToDictionary(kvp => kvp.Key, kvp => JsonConvert.DeserializeObject(kvp.Value[0])));
            VersionManager.Instance.ClientVersionOverride = iv;
            return Content("OK");
        }

        /// <summary>
        /// Utility http webservice to get current versioning mode in case it has been left in an inconsistent state
        /// </summary>
        /// <returns>Data on current and stacked versions</returns>
        public ActionResult Show()
        {
            VersioningMode vm = VersionManager.Instance.Mode;
            string mode = "";
            switch (vm)
            {
                case VersioningMode.All:
                    mode = "all";
                    break;
                case VersioningMode.Current:
                    mode = "current";
                    break;
                case VersioningMode.Public:
                    mode = "public";
                    break;
                case VersioningMode.Specific:
                    mode = "specific = " + VersionManager.Instance.SpecificVersion.ToString();
                    break;
            }

            return Content(string.Format("Currently: {0}, Stack: {1}", mode, VersionManager.Instance.StackDescribe()));
        }
    }
}
