﻿using System;
using System.Configuration;
using BtsPortal.Web.Infrastructure.Authorization;

namespace BtsPortal.Web.Infrastructure.Settings
{
    public class AppSettings
    {
        public static int PageSize => ConfigurationManager.AppSettings["Ia_Bts_PageSize"] == null ? 50 : Convert.ToInt32(ConfigurationManager.AppSettings["BtsPortal_PageSize"]);
        public static string DateTimeDisplay => "MM/dd/yyyy HH:mm";
        public static string DateDisplay => "MM/dd/yyyy";
        public static string TimeDisplay => "HH:mm";
        public static int CommandTimeout => 600;
        public static int MaxDataLength => 40000;
        public static int MaxBtsMessagesToShow => 50;
        public static string EsbExceptionDbConnectionString => ConfigurationManager.ConnectionStrings["EsbExceptionDbConnection"].ConnectionString;
        public static string SsoDbConnectionString => ConfigurationManager.ConnectionStrings["SsoDbConnection"].ConnectionString;
       public static string BtsMsgBoxDatabaseConnectionString => ConfigurationManager.ConnectionStrings["BtsMsgBoxDatabaseConnection"].ConnectionString;
        public static string BtsMgmtBoxDatabaseConnectionString => ConfigurationManager.ConnectionStrings["BtsMgmtBoxDatabaseConnection"].ConnectionString;
        public static string BamDatabaseConnectionString => ConfigurationManager.ConnectionStrings["BamDbConnection"].ConnectionString;
        public static string BtsPortalDatabaseConnectionString => ConfigurationManager.ConnectionStrings["BtsPortalDbConnection"].ConnectionString;
        public static string SsoApplicationBtdfContactEmail => "someone@microsoft.com";
        public static string EdiHomePartyName => ConfigurationManager.AppSettings["BtsPortal_Edi_HomeOrgName"];

        public static bool EnableEdi
        {
            get
            {
                if (ConfigurationManager.AppSettings["BtsPortal_EnableEdi"] == null)
                {
                    return true;
                }

                return Convert.ToBoolean(ConfigurationManager.AppSettings["BtsPortal_EnableEdi"]);
            }
        }

        public static bool EnableBam
        {
            get
            {
                if (ConfigurationManager.AppSettings["BtsPortal_EnableBam"] == null)
                {
                    return true;
                }

                return Convert.ToBoolean(ConfigurationManager.AppSettings["BtsPortal_EnableBam"]);
            }
        }

        public static PortalAuthConfigSection PortalAuth => ConfigurationManager.GetSection(PortalAuthConfigSection.SECTION_NAME) as PortalAuthConfigSection;
    }
}