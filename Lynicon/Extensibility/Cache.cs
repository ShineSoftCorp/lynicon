﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;
//using System.Web.Mvc;
using System.Web.Script.Serialization;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Serialization mode for a cache
    /// </summary>
    public enum CacheSerialization
    {
        Binary,
        Json
    }

    /// <summary>
    /// Shared functionality for a module which has or is a cache
    /// </summary>
    public abstract class Cache : Module
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Utility method to make a binary copy of an item in the cache so that it can be modified without
        /// modifying the version in the cache causing problems for future readers of that item
        /// </summary>
        /// <typeparam name="T">The type of the cache item</typeparam>
        /// <param name="cacheItem">The cache item</param>
        /// <returns>Copy of the cache item</returns>
        public static T CopyCacheItem<T>(T cacheItem) where T : class
        {
            // take advantage of binary serializability of all cached data to do a deep copy which
            // will always work if caching itself works.
            var formatter = new BinaryFormatter();
            using (var memstream = new MemoryStream())
            {
                formatter.Serialize(memstream, cacheItem);
                memstream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(memstream) as T;
            }
        }

        /// <summary>
        /// flag to stop load loading the cache with what's in the cache
        /// </summary>
        protected string CacheBlockKey
        {
            get { return "lyn_cacheblock_" + this.Name; }
        }

        /// <summary>
        /// Flag as to whether the cache is blocked i.e. it should not intercept read or write requests
        /// </summary>
        /// <returns>True if blocked</returns>
        public bool CacheBlocked()
        {
            return (RequestThreadCache.Current.Contains(CacheBlockKey) && (bool)RequestThreadCache.Current[CacheBlockKey]);
        }

        /// <summary>
        /// Test whether a content type is currently cached in a total cache
        /// Allows for efficiencies elsewhere
        /// </summary>
        /// <param name="containerType">The container type to test for being cached</param>
        /// <param name="summaryOnly">If true, we only want to know if summaries will be cached: if false the whole record must be cached</param>
        /// <returns>True if the type is cached</returns>
        public static bool IsTotalCached(Type containerType, bool summaryOnly)
        {
            foreach (var cache in LyniconModuleManager.Instance.Modules.Values.OfType<Cache>())
            {
                string[] nameWords = cache.Name.Split('.');
                if (nameWords.Contains("Caching") && nameWords.Contains("Full"))
                {
                    if (summaryOnly || nameWords.Contains("Items"))
                        return cache.ApplyToType(containerType) && !cache.CacheBlocked();
                }
            }

            return false;
        }

        /// <summary>
        /// Function to test for whether the given type should be handled by the cache module (true if yes)
        /// </summary>
        public Func<Type, bool> ApplyToType { get; set; }

        /// <summary>
        /// Estimate of current number of bytes taken by cache
        /// </summary>
        public long? MemoryBytes { get; set; }

        /// <summary>
        /// Serialization mode for cache
        /// </summary>
        public CacheSerialization SerializationMode { get; set; }

        /// <summary>
        /// Number of items in cache
        /// </summary>
        /// <returns>Number of items in cache</returns>
        public abstract int GetCount();

        /// <summary>
        /// Method to load the cache from the primary data source
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Method to store the cache locally to the file system for rapid restart
        /// </summary>
        public virtual void Dump()
        { }

        /// <summary>
        /// Construct a module with caching
        /// </summary>
        /// <param name="name">Name of the module</param>
        /// <param name="dependentOn">List of names of other modules this is dependent on (these will start up before this one)</param>
        public Cache(string name, params string[] dependentOn) : base(name, dependentOn)
        {
            SerializationMode = CacheSerialization.Binary;
        }
        
        /// <summary>
        /// Attempt to load the dump file from the file system into the cache
        /// </summary>
        /// <typeparam name="T">The type of the cache object</typeparam>
        /// <param name="appDataPath">The path to the file containing the dump file relative to the App_Data folder</param>
        /// <returns>A cache object created from the dumped file</returns>
        public T TryLoadFromBinaryFile<T>(string appDataPath) where T : class
        {
            FileInfo fi = new FileInfo(Paths.AppDataPath + "\\" + appDataPath);
            T cache = null;
            if (fi.Exists)
            {
                try
                {
                    log.Info(this.GetType().Name + ": START Loading dump");

                    switch (SerializationMode)
                    {
                        case CacheSerialization.Binary:
                            var formatter = new BinaryFormatter();
                            using (var stream = fi.OpenRead())
                            {
                                MemoryBytes = stream.Length;
                                cache = formatter.Deserialize(stream) as T;
                            }
                            break;
                        case CacheSerialization.Json:
                            var sz = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
                            using (var stream = fi.OpenText())
                            {
                                MemoryBytes = stream.BaseStream.Length;
                                cache = (T)sz.Deserialize(stream, typeof(T));
                            }
                            break;
                    }
                    

                    log.Info(this.GetType().Name + ": END Loading dump");
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            return cache;
        }

        /// <summary>
        /// Write the cache to a dump file
        /// </summary>
        /// <typeparam name="T">The type of the cache object</typeparam>
        /// <param name="appDataPath">The path of the dump file relative to the App_Data folder</param>
        /// <param name="cache">The cache object to write</param>
        public void SaveToBinaryFile<T>(string appDataPath, T cache) where T : class
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Paths.AppDataPath + "\\" + appDataPath.UpToLast("\\"));
                if (!di.Exists)
                    di.Create();
                FileInfo fi = new FileInfo(Paths.AppDataPath + "\\" + appDataPath);
                switch (SerializationMode)
                {
                    case CacheSerialization.Binary:
                        var formatter = new BinaryFormatter();
                        using (var stream = fi.OpenWrite())
                        {
                            formatter.Serialize(stream, cache);
                        }
                        break;
                    case CacheSerialization.Json:
                        var sz = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
                        using (var stream = fi.CreateText())
                        {
                            sz.Serialize(stream, cache);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error("Error saving cache dump: ", ex);
            }
            
        }
    }
}
