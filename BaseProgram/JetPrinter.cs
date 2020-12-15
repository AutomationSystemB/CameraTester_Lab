using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace PRE_TEST
  {
  class JetPrinter
    {
    private SerialPort JetPrinterPort = new SerialPort();
    private string JetPrinterCommPort = "";
    private string strDataJetPrinter = "";
    private bool DataArrived = false;
    private byte[] RXMessages = new byte[0];

    #region Properties

    public string GetReading
      {
          get { return strDataJetPrinter; }
      }
	
    #endregion

    #region Constructors

    public JetPrinter(string CommPort)
      {
      JetPrinterCommPort = CommPort;
      }

    #endregion 

    #region Events

    private void OnRX(object sender, SerialDataReceivedEventArgs e)
      {
      char chrAux = (char)3;
      string strEndOfMsg = chrAux.ToString();
      strDataJetPrinter = "";

      int NumBytesToRead = JetPrinterPort.BytesToRead;
      byte[] bytRX = new byte[NumBytesToRead];
      JetPrinterPort.Read(bytRX, 0, NumBytesToRead);

      Array.Resize(ref RXMessages, bytRX.Length + RXMessages.Length);
      Array.Copy(bytRX, 0, RXMessages, RXMessages.Length - bytRX.Length, bytRX.Length);

      for (int I = 0; I < RXMessages.Length; I++) {
        if (RXMessages[I].Equals(3)) {
          if (I >= 4) {
            strDataJetPrinter = System.Text.ASCIIEncoding.ASCII.GetString(RXMessages, 0, I);
            RXMessages = new byte[0];
            DataArrived = true;
            return;
            }
          }
        }
      }

    #endregion

    public void InitCommPort()
      {
          if (!JetPrinterPort.IsOpen)
        {
            JetPrinterPort.DataReceived += new SerialDataReceivedEventHandler(OnRX);
            JetPrinterPort.PortName = JetPrinterCommPort;
            JetPrinterPort.BaudRate = 9600;
            JetPrinterPort.Parity = Parity.None;
            JetPrinterPort.DataBits = 8;
            JetPrinterPort.StopBits = StopBits.One;
        try
          {
              JetPrinterPort.Open();
          }
        catch (Exception e)
          {
          throw new Exception("INITCOMMPORT", e);
          }
        }
      }

    public void CloseCommPort()
      {
          if (JetPrinterPort.IsOpen)
          {
        try {
            JetPrinterPort.Close();
          }
        catch (Exception) {
          //throw new Exception("CLOSECOMMPORT", e);
          }
        }
      }

    public bool JetPrinterCmd(string cmd)
      {
      double Ti, Tf;
      DataArrived = false;
      strDataJetPrinter = "";

      //ScanPort.Write(((char)2).ToString());
      JetPrinterPort.Write(cmd);

      Ti = DateTime.Now.TimeOfDay.TotalMilliseconds;
      do {
        Application.DoEvents();
        Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
        if (Tf < Ti) Ti = Tf;
        if ((Tf - Ti) > 6000) break;
        } while (!DataArrived);
      //CloseCommPort();
      if (strDataJetPrinter.Equals("NOREAD"))
        DataArrived = false;
      return DataArrived;
      }
    }
  }