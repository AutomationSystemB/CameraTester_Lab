using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace PPDBAccess
  {
  class PPBaseDB
    {
    protected string serverName = "";
    protected string databaseName = "";
    protected string userName = "";
    protected string userPassword = "";
    protected string lastErrorDescription = "";

    protected SqlConnection conn;

    private DataTable myUsers;

    #region Constructors
    public PPBaseDB(string DBserver, string DBName, string DBUser, string DBUserPsw)
      {
      serverName = DBserver;
      databaseName = DBName;
      userName = DBUser;
      userPassword = DBUserPsw;
      }
    #endregion

    #region Properties
    public string ServerName
      {
      get { return serverName; }
      }
    public string DatabaseName
      {
      get { return databaseName; }
      }
    public string UserName
      {
      get { return userName; }
      }
    #endregion

    #region Methods
    private string EncryptPsw(string pswToEncrypt)
      {
      string encryptedPsw = "";
      char[] arrayOfChar = pswToEncrypt.ToCharArray();

      Encoding Enc = Encoding.GetEncoding(1252);
      byte[] arrayOfBytes = Enc.GetBytes(arrayOfChar);

      for (int i = 0; i < arrayOfBytes.Length; i++)
        arrayOfBytes[i] = (byte)(arrayOfBytes[i] + 80);

      arrayOfChar = Enc.GetChars(arrayOfBytes);

      for (int i = 0; i < arrayOfChar.Length; i++)
        encryptedPsw = encryptedPsw + arrayOfChar[i].ToString();
      return encryptedPsw;
      }

    protected void OpenDBConnection()
      {
      if (conn != null) CloseDBConnection();
      conn = new SqlConnection("Data Source=" + serverName +
                              ";Initial Catalog=" + databaseName +
                              ";Persist Security Info=True" +
                              ";User ID=" + userName +
                              ";Password=" + userPassword);
      try {
        conn.Open();
        }
      catch (SqlException ex) {
        throw new Exception("Error opening SQL connection: ", ex);
        }
      }
    protected void CloseDBConnection()
      {
      if (conn != null) {
        try {
          conn.Close();
          conn.Dispose();
          }
        catch (SqlException ex) {
          Console.WriteLine(ex.ToString());
          throw new Exception("Error closing SQL connection: ", ex);
          }
        }
      }

    public string GetLastErrorDescription()
      {
      return lastErrorDescription;
      }
    public DataSet FillDatasetBySQL(string sqlQuery)
      {
      DataSet ds = new DataSet();
      try {
        OpenDBConnection();
        SqlDataAdapter da = new SqlDataAdapter(sqlQuery, conn);
        da.Fill(ds);
        CloseDBConnection();
        }
      catch (Exception ex) {
        lastErrorDescription = "FillDatasetBySQL: " + ex.ToString();
        return null;
        }
      return ds;
      }
    public DataTable FillDatatableBySQL(string sqlQuery)
      {
      DataTable dt = new DataTable();
      try {
        OpenDBConnection();
        SqlDataAdapter da = new SqlDataAdapter(sqlQuery, conn);
        da.Fill(dt);
        CloseDBConnection();
        }
      catch (Exception ex) {
        lastErrorDescription = "FillDatatableBySQL: " + ex.ToString();
        return null;
        }
      return dt;
      }
    public DataTable GetAppActiveUsers(string appName)
      {
      string sqlQuery = "EXECUTE ProdGeral.dbo.Login_GetAppActiveUsers '" + appName + "'";
      myUsers = FillDatatableBySQL(sqlQuery);
      DataTable dt = myUsers.Copy();
      dt.Columns.Remove("Psw");
      return dt;
      }
    public bool CheckUserPassword(Int32 userID, string userPsw)
      {
      if (myUsers == null) return false;
      myUsers.DefaultView.RowFilter = "";
      myUsers.DefaultView.RowFilter = "ID_User = " + userID.ToString();
      if (myUsers.DefaultView.Count != 1) return false;
      return (myUsers.DefaultView.ToTable().Rows[0]["Psw"].ToString() == EncryptPsw(userPsw));
      }
    #endregion

    }

  class PPTraceDB : PPBaseDB
    {
    protected string[] traceErrors = new string[256];

    #region Constructors
    public PPTraceDB(string DBserver, string DBName, string DBUser, string DBUserPsw)
      : base(DBserver, DBName, DBUser, DBUserPsw)
      {
      //SetTraceErrorsLanguage("PT");
      }
    #endregion

    #region Methods
    public void SetTraceErrorsLanguage(string errorLanguage)
      {
      for (int i = 0; i < 256; i++) traceErrors[i] = "Error description unavailable";

      DataTable dt = FillDatatableBySQL("EXEC ProdGeral.dbo.Trace_GetTraceErrorDescriptions '" + errorLanguage + "'");
      foreach (DataRow drow in dt.Rows) {
        traceErrors[int.Parse(drow["TraceErrorCode"].ToString())] = drow["TraceErrorDescription"].ToString();
        }
      }
    public string GetTraceErrorDescription(int errorNr)
      {
      return traceErrors[errorNr];
      }

    public int GetIDRefFromRefPreh(string refPreh)
      {
      Int32 refID = 0;
      string sqlQuery = "EXECUTE Trace_GetIDRefFromRefPreh '" + refPreh + "'";
      try {
        OpenDBConnection();
        SqlCommand cmd = new SqlCommand(sqlQuery, conn);
        refID = (Int32)cmd.ExecuteScalar();
        CloseDBConnection();
        }
      catch (Exception ex) {
        lastErrorDescription = "GetIDRefFromRefPreh: " + ex.ToString();
        return -1;
        }
      return refID;
      }
    public int GetIDSubwsFromIDWSAndSubWS(Int32 idWS, Int32 subWS)
      {
      Int32 idSubWS = 0;
      string sqlQuery = "EXECUTE Trace_GetIDSubwsFromIDWSAndSubWS " + idWS.ToString() + "," + subWS.ToString();
      try {
        OpenDBConnection();
        SqlCommand cmd = new SqlCommand(sqlQuery, conn);
        idSubWS = (Int32)cmd.ExecuteScalar();
        CloseDBConnection();
        }
      catch (Exception ex) {
        lastErrorDescription = "GetIDSubwsFromIDWSAndSubWS: " + ex.ToString();
        return -1;
        }
      return idSubWS;
      }
    public DataTable GetRefs(Int32 idWS, Int32 subWS)
      {
      string sqlQuery = "EXECUTE Trace_GetRefsOfWSC " + idWS.ToString() + "," + subWS.ToString();
      return FillDatatableBySQL(sqlQuery);
      }

    public int AssignRef(Int64 traceNr, string refPreh, Int32 idWS, Int32 subWS)
      {
      SqlCommand cmd = new SqlCommand("Trace_AssignRef");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNr", traceNr);
      cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@FinalRef", refPreh);
      cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@WSID", idWS);
      cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SubWS", subWS);
      cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "AssignRef: " + ex.ToString();
        return 1;
        }
      }
    public int CheckAssembly(Int64 traceNrParent, Int64 traceNrChild, Int32 idWS, Int32 subWS)
      {
      SqlCommand cmd = new SqlCommand("CheckAssembly");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNrParent", traceNrParent);
      cmd.Parameters["@TNrParent"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@TNrChild", traceNrChild);
      cmd.Parameters["@TNrChild"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@WSID", idWS);
      cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SubWS", subWS);
      cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "CheckAssembly: " + ex.ToString();
        return 1;
        }
      }
    public int SaveAssembly(Int64 traceNrParent, Int64 traceNrChild, Int32 jobID)
      {
      SqlCommand cmd = new SqlCommand("SaveAssembly");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNrParent", traceNrParent);
      cmd.Parameters["@TNrParent"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@TNrChild", traceNrChild);
      cmd.Parameters["@TNrChild"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@WSJobID", jobID);
      cmd.Parameters["@WSJobID"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "SaveAssembly: " + ex.ToString();
        return 1;
        }
      }
    public int CheckLaser(Int64 traceNr)
      {
      SqlCommand cmd = new SqlCommand("CheckLaser");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNr", traceNr);
      cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "CheckLaser: " + ex.ToString();
        return 1;
        }
      }
    public int CheckSerialNr(Int64 traceNr, string refPreh, Int32 idWS, Int32 subWS)
      {
      SqlCommand cmd = new SqlCommand("Trace_CheckTraceNr");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNr", traceNr);
      cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@FinalRef", refPreh);
      cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@WSID", idWS);
      cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SubWS", subWS);
      cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "CheckSerialNr: " + ex.ToString();
        return 1;
        }
      }
    public int JobStart(Int64 traceNr, Int32 idWS, Int32 subWS, ref Int32 jobID)
      {
      jobID = 0;
      SqlCommand cmd = new SqlCommand("Trace_JobStart");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@TNr", traceNr);
      cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@WSID", idWS);
      cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SubWS", subWS);
      cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@JobID", SqlDbType.Int);
      cmd.Parameters["@JobID"].Direction = ParameterDirection.Output;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        jobID = int.Parse(cmd.Parameters["@JobID"].Value.ToString());
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "WSJobStart: " + ex.ToString();
        return 1;
        }
      }
    public int JobEnd(Int32 jobID, byte jobResult)
      {
      SqlCommand cmd = new SqlCommand("Trace_JobEnd");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@JobID", jobID);
      cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@JobResult", jobResult);
      cmd.Parameters["@JobResult"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "WSJobEnd: " + ex.ToString();
        return 1;
        }
      }
    public int JobEndWithErrorDetails(Int32 jobID, byte jobResult, Int32 errorCode, string errorDetails)
      {
      SqlCommand cmd = new SqlCommand("Trace_JobEndWithErrorDetails");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@JobID", jobID);
      cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@JobResult", jobResult);
      cmd.Parameters["@JobResult"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ErrorCode", errorCode);
      cmd.Parameters["@ErrorCode"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ErrorDetails", errorDetails);
      cmd.Parameters["@ErrorDetails"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "WSJobEndWithErrorDetails: " + ex.ToString();
        return 1;
        }
      }
    public int JobSaveError(Int32 jobID, Int32 errorCode, string errorDetails)
      {
      SqlCommand cmd = new SqlCommand("WSJobSaveError");
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@WSJobID", jobID);
      cmd.Parameters["@WSJobID"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ErrorCode", errorCode);
      cmd.Parameters["@ErrorCode"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ErrorDetails", errorDetails);
      cmd.Parameters["@ErrorDetails"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        lastErrorDescription = "WSJobSaveError: " + ex.ToString();
        return 1;
        }
      }
    #endregion

    }

  class PPTraceStation : PPTraceDB
    {
    protected bool myStatus = false;
    protected Int32 myIDWSC = 0;
    protected Int32 myIDWS = 0;
    protected Int32 mySubWS = 0;
    protected Int32 myIDSubWS = 0;
    protected Int32 myIDJob = 0;
    protected Int32 myIDRef = 0;
    protected string myRefPreh = "";
    protected string myWSName = "";
    protected string mySubWSName = "";
    protected string myWSCName = "";
    protected DataTable mySamples;
    protected Int32 mySampleCount = 0;
    protected Int16 mySampleCode = -1;

    #region Constructors
    public PPTraceStation(string DBserver, string DBName, string DBUser, string DBUserPsw, Int32 idWS, Int32 subWS)
      : base(DBserver, DBName, DBUser, DBUserPsw)
      {
      myIDWS = idWS;
      mySubWS = subWS;
      GetSubwsDataFromIDWSAndSubWS(myIDWS, mySubWS);
      myStatus = (bool)(myIDSubWS > 0);
      if (myStatus) LoadSamples();
      }
    #endregion

    #region Properties
    public string RefPrehSelected
      {
      get { return myRefPreh; }
      }
    public Int32 RefIDSelected
      {
      get { return myIDRef; }
      }
    public Int32 JobID
      {
      get { return myIDJob; }
      }
    public Int32 WorkstationID
      {
      get { return myIDWS; }
      }
    public string WorkstationName
      {
      get { return myWSName; }
      }
    public Int32 SubWS
      {
      get { return mySubWS; }
      }
    public Int32 SubWSID
      {
      get { return myIDSubWS; }
      }
    public string SubWSName
      {
      get { return mySubWSName; }
      }
    public Int32 WSCenterID
      {
      get { return myIDWSC; }
      }
    public string WSCenterName
      {
      get { return myWSCName; }
      }
    public bool Status
      {
      get { return myStatus; }
      }
    public Int32 SampleCount
      {
      get { return mySampleCount; }
      }
    public Int16 SampleCode
      {
      get { return mySampleCode; }
      }
    #endregion

    #region Methods
    private void GetSubwsDataFromIDWSAndSubWS(Int32 idWS, Int32 subWS)
      {
      myIDSubWS = 0;
      string sqlQuery = "EXECUTE Trace_GetSubwsDataFromIDWSAndSubWS " + idWS.ToString() + "," + subWS.ToString();
      try {
        DataTable dt = FillDatatableBySQL(sqlQuery);
        DataRow drow = dt.Rows[0];
        myIDSubWS = (Int32)drow["ID_SubWS"];
        myWSName = drow["WSName"].ToString();
        mySubWSName = drow["SubWSName"].ToString();
        myIDWSC = (Int32)drow["ID_WSCenter"];
        myWSCName = drow["WSCName"].ToString();
        }
      catch (Exception ex) {
        lastErrorDescription = "GetSubwsDataFromIDWSAndSubWS: " + ex.ToString();
        return;
        }
      return;
      }
    public void SetReference(string refPreh)
      {
      myRefPreh = refPreh;
      myIDRef = base.GetIDRefFromRefPreh(refPreh);
      myStatus = (bool)(myIDRef > 0);
      }
    public DataTable GetRefs()
      {
      return base.GetRefs(myIDWS, mySubWS);
      }
    public void LoadSamples()
      {
      mySamples = FillDatatableBySQL("EXEC Trace_GetSamplesFromIDSubWS " + myIDSubWS.ToString());
      mySampleCount = mySamples.Rows.Count;
      mySampleCode = -1;
      return;
      }
    public bool CheckSample(Int64 traceNr)
      {
      mySamples.DefaultView.RowFilter = "";
      mySamples.DefaultView.RowFilter = "TraceNr=" + traceNr.ToString();
      if (mySamples.DefaultView.Count == 1) {
        mySampleCode = (Int16)mySamples.DefaultView.Table.Rows[0]["SampleCode"];
        return true;
        }
      else {
        mySampleCode = -1;
        return false;
        }
      }


    public int AssignRef(Int64 traceNr)
      {
      return base.AssignRef(traceNr, myRefPreh, myIDWS, mySubWS);
      //SqlCommand cmd = new SqlCommand("AssignRef_WithIDRefIDSubws");
      //cmd.CommandType = CommandType.StoredProcedure;

      //cmd.Parameters.AddWithValue("@TNr", traceNr);
      //cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@IDFinalRef", myIDRef);
      //cmd.Parameters["@IDFinalRef"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@SubWSID", myIDSubWS);
      //cmd.Parameters["@SubWSID"].Direction = ParameterDirection.Input;

      //cmd.Parameters.Add("@Return", SqlDbType.Int);
      //cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      //try
      //{
      //  OpenDBConnection();
      //  cmd.Connection = conn;
      //  cmd.ExecuteNonQuery();
      //  CloseDBConnection();
      //  return int.Parse(cmd.Parameters["@Return"].Value.ToString());
      //}
      //catch (Exception ex)
      //{
      //  lastErrorDescription = "AssignRef: " + ex.ToString();
      //  return 1;
      //}
      }
    public int CheckAssembly(Int64 traceNrParent, Int64 traceNrChild)
      {
      return base.CheckAssembly(traceNrParent, traceNrChild, myIDWS, mySubWS);
      //SqlCommand cmd = new SqlCommand("CheckAssembly_WithIDSubWS");
      //cmd.CommandType = CommandType.StoredProcedure;

      //cmd.Parameters.AddWithValue("@TNrParent", traceNrParent);
      //cmd.Parameters["@TNrParent"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@TNrChild", traceNrChild);
      //cmd.Parameters["@TNrChild"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@SubWSID", myIDSubWS);
      //cmd.Parameters["@SubWSID"].Direction = ParameterDirection.Input;

      //cmd.Parameters.Add("@Return", SqlDbType.Int);
      //cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      //try
      //{
      //  OpenDBConnection();
      //  cmd.Connection = conn;
      //  cmd.ExecuteNonQuery();
      //  CloseDBConnection();
      //  return int.Parse(cmd.Parameters["@Return"].Value.ToString());
      //}
      //catch (Exception ex)
      //{
      //  lastErrorDescription = "CheckAssembly: " + ex.ToString();
      //  return 1;
      //}
      }
    public int SaveAssembly(Int64 traceNrParent, Int64 traceNrChild)
      {
      return base.SaveAssembly(traceNrParent, traceNrChild, myIDJob);
      }
    public int CheckSerialNr(Int64 traceNr)
      {
      return base.CheckSerialNr(traceNr, myRefPreh, myIDWS, mySubWS);
      //SqlCommand cmd = new SqlCommand("CheckSerialNr_WithIDRefIDSubws");
      //cmd.CommandType = CommandType.StoredProcedure;

      //cmd.Parameters.AddWithValue("@TNr", traceNr);
      //cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@IDFinalRef", myIDRef);
      //cmd.Parameters["@IDFinalRef"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@SubWSID", myIDSubWS);
      //cmd.Parameters["@SubWSID"].Direction = ParameterDirection.Input;

      //cmd.Parameters.Add("@Return", SqlDbType.Int);
      //cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      //try
      //{
      //  OpenDBConnection();
      //  cmd.Connection = conn;
      //  cmd.ExecuteNonQuery();
      //  CloseDBConnection();
      //  return int.Parse(cmd.Parameters["@Return"].Value.ToString());
      //}
      //catch (Exception ex)
      //{
      //  lastErrorDescription = "CheckSerialNr: " + ex.ToString();
      //  return 1;
      //}
      }
    public int JobStart(Int64 traceNr)
      {
      myIDJob = 0;
      return base.JobStart(traceNr, myIDWS, mySubWS, ref myIDJob);
      //SqlCommand cmd = new SqlCommand("WSJobStart_WithIDSubWS");
      //cmd.CommandType = CommandType.StoredProcedure;

      //cmd.Parameters.AddWithValue("@TNr", traceNr);
      //cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

      //cmd.Parameters.AddWithValue("@SubWSID", myIDSubWS);
      //cmd.Parameters["@SubWSID"].Direction = ParameterDirection.Input;

      //cmd.Parameters.Add("@WSJobID", SqlDbType.Int);
      //cmd.Parameters["@WSJobID"].Direction = ParameterDirection.Output;

      //cmd.Parameters.Add("@Return", SqlDbType.Int);
      //cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      //try
      //{
      //  OpenDBConnection();
      //  cmd.Connection = conn;
      //  cmd.ExecuteNonQuery();
      //  CloseDBConnection();
      //  myIDJob = int.Parse(cmd.Parameters["@WSJobID"].Value.ToString());
      //  return int.Parse(cmd.Parameters["@Return"].Value.ToString());
      //}
      //catch (Exception ex)
      //{
      //  lastErrorDescription = "WSJobStart: " + ex.ToString();
      //  return 1;
      //}
      }
    public int JobEnd(byte jobResult)
      {
      int result = base.JobEnd(myIDJob, jobResult);
      myIDJob = 0;
      return result;
      }
    public int JobEndWithErrorDetails(byte jobResult, Int32 errorCode, string errorDetails)
      {
      int result = base.JobEndWithErrorDetails(myIDJob, jobResult, errorCode, errorDetails);
      myIDJob = 0;
      return result;
      }
    public int JobSaveError(Int32 errorCode, string errorDetails)
      {
      return base.JobSaveError(myIDJob, errorCode, errorDetails);
      }
    #endregion

    }

  }
