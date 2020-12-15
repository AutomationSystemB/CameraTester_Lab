using System;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using System.Diagnostics;

namespace BaseProgram
{

    public enum ReadWriteIO { ReadDI, ReadDO, WriteDI, WriteDO }

    //Estrutura Fixa da tabela de DI - Digital Input; 
    public struct DI
    {
        public string IOName;
        public int IOAddress;
        public string IOText;
        public string IOTag;
        public string ModuleType;
        public bool ValueRead;
    }

    //Estrutura Fixa da tabela de DO - Digital Output; 
    public struct DO
    {
        public string IOName;
        public int IOAddress;
        public string IOText;
        public string IOTag;
        public string ModuleType;
        public bool ValueRead;
        public bool ValueWrite;
    }

    //Estrutura Fixa da tabela de AI - Analog Input; 
    public struct AI
    {
        public string IOName;
        public int IOAddress;
        public string IOText;
        public string IOTag;
        public string ModuleType;
        public int ValueRead;
    }

    static class Program
    {
        public static DO[] doMap;
        public static DI[] diMap;
        public static AI[] aiMap;

        public static DataTable Dt_DO;
        public static DataTable Dt_DI;
        public static DataTable Dt_AI; 

        public static bool[] doEnable;

        public static int diMapMax;
        public static int doMapMax;
        public static int aiMapMax;

	
        public static bool cicloIOon;
        public static bool blnExit;
        public static bool NeedToWrite = false;
	
	

        //static public string BK_IPAddress = "172.16.17.1";

        private static object thislock = new object();
    
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            string aProcName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(aProcName).Length > 1)
            {
                MessageBox.Show("Já existe uma instância desta aplicação a correr!");
                return;
            }

            XMLFile xml1 = new XMLFile();
            IOMaps io1 = new IOMaps();

            //Criar o array 2D com todos IOs
            string[,] ioMap = xml1.ArrayIO();
            int ioMapRows = xml1.ArrayLength();

            //Ler a dimensão máxima necessária para cada array de estruturas
            diMapMax = io1.Dimensao(ioMap, "DI");
            doMapMax = io1.Dimensao(ioMap, "DO");
            aiMapMax = io1.Dimensao(ioMap, "AI");

            //Criação de Array de Structs        
            diMap = new DI[diMapMax];
            doMap = new DO[doMapMax];
            aiMap = new AI[aiMapMax];

            //Criação de Array de Structs
            doEnable = new bool[doMapMax];

            //Preenchimento do Array de Structs
            if (diMapMax > 0) io1.PreencheMapaDI(ioMap, diMap);
            if (doMapMax > 0) io1.PreencheMapaDO(ioMap, doMap);
            if (aiMapMax > 0) io1.PreencheMapaAI(ioMap, aiMap);

            //Criação de Array de IO:
            GetIOArrayFromIOMap();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MenuPrincipal());
        }

        private static void GetIOArrayFromIOMap()
        {
            Dt_DO = new DataTable("DO_Table");
            Dt_DI = new DataTable("DI_Table");
            Dt_AI = new DataTable("AI_Table");

            DataColumn DcName, DcAddr, DcVal, DCValToWrite;
            DataColumn[] Keys = new DataColumn[1];

            // Config Table DI:
            DcName = new DataColumn("DIName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(bool));

            Keys[0] = DcName;
            Dt_DI.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal });
            Dt_DI.PrimaryKey = Keys;

            //Config Table DO:
            DcName = new DataColumn("DOName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(bool));
            DCValToWrite = new DataColumn("ValueToWrite", typeof(bool));

            Keys[0] = DcName;
            Dt_DO.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal, DCValToWrite });
            Dt_DO.PrimaryKey = Keys;

            //Config Table AI:
            DcName = new DataColumn("AIName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(int));

            Keys[0] = DcName;
            Dt_AI.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal });
            Dt_AI.PrimaryKey = Keys;

            for (int i = 0; i < diMap.Length; i++) { Dt_DI.Rows.Add(new object[] { diMap[i].IOName, diMap[i].IOAddress, diMap[i].ValueRead }); }
            for (int i = 0; i < doMap.Length; i++) { Dt_DO.Rows.Add(new object[] { doMap[i].IOName, doMap[i].IOAddress, doMap[i].ValueRead, false }); }
            for (int i = 0; i < aiMap.Length; i++) { Dt_AI.Rows.Add(new object[] { aiMap[i].IOName, aiMap[i].IOAddress, aiMap[i].ValueRead }); }
        }

        public static bool UpdateDIORows(int IOIndex, string IOField, Nullable<bool> State, ReadWriteIO ReadWrite, string IOName)
        {
            lock (thislock)
            {

                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);

                try
                {
                    //Para leitura de DI:
                    if (ReadWrite == ReadWriteIO.ReadDI)
                    {
                        if (IOName.Equals(""))
                        {
                            return (bool)Program.Dt_DI.Rows[IOIndex][IOField];
                        }
                        else
                        {
                            int Rowindex = Program.Dt_DI.Rows.IndexOf(Program.Dt_DI.Select("DIName = '" + IOName + "'")[0]);
                            return (bool)Program.Dt_DI.Rows[Rowindex][IOField];
                        }
                    }

                    //Para leitura de DO:
                    else if (ReadWrite == ReadWriteIO.ReadDO)
                    {
                        if (IOName.Equals(""))
                        {
                            return (bool)Program.Dt_DO.Rows[IOIndex][IOField];
                        }
                        else
                        {
                            int Rowindex = Program.Dt_DO.Rows.IndexOf(Program.Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                            return (bool)Program.Dt_DO.Rows[Rowindex][IOField];
                        }
                    }

                    //Para escrita de DI:
                    else if (ReadWrite == ReadWriteIO.WriteDI)
                    {
                        if (IOName.Equals(""))
                        {
                            Program.Dt_DI.Rows[IOIndex][IOField] = State;
                            return true;
                        }
                        else
                        {
                            int Rowindex = Program.Dt_DI.Rows.IndexOf(Program.Dt_DO.Select("DIName = '" + IOName + "'")[0]);
                            Program.Dt_DI.Rows[Rowindex][IOField] = State;
                            return true;
                        }
                    }

                    //Para escrita de DO:
                    else if (ReadWrite == ReadWriteIO.WriteDO)
                    {
                        if (IOName.Equals(""))
                        {
                            Program.Dt_DO.Rows[IOIndex][IOField] = State;
                            return true;
                        }
                        else
                        {
                            int Rowindex = Program.Dt_DO.Rows.IndexOf(Program.Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                            Program.Dt_DO.Rows[Rowindex][IOField] = State;
                            return true;
                        }
                    }

                    else
                    {
                        MessageBox.Show("UpdateDIORows Error: \r\n\r\nWrong Parameters received");
                        return false;
                    }

                }
                catch (Exception exp)
                {
                    MessageBox.Show("UpdateDIORows Error: " + exp.ToString());
                    return false;
                }
            }
        }





    }
}