using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace BaseProgram
  {
  class IOCycle
    {
    public Thread IOThread;
    public ModBus MyBK;

    //Definição de Arrays para leitura de entradas/saídas 
    public byte[] bk_Read_DI = new byte[128];
    public byte[] bk_Read_DO = new byte[128];
    public byte[] bk_Read_AI = new byte[80];
    public byte[] bk_Write_DO = new byte[128];

    public IOCycle(ModBus BK)
      {
      MyBK = BK;

      IOThread = new Thread(new ThreadStart(this.IOReadWrite));
      IOThread.Name = "IOThread";
      Program.blnExit = false;
      IOThread.Start();
      }

    //Função para fazer o Update do ValueRead do Array DI
    private void UpdateArrayDI()
      {
      int j = 0;
      if (bk_Read_DI.Length > 0)
        for (int i = 0; i < Program.Dt_DI.Rows.Count; i++) {
          j = i / 8;
          try {
            Program.UpdateDIORows(i, "Value", ((bk_Read_DI[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDI, "");
            //Program.Dt_DI.Rows[i]["Value"] = ((bk_Read_DI[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8))));
            }
          catch (Exception exp) {
            Console.WriteLine("UpdateArrayDI Error: " + exp.ToString());
            }
          }
      }


    //Função para fazer o Update do ValueRead do Array DO
    private void UpdateArrayDO()
      {
      int j = 0;
      if (bk_Read_DO.Length > 0)
        for (int i = 0; i < Program.Dt_DO.Rows.Count; i++) {
          j = i / 8;
          try {
            //Program.Dt_DO.Rows[i]["Value"] = ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8))));
            //Program.UpdateDORows(i, "Value", ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))));
            Program.UpdateDIORows(i, "Value", ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDO, "");
            }
          catch (Exception exp) {
            Console.WriteLine("UpdateArrayDO Error: " + exp.ToString());
            }
          }
      }


    //Função para actualizar Array BK_Write com o valor a escrever no PLC
    private void UpdateBK_Write()
      {
      try {
        int j = 0;
        for (j = 0; j <= bk_Write_DO.Length / 8; j++) {
          bk_Write_DO[j] = 0;
          }
        for (int i = 0; i < Program.Dt_DO.Rows.Count; i++) {
          //if (Program.doMap[i].ValueWrite) {
          if (Program.UpdateDIORows(i, "ValueToWrite", null, ReadWriteIO.ReadDO, "")) {
            j = i / 8;
            bk_Write_DO[j] += Convert.ToByte(Math.Pow(2, (i - j * 8)));
            }
          }
        }
      catch (Exception) {
        Console.WriteLine("UpdateBK_Write Exception");
        }
      }


    //Função executada pelo Thread para Escrita/Leitura de Entradas/Saídas de PLC
    private void IOReadWrite()
      {

      do {
        //-----------------------------------------------------------------------
        //Ciclo de Leitura de Entradas Digitais:
        //-----------------------------------------------------------------------
        if (bk_Read_DI.Length > 0) {
          //MARIA MyBK.ReadMultipleDigitalInputs(0, (Int16)bk_Read_DI.Length, bk_Read_DI);
            MyBK.ReadDigitalInput(0, (Int16)bk_Read_DI.Length, bk_Read_DI);

          UpdateArrayDI();
          }
        Thread.Sleep(10);

        //-----------------------------------------------------------------------
        //Ciclo de Leitura de Saidas Digitais:
        //-----------------------------------------------------------------------
        if (bk_Read_DO.Length > 0) {
          //MARIA MyBK.ReadMultipleDigitalOutputs(0, (Int16)bk_Read_DO.Length, bk_Read_DO);
            MyBK.ReadDigitalOutput(0, (Int16)bk_Read_DO.Length, bk_Read_DO);
          UpdateArrayDO();
          }
        Thread.Sleep(10);

        //-----------------------------------------------------------------------
        //Ciclo de Escrita de Saídas Digitais:
        //-----------------------------------------------------------------------
        if (bk_Write_DO.Length > 0) {
          if (Program.NeedToWrite) {
            UpdateBK_Write();
            MyBK.WriteMultipleDigitalOutputs(0, (Int16)bk_Write_DO.Length, bk_Write_DO);
            Program.NeedToWrite = false;
            Thread.Sleep(10);
            }
          }
        
        //-----------------------------------------------------------------------
        //Ciclo de Leitura de Entradas Analógicas:
        //-----------------------------------------------------------------------
        if (bk_Read_AI.Length > 0) {
        for (int i = 0; i < Program.Dt_AI.Rows.Count; i++)
          {
            int valorAnalogica = 0;
            MyBK.ReadAnalogInput((short)(i * 2 + 1), ref valorAnalogica);
            //Program.aiMap[i].ValueRead = valorAnalogica;
            Program.Dt_AI.Rows[i]["Value"] = valorAnalogica;
            }
          }

        //-----------------------------------------------------------------------
        //Ciclo de Escrita Saída Analógica:
        //-----------------------------------------------------------------------        
        //MyBK.WriteAnalogOutput(1, MenuPrincipal.writeAnalogValue);
        //MyBK.WriteAnalogOutput(5, ((0.99425 * MenuPrincipal.writeAnalogValue + 0.026) * 327.67));

        Thread.Sleep(10);

        Program.cicloIOon = true;
        } while (MyBK.wsStatus == 1 && Program.blnExit == false);

      Program.cicloIOon = false;
      }
    }
  }
