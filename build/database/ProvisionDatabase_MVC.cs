﻿using EPiServer.Cms.Shell;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Personalization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections;
using System.Collections.Generic;
using System.Web.Security;

namespace AlloyTemplates.App_Code
{
    /// <summary>
    /// Provision the database for easier development by:
    ///  * Enabling project mode
    ///  * Adding some default users
    ///
    /// This file is preferably deployed in the App_Code folder, where it will be picked up and executed automatically.
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ProvisionDatabase : IInitializableModule
    {
        UIUserProvider _userProvider;
        UIRoleProvider _roleProvider;

        public void Initialize(InitializationEngine context)
        {
            // Assume that everything is setup if the WebAdmins role has been created
            if (UIRoleProvider.RoleExists("WebAdmins"))
            {
                return;
            }

            AddUsersAndRoles(context.Locate.Advanced.GetInstance<IContentSecurityRepository>());
        }

        public void Uninitialize(InitializationEngine context) { }

        #region Users and Roles

        private void AddUsersAndRoles(IContentSecurityRepository securityRepository)
        {
            var password = "sparr0wHawk";

            AddRole("WebAdmins", AccessLevel.FullAccess, securityRepository);
            AddRole("WebEditors", AccessLevel.FullAccess ^ AccessLevel.Administer, securityRepository);

            AddUser("cmsadmin", "Administrator Administrator", password, new[] { "WebEditors", "WebAdmins" });
            AddUser("abbie", "Abbie Andrea", password, new[] { "WebEditors", "WebAdmins" });
            AddUser("eddie", "Eddie Elridge", password, new[] { "WebEditors" });
            AddUser("erin", "Erin Ehrhardt", password, new[] { "WebEditors" });
            AddUser("reid", "Reid Rezac", password, new[] { "WebEditors" });
        }

        private void AddUser(string userName, string fullName, string passWord, string[] roleNames)
        {
            if (UIUserProvider.GetUser(userName) == null)
            {
                var email = string.Format("epic-{0}@mailinator.com", userName);
                IEnumerable<string> erros;
                UIUserCreateStatus status;
                var user = UIUserProvider.CreateUser(userName, passWord, email, null, null, true, out status, out erros);
                UIRoleProvider.AddUserToRoles(user.Username, roleNames);

                var profile = EPiServerProfile.Get(user.Username);
                var nameParts = fullName.Split(' ');
                profile["DisplayName"] = fullName;
                profile["FirstName"] = nameParts[0];
                profile["LastName"] = nameParts[1];
                // E-mail must be part of profile properties to be resolved by QueryableNotificationUsersImpl
                profile["Email"] = email;
                profile.Save();
            }
        }

        private void AddRole(string roleName, AccessLevel accessLevel, IContentSecurityRepository securityRepository)
        {
            if (!UIRoleProvider.RoleExists(roleName))
            {
                UIRoleProvider.CreateRole(roleName);

                var permissions = (IContentSecurityDescriptor)securityRepository.Get(ContentReference.RootPage).CreateWritableClone();
                permissions.AddEntry(new AccessControlEntry(roleName, accessLevel));

                securityRepository.Save(ContentReference.RootPage, permissions, SecuritySaveType.Replace);
                securityRepository.Save(ContentReference.WasteBasket, permissions, SecuritySaveType.Replace);
            }
        }


        public UIUserProvider GetDeafultUserProvider()
        {
            UIUserProvider userProvider = null;
            try
            {
                // Owin is not configured that becuase we have catch (membership provider  there is not problem)
                ServiceLocator.Current.TryGetExistingInstance<UIUserProvider>(out userProvider);
                return userProvider;
            }
            catch { }

            // in the case of aspnet identity the rpovider is not in the service locator before owin is sets up then we create own.
            var userManager = new ApplicationUserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            userProvider = new ApplicationUserProvider<ApplicationUser>(() => userManager);
            return userProvider;
        }

        public UIRoleProvider GetDeafultRoleProvider()
        {
            UIRoleProvider roleProvider = null;
            try
            {
                // Owin is not configured that becuase we have catch (membership provider  there is not problem)
                ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out roleProvider);
                return roleProvider;
            }
            catch { }

            // in the case of aspnet identity the rpovider is not in the service locator before owin is sets up then we create own.
            var userManager = new ApplicationUserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            var roleManager = new ApplicationRoleManager<ApplicationUser>(new RoleStore<IdentityRole>(new ApplicationDbContext<ApplicationUser>("EPiServerDB")));
            roleProvider = new ApplicationRoleProvider<ApplicationUser>(() => userManager, () => roleManager);
            return roleProvider;
        }

        UIUserProvider UIUserProvider
        {
            get
            {
                return _userProvider ?? (_userProvider = GetDeafultUserProvider());
            }
        }

        UIRoleProvider UIRoleProvider
        {
            get
            {
                return _roleProvider ?? (_roleProvider = GetDeafultRoleProvider());
            }
        }

        #endregion
    }
}
