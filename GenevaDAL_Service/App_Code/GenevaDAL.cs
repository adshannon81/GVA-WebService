using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Xml;


public class GenevaDAL : IGenevaDAL
{
    //#region "variables"
   
    //private string loaderFlags = "";
    //private string XMLloaderFlags = "";
    //#endregion


    public LoginResponse ValidateLogin(string environment, string genevaUser, string genevaPassword)
    {
        SMTGeneva GS = new SMTGeneva();
        GS.LoadConnectionSettings( environment, genevaUser, genevaPassword);
        LoginResponse Login = GS.submitGenevaLoginValidate();
        return Login;

    }

    public String[] GetPortfolioList(string environment, string genevaUser, string genevaPassword)
    {
        List<String> PortfolioList = new List<string>();

        SMTGeneva GS = new SMTGeneva();
        GVAResult gr = new GVAResult();
        gr.Message = "";
        try
        {
            GS.LoadConnectionSettings(environment, genevaUser, genevaPassword);
            LoginResponse Login = GS.submitGenevaLoginValidate();

            if (Login.OK)
            {
                if (String.IsNullOrEmpty(gr.Message))
                {
                    /* Connect to Geneva */
                    gr = GS.RunSQLSelect("Namesort from portfolio Order By Namesort");

                    string[] portfolios = gr.CSV.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    for (int i = 1; i < portfolios.Length; i++)
                    {
                        PortfolioList.Add(portfolios[i]);
                    }
                }

            }
            else
            {
                gr.Message += " " + Login.Message;
                gr.Status = "Failure";
                gr.OK = false;
            }
        }
        catch (Exception ex)
        {

            gr.Message += " " + ex.Message;
            gr.Status = "Failure";
            gr.OK = false;

            LogEvent(String.Format("GenevaDAL GetPortfolioList Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                    ex.Message, ex.StackTrace), EventLogEntryType.Error);
        }



        return PortfolioList.ToArray();
    }

    public GVAResult GetAccountCalendarPeriods(string environment, string genevaUser, string genevaPassword, string portfolio, int limit)
    {
        string query = "{owner.key} CalendarName, {key} Period, {PriorKnowledgeDate} PriorKnowledgeDate, {PeriodStartDate} PeriodStart, {PeriodEndDate} PeriodEnd, {PeriodCloseDate} KnowledgeDate from navigate off {AccountCalendar[].PeriodName[].key} WHERE {owner.key} ==  \"" + portfolio + "\" ORDER BY {PeriodStartDate} DESCEND LIMIT " + limit.ToString();

        GVAResult gvaResult = new GVAResult();
        try
        {

            gvaResult = RunGVASQL(environment, genevaUser, genevaPassword, query);

            //check data was returned
            if (gvaResult.Data.Tables.Count > 0)
            {
                //set latest Knowledge date to now.
                if (gvaResult.Data.Tables[0].Rows[0]["KnowledgeDate"].ToString() == "1901-01-01T00:00:00")
                {
                    gvaResult.Data.Tables[0].Rows[0]["KnowledgeDate"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                }

                DataSet ds = gvaResult.Data;
                DataTable dt = ds.Tables[0];

                string priorKnowledgeDate = "";

                for (int i = gvaResult.Data.Tables[0].Rows.Count -1; i >= 0; i--)
                {
                    if (i != gvaResult.Data.Tables[0].Rows.Count -1)
                    {
                        gvaResult.Data.Tables[0].Rows[i]["PriorKnowledgeDate"] = priorKnowledgeDate;
                    }

                    priorKnowledgeDate = gvaResult.Data.Tables[0].Rows[i]["KnowledgeDate"].ToString();
                }
            }
            else
            {
                gvaResult.OK = false;
                gvaResult.Message = "No data available";
            }
        }
        catch (Exception ex)
        {
            gvaResult.Message += " " + ex.Message;
            gvaResult.Status = "Failure";
            gvaResult.OK = false;

            LogEvent(String.Format("GenevaDAL GetAccountCalendarPeriods Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                    ex.Message, ex.StackTrace), EventLogEntryType.Error);

        }

        //if the last period hasn't been closed off then the knowledge date will be 1950/01/01
        //to update to the current time use the following line of code.
        //gvaResult.Data.Tables[0].Rows[gvaResult.Data.Tables[0].Rows.Count - 1]["KnowledgeDate"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        return gvaResult;
    }

    public GVAResult RunGVASQL(string environment, string genevaUser, string genevaPassword, string query)
    {
        SMTGeneva GS = new SMTGeneva();
        GVAResult gr = new GVAResult();
        gr.Message = "";
        try
        {    
            GS.LoadConnectionSettings(environment, genevaUser, genevaPassword);
            LoginResponse Login = GS.submitGenevaLoginValidate();

            if (Login.OK)
            {
                if (String.IsNullOrEmpty(gr.Message))
                {
                    /* Connect to Geneva */
                    gr = GS.RunSQLSelect(query);
                }

                /*This function returns unreliable data from geneva */
                //gr = GS.submitGenevaRSLRequest(RSL, extraFlags);

                /* Max connection wait to check any timeout issues */
                //DateTime endTime = DateTime.Now.AddMinutes(15);
                //while (endTime > DateTime.Now)
                //{     /*do stuff*/  }

            }
            else
            {
                gr.Message += " " + Login.Message;
                gr.Status = "Failure";
                gr.OK = false;
            }
        }
        catch (Exception ex)
        {

            gr.Message += " " + ex.Message;
            gr.Status = "Failure";
            gr.OK = false;

            LogEvent(String.Format("GenevaDAL RunrepToGVAResult Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                    ex.Message, ex.StackTrace), EventLogEntryType.Error);
        }


        return gr;
    }
    
    public GVAResult RunrepToGVAResult(string environment, string genevaUser, string genevaPassword, string RSL, string extraFlags)
    {
        LogEvent("GenevaDAL RunrepToGVAResult Start", EventLogEntryType.Information);

        SMTGeneva GS = new SMTGeneva();
        GVAResult gr = new GVAResult();
        gr.Message = "";
        try
        {            
            GS.LoadConnectionSettings(environment, genevaUser, genevaPassword);
            LoginResponse Login = GS.submitGenevaLoginValidate();

            if (Login.OK)
            {
                /*Return demo data here */
                gr = DemoData(RSL);

                /* Set Accounting Type for NAV Report */
                if (RSL == "0052WebPortalShareSplitNAVHistExtract.rsl")
                {
                    extraFlags = extraFlags.Trim() + " -at NAV";

                    //always use latest Knowledgedate
                    int iLocation = extraFlags.IndexOf(" -k ");
                    if(iLocation >= 0)
                    {
                        string temp = extraFlags;                        
                        extraFlags = extraFlags.Substring(0, iLocation);
                        temp = temp.Substring(iLocation + 4);
                        extraFlags += temp.Substring( temp.IndexOfAny(new Char[1]{' '})); 

                    }

                }
                else
                {
                    extraFlags = extraFlags.Trim() + " -at ClosedPeriod";
                }

                if (String.IsNullOrEmpty(gr.Message))
                {
                    /* Connect to Geneva */
                    gr = GS.submitGenevaRSLRequestString(RSL, extraFlags);
                }

                /*This function returns unreliable data from geneva */
                //gr = GS.submitGenevaRSLRequest(RSL, extraFlags);

                /* Max connection wait to check any timeout issues */
                //DateTime endTime = DateTime.Now.AddMinutes(15);
                //while (endTime > DateTime.Now)
                //{     /*do stuff*/  }

            }
            else
            {
                gr.Message += " " + Login.Message;
                gr.Status = "Failure";
                gr.OK = false;
            }
        }
        catch (Exception ex)
        {

            gr.Message += " " + ex.Message;
            gr.Status = "Failure";
            gr.OK = false;

            LogEvent(String.Format("GenevaDAL RunrepToGVAResult Failure\r\nMessage: {0}\r\nStackTrace\r\n{1}",
                    ex.Message, ex.StackTrace), EventLogEntryType.Error);
        }

        
        return gr;
    }

    public GVASession GetGVASession(string environment, string genevaUser, string genevaPassword)
    {
        SMTGeneva GS = new SMTGeneva();
        GVASession failure = null;
        GVASession gs = new GVASession();

        GS.LoadConnectionSettings( environment, genevaUser, genevaPassword);

        if (ValidateLogin(environment, genevaUser, genevaPassword).OK)
        {
            gs = new GVASession(genevaUser, genevaPassword, GS.genevaHostName, Convert.ToString(GS.genevaPort), GS.genevaUrl);
            gs.Valid = true;
            return gs;
        }
        else
        {
            return failure;
        }
    }

    /* Note: this will save using the IUSER account so ensure they have Write access
     * to the destination folder 
     */
    public Boolean SaveToCSV(GVAResult gvaResult, string fullFilePath)
    {
        bool successful = false;
        try
        {
            StreamWriter txtWriter = new StreamWriter(fullFilePath);
            txtWriter.Write(gvaResult.CSV);
            txtWriter.Flush();
            txtWriter.Close();
            txtWriter = null;

            successful = true;
        }
        catch (Exception ex)
        {
            LogEvent(ex.Message, EventLogEntryType.Error);
        }
        return successful;
    }

    public Boolean SaveToXML(GVAResult gvaResult, string fullFilePath)
    {
        bool successful = false;
        try
        {
            gvaResult.Data.WriteXml(fullFilePath, XmlWriteMode.IgnoreSchema);
            successful = true;
        }
        catch (Exception ex)
        {
            LogEvent(ex.Message, EventLogEntryType.Error);
        }
        return successful;
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


    /* #######################################
     * Return demo data until Advent
     * reports are ready 
     * #######################################*/
    private GVAResult DemoData(string RSL)
    {
        GVAResult gvaResult = new GVAResult();

        List<String> demoReports = new List<string>();
        //demoReports.Add("0038WebPortalAmortizationLedger10Extract.rsl");
        //demoReports.Add("0039WebPortalDividendLedger10Extract.rsl");
        //demoReports.Add("0040WebPortalInterestLedger10Extract.rsl");
        //demoReports.Add("0041WebPortalPositions15Extract.rsl");
        //demoReports.Add("0042WebPortalTaxLot10Extract.rsl");
        //demoReports.Add("0043WebPortalTaxLot35Extract.rsl");
        //demoReports.Add("0044WebPortalTB50Extract.rsl");
        //demoReports.Add("0045WebPortalTB55Extract.rsl");
        //demoReports.Add("0046WebPortalTransListing10Extract.rsl");
        //demoReports.Add("0047WebPortalUnsettTrans30Extract.rsl");
        //demoReports.Add("0048WebPortalPositions10Extract.rsl");
        //demoReports.Add("0051WebPortalFXTransactionsandRepos.rsl");
        //demoReports.Add("0052WebPortalShareSplitNAVHistExtract.rsl");
        //demoReports.Add("0053WebPortalShareSplitTRXCUR.rsl");

        if (ConfigurationManager.AppSettings["0038WebPortalAmortizationLedger10Extract"] == "true")
            demoReports.Add("0038WebPortalAmortizationLedger10Extract.rsl");
        if (ConfigurationManager.AppSettings["0039WebPortalDividendLedger10Extract"] == "true")
            demoReports.Add("0039WebPortalDividendLedger10Extract.rsl");
        if (ConfigurationManager.AppSettings["0040WebPortalInterestLedger10Extract"] == "true")
            demoReports.Add("0040WebPortalInterestLedger10Extract.rsl");
        if (ConfigurationManager.AppSettings["0041WebPortalPositions15Extract"] == "true")
            demoReports.Add("0041WebPortalPositions15Extract.rsl");
        if (ConfigurationManager.AppSettings["0042WebPortalTaxLot10Extract"] == "true")
            demoReports.Add("0042WebPortalTaxLot10Extract.rsl");
        if (ConfigurationManager.AppSettings["0043WebPortalTaxLot35Extract"] == "true")
            demoReports.Add("0043WebPortalTaxLot35Extract.rsl");
        if (ConfigurationManager.AppSettings["0044WebPortalTB50Extract"] == "true")
            demoReports.Add("0044WebPortalTB50Extract.rsl");
        if (ConfigurationManager.AppSettings["0045WebPortalTB55Extract"] == "true")
            demoReports.Add("0045WebPortalTB55Extract.rsl");
        if (ConfigurationManager.AppSettings["0046WebPortalTransListing10Extract"] == "true")
            demoReports.Add("0046WebPortalTransListing10Extract.rsl");
        if (ConfigurationManager.AppSettings["0047WebPortalUnsettTrans30Extract"] == "true")
            demoReports.Add("0047WebPortalUnsettTrans30Extract.rsl");
        if (ConfigurationManager.AppSettings["0048WebPortalPositions10Extract"] == "true")
            demoReports.Add("0048WebPortalPositions10Extract.rsl");
        if (ConfigurationManager.AppSettings["0051WebPortalFXTransactionsandRepos"] == "true")
            demoReports.Add("0051WebPortalFXTransactionsandRepos.rsl");
        if (ConfigurationManager.AppSettings["0052WebPortalShareSplitNAVHistExtract"] == "true")
            demoReports.Add("0052WebPortalShareSplitNAVHistExtract.rsl");
        if (ConfigurationManager.AppSettings["0053WebPortalShareSplitTRXCUR"] == "true")
            demoReports.Add("0053WebPortalShareSplitTRXCUR.rsl");

        try
        {
            if (demoReports.Contains(RSL))
            {
                string filepath = (String.Format(@"{0}DemoData\{1}.xml", AppDomain.CurrentDomain.BaseDirectory, RSL));
                gvaResult.Data = new DataSet();
                gvaResult.Data.ReadXml(filepath);
                gvaResult.Message = "DemoData";
                gvaResult.OK = true;

                SMTGeneva GS = new SMTGeneva();
                gvaResult.CSV = GS.convertGVAResultDataToCSV(gvaResult, true);
            }
        }
        catch (Exception ex)
        {
            string s = ex.Message;
        }

        return gvaResult;
    }

    
}
