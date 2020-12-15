using System;
using System.Data;
using System.Data.SqlClient;
using PPDBAccess;

namespace PRE_TEST
  {
  class SQLClass : PPTraceStation
    {

    #region Variables

    private string MyChildTraceNrs = "";
    private string ProcError = "";

    #endregion

    #region Constructors

    public SQLClass(string DBserver, string DBName, string DBUser, string DBUserPsw, Int32 idWS, Int32 subWS)
      : base(DBserver, DBName, DBUser, DBUserPsw, idWS, subWS)
      {

      }

    public SQLClass(string DBserver, string DBName, string DBUser, string DBUserPsw)
      : base(DBserver, DBName, DBUser, DBUserPsw, -1, -1)
      {

      }


    #endregion

    #region Properties

    public string getProcError
      {
      get { return ProcError; }
      }
    public string getChildTraceNrs
      {
      get { return MyChildTraceNrs; }
      }

    #endregion

    #region MyMethods

    public int UpgradeReference(Int64 traceNr, string refPreh, Int32 idWS, Int32 subWS, Int32 JobID)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.CommandText = "Trace_UpgradeRef";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@TNr", traceNr);
        cmd.Parameters["@TNr"].Direction = ParameterDirection.Input;

        cmd.Parameters.AddWithValue("@FinalRef", refPreh);
        cmd.Parameters["@FinalRef"].Direction = ParameterDirection.Input;

        cmd.Parameters.AddWithValue("@WSID", idWS);
        cmd.Parameters["@WSID"].Direction = ParameterDirection.Input;

        cmd.Parameters.AddWithValue("@SubWS", subWS);
        cmd.Parameters["@SubWS"].Direction = ParameterDirection.Input;

        cmd.Parameters.AddWithValue("@JobID", JobID);
        cmd.Parameters["@JobID"].Direction = ParameterDirection.Input;

        cmd.Parameters.Add("@Return", SqlDbType.Int);
        cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

        try
        {
            OpenDBConnection();
            cmd.Connection = conn;
            cmd.ExecuteNonQuery();
            CloseDBConnection();
            return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
        catch (Exception ex)
        {
            ProcError = "Trace_UpgradeRef: " + ex.ToString();
            return -1;
        }
    }

    public int ChildTraceNrs(string FrameTraceNr)
      {
      SqlCommand cmd = new SqlCommand();
      cmd.CommandText = "GetFrameChilds";
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@FrameTraceNr", FrameTraceNr);
      cmd.Parameters["@FrameTraceNr"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@ChildTraceNrs", SqlDbType.VarChar);
      cmd.Parameters["@ChildTraceNrs"].Direction = ParameterDirection.Output;

      cmd.Parameters.Add("@Return", SqlDbType.Int);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();

        MyChildTraceNrs = cmd.Parameters["@ChildTraceNrs"].Value.ToString();
        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception ex) {
        ProcError = "GetChildTraceNr - " + ex.ToString();
        return 1;
        }
      }
    public int CheckICTErrors(string Job_ID)
      {
      SqlCommand cmd = new SqlCommand();
      cmd.CommandText = "CheckICTErrors";
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@Job_ID", Job_ID);
      cmd.Parameters["@Job_ID"].Direction = ParameterDirection.Input;

      cmd.Parameters.Add("@Return", SqlDbType.TinyInt);
      cmd.Parameters["@Return"].Direction = ParameterDirection.ReturnValue;

      try {
        OpenDBConnection();
        cmd.Connection = conn;
        cmd.ExecuteNonQuery();
        CloseDBConnection();

        return int.Parse(cmd.Parameters["@Return"].Value.ToString());
        }
      catch (Exception) {
        return 1;
        }
      }
    public bool BulkCopy(DataTable Dt, string DestinationTableName)
      {
      try {
        OpenDBConnection();
        SqlBulkCopy bulkcopy = new SqlBulkCopy(conn);
        bulkcopy.DestinationTableName = "dbo." + DestinationTableName;
        bulkcopy.WriteToServer(Dt);
        CloseDBConnection();
        }
      catch (Exception) {
        CloseDBConnection();
        return false;
        }
      return true;
      }
    public int UpdateAxisPositions(object[] Items)
      {
      SqlCommand cmd = new SqlCommand();
      cmd.CommandText = "UpdateAxisPositions";
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@NAME", Items[0]);
      cmd.Parameters["@NAME"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SEQUENCE", Items[1]);
      cmd.Parameters["@SEQUENCE"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@POS_X", Items[2]);
      cmd.Parameters["@POS_X"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@POS_Y", Items[3]);
      cmd.Parameters["@POS_Y"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SPEED_X", Items[4]);
      cmd.Parameters["@SPEED_X"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@SPEED_Y", Items[5]);
      cmd.Parameters["@SPEED_Y"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ACC_X", Items[6]);
      cmd.Parameters["@ACC_X"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ACC_Y", Items[7]);
      cmd.Parameters["@ACC_Y"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@INPOS_BAND", Items[8]);
      cmd.Parameters["@INPOS_BAND"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@PUSH", Items[9]);
      cmd.Parameters["@PUSH"].Direction = ParameterDirection.Input;

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
        ProcError = "UpdateAxisPositions: " + ex.ToString();
        return -1;
        }
      }
    public int UpdateDeviceParameter(int ID_Parameter, double ValMin, double ValMax)
      {
      SqlCommand cmd = new SqlCommand();
      cmd.CommandText = "UpdateDeviceParameter";
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@ID_Parameter", ID_Parameter);
      cmd.Parameters["@ID_Parameter"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ValMin", ValMin);
      cmd.Parameters["@ValMin"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ValMax", ValMax);
      cmd.Parameters["@ValMax"].Direction = ParameterDirection.Input;

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
        ProcError = "UpdateDeviceParameter: " + ex.ToString();
        return -1;
        }
      }
    public int SaveResult(int ID_WSJob, string ResultName, double ResultValue)
      {
      SqlCommand cmd = new SqlCommand();
      cmd.CommandText = "SaveDeviceResults";
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("@ID_WSJob", ID_WSJob);
      cmd.Parameters["@ID_WSJob"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ResultName", ResultName);
      cmd.Parameters["@ResultName"].Direction = ParameterDirection.Input;

      cmd.Parameters.AddWithValue("@ResultValue", ResultValue);
      cmd.Parameters["@ResultValue"].Direction = ParameterDirection.Input;

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
        ProcError = "SaveResult: " + ex.ToString();
        return -1;
        }
      }
    public string getDescriptionByError(int bytError)
      {
      string s = "";

      switch (bytError) {

        case 1:
          s = bytError + ": Unknow run-time error";//": Erro de Base Dados desconhecido";
          break;
        case 2:
          s = bytError + ": Serial Number Assigned to another reference";//": Nr. de série atribuido a outra referência";
          break;
        case 3:
          s = bytError + ": Serial Number has no reference assigned";//": Nr. de série não tem nenhuma referência atribuída";
          break;
        case 4:
          s = bytError + ": Workstation unknown";//": Posto de trabalho desconhecido";
          break;
        case 5:
          s = bytError + ": Reference does not exist or is inactive";//": A Referência não existe ou não está activa";
          break;
        case 6:
          s = bytError + ": Serial Number under analysis";//": Nr. de série em análise";
          break;
        case 7:
          s = bytError + ": Last workstation job was NOK";//": Peça NOK no último posto de trabalho";
          break;
        case 8:
          s = bytError + ": Workstation is not repeatable";//": Nr. de série fora da sequência de produção";
          break;
        case 9:
          s = bytError + ": Workstation is not repeatable";//": O Posto de trabalho não permite repetir a operação";
          break;
        case 11:
          s = bytError + ": Assembly not allowed";//": A Montagem/Associação não é permitida";
          break;
        case 12:
          s = bytError + ": No assignment possible";//": A Montagem/Associação não é permitida";
          break;
        case 14:
          s = bytError + ": Serial Number already assembled into  a component";//": Nr. de série já está associado/montado num componente";
          break;
        default:
          s = "";
          break;
        }
      return s;
      }

    #endregion

    }
  }
