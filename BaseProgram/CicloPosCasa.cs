using System;

namespace BaseProgram
{
    public partial class MenuPrincipal
    {

        bool[] Result = new bool[19];
        string[] Description = new string[19];


        public int RunPosCasa(int step)
        {
            lblStep.Text = "Step: " + step;
            Console.WriteLine("Case: " + Convert.ToString(step));
            int nextStep = 0;

            switch (step)
            {
                //============================================================================================================
                case 10:    //Inicializações e ponto de Partida:
                    //============================================================================================================

                    erroPosCasa = false;

                    if (ReadDIByName("Security_Curtin"))
                    {
                        txtInstructions.Text = "A Colocar na Posição Inicial";
                        nextStep = 15;
                    }
                    else
                    {
                        //txtInstructions.Text = TextByTag(203);
                        txtInstructions.Text = "Aguarda barreiras desimpedidas";
                    }

                    break;



                // -------------------------------------------------------------------------------------------------------- //
                // -------------------------------------------------------------------------------------------------------- //
                //                          TRATAMENTO DE ERROS DURANTE O CICLO POSIÇÃO INICIAL                             //
                // -------------------------------------------------------------------------------------------------------- //
                // -------------------------------------------------------------------------------------------------------- //

                //============================================================================================================
                case 201:                   //Falha de Movimentos
                case 202:                   //Falha de Movimentos
                case 203:                   //Falha de Movimentos
                case 204:                   //Falha de Movimentos
                case 205:                   //Falha de Movimentos
                case 206:                   //Falha de Movimentos
                case 207:                   //Falha de Movimentos
                case 208:                   //Falha de Movimentos
                case 209:                   //Falha de Movimentos
                case 210:                   //Falha de Movimentos
                case 211:                   //Falha de Movimentos
                case 212:                   //Falha de Movimentos

                    //============================================================================================================

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

                    int nErro = step - 201;

                    erroPosCasa = true;

                    txtInstructions.Text = TextByTag(265) + "\r\n\r\n" + erro[nErro] + "\r\n" + TextByTag(258);
                    BlinkError(); //Piscar o Led Vermelho e o backColor da TextBox

                    //Saltos
                    if (ReadDIByName("Start_Button"))            //Confirmação de Falha
                        nextStep = 1;

                    break;

                //============================================================================================================
                case 500:    //Dispositivo em posição Inicial !!          
                             //============================================================================================================

                // -------------------------------------------------------------------------------------------------------- //
                // -------------------------------------------------------------------------------------------------------- //
                //                          TRATAMENTO DE ERROS DURANTE O CICLO POSIÇÃO INICIAL                             //
                // -------------------------------------------------------------------------------------------------------- //
                // -------------------------------------------------------------------------------------------------------- //

                //============================================================================================================
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

                    //============================================================================================================

                    string[] erroU = {
                            "Erro Comunicação PLC",
                            "Movimento ....",
                            "Movimento ...",
                            "Movimento ...",
                            "Movimento ...",
                            "Movimento ...",
                            "Movimento ..."
                                };

                    int nErroUDP = step - 501;

                    txtInstructions.Text = TextByTag(265) + "\r\n\r\n" + erroU[nErroUDP] + "\r\n" + TextByTag(258);
                    //txtInstructions.Text = "Falha: " + erro[nErro] + " \r\n\r\nPrima o Botão NOK para continuar..";
                    BlinkError(); //Piscar o Led Vermelho e o backColor da TextBox

                    //Saltos
                    if (ReadDIByName("Start_Button"))
                    {//Confirmação de Falha
                        WriteDOByName("Led_Red", false);
                        nextStep = 1;
                    }
                    break;
            }

            if (nextStep == 0)
                return step;
            else
            {
                first = true;
                ShowPanel = false;
                return nextStep;
            }
        }
    }
}
