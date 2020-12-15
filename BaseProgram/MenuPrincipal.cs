using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Xml;

using System.IO;
using System.IO.Ports;

using System.Net.Sockets;
using System.Net;

using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace BaseProgram
{
    public partial class MenuPrincipal : Form
    {
        public enum DataSource { SQL, XML }

        #region Variables 

        bool firstAddList = true;           // Para carregar apenas as listView uma vez
        bool posCasa;                       // Flag que indica Posição casa
        bool erroPosCasa = false;
        private bool MustSelectRef = true;  // Para obrigar a escolher sempre uma referência
        private bool blnStopTimer = false;  // Para garantir que o Timer termina a sua execução.
        bool eixosPosCasa = false;          // Sempre que se Inicia um Novo Ciclo Colocar na Posição Casa os Eixos 
        bool callLogin = false;             // Sempre que se Inicia um Novo Ciclo Colocar na Posição Casa os Eixos 
        Color colorError = Color.Red;       // Cor da textBox vermelho (Erro)
        Color colorSystem;                  // Cor da textBox por defeito
        bool blnCircuitoSeg_OK = false;
        bool ShowPanel = false;
        int image = 0;

        // bool enableShift = true, timeout = false;
        bool Flag_shown = false;            //Por defeito o turno está habilitado
        int userLevel = 0;                  //Nivel de acesso do utilizador
        int usershift = 0;                  //Gravação no Jet

        //Informação relativa à Referência seleccionada:
        string sel_model, strTinyRef, model_version;

        //Parametros do modelo:   
        string RefMainBoard, RefHousing;
        string pProducts, pVisionHousingSN, pVisionPCBSN, pVisionCoverVerify;
        int pScrewTableID, pPuttyTableID, pGocatorTableID, pScrewCycle, pPuttyType, MaxPuttyLimits, MinPuttyLimits;

        //Base de dados:
        byte SubWS = 0;
        int WSID = 0;
        int ActualRefID;
        int intErrorCode;

        //Configurações de equipamentos no ficheiro Init:
        string OMRON_IPAddress = "", DALSA_IPAddress = "", Gocator_IPAdress = "192.168.1.60", SCANNER_COM = "";
        string StationName = "";

        //Origem de dados:
        DataSource UsersDataSource;
        DataSource RefsDataSource;
        DataSource ParamsDataSource;

        //NewTables
        DataSource ScrewParDataSource;
        DataSource PuttyParDataSource;
        DataSource GocatorParDataSource;

        double[] visionResult = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        #endregion

        #region Instancias/DataSets

        //Instancia para o IO (BK9100);
        ModBus bk1 = new ModBus();

        //Socket Vision
        Socket Vision = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket VisionGocator = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ip, ip_Gocator;

        //Dataset para referencias:
        DataSet dsRefs = new DataSet("Ref DataSet");
        DataSet dsParams = new DataSet("Params DataSet");
        DataSet DGVDSet = new DataSet("DGVDSet");

        //NewTables
        DataSet dsScrewPar = new DataSet("Screw Positions");
        DataSet ScrewParSet = new DataSet("ScrewSet");
        DataSet dsPuttyPar = new DataSet("Putty Positions");
        DataSet PuttyParSet = new DataSet("PuttySet");
        DataSet dsGocatorPar = new DataSet("Gocator Positions");
        DataSet GocatorParSet = new DataSet("GocatorSet");

        //Dataset para a Língua:
        string ActualLang = "EN";
        DataSet dsLang = new DataSet("Languages");

        //Dataset para a inicialização:
        DataSet dsInit = new DataSet("Init");

        EndPoint ep;
        const int FINS_UDP_PORT = 9600;
        string SERV_IP_ADDR = "192.168.001.001";
       
        #endregion

        public MenuPrincipal()
        {
            InitializeComponent();
        }

        private void MenuPrincipal_Load(object sender, EventArgs e)
        {
            //Carrega o INIT.XML para um Dataset:
            if (!InitVars()) { this.Dispose(); return; }

            //Inicia Thread para comunicação e leitura/escrita de IO
            ConnectThreadIO();
            ManualErrorPanel.Hide();


            //Verifica o estado das emergências e afins:
            timerGeneric.Interval = 500;
            timerGeneric.Start();

            //Endereço IP do sistema de visão 
            ip = new IPEndPoint(IPAddress.Parse(DALSA_IPAddress), 1024);
            ip_Gocator = new IPEndPoint(IPAddress.Parse(Gocator_IPAdress), 8190);
           
            //Ligar o relé de emergência:
            //amaro
            //WriteDOByName("Safety_Relay_On", true);

            //Cor Cinza do Sistema
            colorSystem = txtInstructions.BackColor;

            //Login:
            //if (!LogUserIn(UsersDataSource)) { CloseApplication(); return; }
            userLevel = 1;
            UpdatePasswordLogin("Operator Default", "0000");

            //Carrega referencias:
            if (!LoadReferences(ref dsRefs, RefsDataSource)) { CloseApplication(); return; }

            //Carrega Parametros:
            if (!LoadParameters(ref dsParams, ParamsDataSource)) { CloseApplication(); return; }


            //Referenças

            DataRow Dr = dsRefs.Tables[0].NewRow();
            Dr["ID_Ref"] = "0";
            Dr["Ref"] = TextByTag(15);
            dsRefs.Tables[0].Rows.InsertAt(Dr, 0);
            comboBoxModelo.DataSource = dsRefs.Tables[0];
            comboBoxModelo.DisplayMember = "Ref";
            comboBoxModelo.ValueMember = "ID_Ref";

            //Carrega as mensagens da lingua escolhida
            if (!UpdateFormLanguage()) { CloseApplication(); return; }
            this.WindowState = FormWindowState.Maximized;

        }

        private bool LoadReferences(ref DataSet DsLoad, DataSource source)
        {
            try
            {
                if (source == DataSource.SQL)
                {
                    //Carrega Referências através da BD:
                    DsLoad = new DataSet("dsRefs");
                    //SQL//DsLoad = MyDB.FillDatasetBySQL("SELECT * FROM Refs WHERE Active=1 AND RefGroup = 'UIF TT Housing' OR RefGroup = 'UIF R8' ORDER BY RefPreh");
                }
                else if (source == DataSource.XML)
                {
                    //Carrega Referências através de XML:
                    DsLoad = new DataSet("dsRefs");
                    DsLoad.ReadXml("Refs.xml");
                }
                return true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(TextByTag(501) + "'LoadReferences()':\r\n\r\n" + exp.ToString()); //Erro ao inicializar variáveis em
                return false;
            }
        }

        private bool LoadParameters(ref DataSet DsLoad, DataSource source)
        {
            try
            {
                if (source == DataSource.SQL)
                {
                    //Carrega Parametros através da BD:
                    DsLoad = new DataSet("Params");
                    //SQL//DsLoad = MyDB.FillDatasetBySQL("SELECT * FROM Devices_Parameters WHERE ID_WS = '" + WSID + "'");
                }
                else if (source == DataSource.XML)
                {
                    //Carrega Parametros através de XML:
                    DsLoad = new DataSet("Params");
                    DsLoad.ReadXml("Params.xml");
                }
                return true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(TextByTag(501) + "'LoadParameters()':\r\n\r\n" + exp.ToString()); //Erro ao inicializar variáveis em
                return false;
            }
        }

        private bool InitVars()
        {
            try
            {
                //---------------------------------------------------------------------------------------
                //                                    INIT.XML FILE
                //---------------------------------------------------------------------------------------
                dsInit.ReadXml("INIT.xml");

                //Linguagem predefinida:
                ActualLang = dsInit.Tables[0].Rows[0]["DEFAULT_LANGUAGE"].ToString().ToUpper();

                //Definições:        
                WSID = int.Parse(dsInit.Tables[0].Rows[0]["WS_ID"].ToString());
                SubWS = byte.Parse(dsInit.Tables[0].Rows[0]["SUB_WS_ID"].ToString());
                StationName = dsInit.Tables[0].Rows[0]["STATION_NAME"].ToString();

                //Endereços
                OMRON_IPAddress = dsInit.Tables[0].Rows[0]["PLC_IP_ADDR"].ToString();
                DALSA_IPAddress = dsInit.Tables[0].Rows[0]["DALSA_IP_ADDR"].ToString();
                SCANNER_COM = dsInit.Tables[0].Rows[0]["SCANNER_COM"].ToString();

                //Origem de dados (SQL/XML):
                UsersDataSource = dsInit.Tables[0].Rows[0]["USERS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                RefsDataSource = dsInit.Tables[0].Rows[0]["REFS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                ParamsDataSource = dsInit.Tables[0].Rows[0]["PARAMS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;

                //NewTables
                ScrewParDataSource = dsInit.Tables[0].Rows[0]["PARAMS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                PuttyParDataSource = dsInit.Tables[0].Rows[0]["PARAMS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                GocatorParDataSource = dsInit.Tables[0].Rows[0]["PARAMS_DATASOURCE"].ToString().ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;

                //---------------------------------------------------------------------------------------
                //                                  LANG.XML FILE
                //---------------------------------------------------------------------------------------

                dsLang.ReadXml("LANG.xml");
                DataColumn[] Keys = new DataColumn[1];
                Keys[0] = dsLang.Tables[0].Columns["Tag"];
                dsLang.Tables[0].PrimaryKey = Keys;

                return true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(TextByTag(1037) + exp.ToString());
                return false;
            }
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {

            if (tabControl1.SelectedTab == PagAuto)
            {
                WriteDOByName("Safety_Relay_On", true);
            }

            //Escolhido o Tab do Modo Manual (IO) com maquina Parada:
            if (tabControl1.SelectedTab == PagManual && backgroundWorkerAuto.IsBusy == false && backgroundWorkerHome.IsBusy == false)
            {

                for (int j = 0; j < lvDO.Items.Count; j++)
                {
                    //if (j != 16) //Porta de Operação
                    lvDO.Items[j].Checked = false;
                }

                //Desligar
                WriteDOByName("Safety_Relay_On", false);

                if (userLevel > 1)
                {
                    lvDI.Enabled = true;
                    lvDO.Enabled = true;
                    grpScanner.Enabled = true;
                    grpX.Enabled = true;
                    grpX1.Enabled = true;
                    grpY.Enabled = true;
                    grpZ.Enabled = true;

                }

                if (firstAddList == true)
                {
                    AddListDOItems();
                    AddListDIItems();
                    firstAddList = false;
                }

                //Actualiza o estado da máquina quando entra no modo Manual
                UpdateAutoToManual();

                //Iniciar o timer manual
                blnStopTimer = false;
                timerModoManual.Interval = 250;
                timerModoManual.Start();
            }

            //Escolhido o Tab do modo Manual (IO)com maquina em actividade (Home ou Automático):
            else if (tabControl1.SelectedTab == PagManual && (backgroundWorkerAuto.IsBusy == true || backgroundWorkerHome.IsBusy == true))
            {

                lvDI.Enabled = false;
                lvDO.Enabled = false;
                grpScanner.Enabled = false;
                grpX.Enabled = false;
                grpX1.Enabled = false;
                grpY.Enabled = false;
                grpZ.Enabled = false;

                if (firstAddList == true)
                {
                    AddListDOItems();
                    AddListDIItems();
                    firstAddList = false;
                }
            }

            //Escolhido o Tab de configurações com maquina parada:
            if (tabControl1.SelectedTab == PagConfig && backgroundWorkerAuto.IsBusy == false && backgroundWorkerHome.IsBusy == false)
            {
                if (userLevel > 1)
                {
                    //grpLogin.Enabled = true;
                    grpConfParams.Enabled = true;
                    grpConfScrewPar.Enabled = true;
                    grpConfPuttyPar.Enabled = true;
                    grpConfGocatorPar.Enabled = true;
                }
            }

            //Escolhido o Tab do modo de configurações com maquina em actividade (Home ou Automático):
            else if (tabControl1.SelectedTab == PagConfig && (backgroundWorkerAuto.IsBusy == true || backgroundWorkerHome.IsBusy == true))
            {
                grpConfParams.Enabled = false;
                grpConfScrewPar.Enabled = false;
                grpConfPuttyPar.Enabled = false;
                grpConfGocatorPar.Enabled = false;

                //grpLogin.Enabled = true;
            }
        }

        //INICIAR MODO AUTOMATICO
        //Inicia o backgroundworker do modo automatico:
        private void buttonIniciarAuto_Click(object sender, EventArgs e)
        {
            buttonIniciarAuto.Enabled = false;
            buttonPosCasa.Enabled = false;
            backgroundWorkerAuto.RunWorkerAsync();
        }

        //INICIAR POSIÇÃO INICIAL
        //Inicia o backgroundworker da posição inicial:
        private void buttonPosCasa_Click(object sender, EventArgs e)
        {
            buttonIniciarAuto.Enabled = false;
            buttonPosCasa.Enabled = false;
            backgroundWorkerHome.RunWorkerAsync();
        }

        //PARAR MODO AUTOMATICO E POSIÇÃO INICIAL
        private void buttonPararAuto_Click(object sender, EventArgs e)
        {
            backgroundWorkerAuto.CancelAsync();
            backgroundWorkerHome.CancelAsync();
        }

        //PARAR O TIMER DE MODO MANUAL
        private void TabControl_Deselected(object sender, TabControlEventArgs e)
        {
            blnStopTimer = true;
            timerModoManual.Stop();
        }


        private void CloseManualErrorPanel_Click(object sender, EventArgs e)
        {
            ManualErrorPanel.Hide();
            ManualErrorTextBox.Text = "";
        }

        //TIMER MODO MANUAL
        private void timerModoManual_Tick(object sender, EventArgs e)
        {
            timerModoManual.Enabled = false;
            UpdateDOEnableArray();              //Actualiza o array de enable atrvés das restrições
            Application.DoEvents();
            UpdateManualMode();                 //Actualiza o modo manual: enable checked, cor ON e enable  
            Application.DoEvents();
            if (!blnStopTimer)
            {
                timerModoManual.Enabled = true;
            }
        }

        //Verifica o estado das emergências e afins:
        private void timerGeneric_Tick(object sender, EventArgs e)
        {

            labelClock.Text = DateTime.Now.ToString();
            //Mostrar ou ocultar as imagens:
            if (ShowPanel)
            {
                if (Flag_shown == false)
                {
                    showImage(image);
                    Flag_shown = true;
                }
            }
            else
            {
                panelPicture.Visible = false;
                Flag_shown = false;
                showImage(-1);
            }

            //Indicação de Posição Inicial:
            blnCircuitoSeg_OK = ReadDIO("DI", 0);

            //Icon Emergency
            if (blnCircuitoSeg_OK)
                picEmergencyButton.Visible = false;
            else
                picEmergencyButton.Visible = true;

            #region PosHome
            //Home Position
            if (blnCircuitoSeg_OK && ReadDIO("DI", 1) &&
                //Cyl Doseador 1
                ReadDIO("DI", 25) && !ReadDIO("DI", 24) &&
                //Cyl Doseador 2
                ReadDIO("DI", 27) && !ReadDIO("DI", 26) &&
                //Cyl Mask 
                ReadDIO("DI", 33) && !ReadDIO("DI", 32) &&
                ReadDIO("DI", 35) && !ReadDIO("DI", 34) &&
                //Not_Det
                !ReadDIO("DI", 44) &&

                //Apar_Ready
               // ReadDIO("DI", 9) &&

                eixosPosCasa) // fim de condição if

                posCasa = true;
            else
            {
                //if (!ReadDIO("DI", 9))
                //    txtInstructions.Text = "Aparafusadora com erro";
                posCasa = false;
            }
            #endregion

            if (bk1.wsStatus == 1 && Program.cicloIOon == true)
            {
                toolStripStatusLabel9.Text = TextByTag(1013) + SERV_IP_ADDR;
                //toolStripStatusLabel8.Text = "Ligado a BK: " + BK_IPAddress;
                labelNoCom.Visible = false;
            }
            else
            {
                toolStripStatusLabel9.Text = TextByTag(1014);
                //toolStripStatusLabel8.Text = "Sem Comunicação!";
                labelNoCom.Visible = true;
            }

            // verifica se já foi escolhida uma referência:
            if (MustSelectRef)
            {
                textBox1.Text = TextByTag(1005);
                textBox1.BackColor = Color.LightSalmon;
                //textBox1.Text = "Sem referência seleccionada!";
                buttonIniciarAuto.Enabled = false;
                buttonPosCasa.Enabled = false;
            }
            else
            {
                if (!ReadDIByName("Safety_Circuit_On"))
                {
                    textBox1.Text = TextByTag(1007);
                    textBox1.BackColor = Color.LightSalmon;
                    //textBox1.Text = "Circuito de segurança desligado";
                    buttonIniciarAuto.Enabled = false;
                    buttonPosCasa.Enabled = false;
                    //UpdateAutoToManual(); //Colocar as saídas todas a zero
                    // ClearOutputs();
                }
                else
                {
                    if (!ReadDIByName("Air_Pressure"))
                    {
                        textBox1.Text = TextByTag(1008);
                        textBox1.BackColor = Color.LightSalmon;
                        //textBox1.Text = "Falta de pressão de ar";
                        buttonIniciarAuto.Enabled = false;
                        buttonPosCasa.Enabled = false;
                    }
                    else
                    {
                        if (backgroundWorkerAuto.IsBusy == true || backgroundWorkerHome.IsBusy == true)
                        {
                            if (backgroundWorkerAuto.IsBusy == true)
                            {
                                //All Warnings 
                                if (ReadDIByName("Det_Dispensador_Cheio") || ReadDIByName("Doseador1_Nivel_Min") || ReadDIByName("Doseador2_Nivel_Min"))
                                {
                                    //Warning
                                    if (ReadDIByName("Det_Dispensador_Cheio"))
                                    {
                                        textBox1.BackColor = colorError;
                                        picScrew.Visible = true;
                                        textBox1.Text = "Pote dos parafusos Vazio!!";
                                    }
                                    //Warning
                                    if (ReadDIByName("Doseador1_Nivel_Min"))
                                    {
                                        textBox1.BackColor = colorError;
                                        picPutty.Visible = true;
                                        textBox1.Text = "Putty 506 com nivel Baixo!!";
                                    }
                                    //Warning
                                    if (ReadDIByName("Doseador2_Nivel_Min"))
                                    {
                                        textBox1.BackColor = colorError;
                                        picPutty.Visible = true;
                                        textBox1.Text = "Putty 508 com nivel Baixo!!";
                                    }
                                }
                                else
                                {
                                    picScrew.Visible = false;
                                    picPutty.Visible = false;

                                    textBox1.BackColor = colorSystem;
                                    textBox1.Text = "Modo Automático activo";
                                }

                                buttonIniciarAuto.Enabled = false;
                                buttonPosCasa.Enabled = false;
                            }
                            else
                            {
                                if (erroPosCasa == true)
                                {
                                    textBox1.Text = TextByTag(1010);
                                    textBox1.BackColor = Color.LightSalmon;
                                    //textBox1.Text = "Falha a ir para a posição inicial";
                                }
                                else
                                {
                                    textBox1.Text = TextByTag(1002);
                                    textBox1.BackColor = colorSystem;
                                    //textBox1.Text = "A ir para a posição inicial";
                                }
                                buttonIniciarAuto.Enabled = false;
                                buttonPosCasa.Enabled = false;
                            }
                        }
                        else
                        {
                            if (posCasa == true)
                            {
                                textBox1.Text = TextByTag(1011);
                                textBox1.BackColor = colorSystem;
                                //textBox1.Text = "Máquina em posição inicial";
                                buttonIniciarAuto.Enabled = true;
                                buttonPosCasa.Enabled = false;
                            }
                            else
                            {
                                textBox1.Text = TextByTag(1012);
                                textBox1.BackColor = Color.LightSalmon;
                                //textBox1.Text = "Máquina fora da posição inicial";
                                buttonIniciarAuto.Enabled = false;
                                buttonPosCasa.Enabled = true;
                            }
                        }
                    }
                }
            }

            if (picScrew.Visible || picPutty.Visible || labelNoCom.Visible)
                picWarning.Visible = true;
            else
                picWarning.Visible = false;
        }

        #region auxiliary ConnectThreadIO Functions

        private void ConnectThreadIO()
        {
            int i = 0;
            bk1.wsIPAddress = "192.168.001.001";

            while (bk1.wsStatus != 1 && i < 10)
            {
                bk1.ConnectToServer();
                Thread.Sleep(200);
                Application.DoEvents();
                Thread.Sleep(200);
                i++;
            };

            if (bk1.wsStatus == 1)
            {
                // Instancia a Classe onde se inicia o Thread de Leitura e escrita do IO:
                Program.NeedToWrite = true;
                IOCycle MyCycle = new IOCycle(bk1);
            }

        }

        #endregion

        #region auxiliary Generic Functions

        //FUNÇÃO PARA ACRESCENTAR A LISTVIEW DAS ENTRADAS
        //Só é chamada a primeira vez quando entra no modo manual
        private void AddListDIItems()
        {
            string[] strItems = new string[2];

            for (int j = 0; j < Program.Dt_DI.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista 
                strItems[0] = Program.Dt_DI.Rows[j]["Address"].ToString();
                strItems[1] = Program.Dt_DI.Rows[j]["DIName"].ToString();

                //Inicializar um objecto com os items de uma linha da lista
                ListViewItem lvItems = new ListViewItem(strItems);
                lvItems.UseItemStyleForSubItems = false;

                //Colocar uma cor diferente para o nome das reservas
                if (Program.Dt_DI.Rows[j]["DIName"].ToString().Contains("Reserve"))
                    lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvDI.Items.Add(lvItems);
            }
            lvDI.Columns[0].Width = 45;
            lvDI.Columns[1].Width = 295;
        }

        //FUNÇÃO PARA ACRESCENTAR A LISTVIEW DAS SAÍDAS
        //Só é chamada a primeira vez quando entra no modo manual
        private void AddListDOItems()
        {
            string[] strItems = new string[2];

            for (int j = 0; j < Program.Dt_DO.Rows.Count; j++)
            {

                //Array com o nome e endereço para acrescentar os items da lista 
                strItems[0] = Program.Dt_DO.Rows[j]["Address"].ToString();
                strItems[1] = Program.Dt_DO.Rows[j]["DOName"].ToString();

                //Inicializar um objecto com os items de uma linha da lista
                ListViewItem lvItems = new ListViewItem(strItems);
                lvItems.UseItemStyleForSubItems = false;

                //Colocar uma cor diferente para o nome das reservas
                if (Program.Dt_DO.Rows[j]["DOName"].ToString().Contains("Reserve"))
                    lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvDO.Items.Add(lvItems);
            }
            lvDO.Columns[0].Width = 55;
            lvDO.Columns[1].Width = 300;
        }

        //ACTUALIZAR O ESTADO DA MÁQUINA PARA O MODO MANUAL
        //É chamada sempre que entra no modo manual
        private void UpdateAutoToManual()
        {
            //for (int j = 1; j < Program.doMapMax; j++) {
            //  if (j == 0) {
            //    lvDO.Items[j].Checked = true;
            //    }
            //  else {
            //    //O Cilindro de Guiamento fica com o estado que estava
            //    if (j != 16)
            //      lvDO.Items[j].Checked = false;
            //    else {
            //      //Mantém o Estado do Cilindro de Guiamento
            //      if (Program.doMap[j].ValueRead == true)
            //        lvDO.Items[j].Checked = true;
            //      else
            //        lvDO.Items[j].Checked = false;
            //      }
            //    }

            //  /*
            //  //Se uma saída estiver activa coloca o checked para manter activa
            //  if (Program.doMap[j].ValueRead == true)
            //  {
            //      lvDO.Items[j].Checked = true;
            //  }
            //  //Se uma saída estiver desactiva não coloca o checked para manter desactiva
            //  else
            //  {
            //      lvDO.Items[j].Checked = false;
            //  }
            //  */
            //  }
        }

        private void ClearOutputs()
        {
            // WriteDOByName("Safety_Relay_On", false);
            WriteDOByName("Led_Green", false);
            WriteDOByName("Led_Red", false);
            WriteDOByName("Led_Yellow", false);

            WriteDOByName("Screw_CYC1", false);
            WriteDOByName("Screw_CYC2", false);
            WriteDOByName("Screw_CYC4", false);
            WriteDOByName("Screw_VALSP", false);

            WriteDOByName("Screw_SCY", false);
            WriteDOByName("Screw_RESET", false);
            WriteDOByName("Vision_Trigger", false);
            WriteDOByName("Vision_IN1", false);

            WriteDOByName("Vision_IN2", false);
            WriteDOByName("Cyl_Drawer_Vib_W", false);
            WriteDOByName("Cyl_Drawer_Vib_H", false);
            WriteDOByName("Cyl_Guide_Blow_W", false);

            WriteDOByName("Cyl_Guide_Blow_H", false);
            WriteDOByName("Cyl_Pointer_W", false);
            WriteDOByName("Cyl_Pointer_H", false);
            WriteDOByName("Cyl_Mask_W", false);

            WriteDOByName("Cyl_Mask_H", false);
            WriteDOByName("Cyl_Dispenser1_W", false);
            WriteDOByName("Cyl_Dispenser1_H", false);
            WriteDOByName("Cyl_Dispenser2_W", false);

            WriteDOByName("Cyl_Dispenser2_H", false);
            WriteDOByName("Dispenser_valve1", false);
            WriteDOByName("Dispenser_valve2", false);

        }

        //ACTUALIZAR O MODO MANUAL
        //Constantemente actualizado sempre que está seleccionado o modo manual
        private void UpdateManualMode()
        {
            try
            {


                Color CorTrue = Color.LightGreen;
                Color CorFalse = Color.White;
                Color CorEnable = Color.LightGray;

                int Max = Math.Max(Math.Max(Program.Dt_AI.Rows.Count, Program.Dt_DI.Rows.Count),
                                   Math.Max(Program.Dt_DI.Rows.Count, Program.Dt_DI.Rows.Count));

                for (int j = 0; j < Max; j++)
                {

                    if (j < Program.Dt_DI.Rows.Count)
                    {
                        if (Program.UpdateDIORows(j, "Value", null, ReadWriteIO.ReadDI, ""))
                            //if (((bool)Program.Dt_DI.Rows[j]["Value"]) == true)
                            lvDI.Items[j].SubItems[1].BackColor = CorTrue;
                        else
                            lvDI.Items[j].SubItems[1].BackColor = CorFalse;
                    }


                    if (j < Program.Dt_DO.Rows.Count)
                    {

                        if (Program.doEnable[j] == false)
                        {
                            // Se a saída estiver ligada actualiza o pisco:
                            lvDO.Items[j].BackColor = CorEnable;
                            //lvDO.Items[j].Checked = (bool)Program.Dt_DO.Rows[j]["ValueToWrite"];
                            lvDO.Items[j].Checked = Program.UpdateDIORows(j, "ValueToWrite", null, ReadWriteIO.ReadDO, "");
                        }
                        else
                        {
                            lvDO.Items[j].BackColor = CorFalse;
                            WriteDO(j, lvDO.Items[j].Checked);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.ToString());
            }
        }

        //FUNÇÃO DE LEITURA DAS ENTRADAS E DAS SAÍDAS
        private bool ReadDIO(string tipo, int e)
        {
            try
            {
                if (tipo == "DI") { return Program.UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDI, ""); }
                if (tipo == "DO") { return Program.UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDO, ""); }
                else return false;
            }
            catch (Exception)
            {
                Delay(10);
                try
                {
                    if (tipo == "DI") { return Program.UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDI, ""); }
                    if (tipo == "DO") { return Program.UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDO, ""); }
                    else return false;
                }
                catch (Exception) { return false; }
            }
        }

        private int ReadDAI(int e)
        {
            return (int)Program.Dt_AI.Rows[e]["Value"];
        }

        private void WriteDO(int e, bool est)
        {
            Program.NeedToWrite = true;
            try
            {
                //Program.Dt_DO.Rows[e]["ValueToWrite"] = est;
                Program.UpdateDIORows(e, "ValueToWrite", est, ReadWriteIO.WriteDO, "");
            }
            catch (Exception exp)
            {
                Console.WriteLine("WriteDO Error: " + exp.ToString());
            }
        }

        private bool ReadDIByName(string DIName)
        {
            try
            {
                return Program.UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDI, DIName);
            }
            catch (Exception)
            {
                Delay(10);
                try
                {
                    return Program.UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDI, DIName);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error in DI: " + DIName);
                    return false;
                }
            }
        }

        private bool ReadDOByName(string DOName)
        {
            return Program.UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDO, DOName);
            //return (bool)(Program.Dt_DO.Select("DOName = '" + DOName + "'")[0]["Value"]);
        }

        private double ReadAIByName(string AIName)
        {
            int Rowindex = Program.Dt_AI.Rows.IndexOf(Program.Dt_AI.Select("AIName = '" + AIName + "'")[0]);

            if (Rowindex == 0)
                return (double)Math.Round(((double)ReadDAI(Rowindex)), 2); //- 17500) * 0.02925, 2);
            else
                return (double)Math.Round(((double)ReadDAI(Rowindex)), 2); //- 16676) * 0.02925, 2);
        }

        private void WriteDOByName(string DOName, bool est)
        {
            Program.NeedToWrite = true;
            try
            {
                int Rowindex = Program.Dt_DO.Rows.IndexOf(Program.Dt_DO.Select("DOName = '" + DOName + "'")[0]);
                Program.UpdateDIORows(0, "ValueToWrite", est, ReadWriteIO.WriteDO, DOName);
                //Program.UpdateDORows(Rowindex, "ValueToWrite", est);
                //Program.Dt_DO.Rows[Rowindex]["ValueToWrite"] = est;
            }
            catch (Exception exp) { Console.WriteLine("WriteDOByName Error: " + exp.ToString()); }
        }

        #endregion

        #region auxiliary Enable Function

        private void UpdateDOEnableArray()
        {

            List<byte> MyList = new List<byte>(new byte[] { });

            for (byte i = 0; i < Program.doEnable.Length; i++)
            {
                if (!MyList.Contains(i)) Program.doEnable[i] = true;
            }

        }


        #endregion

        #region auxiliary Login
        private bool LogUserIn(DataSource d)
        {
            //Aspiração e amostras não realizadas;
            callLogin = false;

            //Adicionar um "event handler" para update a form
            Login f = new Login(ref ActualLang, ref dsLang, (d == DataSource.XML), StationName);
            f.IdentityUpdated += new Login.IdentityUpdateHandler(FormModel_ButtonClicked);
            f.ShowDialog();

            return f.getLoggedIn ? true : false;
        }

        private void FormModel_ButtonClicked(object sender, IdentityUpdateEventArgs e)
        {
            //e.Psw; //e.ID_User; //e.Identification; //e.Username; //e.Psw;
            userLevel = Convert.ToInt32(e.User_Level);
            usershift = Convert.ToInt32(e.ID_User);
            UpdatePasswordLogin(e.Username, e.Identification);
        }

        private void UpdatePasswordLogin(string userName, string identification)
        {
            //Actualizar os dados do utilizador
            this.Text = TextByTag(2) + "   (" + userName + " - " + identification + ")";
            labelUser.Text = "Utilizador: " + userName + " - " + identification;

            //Verifica as Permissões:
            if (userLevel > 1)
                UpdateUserPermissions(true);
            else
                UpdateUserPermissions(false);
        }

        private void UpdateUserPermissions(bool Enable)
        {

            //Tab Login
            //grpLogin.Enabled = Enable;

            //DataGridView
            if (dataGridView1.Columns.Count > 3)
            {
                if (Enable)
                {
                    dataGridView1.Columns[2].ReadOnly = false;
                    dataGridView1.Columns[3].ReadOnly = false;
                    dataGridView1.Columns[2].DefaultCellStyle.BackColor = Color.White;
                    dataGridView1.Columns[3].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dataGridView1.Columns[2].ReadOnly = true;
                    dataGridView1.Columns[3].ReadOnly = true;
                    dataGridView1.Columns[2].DefaultCellStyle.BackColor = Color.WhiteSmoke;
                    dataGridView1.Columns[3].DefaultCellStyle.BackColor = Color.WhiteSmoke;
                }
            }
            //DataGridViewScrew
            if (dataGridViewScrewPar.Columns.Count > 3)
            {
                dataGridViewScrewPar.Columns[2].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewScrewPar.Columns[3].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewScrewPar.Columns[4].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewScrewPar.Columns[5].ReadOnly = userLevel > 1 ? false : true;

                dataGridViewScrewPar.Columns[2].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewScrewPar.Columns[3].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewScrewPar.Columns[4].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewScrewPar.Columns[5].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
            }
            //DataGridViewPutty
            if (dataGridViewPuttyPar.Columns.Count > 3)
            {
                dataGridViewPuttyPar.Columns[2].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewPuttyPar.Columns[3].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewPuttyPar.Columns[4].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewPuttyPar.Columns[5].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewPuttyPar.Columns[6].ReadOnly = userLevel > 1 ? false : true;

                dataGridViewPuttyPar.Columns[2].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewPuttyPar.Columns[3].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewPuttyPar.Columns[4].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewPuttyPar.Columns[5].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewPuttyPar.Columns[6].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
            }
            //GridViewGocator
            if (dataGridViewGocatorPar.Columns.Count > 3)
            {
                dataGridViewGocatorPar.Columns[2].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewGocatorPar.Columns[3].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewGocatorPar.Columns[4].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewGocatorPar.Columns[5].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewGocatorPar.Columns[6].ReadOnly = userLevel > 1 ? false : true;
                dataGridViewGocatorPar.Columns[7].ReadOnly = userLevel > 1 ? false : true;

                dataGridViewGocatorPar.Columns[2].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewGocatorPar.Columns[3].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewGocatorPar.Columns[4].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewGocatorPar.Columns[5].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewGocatorPar.Columns[6].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridViewGocatorPar.Columns[7].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
            }

            //Tab IO
            lvDI.Enabled = Enable;
            lvDO.Enabled = Enable;
            grpScanner.Enabled = Enable;
            grpX.Enabled = Enable;
            grpX1.Enabled = Enable;
            grpY.Enabled = Enable;
            grpZ.Enabled = Enable;
            grpConfParams.Enabled = Enable;
            grpConfScrewPar.Enabled = Enable;
            grpConfPuttyPar.Enabled = Enable;
            grpConfGocatorPar.Enabled = Enable;
        }

        #endregion

        #region auxiliary DataBase Control
        private void comboBoxModelo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            DGVDSet.RejectChanges();
            //NewTables
            ScrewParSet.RejectChanges();
            PuttyParSet.RejectChanges();
            GocatorParSet.RejectChanges();

            if (comboBoxModelo.SelectedIndex == 0)
            {
                dataGridView1.DataSource = null;
                dataGridView1.Refresh();
                lblParam.Text = TextByTag(8);
                labelVersion.Text = "";
                MustSelectRef = true;

                //NewTables
                dataGridViewScrewPar.DataSource = null;
                dataGridViewScrewPar.Refresh();
                dataGridViewPuttyPar.DataSource = null;
                dataGridViewPuttyPar.Refresh();
                dataGridViewGocatorPar.DataSource = null;
                dataGridViewGocatorPar.Refresh();

            }
            else
            {
                if (!comboBoxModelo.SelectedValue.Equals(0) && !comboBoxModelo.SelectedValue.Equals("0"))
                {
                    UpdateCurrentModelParameters();
                    lblParam.Text = TextByTag(8) + " " + sel_model; //comboBoxModelo.SelectedText;
                    labelVersion.Text = model_version;
                    MustSelectRef = false;
                }
            }
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            string col = dataGridView1.Columns[e.ColumnIndex].Name;
            string row = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();

            //MessageBox.Show("Ocorreu um erro ao editar o campo de dados:\r\n\r\n" + 
            //                "Valor: '" + col + "'\r\nParâmetro: '" + row + "'" +
            //                "\r\n\r\n(Faça OK e Prima ESC para voltar ao valor anterior)", "Erro ao Editar Parametro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            MessageBox.Show(TextByTag(1026) +
                        "Valor: '" + col + "'\r\nParâmetro: '" + row + "'" +
                        TextByTag(1027), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            buttonDescartar.Enabled = true;
            dataGridView1[e.ColumnIndex, e.RowIndex].Style.ForeColor = Color.Red;
        }

        private void buttonDescartar_Click(object sender, EventArgs e)
        {
            ResetDataGridView(false, ref dataGridView1);
            buttonDescartar.Enabled = false;
        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            //if (e.ColumnIndex == 2 || e.ColumnIndex == 3) {

            //  string col = dataGridView1.Columns[e.ColumnIndex].Name;
            //  string row = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();

            //  double d = 0;
            //  if (!double.TryParse(e.FormattedValue.ToString(), out d)) {
            //    MessageBox.Show(TextByTag(1026) +
            //               "Parâmetro: " + row + "    Valor: " + col +
            //               TextByTag(1027), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    e.Cancel = true;
            //    }
            //  }
        }

        private void ResetDataGridView(bool AcceptChanges, ref DataGridView dgv)
        {
            if (AcceptChanges) DGVDSet.AcceptChanges(); else DGVDSet.RejectChanges();

            foreach (DataGridViewRow drw in dgv.Rows)
            {
                foreach (DataGridViewCell drc in drw.Cells)
                {
                    drc.Style.ForeColor = Color.Black;
                }
            }
        }

        //Guarda o dataGridView na BD:
        private void buttonGuardar_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private bool SaveSettings()
        {

            if (ParamsDataSource == DataSource.XML) { return SaveToXML(); }

            //Se não existirem alterações, salta fora:
            if (!DGVDSet.HasChanges()) { MessageBox.Show(TextByTag(1032)); return true; }

            if (MessageBox.Show(TextByTag(1028), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {

                buttonGuardar.Enabled = false;
                StringBuilder ParamName = new StringBuilder(), TinyRefs = new StringBuilder();
                double ValueMin, ValueMax;
                int Count = 0;
                int ID_Parameter = 0;

                try
                {
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        Count = i;
                        ValueMin = double.Parse(dataGridView1.Rows[i].Cells["Min"].Value.ToString());
                        ValueMax = double.Parse(dataGridView1.Rows[i].Cells["Max"].Value.ToString());
                    }
                }
                catch (Exception ex1)
                {
                    MessageBox.Show(TextByTag(1029) + Count + ":\r\n\r\n" + ex1.ToString(), "Erro");
                    buttonGuardar.Enabled = true;
                    return false;
                }

                try
                {
                    for (int i = 0; i < DGVDSet.Tables[0].Rows.Count; i++)
                    {
                        //Guarda apenas as linhas que foram alteradas:
                        if (DGVDSet.Tables[0].Rows[i].RowState == DataRowState.Modified)
                        {
                            ValueMin = double.Parse(DGVDSet.Tables[0].Rows[i]["Min"].ToString());
                            ValueMax = double.Parse(DGVDSet.Tables[0].Rows[i]["Max"].ToString());
                            ID_Parameter = int.Parse(DGVDSet.Tables[0].Rows[i]["ID_Parameter"].ToString());

                            //SQL//MyDB.UpdateDeviceParameter(ID_Parameter, ValueMin, ValueMax);
                        }
                    }
                    ResetDataGridView(true, ref dataGridView1);
                }
                catch (Exception ex1)
                {
                    MessageBox.Show(TextByTag(1030) + ex1.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }

                //Lê novamente os parametros:
                try
                {
                    LoadParameters(ref dsParams, ParamsDataSource);
                    buttonGuardar.Enabled = true;
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }

                //Actualiza a DataGridView com os Dados do Servidor:
                try
                {
                    UpdateCurrentModelParameters();
                    buttonGuardar.Enabled = true;
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }
            }
            return true;
        }


        private bool SaveToXML()
        {
            if (!DGVDSet.HasChanges()) { MessageBox.Show(TextByTag(1032)); return true; }

            if (MessageBox.Show(TextByTag(1028), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {

                buttonGuardar.Enabled = false;

                try
                {
                    DGVDSet.WriteXml("Params.xml", XmlWriteMode.WriteSchema);
                    ResetDataGridView(true, ref dataGridView1);
                }
                catch (Exception ex1)
                {
                    MessageBox.Show(TextByTag(1030) + ex1.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }

                //Lê novamente os parametros:
                try
                {
                    LoadParameters(ref dsParams, ParamsDataSource);
                    buttonGuardar.Enabled = true;
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }

                try
                {
                    UpdateCurrentModelParameters();
                    buttonGuardar.Enabled = true;
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                    buttonGuardar.Enabled = true;
                    return false;
                }
            }
            return true;
        }

        private bool SaveToXMLAuto()
        {
            //if (!DGVDSet.HasChanges()) { MessageBox.Show(TextByTag(1032)); return true; }

            //if (MessageBox.Show(TextByTag(1028), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            //{

            //buttonGuardar.Enabled = false;

            try
            {
                DGVDSet.WriteXml("Params.xml", XmlWriteMode.WriteSchema);
                ResetDataGridView(true, ref dataGridView1);
            }
            catch (Exception ex1)
            {
                MessageBox.Show(TextByTag(1030) + ex1.ToString(), "");
                //buttonGuardar.Enabled = true;
                return false;
            }

            //Lê novamente os parametros:
            try
            {
                LoadParameters(ref dsParams, ParamsDataSource);
                //buttonGuardar.Enabled = true;
            }
            catch (Exception ex2)
            {
                MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                //buttonGuardar.Enabled = true;
                return false;
            }

            try
            {
                UpdateCurrentModelParameters();
                //buttonGuardar.Enabled = true;
            }
            catch (Exception ex2)
            {
                MessageBox.Show(TextByTag(1032) + ex2.ToString(), "");
                //buttonGuardar.Enabled = true;
                return false;
            }
            //}
            return true;
        }


        private bool SetParameter(string name, string writeString)
        {

            DataRow[] Dr = DGVDSet.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {
                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        Dr[0]["Min"] = writeString;
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        {

                        }

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                r["Min"] = writeString;
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    r["Min"] = writeString;
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }

            return true;
        }

        #endregion

      
        #region auxiliary DataBase
        private void UpdateCurrentModelParameters()
        {
            int Rowindex_Ref = int.Parse(comboBoxModelo.SelectedValue.ToString());
            //dsRefs.Tables[0].Rows.IndexOf(dsRefs.Tables[0].Select("ID_Ref = " + comboBoxModelo.SelectedValue)[0]);
            sel_model = dsRefs.Tables[0].Rows[Rowindex_Ref]["Ref"].ToString();
            //strTinyRef = sel_model.Substring(20, 3);
            ActualRefID = int.Parse(dsRefs.Tables[0].Rows[Rowindex_Ref]["ID_Ref"].ToString());
            model_version = dsRefs.Tables[0].Rows[Rowindex_Ref]["RefDescription"].ToString();

            String[] substrings = model_version.Split('/');
            //RefMainBoard = model_version.Substring(0, 3);
            //RefHousing = model_version.Substring(4, 19);
            //strTinyRef = model_version.Substring(24, 3);

            RefMainBoard = substrings[0];
            RefHousing = substrings[1];
            strTinyRef = substrings[2];

            GetParameter("Products", ref pProducts);
            GetParameter("ScrewTableID", ref pScrewTableID);
            GetParameter("PuttyTableID", ref pPuttyTableID);
            GetParameter("GocatorTableID", ref pGocatorTableID);
            GetParameter("VisionHousingSN", ref pVisionHousingSN);
            GetParameter("VisionPCBSN", ref pVisionPCBSN);
            GetParameter("VisionCoverVerify", ref pVisionCoverVerify);
            GetParameter("PuttyType", ref pPuttyType);
            GetParameter("PuttyLimits", ref MinPuttyLimits,ref MaxPuttyLimits);
            GetParameter("Screw_Cycle", ref pScrewCycle);


            //Criar um Dataset que servirá de DataSource para a DataGridView:
            DGVDSet = new DataSet("DGVDSet");
            DataTable Dt = new DataTable("TableRefs");
            Dt = dsParams.Tables[0].Clone();

            foreach (DataRow Dr in dsParams.Tables[0].Rows) { Dt.ImportRow(Dr); }
            DGVDSet.Tables.Add(Dt);
            DGVDSet.AcceptChanges();
            ConfigDataGridView(ref DGVDSet);

            
        }

        private void ConfigDataGridView(ref DataSet DSet)
        {
            System.Drawing.Font f1 = new Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            System.Drawing.Font f2 = new Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            
            //testar seleção de apenas parametros referentes a peça especifica//

            //DataTable dtTarget = new DataTable();
            //dtTarget = DSet.Tables[0].Clone();
            //DataRow[] rowsToCopy;
            //rowsToCopy = DSet.Tables[0].Select("TinyRefs ='" + Globals2Work.ModelWorking + "'");
            //// DSet.Tables[0].DefaultView.RowFilter = "TinyRefs = 'ALL' OR TinyRefs ='" + strTinyRef + "'";
            //foreach (DataRow dr in rowsToCopy)
            //{
            //    dtTarget.ImportRow(dr);
            //}
            //dataGridView1.DataSource = dtTarget;
            dataGridView1.DataSource = DSet.DefaultViewManager;
            dataGridView1.DataMember = DSet.Tables[0].TableName;
            dataGridView1.Refresh();
            dataGridView1.DefaultCellStyle.Font = f1;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = f2;
          
            //Column 'Parameter ID':
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[0].MinimumWidth = 30;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView1.Columns[0].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.Columns[0].DefaultCellStyle.Font = f1;
            //Hide column:
            dataGridView1.Columns[0].Visible = false;

            //Column 'Parameter Name':
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[1].MinimumWidth = 180;
            dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView1.Columns[1].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.Columns[1].DefaultCellStyle.Font = f1;

            //Column 'Min Value':
            dataGridView1.Columns[2].MinimumWidth = 240;
            dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[2].ValueType = typeof(double);
            dataGridView1.Columns[2].DefaultCellStyle.Font = f2;

            //Column 'Max Value':
            dataGridView1.Columns[3].MinimumWidth = 120;
            dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[3].ValueType = typeof(double);
            dataGridView1.Columns[3].DefaultCellStyle.Font = f2;

            //Column 'Parameter Description':
            dataGridView1.Columns[4].ReadOnly = true;
            dataGridView1.Columns[4].MinimumWidth = 580;
            dataGridView1.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView1.Columns[4].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.Columns[4].DefaultCellStyle.Font = f1;

            //Column TinyRefs:
            dataGridView1.Columns[5].ReadOnly = true;
            dataGridView1.Columns[5].MinimumWidth = 100;
            dataGridView1.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[5].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.Columns[5].DefaultCellStyle.Font = f1;

            //Column ID_WS:
            dataGridView1.Columns[6].ReadOnly = true;
            dataGridView1.Columns[6].MinimumWidth = 100;
            dataGridView1.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Columns[6].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.Columns[6].DefaultCellStyle.Font = f1;

            if (dataGridView1.Columns.Count > 3)
            {
                dataGridView1.Columns[2].ReadOnly = userLevel > 1 ? false : true;
                dataGridView1.Columns[3].ReadOnly = userLevel > 1 ? false : true;
                dataGridView1.Columns[2].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
                dataGridView1.Columns[3].DefaultCellStyle.BackColor = userLevel > 1 ? Color.White : Color.WhiteSmoke;
            }

            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private bool GetParameter(string name, ref double ValMin, ref double ValMax)
        {

            DataRow[] Dr = dsParams.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {

                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        ValMin = double.Parse(Dr[0]["Min"].ToString());
                        ValMax = double.Parse(Dr[0]["Max"].ToString());
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        //if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        //{

                        //}

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                ValMin = double.Parse(r["Min"].ToString());
                                ValMax = double.Parse(r["Max"].ToString());
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    ValMin = double.Parse(r["Min"].ToString());
                                    ValMax = double.Parse(r["Max"].ToString());
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }

            return true;
        }

        private bool GetParameter(string name, ref int ValMin, ref int ValMax)
        {

            DataRow[] Dr = dsParams.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {

                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        ValMin = int.Parse(Dr[0]["Min"].ToString());
                        ValMax = int.Parse(Dr[0]["Max"].ToString());
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        //if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        //{

                        //}

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                ValMin = int.Parse(r["Min"].ToString());
                                ValMax = int.Parse(r["Max"].ToString());
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    ValMin = int.Parse(r["Min"].ToString());
                                    ValMax = int.Parse(r["Max"].ToString());
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }
            return true;
        }

        private bool GetParameter(string name, ref int Value)
        {

            DataRow[] Dr = dsParams.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {

                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        Value = int.Parse(Dr[0]["Min"].ToString());
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        //if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        //{

                        //}

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                Value = int.Parse(r["Min"].ToString());
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    Value = int.Parse(r["Min"].ToString());
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }

                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }

            return true;
        }

        private bool GetParameter(string name, ref string Value)
        {

            DataRow[] Dr = dsParams.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {

                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        Value = Dr[0]["Min"].ToString();
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        //if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        //{

                        //}

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                Value = r["Min"].ToString();
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    Value = r["Min"].ToString();
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }
            return true;
        }

        private bool GetParameter(string name, ref bool Value)
        {

            DataRow[] Dr = dsParams.Tables[0].Select("Parameter = '" + name + "'");
            bool ALL_Found = false, REF_Found = false;

            try
            {

                if (Dr.Length == 0)
                {
                    MessageBox.Show(name + " - Parametro não encontrado!");
                    return false;
                }
                else if (Dr.Length == 1)
                {
                    if (Dr[0]["TinyRefs"].ToString().Equals("ALL") || Dr[0]["TinyRefs"].ToString().Equals(strTinyRef))
                    {
                        Value = Dr[0]["Min"].ToString().Equals("1") ? true : false;
                    }
                    else
                    {
                        MessageBox.Show(name + " - Parametro não encontrado!");
                        return false;
                    }
                }
                else if (Dr.Length > 1)
                {

                    for (int i = 0; i < Dr.Length; i++)
                    {
                        //if (Dr[i]["TinyRefs"].ToString().Equals(strTinyRef))
                        //{

                        //}

                        foreach (DataRow r in Dr)
                        {
                            if (r["TinyRefs"].ToString().Equals(strTinyRef))
                            {
                                Value = r["Min"].ToString().Equals("1") ? true : false;
                                REF_Found = true;
                                break;
                            }
                        }

                        if (!REF_Found)
                        {
                            foreach (DataRow r in Dr)
                            {
                                if (r["TinyRefs"].ToString().Equals("ALL"))
                                {
                                    Value = r["Min"].ToString().Equals("1") ? true : false;
                                    ALL_Found = true;
                                    break;
                                }
                            }
                        }

                        if (!REF_Found && !ALL_Found)
                        {
                            MessageBox.Show(name + " - Parametro não encontrado!");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erro ao procurar o parametro: " + name + "\r\n\r\n" + exp.ToString());
                return false;
            }

            return true;
        }

        #endregion

        //backgroundWorker do cicloAuto - Responsável pela execução do ciclo Automático:
        private void backgroundWorkerAuto_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("THREAD AUTOMATICO");
            Control.CheckForIllegalCrossThreadCalls = false;

            int i, nextStep = 5;     //Iniciar no passo 10
            first = true;            //Inicia o timer no passo 10. Variavel do CicloAuto

            comboBoxModelo.Enabled = false;
            UpdateCurrentModelParameters();

            while (backgroundWorkerAuto.CancellationPending == false && nextStep != 1 && blnCircuitoSeg_OK)
            {
                i = RunCicloAuto(nextStep);   // descomentar e retirar o gocator teste 
                //i = RungocatorTeste(nextStep);
                nextStep = i;
                Thread.Sleep(100);

            }
            ShowPanel = false;
        }

        //backgroundWorker do cicloAuto - Após conclusão do Thread:
        private void backgroundWorkerAuto_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Colocar aqui procedimento após o CicloAuto
            ClearOutputs();                            //Excepto os movimentos dos cilindros
            txtInstructions.Clear();                   //Limpar as mensagens
            txtInstructions.BackColor = colorSystem;   //Limpar o Fundo das Mensagens       
                                                       //1º Ciclo
            eixosPosCasa = false;
            comboBoxModelo.Enabled = true;

            if (callLogin)
            {
                MessageBox.Show("Mudança de Turno. Necessário Login", "Turno", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                if (!LogUserIn(UsersDataSource)) { CloseApplication(); return; }
                //buttonLogin_Click(new object(), new EventArgs());
            }
        }

        //backgroundWorker do cicloHome - Responsável pela execução do ciclo Posição Casa:
        private void backgroundWorkerHome_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("THREAD POSIÇÃO CASA");
            Control.CheckForIllegalCrossThreadCalls = false;

            int nextStep = 10;  //Iniciar no passo 10
            first = true;       //Inicia o timer no passo 10. Variavel do CicloAuto
            int i;

            while (backgroundWorkerHome.CancellationPending == false && nextStep != 1 && blnCircuitoSeg_OK)
            {
                i = RunPosCasa(nextStep);
                nextStep = i;
                Thread.Sleep(100);
            }

            ShowPanel = false;
        }

        //backgroundWorker do cicloHome - Após conclusão do Thread:
        private void backgroundWorkerHome_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Colocar aqui procedimento após o CicloPosCasa
            txtInstructions.Clear();                       //Limpar as mensagens
            txtInstructions.BackColor = colorSystem;       //Limpar o Fundo das Mensagens

        }

        private void MenuPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Desliga o Relé de Emergência:
            WriteDO(0, false);

            Application.DoEvents();
        }

        private void MenuPrincipal_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseApplication();
        }

        private void CloseApplication()
        {
            //Desliga o Relé de Emergência:
            WriteDO(0, false);

            Thread.Sleep(500);

            Program.blnExit = true;
            Application.DoEvents();

            bk1.CloseSockect();

            timerGeneric.Stop();

            this.Dispose();
            //  PLC1.Close();
            GC.Collect();
        }

        #region auxiliary ToolBox

        private void picPT_Click(object sender, EventArgs e)
        {
            if (ActualLang == "PT") return;
            ActualLang = "PT";
            UpdateFormLanguage();
        }

        private void picPT_MouseHover(object sender, EventArgs e)
        {
            toolTip1 = new ToolTip();
            toolTip1.SetToolTip(picPT, "Mensagens em Português");
        }

        private void picEN_Click(object sender, EventArgs e)
        {
            if (ActualLang == "EN") return;
            ActualLang = "EN";
            UpdateFormLanguage();
        }

        private void picEN_MouseHover(object sender, EventArgs e)
        {
            toolTip1 = new ToolTip();
            toolTip1.SetToolTip(picEN, "Messages in English");
        }

        private void picCHI_Click(object sender, EventArgs e)
        {
            if (ActualLang == "CH") return;
            ActualLang = "CH";
            UpdateFormLanguage();
        }

        private void picCHI_MouseHover(object sender, EventArgs e)
        {
            toolTip1 = new ToolTip();
            toolTip1.SetToolTip(picCHI, " ");
        }

        private void picLogin_Click(object sender, EventArgs e)
        {
            if (!LogUserIn(UsersDataSource)) CloseApplication();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel9_Click(object sender, EventArgs e)
        {

        }

        private void pic2Login_Click(object sender, EventArgs e)
        {
            if (!LogUserIn(UsersDataSource)) CloseApplication();
        }

        private void picLogin_MouseHover(object sender, EventArgs e)
        {
            toolTip1 = new ToolTip();
            toolTip1.SetToolTip(picLogin, "Logoff");
        }

        //CARREGAR NA BARRA PARA RECUPERAR COMUNICAÇÃO
        private void TollStripStatusLabel_Click(object sender, EventArgs e)
        {
            ConnectThreadIO();
        }

        #endregion

        #region auxiliary Language
        private bool UpdateFormLanguage()
        {
            try
            {
                dsRefs.Tables[0].Rows[0]["Ref"] = TextByTag(15);

                lvDI.Columns[1].Text = TextByTag(1015);
                lvDO.Columns[1].Text = TextByTag(1016);

                foreach (Control c in this.Controls)
                {

                    if (c.Tag != null) if (!c.Tag.Equals(""))
                            c.Text = TextByTag(int.Parse(c.Tag.ToString()));

                    if (c.HasChildren)
                    {
                        foreach (Control d in c.Controls)
                        {
                            if (d.Tag != null) if (!d.Tag.Equals(""))
                                    d.Text = TextByTag(int.Parse(d.Tag.ToString()));

                            if (d.HasChildren)
                            {
                                foreach (Control e in d.Controls)
                                {
                                    if (e.Tag != null) if (!e.Tag.Equals(""))
                                            e.Text = TextByTag(int.Parse(e.Tag.ToString()));

                                    if (e.HasChildren)
                                    {
                                        foreach (Control f in e.Controls)
                                        {
                                            if (f.Tag != null) if (!f.Tag.Equals(""))
                                                    f.Text = TextByTag(int.Parse(f.Tag.ToString()));

                                            if (f.HasChildren)
                                            {
                                                foreach (Control g in f.Controls)
                                                    if (g.Tag != null) if (!g.Tag.Equals(""))
                                                            g.Text = TextByTag(int.Parse(g.Tag.ToString()));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(TextByTag(505) + "'UpdateFormLanguage()':\r\n\r\n" + exp.ToString());
                //MessageBox.Show("Erro ao actualizar os parametros da Linguagem em 'UpdateFormLanguage()':\r\n\r\n" + exp.ToString());
                return false;
            }
        }

        private string TextByTag(int intTag)
        {
            try
            {
                return dsLang.Tables[0].Select("Tag = '" + intTag + "'")[0][ActualLang].ToString().Replace("|", "\r\n");
            }
            catch (Exception)
            {
                return intTag.ToString();
            }
        }

        #endregion

        #region auxiliary ImageBox
        public void showImage(int i)
        {

            Image test;
            switch (i)
            {
                case 1:

                    test = Image.FromFile(@"C:\fotos\ColocarPeca.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 2:

                    test = Image.FromFile(@"C:\fotos\ColocarPcb.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 3:
                    
                    test = Image.FromFile(@"C:\fotos\ColocarTampa.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;
                    
                    break;

                case 4:
                    
                    //test= Image.FromFile(@"C:\software_release\images\OUT.jpg");

                    //Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 5:
                    
                    test = Image.FromFile(@"C:\Fotos\Aparafusar.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 6:


                    test = Image.FromFile(@"C:\Fotos\RetirarPecaOk.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 7:

                    test = Image.FromFile(@"C:\Fotos\RetirarPecaNok.png");

                    Test_imageBox.Image = test;
                    Test_imageBox.SizeMode = PictureBoxSizeMode.StretchImage;

                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;

                    break;

                case 8:
                    

                    break;

                case 9:

                    
                    break;
                case 10:


                    panelPicture.Visible = true;
                    Test_imageBox.Visible = true;
                    break;
                default:
                    test = null;
                    break;

            }
        }
        #endregion

        #region Dalsa
        public bool Vision_inspection(string send_data, ref string[] outputvalues)
        {
            try
            {
                if (!Vision.Connected)
                    Vision.Connect(ip);

                if (Vision.Connected)
                {
                    //string stringData = Encoding.ASCII.GetString(send_data, 0, 3);
                    //Console.WriteLine(stringData);
                    byte[] bytes = Encoding.ASCII.GetBytes(send_data);
                    Console.WriteLine(send_data);

                    Vision.Send(bytes);

                    //Done. Now let's listen for data
                    byte[] receiveData = new byte[200];
                    int receivedDataLength = Vision.Receive(receiveData);

                    //if the response is a string message
                    string stringDataReceive = Encoding.ASCII.GetString(receiveData, 0, receivedDataLength);
                    Console.WriteLine(stringDataReceive);

                    Char delimiter = '#';
                    outputvalues = stringDataReceive.Split(delimiter);

                    //int j = substrings.Length;
                    //int x = 0;

                    //if (j > 0)
                    //{
                    //for (int i = 1; i < j - 1; i++)
                    //{
                    //	//Console.WriteLine(substrings[5*i+2]);
                    //	//x = Int32.Parse(substrings[5 * i - 2]);
                    //	outputvalues[i - 1] = Double.Parse(substrings[i]);
                    //}
                    //}

                    // Release the socket.
                    //Vision.Shutdown(SocketShutdown.Both);
                    //Vision.Disconnect(true);

                    if (receivedDataLength > 1)
                        return true;
                    else
                        return false;
                }
                else
                {
                    //MessageBox.Show("IFM Vision Socket: Not Connected");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //myFunc.CreateErrorLog("Error in function 'Process_SerialNumbers_SavedIn_Nests'. Error = " + ex.ToString());
                MessageBox.Show("Vision Socket: Not Connected " + ex.ToString());

                return false;
            }
        }

        private void buttonReadScanner_Click(object sender, EventArgs e)
        {
           
        }

        //public bool Vision_inspection(byte[] send_data ,ref double[] outputvalues)
        public void Vision_ManualInspection(string send_data)
        {
            try
            {


            }
            catch (Exception ex)
            {
                //myFunc.CreateErrorLog("Error in function 'Process_SerialNumbers_SavedIn_Nests'. Error = " + ex.ToString());
                MessageBox.Show("Vision Socket: Not Connected " + ex.ToString());
            }
        }

        //public bool Vision_inspection(byte[] send_data ,ref string[] outputvalues)
        public bool Vision_inspection_Gocator(string send_data, ref string[] outputvalues)
        {
            try
            {
                if (!VisionGocator.Connected)
                    VisionGocator.Connect(ip_Gocator);

                if (VisionGocator.Connected)
                {
                    //string stringData = Encoding.ASCII.GetString(send_data, 0, 3);
                    //Console.WriteLine(stringData);
                    byte[] bytes = Encoding.ASCII.GetBytes(send_data);
                    Console.WriteLine(send_data);

                    VisionGocator.Send(bytes);

                    //Done. Now let's listen for data
                    byte[] receiveData = new byte[200];
                    int receivedDataLength = VisionGocator.Receive(receiveData);



                    //if the response is a string message
                    string stringDataReceive = Encoding.ASCII.GetString(receiveData, 0, receivedDataLength);
                    Console.WriteLine(stringDataReceive);

                    Char delimiter = ';';
                    outputvalues = stringDataReceive.Split(delimiter);


                    // if (receivedDataLength > 1)
                    return true;
                    // else
                    //     return false;
                }
                else
                {

                    return false;
                }
            }
            catch (Exception ex)
            {
                //myFunc.CreateErrorLog("Error in function 'Process_SerialNumbers_SavedIn_Nests'. Error = " + ex.ToString());
                MessageBox.Show("Vision Socket: Not Connected " + ex.ToString());

                return false;
            }
        }

        #endregion

        #region  UDP Socket

        private void ConnectSocket()
        {
            SERV_IP_ADDR = OMRON_IPAddress;
            ep = new IPEndPoint(IPAddress.Parse(SERV_IP_ADDR), FINS_UDP_PORT);

            Console.WriteLine("connection established ");
        }

        #endregion

        #region  Auxiliary
        private void Delay(int mSec)
        {
            double Ti = DateTime.Now.TimeOfDay.TotalMilliseconds, Tf;
            do
            {
                Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                if (Tf < Ti) Ti = Tf;
                Application.DoEvents();
            } while ((Tf - Ti) < (mSec));
        }

        private void unblock_ButtonsE1()
        {
            E1_JogM_check.Enabled = true;
            E1_JogP_Check.Enabled = true;
            E1_Homing_button.Enabled = true;
            E1_Move2Pos_Button.Enabled = true;
        }
        private void unblock_ButtonsE2()
        {
            E2_JogM_check.Enabled = true;
            E2_JogP_check.Enabled = true;
            E2_Homing_button.Enabled = true;
            E2_Move2Pos_button.Enabled = true;
        }
        private void unblock_ButtonsE3()
        {
            E3_JogM_check.Enabled = true;
            E3_JogP_check.Enabled = true;
            E3_Homing_Button.Enabled = true;
            E3_Move2Pos_Button.Enabled = true;
        }
        private void unblock_ButtonsE4()
        {
            E4_JogM_check.Enabled = true;
            E4_JogP_check.Enabled = true;
            E4_Homing_Button.Enabled = true;
            E4_Move2Pos_button.Enabled = true;
        }

        #endregion

    }
}