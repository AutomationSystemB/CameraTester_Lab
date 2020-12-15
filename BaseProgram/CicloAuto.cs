using System;
using System.Collections;
using System.Threading;
using System.Windows.Forms;
using System.Drawing; 
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using System.Collections.Generic;

namespace BaseProgram
{    public partial class MenuPrincipal
    {
        private bool first = true;
        public static DateTime time_ini;         //variável de tempo de execução de step
        public static DateTime time_iniPress;    //variável de tempo de execução da aspiração
        int passoSaida = 200;
        bool BlinkGreen = false;
        string SaveToLog;
        int[] Putty=new int[10];
        const int TimeOutDelay = 4000;

        public int RunCicloAuto(int step)
        {

            lblStep.Text = "Step: " + step;
            Console.WriteLine("Case: " + Convert.ToString(step));

            int nextStep;
            nextStep = 0;

            switch (step)
            {
                //===================================================================================
                case 5:   //Init
                    //===================================================================================

                    nextStep = 10;

                    break;

                
                

                // ------------------------------------------------------------------------------- //
                // ------------------------------------------------------------------------------- //
                //                 TRATAMENTO DE ERROS DURANTE O CICLO AUTOMÁTICO                  //
                // ------------------------------------------------------------------------------- //
                // ------------------------------------------------------------------------------- //

                //===================================================================================
                case 500:   //Falta Pressão de Ar          
                    //===================================================================================

                    BlinkError();

                    SaveToLog = txtInstructions.Text = TextByTag(243);
                    //txtInstructions.Text = "Falha Pressão Ar - Confirme no Botão NOK";

                    if (ReadDIByName("Start_Button"))      //Botão NOK
                        nextStep = 1;                          //Terminar Ciclo

                    break;

                //===================================================================================

                case 501:                   //Falha de Movimentos 
                case 502:                   //Falha de Movimentos 
                case 503:                   //Falha de Movimentos
                case 504:                   //Falha de Movimentos
                case 505:                   //Falha de Movimentos 
                case 506:                   //Falha de Movimentos 
                case 507:                   //Falha de Movimentos
                case 508:                   //Falha de Movimentos
                case 509:                   //Falha de Movimentos 
                case 510:                   //Falha de Movimentos 
                case 511:                   //Falha de Movimentos
                case 512:                   //Falha de Movimentos
                case 513:                   //Falha de Movimentos 
                case 514:                   //Falha de Movimentos 

                    //=================================================================================== 

                    string[] erro = {
                                "Movimento do Cilindro da Mascara de aparafusamento",
                                "Movimento do Eixo X",
                                "Movimento do Eixo X1",
                                "Movimento do Eixo Y",
                                "Movimento do Eixo Z",
                                "Dispensar da Pasta",
                                "Movimento do Cilindro da Ponteira da Aparafusadora",
                                "Movimento do Cilindro da Gaveta do Alimentador da Aparafusadora"
                                    };

                    int nErro = step - 501;
                    txtInstructions.Text = "\r\nERRO NÃO RECUPERAVEL!!\r\n" + TextByTag(265) + erro[nErro] + "\r\n" + TextByTag(253);
                    BlinkError(); //Piscar o Led Vermelho e o backColor TextBox

                    if (ReadDIByName("Start_Button"))
                        nextStep = 1;
                    
                    break;

                //=======================================================================================
                case 600:    //Falha: Falha acção do operador
                case 601:    //Falha: envio de mensagem para PLC
                case 602:    //Falha: Falha no putty
                case 603:    //Falha: Falha no aparafusamento print ticket
                case 604:    //Falha: Peça foi retirada do ninho antes de concluida
                case 605:    //Falha: Erro na leitura do barcode
                case 606:    //Falha: 
                case 607:    //Falha: Controlador da aparafusadora
                case 608:    //Falha: 
                case 609:    //Falha: 
                             //======================================================================================= 

                    BlinkError();

                    intErrorCode = step;

                    switch (step)
                    {
                        case 600:

                            txtInstructions.Text = "Falha a acção do operador não foi a esperada\r\n\r\n" + TextByTag(258); //Confirme no Botão NOK

                            break;
                       
                    }

                    if (ReadDIByName("Start_Button"))
                        nextStep = 700;

                    break;

                // ------------------------------------------------------------------------------- //
                // ------------------------------------------------------------------------------- //
                //                 RECUPERAÇÃO DE ERROS DURANTE O CICLO AUTOMÁTICO                 //
                // ------------------------------------------------------------------------------- //
                // ------------------------------------------------------------------------------- //

                //===================================================================================        
                case 700:   //Espera que Largue o Botão NOK
                    //=================================================================================== 

                    Console.WriteLine("Passo de saída: " + passoSaida);

                    WriteDOByName("Led_Red", false);


                    
                   
                    break;

               

            } // End Switch

            if (nextStep == 0)
                return step;
            else
            {
                first = true;
                txtInstructions.Clear();
                ShowPanel = false;
                txtInstructions.BackColor = colorSystem;
                return nextStep;

            }
            //Sempre que mudar de Passo, limpar a mensagem de ciclo, limpar o led de erro e fundo de erro da mensagem
        }

        #region auxiliary functions

        private void SaveResultsToDB()
        {
            //MyDB.SaveResult(WSJobID, "Blende_Mate_L_Sup", AI_0);
        }


        //Função para piscar o led NOK e Backcolor textBox
        private void BlinkError()
        {
            if (first == true) { first = false; Auxiliar_methods.StartTimer(); }

            if (Auxiliar_methods.StepTime() > 500)
            {

                if (txtInstructions.BackColor == colorSystem)
                {
                    txtInstructions.BackColor = colorError;
                    WriteDOByName("Led_Red", true);
                }
                else
                {
                    txtInstructions.BackColor = colorSystem;
                    WriteDOByName("Led_Red", false);
                }

                Auxiliar_methods.StartTimer();
            }
        }

        //Função para piscar o led OK
        private void BlinkOK()
        {
            if (first == true) { first = false; Auxiliar_methods.StartTimer(); }

            if (Auxiliar_methods.StepTime() > 500)
            {
                if (BlinkGreen)
                {
                    WriteDOByName("Led_Green", false);
                    BlinkGreen = false;
                }
                else
                {
                    WriteDOByName("Led_Green", true);
                    BlinkGreen = true;
                }

                Auxiliar_methods.StartTimer();
            }
        }


        //Função para piscar o led NOK e Backcolor textBox
        private void BlinkBackColorText()
        {
            if (first == true) { first = false; Auxiliar_methods.StartTimer(); }

            if (Auxiliar_methods.StepTime() > 500)
            {
                if (txtInstructions.BackColor == colorSystem)
                    txtInstructions.BackColor = colorError;
                else
                    txtInstructions.BackColor = colorSystem;

                Auxiliar_methods.StartTimer();
            }
        }

        #endregion

    }
}
