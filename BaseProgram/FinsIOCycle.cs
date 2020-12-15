using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace BaseProgram
{
    class FinsIOCycle
    {

        public Thread IOThread;
        public Fins_Udp_Connection MySk;
        Stopwatch stopwatch = new Stopwatch();
        Stopwatch stopwatch1 = new Stopwatch();
        //Definição de Arrays para leitura de entradas/saídas 
        public byte[] bk_Read_DI = new byte[Fins_Msg_Control.SizeHeader + Globals2Work.Di_Plc.N_Word2Read * 2];
        public byte[] bk_Read_DO = new byte[Fins_Msg_Control.SizeHeader + Globals2Work.Di_Plc.N_Word2Read * 2];
        public byte[] bk_Read_AI = new byte[80];
        public byte[] bk_Write_DO;

        public FinsIOCycle(Fins_Udp_Connection BK)
        {
            MySk = BK;

            IOThread = new Thread(new ThreadStart(this.IOReadWrite))
            {
                Name = "IOThread"
            };
            Program.blnExit = false;
            IOThread.Start();
        }

        //Função para fazer o Update do ValueRead do Array DI
        private void UpdateArrayDI()
        {
            int j = 0;
            if (bk_Read_DI.Length > 0)
                for (int i = 0; i < Program.Dt_DI.Rows.Count; i++)
                {
                    j = i / 8;
                    try
                    {
                        Program.UpdateDIORows(i, "Value", ((bk_Read_DI[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDI, "");
                        //Program.Dt_DI.Rows[i]["Value"] = ((bk_Read_DI[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8))));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("UpdateArrayDI Error: " + exp.ToString());
                    }
                }
        }


        //Função para fazer o Update do ValueRead do Array DO
        private void UpdateArrayDO()
        {
            int j = 0;
            if (bk_Read_DO.Length > 0)
                for (int i = 0; i < Program.Dt_DO.Rows.Count; i++)
                {
                    j = i / 8;
                    try
                    {
                        //Program.Dt_DO.Rows[i]["Value"] = ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8))));
                        //Program.UpdateDORows(i, "Value", ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))));
                        Program.UpdateDIORows(i, "Value", ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDO, "");
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("UpdateArrayDO Error: " + exp.ToString());
                    }
                }
        }


        //Função para actualizar Array BK_Write com o valor a escrever no PLC
        private void UpdateBK_Write()
        {
            try
            {
                int j = 0;
                for (j = 0; j <= bk_Write_DO.Length / 8; j++)
                {
                    bk_Write_DO[j] = 0;
                }
                for (int i = 0; i < Program.Dt_DO.Rows.Count; i++)
                {
                    //if (Program.doMap[i].ValueWrite) {
                    if (Program.UpdateDIORows(i, "ValueToWrite", null, ReadWriteIO.ReadDO, ""))
                    {
                        j = i / 8;
                        bk_Write_DO[j] += Convert.ToByte(Math.Pow(2, (i - j * 8)));
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("UpdateBK_Write Exception");
            }
        }


        //Função executada pelo Thread para Escrita/Leitura de Entradas/Saídas de PLC
        private void IOReadWrite()
        {

            string error = "";
            byte[] Msg2Send, ReceivedMsg, DO_Order_Msg;

            do
            {
                stopwatch.Start();
                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Entradas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Read_DI.Length > 0)
                {
                    //MARIA MyBK.ReadMultipleDigitalInputs(0, (Int16)bk_Read_DI.Length, bk_Read_DI);
                    //Filipe  MyBK.ReadDigitalInput(0, (Int16)bk_Read_DI.Length, bk_Read_DI);
                    if (MySk.Status == 1)
                    {
                        //// Prepare the fins message to send

                        Msg2Send = Globals2Work.Di_Plc.Read_DataFromPLC();

                        error = MySk.Send_Data(Msg2Send);
                        ReceivedMsg = MySk.Receive_Data();

                        //Print error case Message failed to send
                        //Console.WriteLine(error);
                        // Remove header from the response message	
                        if (ReceivedMsg != null)
                        {
                            bk_Read_DI = Fins_Msg_Control.Remove_HeaderFromOrigMSG(ReceivedMsg);
                            // order the message from 0-16 bit
                            bk_Read_DI = Fins_SMC_Control.OrderFinsMsg(bk_Read_DI);
                            // update Array of inputs
                            UpdateArrayDI();
                        }
                    }

                }

                Thread.Sleep(20);

                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Saidas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Read_DO.Length > 0)
                {
                    //MARIA MyBK.ReadMultipleDigitalOutputs(0, (Int16)bk_Read_DO.Length, bk_Read_DO);
                    // MyBK.ReadDigitalOutput(0, (Int16)bk_Read_DO.Length, bk_Read_DO);
                    //		// Prepare the fins message to send
                    if (MySk.Status == 1)
                    {
                        Globals2Work.Do_Plc.comand = Fins_SMC_Control.Command.Read;
                        Msg2Send = Globals2Work.Do_Plc.Read_DataFromPLC();
                        error = MySk.Send_Data(Msg2Send);
                        ReceivedMsg = MySk.Receive_Data();
                        // Remove header from the response message
                        if (ReceivedMsg != null)
                        {
                            bk_Read_DO = Fins_Msg_Control.Remove_HeaderFromOrigMSG(ReceivedMsg);
                            // order the message from 0-16 bit
                            bk_Read_DO = Fins_SMC_Control.OrderFinsMsg(bk_Read_DO);
                            // update Array of inputs
                            UpdateArrayDO();
                        }
                    }
                }
                Thread.Sleep(20);

                int test = Program.Dt_DO.Rows.Count;
                int r = test / 16;
                r += (test % 16 == 0 ? 0 : 1);
                bk_Write_DO = new byte[r * 2];
                //-----------------------------------------------------------------------
                //Ciclo de Escrita de Saídas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Write_DO.Length > 0)
                {
                    if (Program.NeedToWrite)
                    {
                        UpdateBK_Write();

                        //MyBK.WriteMultipleDigitalOutputs(0, (Int16)bk_Write_DO.Length, bk_Write_DO);
                        // Prepare the fins message to send
                        Globals2Work.Do_Plc.comand = Fins_SMC_Control.Command.Write;
                        Globals2Work.Do_Plc.N_words = ((ushort)r);
                        // order the message to send
                        DO_Order_Msg = Fins_SMC_Control.OrderFinsMsg(bk_Write_DO);

                        Msg2Send = Globals2Work.Do_Plc.BuildFinsMsg(DO_Order_Msg);
                        error = MySk.Send_Data(Msg2Send);
                        // mensagem recebida de resposta indicadora de erro na mensagem
                        ReceivedMsg = MySk.Receive_Data();
                        Array.Clear(DO_Order_Msg, 0, DO_Order_Msg.Length);
                        Array.Clear(Msg2Send, 0, Msg2Send.Length);
                        Array.Clear(bk_Write_DO, 0, bk_Write_DO.Length);

                        Program.NeedToWrite = false;


                    }
                }
                Thread.Sleep(20);

                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Entradas Analógicas:
                //-----------------------------------------------------------------------
                if (bk_Read_AI.Length > 0)
                {
                    for (int i = 0; i < Program.Dt_AI.Rows.Count; i++)
                    {
                        int valorAnalogica = 0;
                        //   MyBK.ReadAnalogInput((short)(i * 2 + 1), ref valorAnalogica);
                        //Program.aiMap[i].ValueRead = valorAnalogica;
                        Program.Dt_AI.Rows[i]["Value"] = valorAnalogica;
                    }
                }

                //-----------------------------------------------------------------------
                //Ciclo de Escrita Saída Analógica:
                //-----------------------------------------------------------------------        
                //MyBK.WriteAnalogOutput(1, MenuPrincipal.writeAnalogValue);
                //MyBK.WriteAnalogOutput(5, ((0.99425 * MenuPrincipal.writeAnalogValue + 0.026) * 327.67));
                Thread.Sleep(20);

                Program.cicloIOon = true;
                stopwatch.Stop();


                //Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
                stopwatch.Reset();
            } while (MySk.Status == 1 && Program.blnExit == false);

            Program.cicloIOon = false;
        }



    }
}
