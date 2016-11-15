﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace Lynicon.Test.Models
{
    [Serializable]
    public class HeaderSummary2 : Summary
    {
        public Image Image { get; set; }
        public int SubTestsCount { get; set; }

        public HeaderSummary2()
        {
            Image = new Image();
        }
    }

    [Serializable, RedirectPropertySource("Common"), SummaryType(typeof(HeaderSummary))]
    public class HeaderContent2 : PageContent
    {
        [Summary]
        public string Title { get; set; }

        [Summary]
        public Image Image { get; set; }

        [UIHint("MinHtml")]
        public string HeaderBody { get; set; }

        public string Common { get; set; }

        [Summary]
        public int SubTestsCount
        {
            get { return SubTests == null ? 0 : SubTests.Count; }
            set { }
        }
        public List<SubTest> SubTests { get; set; }

        private List<Summary> childItems = null;
        [JsonIgnore, ScaffoldColumn(false)]
        public List<Summary> ChildItems
        {
            get
            {
                if (childItems == null)
                    childItems = GetPathChildren<Summary>().ToList();
                return childItems;
            }
        }

        public HeaderContent2()
        {
            this.SubTests = new List<SubTest>();
            this.Image = new Image();
        }
    }
}