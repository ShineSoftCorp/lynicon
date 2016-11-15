﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Reflection;
using System.IO;
using System.Web.UI;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Lynicon.Extensibility;
using Lynicon.Extensions;
using Lynicon.Membership;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Lynicon.Controllers
{
    /// <summary>
    /// Controller for the Items and Filters CMS pages
    /// </summary>
    public class ItemsController : Controller
    {
        /// <summary>
        /// Serve the List page of all content items by type with search and paging
        /// </summary>
        /// <returns>The List page</returns>
        public ActionResult Index()
        {
            ViewData.Add("UrlPermission", LyniconSecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            return View();
        }

        /// <summary>
        /// Serve the Filters page listing content items with filters and operations
        /// </summary>
        /// <returns>The Filters page</returns>
        [Authorize(Roles = Lynicon.Membership.User.EditorRole)]
        public ActionResult List()
        {
            return View();
        }

        public ActionResult Find()
        {
            ViewData.Add("UrlPermission", LyniconSecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            return PartialView("LyniconItems");
        }

        /// <summary>
        /// Get markup to refresh an individual item on the List page
        /// </summary>
        /// <param name="datatype">Type of the item</param>
        /// <param name="id">Id of the item</param>
        /// <returns>Markup for the item</returns>
        public ActionResult GetItem(string datatype, string id)
        {
            ViewData.Add("UrlPermission", LyniconSecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            Type type = ContentTypeHierarchy.GetContentType(datatype);
            var summ = Collator.Instance.Get<Summary>(new ItemId(type, id));
            return PartialView("ItemListSummary", summ);
        }

        /// <summary>
        /// Get markup to show all the items of a type in a paged box on the List page
        /// </summary>
        /// <param name="datatype">The data type</param>
        /// <returns>Markup of the paged box listing the items</returns>
        public ActionResult GetPage(string datatype)
        {
            ViewData.Add("UrlPermission", LyniconSecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.EditorRole));
            ViewData.Add("DelPermission", LyniconSecurityManager.Current.CurrentUserInRole(Lynicon.Membership.User.AdminRole));
            Type type = ContentTypeHierarchy.GetContentType(datatype);
            Type containerType = Collator.Instance.ContainerType(type);
            // invoke Collator.Instance.GetList<Summary, type>(new Type[] { type }, RouteData).ToArray();
            var summs = (IEnumerable<Summary>)ReflectionX.InvokeGenericMethod(Collator.Instance, "GetList", new Type[] { typeof(Summary), containerType }, new Type[] { type }, RouteData);
            var data = summs.ToArray();
            return PartialView("ItemPage", data);
        }

        /// <summary>
        /// Get markup to show the filter based lister
        /// </summary>
        /// <returns>Markup of filter based lister as PartialView</returns>
        public ActionResult ItemLister()
        {
            var u = LyniconSecurityManager.Current.User;
            var v = VersionManager.Instance.CurrentVersion;
            ViewData["VersionSelector"] = VersionManager.Instance.SelectionViewModel(u, v);
            return PartialView(new ItemListerViewModel());
        }

        /// <summary>
        /// Get the items to show on the filter based lister given filter values
        /// </summary>
        /// <param name="versionFilter">The filter for the version</param>
        /// <param name="classFilter">The filter for the data type or data types</param>
        /// <param name="filters">The custom filters</param>
        /// <returns>Markup for the paged list of items</returns>
        [Authorize(Roles = Lynicon.Membership.User.EditorRole)]
        public ActionResult FilterItems(List<string> versionFilter, string[] classFilter, List<ListFilter> filters)
        {
            var pagingSpec = PagingSpec.Create(Request.Params);
            if (filters == null)
                filters = new List<ListFilter>();

            var pagedResult = FilterManager.Instance.RunFilter(versionFilter, classFilter, filters, pagingSpec);

            RouteData.DataTokens["@Paging"] = pagingSpec;

            ViewData["ShowFilts"] = filters.Where(f => f.Show).ToList();

            return PartialView(pagedResult);
        }

        /// <summary>
        /// Get the items from the filter based lister as a CSV file
        /// </summary>
        /// <param name="versionFilter">The filter for the version</param>
        /// <param name="classFilter">The filter for the data type or data types</param>
        /// <param name="filters">The custom filters</param>
        /// <returns>A CSV file containing the listed data for all the relevant content items</returns>
        [Authorize(Roles = Lynicon.Membership.User.EditorRole)]
        public ActionResult FilterCsv(List<string> versionFilter, string[] classFilter, List<ListFilter> filters)
        {
            var pagingSpec = PagingSpec.Create(Request.Params);
            if (filters == null)
                filters = new List<ListFilter>();

            string csv = FilterManager.Instance.GenerateCsv(versionFilter, classFilter, filters, pagingSpec);

            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "report.csv");
        }
    }
}
