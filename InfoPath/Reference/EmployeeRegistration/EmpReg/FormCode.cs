using Microsoft.Office.InfoPath;
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Linq;

namespace EmpReg
{
    public partial class FormCode
    {
        // Member variables are not supported in browser-enabled forms.
        // Instead, write and read these values from the FormState
        // dictionary using code such as the following:
        //
        // private object _memberVariable
        // {
        //     get
        //     {
        //         return FormState["_memberVariable"];
        //     }
        //     set
        //     {
        //         FormState["_memberVariable"] = value;
        //     }
        // }

        // NOTE: The following procedure is required by Microsoft InfoPath.
        // It can be modified using Microsoft InfoPath.

        private enum UserOpType
        {
            AddUser = 1,
            RemoveUser = 2
        }

        public void InternalStartup()
        {
            EventManager.FormEvents.Loading += new LoadingEventHandler(FormEvents_Loading);
            EventManager.XmlEvents["/my:EmployeeForm/my:ddlCountry"].Changed += new XmlChangedEventHandler(ddlCountry_Changed);
            EventManager.XmlEvents["/my:EmployeeForm/my:ddlState"].Changed += new XmlChangedEventHandler(ddlState_Changed);
            EventManager.FormEvents.Submit += new SubmitEventHandler(FormEvents_Submit);
            ((ButtonEvent)EventManager.ControlEvents["btnGetNameAndManager"]).Clicked += new ClickedEventHandler(btnGetNameAndManager_Clicked);
        }

        public void FormEvents_Loading(object sender, LoadingEventArgs e)
        {
            // Write your code here.
            //DataConnection connection = this.DataConnections["EmpDesignation"];
            //connection.Execute();
            SharePointListRWQueryConnection spsConn = (SharePointListRWQueryConnection)this.DataSources["EmpDesignation"].QueryConnection;
            spsConn.Execute();

            DataConnection connectionEmpCountry = this.DataConnections["EmpCountry"];
            connectionEmpCountry.Execute();

            DataConnection connectionSiteGroups = this.DataConnections["SiteGroupsRestService"];
            connectionSiteGroups.Execute();

            MainDataSource.CreateNavigator().SelectSingleNode("/my:EmployeeForm/my:siteUrl", NamespaceManager).SetValue(spsConn.SiteUrl.ToString());
            LoadUserSiteGroups();
        }

        private void LoadUserSiteGroups()
        {
            XPathNavigator form = this.MainDataSource.CreateNavigator();
            string siteUrl = form.SelectSingleNode("/my:EmployeeForm/my:siteUrl", NamespaceManager).Value;

            using (SPSite site = new SPSite(siteUrl))
            {
                using (SPWeb web = site.RootWeb)
                {
                    string userID = form.SelectSingleNode("/my:EmployeeForm/my:txtUserID", NamespaceManager).Value;
                    SPUser user = web.EnsureUser(userID);
                    SPGroupCollection userGroups = user.Groups;

                    if (userGroups.Count > 0)
                    {
                        XPathNavigator siteGroups = form.SelectSingleNode("//my:SiteGroups", NamespaceManager);
                        XPathNavigator siteGroup = form.SelectSingleNode("//my:SiteGroups/my:SiteGroup", NamespaceManager);
                        string prevSiteGroups = "";

                        foreach (SPGroup group in userGroups)
                        {
                            XPathNavigator newSiteGroup = siteGroup.Clone();
                            newSiteGroup.SetValue(group.ID.ToString());
                            siteGroups.AppendChild(newSiteGroup);

                            if (prevSiteGroups.Length > 0) { prevSiteGroups += ","; }
                            prevSiteGroups += group.Name;
                        }

                        form.SelectSingleNode("/my:EmployeeForm/my:prevSiteGroups", NamespaceManager).SetValue(prevSiteGroups);
                    } // if (userGroups.Count > 0)
                } // using (SPWeb web
            } // using (SPSite site
        } // private void LoadUserSiteGroups()

        public void ddlCountry_Changed(object sender, XmlEventArgs e)
        {
            XPathNavigator form = this.MainDataSource.CreateNavigator();
            FileQueryConnection conState = (FileQueryConnection)DataConnections["restwebserviceState"];

            string stateQuery = conState.FileLocation;
            if (stateQuery.IndexOf("?") > 0)
            {
                stateQuery = stateQuery.Substring(0, stateQuery.IndexOf("?"));
            }
            conState.FileLocation = stateQuery + "?$filter=CountryId eq " + e.NewValue + "&noredirect=true";
            conState.Execute();
        }

        public void ddlState_Changed(object sender, XmlEventArgs e)
        {
            XPathNavigator form = this.MainDataSource.CreateNavigator();
            XPathNavigator ddlstateNode = form.SelectSingleNode("/my:EmployeeForm/my:ddlState", NamespaceManager);
            this.DataSources["EmpCity"].CreateNavigator().SelectSingleNode("/dfs:myFields/dfs:queryFields/q:SharePointListItem_RW/q:State", NamespaceManager).SetValue(ddlstateNode.Value);
            this.DataSources["EmpCity"].QueryConnection.Execute();

        }

        private void AddOrRemoveUserToGroup(IEnumerable<string> groups, UserOpType operationType)
        {
            XPathNavigator root = MainDataSource.CreateNavigator();
            string siteUrl = root.SelectSingleNode("/my:EmployeeForm/my:siteUrl", NamespaceManager).Value;
            string userID = root.SelectSingleNode("/my:EmployeeForm/my:txtUserID", NamespaceManager).Value;

            if (groups.Count() > 0)
            {
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.RootWeb)
                    {
                        web.AllowUnsafeUpdates = true;
                        SPUser user = web.EnsureUser(userID);

                        foreach (string group in groups)
                        {
                            SPGroup spGroup = web.Groups[group];
                            if (spGroup != null)
                            {
                                if (operationType.Equals(UserOpType.AddUser))
                                {
                                    spGroup.AddUser(user);
                                }
                                else
                                {
                                    spGroup.RemoveUser(user);
                                }
                            }
                        } //  foreach (string group
                       
                        web.AllowUnsafeUpdates = false;
                    }
                }
            } // if (groups.Count() > 0)            
        }

        private void AddUserToSelectedSiteGroups()
        {
            XPathNavigator root = MainDataSource.CreateNavigator();

            List<string> selectedGroups = new List<string>();

            XPathNodeIterator iter = root.Select("//my:EmployeeForm/my:SiteGroups/my:SiteGroup", NamespaceManager);
            string prevGroups = root.SelectSingleNode("/my:EmployeeForm/my:prevSiteGroups", NamespaceManager).Value;
            string[] prevSitegroups = new string[] { };
            string[] currSitegroups = new string[] { };

            if (prevGroups.Length > 0)
            {
                prevSitegroups = prevGroups.Split(','); // get all previously selected site groups
            }

            while (iter.MoveNext())
            {
                string value = iter.Current.Value;

                if (value != null && value != "")
                {
                    XPathNavigator siteGroups = DataSources["SiteGroupsRestService"].CreateNavigator();
                    XPathNavigator nav = siteGroups.SelectSingleNode("/ns2:feed/ns2:entry/ns2:content/m:properties/ns1:Title[../ns1:Id = '" + value + "']", NamespaceManager);

                    if (!selectedGroups.Contains(nav.Value))
                    {
                        selectedGroups.Add(nav.Value);
                    }
                }
            } //  while (iter.MoveNext())

            if (selectedGroups.Count > 0)
            {
                if (prevSitegroups.Length > 0)
                {
                    currSitegroups = selectedGroups.ToArray();

                    IEnumerable<string> deleteUserFromGroups = prevSitegroups.Except(currSitegroups);
                    IEnumerable<string> addUserToGroups = currSitegroups.Except(prevSitegroups);

                    AddOrRemoveUserToGroup(deleteUserFromGroups, UserOpType.RemoveUser);
                    AddOrRemoveUserToGroup(addUserToGroups, UserOpType.AddUser);
                }
                else // Add user to site groups if there is no previously selected groups
                {
                    AddOrRemoveUserToGroup(selectedGroups, UserOpType.AddUser);
                }
            } // if (groups.Count > 0)
            else if (prevSitegroups.Length > 0) // if enduser unselect all options, remove user from site group 
            {
                AddOrRemoveUserToGroup(prevSitegroups, UserOpType.RemoveUser);
            }

        } // private void AddUserToSiteGroups()

        public void FormEvents_Submit(object sender, SubmitEventArgs e)
        {
            // If the submit operation is successful, set
            // e.CancelableArgs.Cancel = false;
            // Write your code here.
            try
            {
                AddUserToSelectedSiteGroups();
                FileSubmitConnection conSubmit = (FileSubmitConnection)DataConnections["SharePoint Library Submit"];
                conSubmit.Execute();
                e.CancelableArgs.Cancel = false;
            }
            catch (Exception ex)
            {
                e.CancelableArgs.Message = ex.Message;
                e.CancelableArgs.Cancel = true;
            }

            this.ViewInfos.SwitchView("Thanks");

        }

        public void btnGetNameAndManager_Clicked(object sender, ClickedEventArgs e)
        {
            string firstname = string.Empty;
            string lastname = string.Empty;
            string manager = string.Empty;

            XPathNavigator form = this.MainDataSource.CreateNavigator();
            form.SelectSingleNode("/my:EmployeeForm/my:txtError", NamespaceManager).SetValue("");

            try
            {
                string userID = form.SelectSingleNode("/my:EmployeeForm/my:txtUserID", NamespaceManager).Value;
                XPathNavigator profileNav = this.DataSources["GetUserProfileByName"].CreateNavigator();
                profileNav.SelectSingleNode("/dfs:myFields/dfs:queryFields/tns:GetUserProfileByName/tns:AccountName", NamespaceManager).SetValue(userID);

                WebServiceConnection webServiceConnection = (WebServiceConnection)this.DataConnections["GetUserProfileByName"];
                webServiceConnection.Execute();

                string profileXPath = "/dfs:myFields/dfs:dataFields/tns:GetUserProfileByNameResponse/tns:GetUserProfileByNameResult/tns:PropertyData/tns:Values/tns:ValueData/tns:Value[../../../tns:Name = '{0}']";

                if (profileNav.SelectSingleNode(string.Format(profileXPath, "FirstName"), NamespaceManager) != null)
                {
                    firstname = profileNav.SelectSingleNode(string.Format(profileXPath, "FirstName"), NamespaceManager).Value;
                }
                if (profileNav.SelectSingleNode(string.Format(profileXPath, "LastName"), NamespaceManager) != null)
                {
                    lastname = profileNav.SelectSingleNode(string.Format(profileXPath, "LastName"), NamespaceManager).Value;
                }
                if (profileNav.SelectSingleNode(string.Format(profileXPath, "Manager"), NamespaceManager) != null)
                {
                    manager = profileNav.SelectSingleNode(string.Format(profileXPath, "Manager"), NamespaceManager).Value;
                }

                string userName = string.Format("{0} {1}", firstname, lastname);

                form.SelectSingleNode("/my:EmployeeForm/my:txtName", NamespaceManager).SetValue(userName);
                form.SelectSingleNode("/my:EmployeeForm/my:txtManager", NamespaceManager).SetValue(manager);

            }
            catch (Exception ex)
            {
                form.SelectSingleNode("/my:EmployeeForm/my:txtError", NamespaceManager).SetValue(ex.Message);
            }
        }
       
    }
}
