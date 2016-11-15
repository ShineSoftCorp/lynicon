﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Lynicon.Utility;
using Lynicon.Attributes;
using Lynicon.Collation;

namespace Lynicon.Models
{
    /// <summary>
    /// Represents a stored state of all the content classes on the site, and provides methods to compare the stored state to
    /// the current state and report any dangerous changes.
    /// </summary>
    public class ContentModelSchema
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Build a ContentModelSchema from the current state of the content types
        /// </summary>
        /// <returns>A ContentModelSchema storing the current state</returns>
        public static ContentModelSchema Build()
        {
            var schema = new ContentModelSchema();
            var contentPersistenceTypes = ContentTypeHierarchy.AllContentTypes
                .Where(ct => Collator.Instance.Registered(ct) is ContentCollator);
            foreach (Type t in contentPersistenceTypes)
                schema.AddContentType(t);
            foreach (var kvp in ContentTypeHierarchy.SummaryTypes)
                schema.AddSummaryType(kvp.Key, kvp.Value);
            return schema;
        }

        Dictionary<string, Dictionary<string, string>> typeProperties = new Dictionary<string,Dictionary<string,string>>();
        /// <summary>
        /// Dictionary where the key is the type name and the value is a dictionary of property name keys with type name values
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> TypeProperties
        {
            get { return typeProperties; }
            set { typeProperties = value; }
        }

        List<string> contentTypes = new List<string>();
        /// <summary>
        /// List of names of all the content types
        /// </summary>
        public List<string> ContentTypes
        {
            get { return contentTypes; }
            set { contentTypes = value; }
        }

        Dictionary<string, string> summaryTypes = new Dictionary<string, string>();
        /// <summary>
        /// Dictionary where the key is the content type name and the value is the summary type name
        /// </summary>
        public Dictionary<string, string> SummaryTypes
        {
            get { return summaryTypes; }
            set { summaryTypes = value; }
        }

        Dictionary<string, Dictionary<string, string>> summaryMap = new Dictionary<string, Dictionary<string, string>>();
        /// <summary>
        /// Dictionary where the key is a summary type name and the value is a dictionary where the key is a property
        /// name and the value the property's type's name, and the properties listed are those properties on the content type
        /// with an attached SummaryAttribute.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> SummaryMap
        {
            get { return summaryMap; }
            set { summaryMap = value; }
        }

        /// <summary>
        /// Add all the details for a content type to this
        /// </summary>
        /// <param name="t">The content type</param>
        public void AddContentType(Type t)
        {
            ContentTypes.Add(t.FullName);
            AddTypeInner(t);
            
        }

        /// <summary>
        /// Add all the details for a summary type to this
        /// </summary>
        /// <param name="content">The content type</param>
        /// <param name="t">Ths summary type for the content type</param>
        public void AddSummaryType(Type content, Type t)
        {
            SummaryTypes.Add(content.FullName, t.FullName);
            AddTypeInner(t);
            AddSummaryMap(content);
        }
        private void AddTypeInner(Type t)
        {
            if (t.GetInterface("IList") != null)
                t = ReflectionX.ElementType(t);

            if (TypeProperties.ContainsKey(t.FullName))
                return;

            var typeDict = new Dictionary<string, string>();
            TypeProperties.Add(t.FullName, typeDict);
            foreach (var prop in t.GetPersistedProperties())
            {
                if (typeDict.ContainsKey(prop.Name))
                {
                    log.Error("Property " + prop.Name + " repeated on type " + t.FullName);
                    continue;
                }
                typeDict.Add(prop.Name, prop.PropertyType.FullName);
                if (!prop.PropertyType.FullName.StartsWith("System."))
                    AddTypeInner(prop.PropertyType);
            }
        }

        private void AddSummaryMap(Type t)
        {
            // TO DO Handle multiple content types using same summary type
            var sta = t.GetCustomAttribute<SummaryTypeAttribute>();
            if (sta != null)
            {
                if (SummaryMap.ContainsKey(sta.SummaryType.FullName))
                    return;
                var map = new Dictionary<string, string>();
                foreach (var prop in t.GetPersistedProperties())
                {
                    var sa = prop.GetCustomAttribute<SummaryAttribute>();
                    if (sa != null)
                        map.Add(prop.Name, sa.SummaryProperty ?? prop.Name);
                }
                if (map.Count > 0)
                    SummaryMap.Add(sta.SummaryType.FullName, map);
            }
        }

        /// <summary>
        /// Find problems in the changes between two schemas by comparing this
        /// recorded schema to a given schema (usually the current schema)
        /// </summary>
        /// <param name="codeSchema">The current schema</param>
        /// <returns>List of potential problems arising from the differences between the schemas</returns>
        public List<ChangeProblem> FindProblems(ContentModelSchema codeSchema)
        {
            var changeProblems = new List<ChangeProblem>();

            foreach (string typeName in this.ContentTypes.Except(codeSchema.ContentTypes))
            {
                changeProblems.Add(new ChangeProblem(typeName, null, ChangeProblemType.DeletionNeeded));
            }

            var otherTypes = new Dictionary<string, Type>();

            foreach (string typeName in codeSchema.TypeProperties.Keys)
            {
                // Check its binary serializable
                Type type = ContentTypeHierarchy.GetAnyType(typeName);
                if (type == null)
                {
                    type = ReflectionX.GetLoadedType(typeName);
                    otherTypes.Add(typeName, type);
                }
                if (type != null && type.GetCustomAttribute<SerializableAttribute>() == null)
                    changeProblems.Add(new ChangeProblem(typeName, null, ChangeProblemType.NotBinarySerializable));
            }

            foreach (string typeName in codeSchema.ContentTypes)
            {
                // Check it doesn't initialise with a null value for an object or list property
                Type type = ContentTypeHierarchy.GetAnyType(typeName);
                if (type == null)
                    type = otherTypes[typeName];
                PropertyInfo nullProperty = new NoNullObjectCheck().Run(type);
                // Exception for UniqueId of Summary
                if (nullProperty != null && !(nullProperty.DeclaringType == typeof(Summary) && nullProperty.Name == "UniqueId"))
                    changeProblems.Add(new ChangeProblem(nullProperty.ReflectedType.FullName, nullProperty.Name, ChangeProblemType.NullObjectValue));
            }

            // Types existing in both code and data schemas
            foreach (string typeName in codeSchema.TypeProperties.Keys.Intersect(TypeProperties.Keys))
            {
                bool isSummary = ContentTypeHierarchy.GetSummaryType(typeName) != null;
                if (isSummary && !this.SummaryMap.ContainsKey(typeName))
                {
                    log.Warn("Unused summary type: " + typeName);
                    continue;
                }
                foreach (string deletedProp in TypeProperties[typeName].Keys.Except(codeSchema.TypeProperties[typeName].Keys))
                {
                    if (isSummary)
                    {
                        // Test for when a property is dropped from the summary which was mapped to the content type
                        // In this case the property data needs copied from the Summary db field to the Content one
                        if (this.SummaryMap[typeName].ContainsValue(deletedProp)
                            && this.SummaryTypes.ContainsValue(typeName))
                        {
                            var oldContentProp = this.SummaryMap[typeName].Single(kvp => kvp.Value == deletedProp).Key;
                            bool anyContentTypeHadProperty = this.SummaryTypes
                                .Where(kvp => kvp.Value == typeName)
                                .Any(kvp => TypeProperties[kvp.Key].ContainsKey(oldContentProp));
                            if (anyContentTypeHadProperty)
                                changeProblems.Add(new ChangeProblem(typeName, deletedProp, ChangeProblemType.PropertyDroppedFromSummary));
                        }
                    }
                    else
                    {
                        changeProblems.Add(new ChangeProblem(typeName, deletedProp, ChangeProblemType.PropertyDropped));
                    }
                }

                foreach (string addedProp in codeSchema.TypeProperties[typeName].Keys.Except(TypeProperties[typeName].Keys))
                {
                    if (isSummary)
                    {
                        // Test for when a property is added to the summary which is mapped from the content type
                        // In this case the data needs copied from the Content db field to the Summary
                        if (codeSchema.SummaryMap[typeName].ContainsValue(addedProp)
                            && codeSchema.SummaryTypes.ContainsValue(typeName))
                        {
                            var newContentProp = codeSchema.SummaryMap[typeName].Single(kvp => kvp.Value == addedProp).Key;
                            var anyContentTypeContainedProp = codeSchema.SummaryTypes
                                .Where(kvp => kvp.Value == typeName)
                                .Any(kvp => TypeProperties[kvp.Key].ContainsKey(newContentProp));
                            if (anyContentTypeContainedProp)
                                changeProblems.Add(new ChangeProblem(typeName, addedProp, ChangeProblemType.PropertyAddedToSummary));
                        }
                    }
                }
            }

            return changeProblems;
        }

        /// <summary>
        /// Reverts part of the schema relevant to a problem to the state which when compared to its current
        /// state, regenerates the problem. The inverse of ResolveProblem.
        /// </summary>
        /// <param name="oldSchema">the old schema which generated the problem when compared to the current schema</param>
        /// <param name="problem">the problem it generated</param>
        public void ApplyProblem(ContentModelSchema oldSchema, ChangeProblem problem)
        {
            switch (problem.ProblemType)
            {
                case ChangeProblemType.DeletionNeeded:
                    // copy the deleted type to the new schema
                    TypeProperties.Add(problem.TypeName, oldSchema.TypeProperties[problem.TypeName]);
                    break;
                case ChangeProblemType.PropertyDropped:
                case ChangeProblemType.PropertyDroppedFromSummary:
                    // copy the dropped property to the new schema
                    TypeProperties[problem.TypeName].Add(problem.PropertyName, oldSchema.TypeProperties[problem.TypeName][problem.PropertyName]);
                    break;

                // Problems not requiring data modification
                case ChangeProblemType.NotBinarySerializable:
                case ChangeProblemType.NullObjectValue:
                    break;
            }
        }

        /// <summary>
        /// Updates this schema so it can no longer generate a given problem when compared to another schema.
        /// </summary>
        /// <param name="problem">The change problem</param>
        public void ResolveProblem(ChangeProblem problem)
        {
            switch (problem.ProblemType)
            {
                case ChangeProblemType.DeletionNeeded:
                    // Remove the problem for which deletion was needed
                    TypeProperties.Remove(problem.TypeName);
                    break;
                case ChangeProblemType.PropertyDropped:
                case ChangeProblemType.PropertyDroppedFromSummary:
                    // Remove the property which was dropped
                    TypeProperties[problem.TypeName].Remove(problem.PropertyName);
                    break;

                // Problems not requiring data modification
                case ChangeProblemType.NotBinarySerializable:
                case ChangeProblemType.NullObjectValue:
                    break;
            }
        }

    }
}
