using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;

/// <summary>
/// Summary description for SMTGeneva
/// </summary>

    public class SMTGeneva
    {
        public string genevaEnv = "";
        public string genevaUrl = "";
        public string genevaHostName = "";
        public int genevaPort = 0;
        public int genevaTimeOut = 0;
        public string genevaUser = "";
        public string genevaPassword = "";
        public string genevaFlags = "";
        private string loaderFlags = "";
        private string XMLloaderFlags = "";

        public string ProxyURL = "http://127.0.0.1";
        public bool ProxyByPassOnLocal = true;

        public void LoadConnectionSettings(/*ref SMTGeneva sGVA, */string env, string gvaUser = "", string gvaPassword = "")
        {
            LogEvent("LoadConnectionSettings: " + env + ", " + gvaUser + ", " + gvaPassword, EventLogEntryType.Information);


            genevaEnv = env;
            genevaUrl = ConfigurationManager.AppSettings[env + "_genevaUrl"];
            genevaHostName = ConfigurationManager.AppSettings[env + "_genevaHostName"];
            genevaPort = Convert.ToInt32(ConfigurationManager.AppSettings[env + "_genevaPort"]);
            genevaTimeOut = Convert.ToInt32(ConfigurationManager.AppSettings[env + "_genevaTimeOut"]);
            genevaUser = ConfigurationManager.AppSettings[env + "_genevaUser"];
            genevaPassword = ConfigurationManager.AppSettings[env + "_genevaPassword"];
            genevaFlags = ConfigurationManager.AppSettings[env + "_genevaFlags"];
            XMLloaderFlags = ConfigurationManager.AppSettings[env + "_XMLloaderFlags"];
            loaderFlags = ConfigurationManager.AppSettings[env + "_loaderFlags"];
            
            ProxyURL = ConfigurationManager.AppSettings["ProxyURL"];
            ProxyByPassOnLocal = Convert.ToBoolean(ConfigurationManager.AppSettings["ProxyByPassOnLocal"]);
                        
            // If caller supplies ID/Password then use those
            if (!String.IsNullOrEmpty(gvaUser) || !String.IsNullOrEmpty(gvaPassword))
            {
                genevaUser = gvaUser;
                genevaPassword = gvaPassword;
            }
        }

        public GVAResult submitGenevaRSLRequest(string rslFileName, string rslParameters)
        {
            object results = new object();
            GenevaSOAP.reportResultsPortfolioStruct RSLresults = new GenevaSOAP.reportResultsPortfolioStruct();

            GVAResult gvaResult = new GVAResult();
            StringBuilder sb = new StringBuilder();

            try
            {

                string runrepSessionID;
                GenevaSOAP.Service webServ = new GenevaSOAP.Service();
                webServ.Url = genevaUrl;
                webServ.Timeout = genevaTimeOut;

                //webServ.Proxy = new System.Net.WebProxy(ProxyURL, ProxyByPassOnLocal) { Credentials = System.Net.CredentialCache.DefaultCredentials };

                runrepSessionID = webServ.StartCallableRunrep(genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags);
                webServ.RunCallableRunrepReadFile(runrepSessionID, rslFileName);

                /* This function returns unreliable results!
                 * better option is to return xml format usring RunCallableRunrepRunReportString
                 */
                RSLresults = webServ.RunCallableRunrepRunReport(runrepSessionID, rslFileName, rslParameters);
                webServ.ShutdownCallableSession(runrepSessionID);


                foreach (GenevaSOAP.reportResultsStruct rs in RSLresults.results)
                {
                    List<String> row = new List<string>();

                    //header
                    for (int i = 0; i < rs.record[0].field.Count(); i++)
                    {
                        row.Add(rs.record[0].field[i].name);
                    }
                    sb.AppendLine(String.Join(",", row.ToArray()));

                    //text
                    foreach (GenevaSOAP.reportResultsRecordStruct record in rs.record)
                    {
                        row = new List<string>();
                        foreach (GenevaSOAP.reportResultsVectorElement field in record.field)
                        {
                            row.Add(field.value);
                        }
                        sb.AppendLine(String.Join(",", row.ToArray()));
                    }
                }
                gvaResult.CSV = sb.ToString();
                gvaResult.OK = true;
            }
            catch (Exception ex)
            {
                //results = "<Message>" + e.Message + "</Message>";
                //return results;
                gvaResult.Message = ex.Message;
                gvaResult.Status = "Failure";
                gvaResult.OK = false;


                LogEvent(String.Format("GenevaDAL submitGenevaRSLRequest Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                        ex.Message, ex.StackTrace), EventLogEntryType.Error);
            }

            return gvaResult;
        }


        public GVAResult submitGenevaRSLRequestString(string rslFileName, string rslParameters)
        {

            LogEvent("submitGenevaRSLRequestString -  RSL:" + rslFileName + ", parameters:" + rslParameters, EventLogEntryType.Information);

            GVAResult gr = new GVAResult();
            string results = "";
            DataSet ds = new DataSet();
            gr.Message = "";
            gr.Status = "OK";

            gr.CSV = "";


            //Sometimes the session ID fails. Try and repeat a few times before failing.
            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    string runrepSessionID;
                    GenevaSOAP.Service webServ = new GenevaSOAP.Service();
                    webServ.Url = genevaUrl;
                    webServ.Timeout = genevaTimeOut;

                    //webServ.Proxy = new System.Net.WebProxy(ProxyURL, ProxyByPassOnLocal) { Credentials = System.Net.CredentialCache.DefaultCredentials };


                    //Start the session
                    LogEvent(String.Format("submitGenevaRSLRequestString\r\n\r\nStartSession\r\nPort: {0}\r\nHostname: {1}\r\nUser: {2}\r\nPassword: {3}\r\nFlags: {4}",
                        genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags), EventLogEntryType.Information);
                    runrepSessionID = webServ.StartCallableRunrep(genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags);
                    //Read the rsl file
                    LogEvent(String.Format("submitGenevaRSLRequestString\r\n\r\nReadRSL\r\nSessionID: {0}\r\nRSL: {1}",
                        runrepSessionID, rslFileName), EventLogEntryType.Information);
                    webServ.RunCallableRunrepReadFile(runrepSessionID, rslFileName);
                    //run the rsl
                    LogEvent(String.Format("submitGenevaRSLRequestString\r\n\r\nRunRSL\r\nSessionID: {0}\r\nRSL: {1}\r\nParameters: {2}",
                        runrepSessionID, rslFileName, rslParameters), EventLogEntryType.Information);
                    results = webServ.RunCallableRunrepRunReportString(runrepSessionID, rslFileName, rslParameters);

                    LogEvent("submitGenevaRSLRequestString Read results", EventLogEntryType.Information);

                    StringReader sr = new StringReader(results);
                    ds.ReadXml(sr);

                    LogEvent("submitGenevaRSLRequestString Results ready. Start shutdown sessionID " + runrepSessionID, EventLogEntryType.Information);

                    //Shutdown
                    webServ.ShutdownCallableSession(runrepSessionID);
                    gr.OK = true;

                    LogEvent("submitGenevaRSLRequestString Complete", EventLogEntryType.Information);

                    break;

                }
                catch (Exception ex)
                {
                    gr.Message += " " + ex.Message;
                    gr.Status = "Failure";

                    LogEvent(String.Format("GenevaDAL submitGenevaRSLRequestString Failure #{0}\r\nMessage: {1}\r\nStackTrace\r\n{2}",
                            i.ToString(), ex.Message, ex.StackTrace), EventLogEntryType.Error);
                }
            }

            gr.Data = ds;
            gr.Data = removeHiddenColumnsFromDataSet(ds);

            //add csv format
            gr.CSV = convertGVAResultDataToCSV(gr, true);
            return gr;

        }

        public GVAResult RunSQLSelect(string query)
        {
            GVAResult gr = new GVAResult();

            DataSet ds = new DataSet();

            GenevaSOAP.Service webServ = new GenevaSOAP.Service();
            //Start the session
            string runrepSessionID = webServ.StartCallableRunrep(genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags);

            string results = webServ.RunCallableRunrepRunSelectString(runrepSessionID, query, string.Empty);
            StringReader sr = new StringReader(results);
            ds.ReadXml(sr);

            //Shutdown
            webServ.ShutdownCallableSession(runrepSessionID);
            gr.OK = true;

            gr.Data = removeHiddenColumnsFromDataSet(ds);

            //ds.ReadXml(s);

            gr.CSV = convertGVAResultDataToCSV(gr, true);

            return gr;
        }


        public String convertGVAResultDataToCSV(GVAResult gvaResult, bool showHeader)
        {
            // **** don't show hidden columns. *****

            StringBuilder sb = new StringBuilder();

            try
            {

                if (showHeader)
                {
                    List<string> headers = new List<string>();
                    foreach (DataColumn col in gvaResult.Data.Tables[gvaResult.Data.Tables.Count - 1].Columns)
                    {
                        if (col.ColumnMapping != MappingType.Hidden)
                        {
                            headers.Add(col.ColumnName);
                        }
                    }
                    sb.AppendLine(string.Join(",", headers.ToArray()));
                }

                foreach (DataRow row in gvaResult.Data.Tables[gvaResult.Data.Tables.Count - 1].Rows)
                {
                    List<string> r = new List<string>();
                    foreach (DataColumn col in gvaResult.Data.Tables[gvaResult.Data.Tables.Count - 1].Columns)
                    {
                        if (col.ColumnMapping != MappingType.Hidden)
                        {
                            r.Add(row[col].ToString());
                        }
                    }
                    sb.AppendLine(string.Join(",", r.ToArray()));
                }
            }
            catch (Exception ex)
            {
                LogEvent(String.Format("GenevaDAL convertGVAResultDataToCSV Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                        ex.Message, ex.StackTrace), EventLogEntryType.Error);
            }             

            return sb.ToString();
        }

        public DataSet removeHiddenColumnsFromDataSet(DataSet ds)
        {
            try
            {
                if (ds.Tables.Count > 0)
                {
                    List<KeyValuePair<int, string>> hiddenCol = new List<KeyValuePair<int, string>>();
                    foreach (DataColumn col in ds.Tables[ds.Tables.Count - 1].Columns)
                    {
                        if (col.ColumnMapping == MappingType.Hidden)
                        {
                            hiddenCol.Add(new KeyValuePair<int, string>(col.Ordinal, col.ColumnName)); //stringcol.ColumnName);
                        }
                    }

                    foreach (KeyValuePair<int, string> col in hiddenCol)
                    {
                        ds.Tables[ds.Tables.Count - 1].Columns.Remove(col.Value);
                        ds.Tables[ds.Tables.Count - 1].Columns.RemoveAt(col.Key);
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogEvent(String.Format("GenevaDAL removeHiddenColumnsFromDataSet Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                        ex.Message, ex.StackTrace), EventLogEntryType.Warning);
            }

            return ds;
        }

        /// <summary>
        /// Returns a LoginResponse object with a OK property of True or False and a Message
        /// which can be Blank, "Login Failed" or "Login Failed followed by an error message" 
        /// </summary>
        public LoginResponse submitGenevaLoginValidate()
        {

            LogEvent(String.Format("ValidateLogin\r\n\r\nPort: {0}\r\nHostname: {1}\r\nUser: {2}\r\nPassword: {3}\r\nFlags: {4}",
                    genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags), EventLogEntryType.Information);


            LoginResponse Login = new LoginResponse();
            object results = new object();
            GenevaSOAP.reportResultsPortfolioStruct RSLresults = new GenevaSOAP.reportResultsPortfolioStruct();
            try
            {
                string runrepSessionID;
                GenevaSOAP.Service webServ = new GenevaSOAP.Service();
                webServ.Url = genevaUrl;
                webServ.Timeout = genevaTimeOut;
                
                //webServ.Proxy = new System.Net.WebProxy(ProxyURL, ProxyByPassOnLocal) { Credentials = System.Net.CredentialCache.DefaultCredentials};
                
                runrepSessionID = webServ.StartCallableRunrep(genevaPort, genevaHostName, genevaUser, genevaPassword, genevaFlags);
                
                if (Convert.ToInt16(runrepSessionID) > 0)
                {
                    webServ.ShutdownCallableSession(runrepSessionID);
                    Login.OK = true;
                    Login.Message = "";

                    LogEvent("ValidateLogin- Login Successfully validated. All good in the hood", EventLogEntryType.Information);

                    return Login;
                }
                else
                {
                    Login.OK = false;
                    Login.Message = "Login Failed";

                    LogEvent(String.Format("GenevaDAL Login Failure\r\nEnvironment {0}\r\nHost {1}\r\nPort {2}\r\nUser {3}\r\nPassword {4}",
                        genevaEnv, genevaHostName, genevaPort, genevaUser, genevaPassword), EventLogEntryType.Error);
                    return Login;
                }
            }

            catch (Exception e)
            {
                Login.OK = false;
                Login.Message = "Login Failed " + e.Message;
                LogEvent(String.Format("GenevaDAL Login Failure\r\nEnvironment {0}\r\nHost {1}\r\nPort {2}\r\nUser {3}\r\nPassword {4}",
                        genevaEnv, genevaHostName, genevaPort, genevaUser, genevaPassword), EventLogEntryType.Error);
                return Login;
            }


        }

        private void LogEvent(string message, EventLogEntryType error = EventLogEntryType.Information)
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"]) )
            {

                string sSource = "Geneva DAL";
                string sLog = "Application";

                if (!EventLog.SourceExists(sSource))
                {
                    EventLog.CreateEventSource(sSource, sLog);
                }

                EventLog.WriteEntry(sSource, message, error);
                
            }
        }


    }

