﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EmployeeRegistration.MVCWeb.Models
{
    public enum UserOpType
    {
        AddUser = 1,
        RemoveUser = 2
    }

    public class Skill
    {
        public Skill()
        {
            Technology = "";
            Experience = "";
        }

        public string Technology { get; set; }
        public string Experience { get; set; }
    }

    public class EmpAttachment
    {
        public string FileRelativeUrl { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
    }

    public class SiteGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Checked { get; set; }
    }

    public class Employee
    {
        public Employee()
        {
            Skills = new List<Skill>();
        }
        public string Id { get; set; }
        public string Name { get; set; }

        [DisplayName("Emp Number")]
        public string EmpNumber { get; set; }

        public string Designation { get; set; }
        public SelectList Designations { get; set; }

        public string Location { get; set; }
        public List<Skill> Skills { get; set; }

        [DisplayName("User ID")]
        public string UserID { get; set; }
        [DisplayName("Emp Manager")]
        public string EmpManager { get; set; }

        public string CountryID { get; set; }
        public SelectList Countries { get; set; }
        public string StateID { get; set; }

        public int SkillsCount { get; set; }

        public string ActionName { get; set; }

        public string SubmitButtonName { get; set; }

        [DisplayName("Attachments")]
        public List<EmpAttachment> Attachments { get; set; }
        public int AttachmentsCount { get; set; }
        public string AttachmentID { get; set; }
        public bool isFileUploaded { get; set; }

        public int[] PreviouslySelectedSiteGroups { get; set; }
        public int PreviouslySelectedSiteGroupsCount { get; set; }
        [DisplayName("Add User To Site Groups")]
        public List<SiteGroup> SiteGroups { get; set; }
        public int SiteGroupsCount { get; set; }
    }
}