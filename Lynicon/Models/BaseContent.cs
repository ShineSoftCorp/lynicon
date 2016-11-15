﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Newtonsoft.Json;
using Lynicon.Utility;
using Lynicon.Collation;
using System.Reflection;
using Lynicon.Repositories;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using Lynicon.Membership;
using Lynicon.Attributes;

namespace Lynicon.Models
{
    /// <summary>
    /// Base class for content classes which will be used on the Content persistence model.  Provides shared
    /// functionality including making container metadata available in the content item.
    /// </summary>
    [Serializable, ContentTypeDisplayName("All Content")]
    public class BaseContent
    {
        /// <summary>
        /// Ensure all properties which would default to null (except string type) have default initialised
        /// instances assigned to them (applied recursively to those instances)
        /// </summary>
        /// <param name="o">The content item</param>
        public static void InitialiseProperties(object o)
        {
            foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
            {
                ScaffoldColumnAttribute sca = prop.GetCustomAttribute<ScaffoldColumnAttribute>();
                if (sca != null && !sca.Scaffold)
                    continue;

                if (prop.PropertyType.IsArray)
                    throw new Exception("Content types may not have array properties: " + o.GetType().FullName);

                if (prop.PropertyType != typeof(string)
                    && prop.PropertyType.IsClass
                    && prop.CanWrite
                    && prop.GetIndexParameters().Length == 0)
                {
                    if (prop.PropertyType.GetConstructor(Type.EmptyTypes) == null)
                        throw new ArgumentException("Cannot initialise type " + prop.PropertyType.FullName + " it has no default constructor");
                    object init = Activator.CreateInstance(prop.PropertyType);
                    InitialiseProperties(init);
                    prop.SetValue(o, init);
                }
            }
        }

        /// <summary>
        /// Take a content type and display its name in a user-friendly way
        /// </summary>
        /// <param name="t">The content type</param>
        /// <returns>Friendly display of its name</returns>
        public static string ContentClassDisplayName(Type t)
        {
            if (t == null)
                return "";
            if (t.Name == "Reference`1")
                t = t.GenericTypeArguments[0];
            var attr = t.GetCustomAttribute<ContentTypeDisplayNameAttribute>();
            if (attr != null)
                return attr.DisplayName;
            string name = t.Name.UpToLast("Content").ExpandCamelCase();
            return name.UpTo("`").Trim();
        }

        /// <summary>
        /// Identity of the container of this content item
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public Guid Identity
        {
            get { return this.OriginalRecord == null ? Guid.Empty : this.OriginalRecord.Identity; }
        }

        /// <summary>
        /// ItemId of the container of this content item
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public ItemId ItemId
        {
            get { return new ItemId(this.GetType(), this.Identity); }
        }

        /// <summary>
        /// The container which contained this content item
        /// </summary>
        [JsonIgnore, ScaffoldColumn(false)]
        public ContentItem OriginalRecord { get; set; }

        protected virtual string PathSep
        {
            get { return "&"; }
        }

        /// <summary>
        /// Find all content items of a given type whose path begins with this one's: this will usually
        /// be content items on urls like this one's with an extra element
        /// </summary>
        /// <typeparam name="T">Type of content item to get</typeparam>
        /// <returns>The list of content items</returns>
        protected virtual IEnumerable<T> GetPathChildren<T>() where T : class
        {
            string pathPattern = this.OriginalRecord.Path + PathSep;
            return GetPathPattern<T>(pathPattern);
        }

        /// <summary>
        /// Find the content item of a given type whose path is this one's with the last element removed:
        /// this will usually be content items on urls like this one's minus the last element
        /// </summary>
        /// <typeparam name="T">Type of content item to get</typeparam>
        /// <returns>The content item</returns>
        protected virtual T GetPathParent<T>() where T : class
        {
            string path = this.OriginalRecord.Path;
            if (string.IsNullOrEmpty(path))
                return null;
            string pathPattern = this.OriginalRecord.Path.UpTo(PathSep);
            return GetPathPattern<T>(pathPattern).FirstOrDefault();
        }

        /// <summary>
        /// Get the content items of a given type whose path starts with a string
        /// </summary>
        /// <typeparam name="T">The type of content item to get</typeparam>
        /// <param name="pathPattern">The string the path must start with</param>
        /// <returns>The content items</returns>
        protected IEnumerable<T> GetPathPattern<T>(string pathPattern) where T : class
        {
            return Collator.Instance.Get<T, ContentItem>(iq => iq.Where(ci => ci.Path.StartsWith(pathPattern)));
        }

        /// <summary>
        /// Get the current user in the customised user type
        /// </summary>
        /// <typeparam name="T">The customised user type</typeparam>
        /// <returns>The current user</returns>
        public T GetUser<T>() where T : IUser
        {
            return (T)(LyniconSecurityManager.Current.User ?? Activator.CreateInstance(CompositeTypeManager.Instance.ExtendedTypes[typeof(User)]));
        }
    }
}
