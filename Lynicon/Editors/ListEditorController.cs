﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Lynicon.Collation;
using Lynicon.Config;
using Lynicon.Utility;
using Lynicon.Routing;
using Lynicon.Models;
using Lynicon.Repositories;

namespace Lynicon.Editors
{
    /// <summary>
    /// Editor controller for list/detail editor
    /// </summary>
    public class ListEditorController : EditorController
    {
        /// <summary>
        /// Show the list/detail editor
        /// </summary>
        /// <param name="data">list of data items</param>
        /// <param name="view">the view to use to render the list editor (defaults to LyniconListDetail)</param>
        /// <param name="rowFields">the fields to show on each row of the list</param>
        /// <returns>Markup of editor</returns>
        [HttpGet, Authorize(Roles = Membership.User.EditorRole)]
        public ActionResult Index(object data, string view, string rowFields)
        {
            SetViewBag(data, 0, rowFields);
            return View(view ?? "LyniconListDetail", data);
        }

        /// <summary>
        /// Update/add a record in the list/detail editor
        /// </summary>
        /// <param name="data">Original list of items</param>
        /// <param name="view">the view to use to render the list editor (defaults to LyniconListDetail)</param>
        /// <param name="rowFields">the fields to show on each row of the list</param>
        /// <param name="lynicon_itemIndex">the index of the item being edited or -1 to add a record</param>
        /// <param name="form">The form data of the edited / added item</param>
        /// <param name="editAction">Deprecated indication of a specific edit action to take on the data</param>
        /// <param name="formState">The UI state of the form</param>
        /// <returns>Markup of editor</returns>
        [HttpPost, ValidateInput(false), Authorize(Roles = Membership.User.EditorRole)]
        public ActionResult Index(object data, string view, string rowFields,
            int? lynicon_itemIndex, FormCollection form, string editAction, string formState)
        {
            object item;
            Type type = data.GetType().GetGenericArguments()[0];
            if (lynicon_itemIndex < 0) // create new
            {
                item = Collator.Instance.GetNew(type, null);
                lynicon_itemIndex = ((IList)data).Count;
                ((IList)data).Add(item);
                RouteData.DataTokens.Add("LynNewItem", true);
            }
            else
            {
                item = ((IList)data)[lynicon_itemIndex ?? 0];
                type = item.GetType();
            }

            //FormCollection itemForm = new FormCollection();
            //form.ToKeyValues()
            //    .Where(kvp => kvp.Key.StartsWith("item"))
            //    .Do(kvp => itemForm.Add(kvp.Key.After("item."), kvp.Value));

            base.Bind(item, form);

            //if (editAction != null)
            //{
            //    IList list;
            //    Type itemType;
            //    switch (editAction.UpTo("-"))
            //    {
            //        case "add":
            //            list = ReflectionX.GetPropertyValueByPath(data, editAction.After("-")) as IList;
            //            itemType = list.GetType().GetGenericArguments()[0];
            //            if (list != null)
            //                list.Add(CreateInstance(itemType));
            //            break;
            //        case "del":
            //            list = ReflectionX.GetPropertyValueByPath(data, editAction.After("-").UpToLast("[")) as IList;
            //            itemType = list.GetType().GetGenericArguments()[0];
            //            if (list != null)
            //            {
            //                ModelState.Clear(); // templating system will take old values out of the ModelState unless you do this
            //                list.RemoveAt(int.Parse(editAction.LastAfter("[").UpTo("]")));
            //            }
            //            break;
            //    }
            //}

            if (ModelState.IsValid)
            {
                Save(item);

                // we now know there were no errors, so we do this to allow any changes made to 'data' to be shown on the form 
                ModelState.Clear();
            }

            SetViewBag(data, lynicon_itemIndex.HasValue && lynicon_itemIndex.Value > 0 ? lynicon_itemIndex.Value : 0, rowFields);

            return View(view ?? "LyniconListDetail", data);
        }

        /// <summary>
        /// Delete an item from the row / detail editor
        /// </summary>
        /// <param name="data">The original list of items</param>
        /// <param name="idx">The index of the item to delete</param>
        /// <returns>The status of the operation</returns>
        [HttpPost, Authorize(Roles = Membership.User.AdminRole)]
        public ActionResult Delete(object data, int? idx)
        {
            object item = ((IList)data)[idx ?? 0];
            Type type = item.GetType();
            Collator.Instance.Delete(item);
            return Content("OK");
        }

        /// <summary>
        /// Gets the markup for the detail part of the form
        /// </summary>
        /// <param name="data">The list of items</param>
        /// <param name="idx">The index of the item being edited, or -1 for adding an item</param>
        /// <returns></returns>
        [HttpPost, Authorize(Roles = Membership.User.EditorRole)]
        public ActionResult GetValues(object data, int? idx)
        {
            object item;
            if (idx == null || idx.Value < 0)
            {
                // create new
                Type type = data.GetType().GenericTypeArguments[0];
                item = Collator.Instance.GetNew(type, null);
            }
            else
                item = ((IList)data)[idx.Value];

            ViewData["ItemIndex"] = idx ?? 0;

            //string url = Request.RawUrl.UpTo("?");
            //var idPi = LinqX.GetIdProp(item.GetType(), null);
            //string idVal = idPi.GetValue(item).ToString();
            //if (idPi.PropertyType == typeof(string))
            //    idVal = "'" + idVal + "'";
            //else if (idPi.PropertyType == typeof(Guid))
            //    idVal = "guid'" + idVal + "'";
            //string idFilter = string.Format("$filter={0} eq {1}", idPi.Name, idVal);

            string url = Request.RawUrl.UpTo("?");
            string query = Request.RawUrl.After("?");
            query = query.Split('&').Where(s => s != "$mode=getValues").Join("&");
            ViewBag.Url = url + (string.IsNullOrEmpty(query) ? "" : "?" + query);

            object container = Collator.Instance.GetContainer(item);
            string id = Collator.Instance.GetIdProperty(container.GetType()).GetValue(container).ToString();
            ViewBag.Id = id;

            return PartialView("~/Areas/Lynicon/Views/Shared/EditorTemplates/LyniconEditPanel.cshtml", item);
        }

        private void SetViewBag(object data, int itemIndex, string rowFields)
        {
            base.SetViewBag();
            
            Type itemType = data.GetType().GetGenericArguments()[0];

            ViewBag.ListView = this.RouteData.DataTokens["listView"];
            ViewBag.ListFields = rowFields
                .Split(',')
                .Select(fn => fn.Trim())
                .Where(fn => !string.IsNullOrEmpty(fn))
                .ToList();
            if (ViewBag.DisplayFields == null)
                ViewBag.DisplayFields = itemType.GetProperties().Select(pi => pi.Name).ToList();
            ViewData["ItemIndex"] = itemIndex;
            ViewBag.CanAdd = false;
            try
            {
                var newItem = Collator.Instance.GetNew(itemType, null);
                ViewBag.CanAdd = true;
            }
            catch { }

            object container = Collator.Instance.GetContainer(((IList)data)[itemIndex]);
            string id = Collator.Instance.GetIdProperty(container.GetType()).GetValue(container).ToString();
            ViewBag.Id = id;
        }

        [HttpGet, Authorize(Roles = Membership.User.EditorRole)]
        public ActionResult Empty()
        {
            return View(ConfigHelper.GetViewPath("LyniconEmptyUrl.cshtml"));
        }

        public override ActionResult PropertyItemHtml(object data, string propertyPath, int depth, string pathPrefix)
        {
            //propertyPath = propertyPath.After("item.");
            Type itemType = data.GetType().GetGenericArguments()[0];
            data = CreateInstance(itemType);
            //pathPrefix = "item.";
            return base.PropertyItemHtml(data, propertyPath, depth, pathPrefix);
        }

        protected override void Save(object data)
        {
            var currAddress = new Address(data);
            if (currAddress.Count == 0) // content item with no AddressComponent fields
                currAddress = null;

            try
            {
                Collator.Instance.Set(currAddress, data, GetIfCreate());
            }
            catch (LyniconUpdateException lux)
            {
                ModelState.AddModelError("updateFail", lux.Message);
            }
        }

    }
}
