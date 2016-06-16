using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using System.Xml;


[ServiceContract]
public interface IGenevaDAL
{

    [OperationContract]
    LoginResponse ValidateLogin(string environment, string genevaUser, string genevaPassword);

    [OperationContract]
    GVAResult RunrepToGVAResult(string environment, string genevaUser, string genevaPassword, string RSL, string extraFlags);

    [OperationContract]
    GVAResult RunGVASQL(string environment, string genevaUser, string genevaPassword, string query);

    [OperationContract]
    String[] GetPortfolioList(string environment, string genevaUser, string genevaPassword);

    [OperationContract]
    GVAResult GetAccountCalendarPeriods(string environment, string genevaUser, string genevaPassword, string portfolio, int limit);

    [OperationContract]
    Boolean SaveToCSV(GVAResult gvaResult, string fullFilePath);

    [OperationContract]
    Boolean SaveToXML(GVAResult gvaResult, string fullFilePath);

    [OperationContract]
    GVASession GetGVASession(string environment, string genevaUser, string genevaPassword);
}

#region "SMT Geneva Objects"


[DataContract]
public class LoginResponse
{
    [DataMember]
    public Boolean OK { get; set; }
    [DataMember]
    public string Message { get; set; }
}
[DataContract]
public class GVAResult
{
    [DataMember]
    public Boolean OK { get; set; }
    [DataMember]
    public string Status { get; set; }
    [DataMember]
    public string Message { get; set; }
    [DataMember]
    public DataSet Data { get; set; }
    [DataMember]
    public String CSV { get; set; }
}

[DataContract]
public class GVASession 
    {
        [DataMember]
        public string User { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string AGAServer { get; set; }
        [DataMember]
        public string AGAPort { get;  set; }
        [DataMember]
        public string GenevaSoapURL { get;  set; }
        [DataMember]
        public Boolean Valid { get; set; }

        public GVASession(string genevaUser, string genevaPassword, string GVAAGAServer, string GVAAGAPort, string GVAGenevaSoapURL)
        {
            this.User = genevaUser;
            this.Password = genevaPassword;
            this.AGAServer = GVAAGAServer;
            this.AGAPort = GVAAGAPort;
            this.GenevaSoapURL = GVAGenevaSoapURL;
            this.Valid = false;
        }

        public GVASession()
        {
        }

    }
#endregion