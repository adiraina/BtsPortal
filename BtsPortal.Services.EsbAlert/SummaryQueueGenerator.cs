﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using BtsPortal.Cache;
using BtsPortal.Entities.Esb;
using BtsPortal.Helpers;
using BtsPortal.Repositories.Db;
using BtsPortal.Repositories.Interface;

namespace BtsPortal.Services.EsbAlert
{
    class SummaryQueueGenerator : Worker
    {
        private IEsbExceptionDbRepository _esbExceptionDbRepo;
        private ICacheProvider _cacheProvider;
        string LdapRoot { get; set; }
        internal override void DoWork(ICacheProvider cacheProvider)
        {
            CanForceStop = false;
            while (!MarkAsStop)
            {
                _esbExceptionDbRepo = new EsbExceptionDbRepository(new SqlConnection(AppSettings.EsbExceptionDbConnectionString));
                _cacheProvider = cacheProvider;

                var esbConfiguration = _esbExceptionDbRepo.GetEsbConfiguration();
                int qInterval = AppSettings.DEFAULT_INTERVAL;
                bool qEnabled = Convert.ToBoolean(esbConfiguration.FirstOrDefault(m => m.Name.ToUpper() == "ISSUMMARYQUEUEENABLED")?.Value);
                LdapRoot = esbConfiguration.FirstOrDefault(m => m.Name.ToUpper() == "LDAPROOT")?.Value;
                try
                {
                    qInterval = Convert.ToInt32(esbConfiguration.FirstOrDefault(m => m.Name.ToUpper() == "QUEUESUMMARYINTERVAL")?.Value);
                }
                catch
                {
                    Process.HandleException("Invalid value of 'QUEUESUMMARYINTERVAL' in configuration settings. Using default of " + AppSettings.DEFAULT_INTERVAL, EventLogEntryType.Warning);
                }

                try
                {
                    if (qEnabled)
                    {
                        //load unprocessed faults
                        var data = _esbExceptionDbRepo.GetAlertSummaryNotifications();
                        try
                        {
                            //process the faults
                            ProcessFaults(data);
                        }
                        catch (Exception exception)
                        {
                            Process.HandleException(exception);
                        }
                    }
                    else
                    {
                        EventLog.WriteEntry(AppSettings.SERVICE_NAME, $"Summary queue has not been enabled. Please enable for generating summary alerts. Key name : ISSUMMARYQUEUEENABLED ", EventLogEntryType.Warning);
                    }
                }
                finally
                {
                    CanForceStop = true;
                    try
                    {
                        if (!MarkAsStop)
                        {
                            //Sleep
                            System.Threading.Thread.Sleep(qInterval);
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        //Sleep five seconds
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }
        }

        private void ProcessFaults(AlertNotification data)
        {
            List<Guid> alerts = data.AlertFaultSummaries.Select(m => m.AlertId).Distinct().ToList();
            List<AlertEmailNotification> notifications = new List<AlertEmailNotification>();
            foreach (var alertId in alerts)
            {
                var alertSummary = data.AlertFaultSummaries.Where(m => m.AlertId == alertId).ToList();
                var alertSubscriptions = data.AlertSubscriptions.Where(m => m.AlertId == alertId).ToList();
                var alertName = data.AlertSubscriptions.FirstOrDefault(m => m.AlertId == alertId)?.Name;
                List<string> emails = GetEmails(alertSubscriptions);
                string xml = SerializationHelper.Serialize(alertSummary);
                notifications.Add(new AlertEmailNotification()
                {
                    AlertId = alertId,
                    BatchId = Guid.Empty,
                    Body = xml,
                    To = string.Join(",", emails),
                    Subject = string.Format("Esb Fault Summary - {0}", alertName),
                    IsSummaryAlert = true
                });

            }

            _esbExceptionDbRepo.InsertAlertEmail(notifications);
        }

        private List<string> GetEmails(List<EsbAlertSubscription> alertSubscriptions)
        {
            List<string> emails = new List<string>();
            foreach (var subscription in alertSubscriptions)
            {
                if (subscription.IsGroup)
                {
                    foreach (
                        SearchResult result in
                            ActiveDirectoryHelper.GetMembersOfGroup(subscription.Subscriber, _cacheProvider, LdapRoot))
                    {
                        //System.Diagnostics.Trace.WriteLine("Email notification added for fault " + fault.FaultID.ToString() + " which has InsertedDate as " + fault.InsertedDate.ToString());
                        string email = Convert.ToString(result.Properties["mail"][0]);
                        if (!emails.Contains(email))
                        {
                            emails.Add(email);
                        }
                    }
                }
                else
                {
                    //System.Diagnostics.Trace.WriteLine("Email notification added for fault " + fault.FaultID.ToString() + " which has InsertedDate as " + fault.InsertedDate.ToString());
                    string email = string.IsNullOrEmpty(subscription.CustomEmail)
                        ? ActiveDirectoryHelper.GetEmailAddress(subscription.Subscriber, _cacheProvider, LdapRoot)
                        : subscription.CustomEmail;
                    if (!emails.Contains(email) && !string.IsNullOrWhiteSpace(email))
                    {
                        emails.Add(email);
                    }
                }
            }

            return emails;
        }
    }
}
