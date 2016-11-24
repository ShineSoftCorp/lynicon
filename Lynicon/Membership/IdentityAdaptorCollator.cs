﻿using Lynicon.Collation;
using Lynicon.Utility;
using Lynicon.Repositories;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Lynicon.Membership
{
    /// <summary>
    /// IdentityAdaptorCollator can be used as part of plugin functionality to convert Lynicon to work with standard
    /// ASP.Net Identity.  It can be registered for use with the User type and converts it to a supplied inheritor
    /// of IdentityUser for consumption by an inner collator and repository built to manage interfacing with the data
    /// store for ASP.Net Identity.
    /// </summary>
    /// <typeparam name="TUser">The client code's inheritor of IdentityUser (e.g. ApplicationUser)</typeparam>
    public class IdentityAdaptorCollator<TUser, TUserManager> : AdaptorCollator<TUser, User>
        where TUser : IdentityUser, new()
        where TUserManager : UserManager<TUser>
    {
        Func<UserManager<TUser>> userManager;
        List<Tuple<PropertyInfo, PropertyInfo>> propertyInfos;
        Type extUserType;

        /// <summary>
        /// Create a new IdentityAdaptorCollator
        /// </summary>
        public IdentityAdaptorCollator(Func<TUserManager> getUserManager) : base(new BasicCollator(Repository.Instance), null, null)
        {
            this.PropertyMap = new Dictionary<string, string>();
            this.IdWriteConvert = id => "'" + id.ToString() + "'";
            this.readConvert = ReadConvert;
            this.writeConvert = WriteConvert;
            this.userManager = getUserManager;

            extUserType = CompositeTypeManager.Instance.ExtendedTypes.ContainsKey(typeof(User))
                ? CompositeTypeManager.Instance.ExtendedTypes[typeof(User)]
                : typeof(User);

            propertyInfos = new List<Tuple<PropertyInfo, PropertyInfo>>();
            foreach (PropertyInfo innerPi in typeof(TUser).GetPersistedProperties())
            {
                var outerPi = extUserType.GetProperty(innerPi.Name);
                if (outerPi != null)
                    propertyInfos.Add(Tuple.Create(outerPi, innerPi));
            }
        }

        /// <summary>
        /// Convert a Lynicon User to an ASP.Net Identity TUser
        /// </summary>
        /// <param name="u">Updated Lynicon User</param>
        /// <param name="iu">Current ASP.Net Identity TUser</param>
        /// <returns></returns>
        public virtual TUser WriteConvert(User u, TUser iu)
        {
            if (iu == null)
            {
                iu = new TUser();
                iu.SecurityStamp = Guid.NewGuid().ToString("D");
            }
                
            TUser iuOut = new TUser
            {
                AccessFailedCount = iu.AccessFailedCount,
                Email = u.Email,
                EmailConfirmed = iu.EmailConfirmed,
                Id = u.IdAsString,
                LockoutEnabled = iu.LockoutEnabled,
                LockoutEndDateUtc = iu.LockoutEndDateUtc,
                PasswordHash = iu.PasswordHash,
                PhoneNumber = iu.PhoneNumber,
                PhoneNumberConfirmed = iu.PhoneNumberConfirmed,
                SecurityStamp = iu.SecurityStamp,
                TwoFactorEnabled = iu.TwoFactorEnabled,
                UserName = u.UserName
            };

            string[] excludeProps = new string[] { "Email", "UserName", "Id", "IdAsString", "Roles" };
            CopyPropertiesForWrite(u, iuOut, propertyInfos.Where(pip => !excludeProps.Contains(pip.Item1.Name)));

            return iuOut;
        }

        protected virtual void CopyPropertiesForWrite(User outer, TUser inner, IEnumerable<Tuple<PropertyInfo, PropertyInfo>> properties)
        {
            foreach (var pis in properties)
            {
                object outerVal = pis.Item1.GetValue(outer);
                pis.Item2.SetValue(inner, outerVal);
            }
        }

        /// <summary>
        /// Convert an ASP.Net TUser to a Lynicon User
        /// </summary>
        /// <param name="iu">ASP.Net TUser</param>
        /// <returns>Lynicon User</returns>
        public virtual User ReadConvert(TUser iu)
        {
            User u = (User)Activator.CreateInstance(extUserType);
            u.Email = iu.Email;
            u.Id = new Guid(iu.Id);
            u.UserName = iu.UserName;
            u.Roles = new string(iu.Roles.Where(r => r.RoleId.Length == 1).Select(r => r.RoleId[0]).ToArray());

            string[] excludeProps = new string[] { "Email", "UserName", "Id", "IdAsString", "Roles" };
            CopyPropertiesForRead(iu, u, propertyInfos.Where(pip => !excludeProps.Contains(pip.Item1.Name)));

            return u;
        }

        protected virtual void CopyPropertiesForRead(TUser inner, User outer, IEnumerable<Tuple<PropertyInfo, PropertyInfo>> properties)
        {
            foreach (var pis in properties)
            {
                object innerVal = pis.Item2.GetValue(inner);
                pis.Item1.SetValue(outer, innerVal);
            }
        }

        protected override bool SetInner(Address a, TUser current, User data, Dictionary<string, object> setOptions)
        {
            // ensure record exists by calling base first
            bool wasAdded = base.SetInner(a, current, data, setOptions);

            var um = userManager();

            // Fix up roles through usermanager

            foreach (char lynRole in data.Roles)
            {
                string sLynRole = lynRole.ToString();

                if (wasAdded || !current.Roles.Any(iur => iur.RoleId == sLynRole))
                    um.AddToRole(data.Id.ToString(), sLynRole);
            }

            if (!wasAdded)
            {
                foreach (string idRole in current.Roles.Select(iur => iur.RoleId))
                {
                    if (!data.Roles.Contains(idRole))
                        um.RemoveFromRole(current.Id, idRole);
                }
            }

            return wasAdded;
        }
    }
}