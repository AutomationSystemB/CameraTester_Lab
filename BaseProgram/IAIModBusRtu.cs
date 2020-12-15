using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace PRE_TEST
  {
  class IAIModBusRtu
    {
    #region Variables

    private byte SLAVEADDRESSID = 255;
    private string PORT = "";
    private int BAUDRATE = 0;

    SerialPort comPort;
    byte TimeOut = 10;    // 10 msec

    #endregion

    #region Constructor

    public IAIModBusRtu(byte SlaveAddress, string CommPort, int BaudRate)
      {
      SLAVEADDRESSID = SlaveAddress;
      PORT = CommPort;
      BAUDRATE = BaudRate;
      }

    #endregion

    #region properties

    private string ERRORSTRING;
    public string ErrorString
      {
      get { return ERRORSTRING; }
      set { ERRORSTRING = value; }
      }

    #endregion

    #region Device control register 1
    const ushort SafetySpeedCommand = 0x0401;
    const ushort ServoOnCommand = 0x0403;
    const ushort AlarmResetCommand = 0x0407;
    const ushort BrakeForceReleaseCommand = 0x0408;
    const ushort PauseCommand = 0x040A;
    const ushort HomingCommand = 0x040B;
    const ushort PositionStartCommand = 0x040C;
    const ushort JogInchCommand = 0x0411;
    const ushort TeachingModeCommand = 0x0414;
    const ushort PositionDataLoadComamnd = 0x415;
    const ushort JogPlusCommand = 0x0416;
    const ushort JogMinusCommand = 0x0416;
    #endregion

    #region Controller Monitor Information Registers
    const ushort CurrentPositionMonitor = 0x9000;
    const ushort PresentAlarmCodeQuery = 0x9002;
    const ushort InputPortQuery = 0x9003;
    const ushort OutputPortMonitorQuery = 0x9004;
    const ushort DeviceStatusQuery1 = 0x9005;
    const ushort DeviceStatusQuery2 = 0x9006;
    const ushort ExpansionDeviceStatusQuery = 0x9007;
    const ushort SystemStatusQuery = 0x9008;
    const ushort CurrentSpeedMonitor = 0x900A;
    const ushort CurrentAmpereMonitor = 0x900C;
    const ushort DeviationMonitor = 0x900E;
    const ushort SystemTimerQuery = 0x9010;
    const ushort SpecialInputPortQuery = 0x9012;
    const ushort ZoneStatusQuery = 0x9013;
    const ushort CompletePositionNumberStatusQuery = 0x9014;
    #endregion

    #region Device Status register
    public ushort ServoOnStatus = 0x0103;
    public ushort HomingCompletationStatus = 0x010B;
    public ushort PositioningCompletationStatus = 0x10C;
    #endregion

    public bool OpenComPort()
      {
      try {
        comPort = new SerialPort(PORT, BAUDRATE, Parity.None, 8, StopBits.One);
        comPort.Handshake = Handshake.None;
        comPort.ReceivedBytesThreshold = 1;
        comPort.Open();
        return true;
        }
      catch (Exception ex) {
        ERRORSTRING = ex.ToString();
        return false;
        }
      }

    public void CloseComPort()
      {
      if (comPort.IsOpen) comPort.Close();
      }

    #region primitive functions
    private int Convert4ByteToInt(byte Byte1, byte Byte2, byte Byte3, byte Byte4)
      {
      //return Byte3 * (int)Math.Pow(256, 3) + Byte4 * (int)Math.Pow(256, 2) + Byte1 * 256 + Byte2;
      return Byte4 * (int)Math.Pow(256, 3) + Byte3 * (int)Math.Pow(256, 2) + Byte2 * 256 + Byte1;
      }

    private UInt16 CalcCRC(byte[] chkbuf, byte len)
      {
      #region CRC tables

      byte[] arCRCHi = new byte[256]
      {
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40 
      };

      byte[] arCRCLo = new byte[256]
      {
        0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04,
        0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8,
        0xD8, 0x18, 0x19, 0xD9, 0x1D, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC,
        0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3, 0x11, 0xD1, 0xD0, 0x10,
        0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
        0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
        0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C,
        0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26, 0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0,
        0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4,
        0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
        0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C,
        0xB4, 0x74, 0x75, 0xB5, 0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
        0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54,
        0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98,
        0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
        0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80, 0x40    
      };

      #endregion

      byte uchCRCHi = 0xff;
      byte uchCRCLo = 0xff;
      UInt16 uIndex;
      UInt16 temp_code;
      byte i = 0;

      while (len > 0) {
        uIndex = (UInt16)(uchCRCHi ^ chkbuf[i++]);

        uchCRCHi = (byte)(uchCRCLo ^ arCRCHi[uIndex]);
        uchCRCLo = arCRCLo[uIndex];
        len--;
        }
      temp_code = (UInt16)uchCRCHi;
      temp_code = (UInt16)(temp_code << 8);
      return (UInt16)(temp_code | uchCRCLo);
      }

    private bool ReadCoilStatus(ushort StartAddress, ushort Count, byte[] RetBuf)
      {
      byte[] ar = new byte[8];
      UInt16 CRC;
      const byte FunctionCode = 0x01;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      ar[4] = Convert.ToByte(Count / 256);            // Count High 
      ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)
      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private bool ReadInputStatus(ushort StartAddress, ushort Count, byte[] RetBuf)
      {
      byte[] ar = new byte[8];
      UInt16 CRC;
      const byte FunctionCode = 0x02;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      ar[4] = Convert.ToByte(Count / 256);            // Count High 
      ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)
      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private bool ForceSingleCoil(ushort StartAddress, bool Status, byte[] RetBuf)
      {
      byte[] ar = new byte[8];
      UInt16 CRC;
      const byte FunctionCode = 0x05;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      if (Status == true) {
        ar[4] = 0xFF;
        ar[5] = 0x00;
        }
      else {
        ar[4] = 0x00;
        ar[5] = 0x00;
        }
      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private bool ReadHoldingRegisters(ushort StartAddress, ushort Count, byte[] RetBuf)
      {
      byte[] ar = new byte[8];
      UInt16 CRC;
      const byte FunctionCode = 0x03;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      ar[4] = Convert.ToByte(Count / 256);            // Count High 
      ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)
      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private void ReadInputRegisters()
      {

      }

    private bool PresetSingleRegister(ushort StartAddress, Int16 Value, byte[] RetBuf)
      {
      byte[] ar = new byte[8];
      UInt16 CRC;
      const byte FunctionCode = 0x6;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      ar[4] = Convert.ToByte(Value / 256);            // Value High 
      ar[5] = Convert.ToByte(Value % 256);            // Value Low 

      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    public bool ReadExeptionStatus(ref byte ExceptionStatus)
      {
      byte[] RetBuf = new byte[5];
      byte[] ar = new byte[4];
      UInt16 CRC;
      const byte FunctionCode = 0x07;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      CRC = CalcCRC(ar, 2);
      ar[2] = Convert.ToByte(CRC / 256);
      ar[3] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 4);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode)) {
        ExceptionStatus = RetBuf[2];
        return true;
        }
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private void ForceMultipleCoils()
      {

      }

    private bool PresetMultipleRegisters(ushort StartAddress, ushort RegisterCount,
                                         ushort ByteCount, byte[] InpBuf, byte[] RetBuf)
      {
      byte[] ar = new byte[255];
      UInt16 CRC;
      const byte FunctionCode = 0x10;   // 16 dec

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
      ar[4] = Convert.ToByte(RegisterCount / 256);    // Count High 
      ar[5] = Convert.ToByte(RegisterCount % 256);    // Count Low (number of locations to be read)

      CRC = CalcCRC(ar, 6);
      ar[6] = Convert.ToByte(CRC / 256);
      ar[7] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 8);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }

    private void ReportSlave()
      {

      }

    private void ReadWritRegisters()
      {

      }
    #endregion

    #region ForceSingleCoil -- Write
    public bool ServoOn(bool SOn)
      {
      byte[] ar = new byte[255];
      return ForceSingleCoil(ServoOnCommand, SOn, ar);
      }

    public bool SafetySpeed(bool OnOff)
      {
      byte[] ar = new byte[255];
      return ForceSingleCoil(SafetySpeedCommand, OnOff, ar);
      }

    public bool AlarmReset()
      {
      byte[] ar = new byte[255];
      return (ForceSingleCoil(AlarmResetCommand, true, ar) && ForceSingleCoil(AlarmResetCommand, false, ar));
      }

    public bool Pause(bool Status)
      {
      byte[] ar = new byte[255];
      return ForceSingleCoil(PauseCommand, Status, ar);
      }

    public bool Homing()
      {
      byte[] ar = new byte[255];

      return (ForceSingleCoil(HomingCommand, true, ar) && ForceSingleCoil(HomingCommand, false, ar));
      }

    public bool PositionStart()
      {
      byte[] ar = new byte[255];
      return (ForceSingleCoil(PositionStartCommand, true, ar) && ForceSingleCoil(PositionStartCommand, false, ar));
      }

    public bool MoveActuator(int TargetPosition, ushort InPositionBand,
                    ushort Speed, ushort AccDec, ushort PushCurrentLimiting, ushort ControlFlag, byte[] RetBuf)
      {
      byte[] ar = new byte[27];
      UInt16 CRC;
      const byte FunctionCode = 0x10;   // 16 dec

      int P3 = (int)Math.Pow(256, 3);
      int P2 = (int)Math.Pow(256, 2);
      int P1 = 256;
      int StartAddress = 0x9900;

      ar[0] = SLAVEADDRESSID;
      ar[1] = FunctionCode;                           // function code
      ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
      ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from

      ar[4] = 0;
      ar[5] = 9;                                      // resgister count
      ar[6] = 18;                                     // byte copunt 

      ar[8] = Convert.ToByte((TargetPosition % P3) / P2);
      ar[7] = Convert.ToByte(TargetPosition / P3);
      ar[9] = Convert.ToByte((TargetPosition % P2) / P1);
      ar[10] = Convert.ToByte((TargetPosition % P1));

      ar[12] = Convert.ToByte((InPositionBand % P3) / P2);
      ar[11] = Convert.ToByte(InPositionBand / P3);
      ar[13] = Convert.ToByte((InPositionBand % P2) / P1);
      ar[14] = Convert.ToByte((InPositionBand % P1));

      ar[15] = Convert.ToByte((Speed % P3) / P2);
      ar[16] = Convert.ToByte(Speed / P3);
      ar[17] = Convert.ToByte((Speed % P2) / P1);
      ar[18] = Convert.ToByte((Speed % P1));

      ar[19] = Convert.ToByte(AccDec / 256);
      ar[20] = Convert.ToByte(AccDec % 256);

      ar[21] = Convert.ToByte(PushCurrentLimiting / 256);
      ar[22] = Convert.ToByte(PushCurrentLimiting % 256);

      ar[23] = Convert.ToByte(ControlFlag / 256);
      ar[24] = Convert.ToByte(ControlFlag % 256);

      CRC = CalcCRC(ar, 25);
      ar[25] = Convert.ToByte(CRC / 256);
      ar[26] = Convert.ToByte(CRC % 256);

      comPort.Write(ar, 0, 27);
      System.Threading.Thread.Sleep(TimeOut * ar.Length);
      comPort.Read(RetBuf, 0, comPort.BytesToRead);

      if ((RetBuf[0] == SLAVEADDRESSID) && (RetBuf[1] == FunctionCode))
        return true;
      else {
        ErrorString = "mensagem corrompida";
        return false;
        }
      }
    #endregion

    #region (Read holding registers) -- Read Controller Monitor Information Registers
    public bool ReadCurrentPosition(ref int MotorPos)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(CurrentPositionMonitor, 2, ar)) {
        //    MotorPos = (ar[5] * 256 + ar[6]) ; em condições normais
        //MotorPos = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        MotorPos = Convert4ByteToInt(ar[6], ar[5], ar[4], ar[3]);
        return true;
        }
      else
        return false;
      }

    public bool ReadPresentAlarmCode(ref int AlarmCode)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(PresentAlarmCodeQuery, 1, ar)) {
        AlarmCode = ar[3] * 256 + ar[4];
        return true;
        }
      else
        return false;
      }

    public bool ReadDeviceStatusQuery1(ref int Status)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(ZoneStatusQuery, 1, ar)) {
        Status = ar[3] * 256 + ar[4];
        return true;
        }
      else
        return false;
      }

    public bool ReadSystemStatus(ref int Status)  // 2 registers (4 byte)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(SystemStatusQuery, 2, ar)) {
        Status = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        return true;
        }
      else
        return false;
      }

    public bool ReadCurrentSpeed(ref int Speed)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(CurrentSpeedMonitor, 2, ar)) {
        Speed = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        return true;
        }
      else
        return false;
      }

    public bool ReadCurrentAmpere(ref int Ampere)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(CurrentAmpereMonitor, 2, ar)) {
        Ampere = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        return true;
        }
      else
        return false;
      }

    public bool ReadDeviation(ref int Dev)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(DeviationMonitor, 2, ar)) {
        Dev = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        return true;
        }
      else
        return false;
      }

    public bool ReadSystemTimer(ref int msec)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(SystemTimerQuery, 2, ar)) {
        msec = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
        return true;
        }
      else
        return false;
      }

    public bool ReadZoneStatus(ref int ZStatus)
      {
      byte[] ar = new byte[255];
      if (ReadHoldingRegisters(ZoneStatusQuery, 1, ar)) {
        ZStatus = ar[3] * 256 + ar[4];
        return true;
        }
      else
        return false;
      }

    //

    public bool ReadRegisterBit(ushort BitAddress, ref bool OnOff)
      {
      byte[] ar = new byte[255];
      if (ReadInputStatus(BitAddress, 1, ar)) {
        if (ar[3] == 0) {
          OnOff = false;
          return true;
          }
        else if (ar[3] == 1) {
          OnOff = true;
          return true;
          }
        else
          return false;
        }
      else
        return false;
      }
    #endregion
    }
  }
