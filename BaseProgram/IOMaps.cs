using System;
using System.Collections.Generic;
using System.Text;

namespace BaseProgram
{
  class IOMaps
    {
    //Função para identificar a dimensão dos arrays de estruturas a criar
    //Tem de ter a dimensão correspondente ao máximo valor
    public int Dimensao(string[,] tabela, string tipo)
      {
      int nlinhas = tabela.GetLength(0);       //variavel com o nº de linhas 
      int ncolunas = tabela.GetLength(1);      //variavel com o nº de colunas
      int n = 0;                               //variavel com o nº de tipos
      int min = 0;                             //variavel com o menor nº 
      int max = 0;                             //variavel com o maior nº 
      bool first = false;                      //variável para executar apenas no 1º ciclo

      if (tipo == "DI" | tipo == "DO" | tipo == "AI") {
        for (int j = 0; j < nlinhas; j++) {
          if (tabela[j, 1] == tipo) {
            if (!first) {
              min = max = Convert.ToInt16(tabela[j, 2]);
              first = true;
              }
            if (Convert.ToInt16(tabela[j, 2]) < min) min = Convert.ToInt16(tabela[j, 2]);
            if (Convert.ToInt16(tabela[j, 2]) > max) max = Convert.ToInt16(tabela[j, 2]);
            n++;
            }
          }
        }
      //No caso de n ser 0, é preciso diferenciar se existe um elemento ou não
      if (n > 0) return max + 1;  //para criar um array de dimensão equivalente
      else return max;            //para informar que não há elementos
      }

    //Função para Preenccher Array de Structs DI - Digital Inputs
    public void PreencheMapaDI(string[,] Map, DI[] DIMap)
      {
      int nlinhas = Map.GetLength(0);     //variavel com o nº de linhas 
      int ncolunas = Map.GetLength(1);    //variavel com o nº de colunas
      int nDI = 0;                        //variavel com o nº de DIs
      int minDI = 0;                      //variavel com o menor nº de DIs
      int maxDI = 0;                      //variavel com o maior nº de DIs
      bool first = false;                 //variável para executar apenas no 1º ciclo
      int nDIMap = 0;                     //variavel com o nº elementos de DIMap


      //Ciclo para encontrar variáveis DI na tabela
      for (int j = 0; j < nlinhas; j++) {
        if (Map[j, 1] == "DI") {
          if (!first) {
            minDI = maxDI = Convert.ToInt16(Map[j, 2]);
            first = true;
            }
          if (Convert.ToInt16(Map[j, 2]) < minDI) minDI = Convert.ToInt16(Map[j, 2]);
          if (Convert.ToInt16(Map[j, 2]) > maxDI) maxDI = Convert.ToInt16(Map[j, 2]);
          nDI++;
          }
        }

      //Ciclo para preenchimento da struct DIMap
      for (int z = 0; z < nlinhas; z++) {
        nDIMap = Convert.ToInt16(Map[z, 2]);
        if (Map[z, 1] == "DI") {
          DIMap[nDIMap].IOName = Map[z, 0];
          DIMap[nDIMap].IOAddress = Convert.ToInt16(Map[z, 2]);
          DIMap[nDIMap].IOText = Map[z, 3];
          DIMap[nDIMap].IOTag = Map[z, 4];
          DIMap[nDIMap].ModuleType = Map[z, 5];
          }
        }
      PreencheReservasMapaDI(Program.diMap, Program.diMapMax);
      return;
      }

    //Função para Preencher Array de Structs DO - Digital Outputs
    public void PreencheMapaDO(string[,] Map, DO[] DOMap)
      {
      int nlinhas = Map.GetLength(0);     //variavel com o nº de linhas 
      int ncolunas = Map.GetLength(1);    //variavel com o nº de colunas
      int nDO = 0;                        //variavel com o nº de DOs
      int minDO = 0;                      //variavel com o menor nº de DOs
      int maxDO = 0;                      //variavel com o maior nº de DOs
      bool first = false;                 //variável para executar apenas no 1º ciclo
      int nDOMap = 0;                     //variavel com o nº elementos de DOMap

      //Ciclo para encontrar variáveis DO na tabela
      for (int j = 0; j < nlinhas; j++) {
        if (Map[j, 1] == "DO") {
          if (!first) {
            minDO = maxDO = Convert.ToInt16(Map[j, 2]);
            first = true;
            }
          if (Convert.ToInt16(Map[j, 2]) < minDO) minDO = Convert.ToInt16(Map[j, 2]);
          if (Convert.ToInt16(Map[j, 2]) > maxDO) maxDO = Convert.ToInt16(Map[j, 2]);
          nDO++;
          }
        }

      //Ciclo para preenchimento da struct DOMap
      for (int z = 0; z < nlinhas; z++) {
        nDOMap = Convert.ToInt16(Map[z, 2]);
        if (Map[z, 1] == "DO") {
          DOMap[nDOMap].IOName = Map[z, 0];
          DOMap[nDOMap].IOAddress = Convert.ToInt16(Map[z, 2]);
          DOMap[nDOMap].IOText = Map[z, 3];
          DOMap[nDOMap].IOTag = Map[z, 4];
          DOMap[nDOMap].ModuleType = Map[z, 5];
          }
        }
      PreencheReservasMapaDO(Program.doMap, Program.doMapMax);
      return;
      }

    //Função para Preencher Array de Structs AI - Analogic Inputs
    public void PreencheMapaAI(string[,] Map, AI[] AIMap)
      {
      int nlinhas = Map.GetLength(0);     //variavel com o nº de linhas 
      int ncolunas = Map.GetLength(1);    //variavel com o nº de colunas
      int nAI = 0;                        //variavel com o nº de AIs
      int minAI = 0;                      //variavel com o menor nº de AIs
      int maxAI = 0;                      //variavel com o maior nº de AIs
      bool first = false;                 //variável para executar apenas no 1º ciclo
      int nAIMap = 0;                     //variavel com o nº elementos de AIMap

      //Ciclo para encontrar variáveis AI na tabela
      for (int j = 0; j < nlinhas; j++) {
        if (Map[j, 1] == "AI") {
          if (!first) {
            minAI = maxAI = Convert.ToInt16(Map[j, 2]);
            first = true;
            }
          if (Convert.ToInt16(Map[j, 2]) < minAI) minAI = Convert.ToInt16(Map[j, 2]);
          if (Convert.ToInt16(Map[j, 2]) > maxAI) maxAI = Convert.ToInt16(Map[j, 2]);
          nAI++;
          }
        }

      //Ciclo para preenchimento da struct AIMap
      for (int z = 0; z < nlinhas; z++) {
        nAIMap = Convert.ToInt16(Map[z, 2]);
        if (Map[z, 1] == "AI") {
          AIMap[nAIMap].IOName = Map[z, 0];
          AIMap[nAIMap].IOAddress = Convert.ToInt16(Map[z, 2]);
          AIMap[nAIMap].IOText = Map[z, 3];
          AIMap[nAIMap].IOTag = Map[z, 4];
          AIMap[nAIMap].ModuleType = Map[z, 5];
          }
        }
      PreencheReservasMapaAI(Program.aiMap, Program.aiMapMax);
      return;
      }

    //Função para Preencher as Reservas do Array de Structs DO
    public void PreencheReservasMapaDI(DI[] DIMap, int DIMapMax)
      {
      //Ciclo para preenchimento da struct DOMap
      for (int z = 0; z < DIMapMax; z++) {
        if (DIMap[z].IOName == null) {
          DIMap[z].IOName = "Reserve";
          DIMap[z].IOAddress = z;
          }
        }
      return;
      }

    //Função para Preenccher as Reservas do Array de Structs DO
    public void PreencheReservasMapaDO(DO[] DOMap, int DOMapMax)
      {
      //Ciclo para preenchimento da struct DOMap
      for (int z = 0; z < DOMapMax; z++) {
        if (DOMap[z].IOName == null) {
          DOMap[z].IOName = "Reserve";
          DOMap[z].IOAddress = z;
          }
        }
      return;
      }

    //Função para Preencher as Reservas do Array de Structs DO
    public void PreencheReservasMapaAI(AI[] AIMap, int AIMapMax)
      {
      //Ciclo para preenchimento da struct DOMap
      for (int z = 0; z < AIMapMax; z++) {
        if (AIMap[z].IOName == null) {
          AIMap[z].IOName = "Reserve";
          AIMap[z].IOAddress = z;
          AIMap[z].ValueRead = 0;
          }
        }
      return;
      }
    }
  }
