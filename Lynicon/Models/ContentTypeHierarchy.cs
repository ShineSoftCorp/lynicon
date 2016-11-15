﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Routing;
using Lynicon.Utility;

namespace Lynicon.Models
{
    /// <summary>
    /// Information registry about content types
    /// </summary>
    public class ContentTypeHierarchy
    {
        /// <summary>
        /// Maps content type to its associated summary type
        /// </summary>
        public static Dictionary<Type, Type> SummaryTypes = new Dictionary<Type, Type>();

        /// <summary>
        /// Maps every used summary type to its base type, and that type to its base type etc. up to Summary
        /// </summary>
        public static Dictionary<Type, Type> SummaryBaseTypes = new Dictionary<Type, Type>();

        /// <summary>
        /// All content types mapped to urls, plus those manually registered
        /// </summary>
        public static List<Type> AllContentTypes = new List<Type>();

        /// <summary>
        /// Maps content type name to the content type itself
        /// </summary>
        public static Dictionary<string, Type> AllContentTypesByName = new Dictionary<string, Type>();

        /// <summary>
        /// Cache of supertypes of content types
        /// </summary>
        public static Dictionary<Type, List<Type>> ContentSubtypes = new Dictionary<Type, List<Type>>();

        /// <summary>
        /// All controller names with actions listed
        /// </summary>
        public static Dictionary<string, List<string>> ControllerActions = new Dictionary<string, List<string>>();

        /// <summary>
        /// Initialise the content type hierarchy.  Be careful this is not triggered before the route table has been
        /// generated.
        /// </summary>
        static ContentTypeHierarchy()
        {
            try
            {
                //var contentTypes = RouteTable.Routes
                //    .OfType<Route>()
                //    .Where(r => r.GetType().IsGenericType && r.GetType().GetGenericTypeDefinition() == typeof(DataRoute<>))
                //    .Select(r => r.GetType().GetGenericArguments()[0])
                //    .Distinct()
                //    .ToList();
                //contentTypes.Do(t => RegisterType(t));

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                List<Type> controllerTypes = new List<Type>();
                foreach (Assembly ass in assemblies)
                {
                    if (ass.FullName.StartsWith("System."))
                        continue;
                    try
                    {
                        foreach (Type t in ass.GetTypes())
                            if (typeof(Controller).IsAssignableFrom(t))
                                controllerTypes.Add(t);
                    }
                    catch { }
                }

                ControllerActions = controllerTypes
                    .Select(t => new
                        {
                            ControllerName = t.Name.Replace("Controller", "").ToLower(),
                            ActionNames = t.GetMethods()
                                .Where(mi => mi.IsPublic && typeof(ActionResult).IsAssignableFrom(mi.ReturnType) && mi.GetCustomAttribute<NonPageAttribute>() == null)
                                .Select(mi => mi.Name.ToLower())
                                .ToList()
                        })
                    .Where(ti => ti.ActionNames.Count > 0)
                    .ToDictionary(ti => ti.ControllerName, ti => ti.ActionNames);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Register a type as a content type
        /// </summary>
        /// <param name="type">The content type</param>
        public static void RegisterType(Type type)
        {
            // Ignore List<T> types, these don't represent actual content types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return;

            if (AllContentTypes.Contains(type))
                return;

            AllContentTypes.Add(type);
            AllContentTypesByName.Add(type.Name, type);
            if (type.FullName != type.Name)
                AllContentTypesByName.Add(type.FullName, type);
            PropertyInfo summaryProp = type.GetProperties().FirstOrDefault(p => typeof(Summary).IsAssignableFrom(p.PropertyType));
            Type summaryType;
            if (summaryProp == null)
            {
                var summaryTypeAttribute = type.GetCustomAttribute<SummaryTypeAttribute>();

                if (summaryTypeAttribute == null)
                    summaryType = typeof(Summary);
                else
                    summaryType = summaryTypeAttribute.SummaryType;
            }
            else
                summaryType = summaryProp.PropertyType;
            SummaryTypes.Add(type, summaryType);
            AddAllParentSummaryTypes(summaryType);
            AddAllSuperTypes(type);
        }

        private static void AddToContentSubtypes(Type type, Type superType)
        {
            if (ContentSubtypes.ContainsKey(superType))
                ContentSubtypes[superType].Add(type);
            else
                ContentSubtypes.Add(superType, new List<Type> { type });
        }

        private static void AddAllSuperTypes(Type type)
        {
            Type super = type.BaseType;
            while (super != typeof(object) && super != null)
            {
                AddToContentSubtypes(type, super);
                super = super.BaseType;
            }

            foreach (Type intf in type.GetInterfaces())
                AddToContentSubtypes(type, intf);
        }


        private static void AddAllParentSummaryTypes(Type type)
        {
            if (type == typeof(Summary) || SummaryBaseTypes.ContainsKey(type))
                return;

            SummaryBaseTypes.Add(type, type.BaseType);
            AddAllParentSummaryTypes(type.BaseType);
        }

        /// <summary>
        /// Get the immediate subtype of a summary type
        /// </summary>
        /// <param name="type">a summary type</param>
        /// <returns>the subtype</returns>
        public static List<Type> GetImmediateSubtypes(Type type)
        {
            return SummaryBaseTypes.Where(kvp => kvp.Value == type).Select(kvp => kvp.Key).ToList();
        }

        /// <summary>
        /// Get all the subtypes of a list of summary type
        /// </summary>
        /// <param name="types">list of summary types</param>
        /// <returns>all the subtypes of all the summary types</returns>
        public static List<Type> GetAllSubtypes(List<Type> types)
        {
            if (types == null || types.Count == 0)
                return new List<Type>();
            else
                return types.SelectMany(t => GetAllSubtypes(GetImmediateSubtypes(t)).Concat(t)).ToList();
        }

        /// <summary>
        /// Get all the summary types which can contain an instance whose type is given
        /// </summary>
        /// <param name="summaryType">the summary type</param>
        /// <returns>the summary types that can container the summary type</returns>
        public static List<Type> GetSummaryContainers(Type summaryType)
        {
            List<Type> allSubtypes = GetAllSubtypes(new List<Type> { summaryType });
            return SummaryTypes.Where(kvp => kvp.Value == summaryType || allSubtypes.Contains(kvp.Value)).Select(kvp => kvp.Key).ToList();
        }

        /// <summary>
        /// Get a content type by name
        /// </summary>
        /// <param name="name">The name of a content type</param>
        /// <returns>The type</returns>
        public static Type GetContentType(string name)
        {
            return AllContentTypesByName.ContainsKey(name) ? AllContentTypesByName[name] : null;
        }

        /// <summary>
        /// Get a summary type by name
        /// </summary>
        /// <param name="name">The name or the full namespace name of a summary type</param>
        /// <returns>The summary type</returns>
        public static Type GetSummaryType(string name)
        {
            return SummaryTypes.Values.FirstOrDefault(t => t.FullName == name || t.Name == name);
        }

        /// <summary>
        /// Get a summary or content type by name
        /// </summary>
        /// <param name="name">The name of the summary/content type</param>
        /// <returns>The summary/content type</returns>
        public static Type GetAnyType(string name)
        {
            return GetContentType(name) ?? GetSummaryType(name) ?? ContentSubtypes.Keys.FirstOrDefault(t => t.FullName == name || t.Name == name);
        }

        /// <summary>
        /// Gets the content types which can be assigned to a type or else
        /// if the type is a container, the content types it can contain
        /// </summary>
        /// <param name="targetType">the type</param>
        /// <returns>list of types which can be assigned to / contained by a type</returns>
        public static List<Type> GetAssignableContentTypes(Type targetType)
        {
            List<Type> types = AllContentTypes.Where(t => targetType.IsAssignableFrom(t)).ToList();
            if (types.Count > 0)
                return types;

            types = AllContentTypes.Where(t => Collator.Instance.ContainerType(t) == targetType).ToList();
            if (types.Count > 0)
                return types;

            throw new Exception("Type " + targetType.FullName + " cannot contain any content types, it may need to be registered in LyniconConfig");
        }
    }
}
