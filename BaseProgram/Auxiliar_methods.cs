using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace BaseProgram
{
    class Auxiliar_methods
    {

        // funções auxiliares para coisas expecificas
        ////////////////////////////////////////////////////////////////////////////

        #region Timer counter
        private static DateTime time_ini;

        public static void StartTimer()
        {
            time_ini = DateTime.Now;
        }
        public static double StepTime()
        {
            DateTime time_fin = DateTime.Now;
            TimeSpan time = time_fin - time_ini;
            double timer = Convert.ToDouble(time.TotalMilliseconds);
            return timer;
        }
        #endregion

        #region convert to Byte array
        public static byte[] GetIntToByte(uint MSG)
        {
            byte[] Value = new byte[4];

            Value = IntToByteArray(MSG);


            return Value;
        }
        public static byte[] IntToByteArray(ushort convert)
        {
            byte[] result = new byte[sizeof(ushort)];

            ushort mask = 0xff;

            for (int i = result.Length - 1; i >= 0; i--)
            {
                result[i] = (byte)(convert & mask);
                convert >>= 8;

            }

            return result;
        }
        public static byte[] IntToByteArray(uint convert)
        {
            byte[] result = new byte[sizeof(uint)];

            uint mask = 0xff;

            for (int i = result.Length - 1; i >= 0; i--)
            {
                result[i] = (byte)(convert & mask);
                convert >>= 8;

            }

            return result;
        }
        #endregion

        #region // Functions to print
        // Auxiliary functions for prints///
        static string BytearrayTostring(byte[] value)
        {
            StringBuilder result = new StringBuilder();

            foreach (byte b in value)
            {
                result.Append(string.Format(" {0}", ByteToBinaryString(b)));
            }

            return result.ToString();
        }
        public static string ByteToBinaryString(byte value)
        {
            string result = string.Empty;

            int counter = sizeof(byte) * 8;
            int mask = 1;

            while (counter-- > 0)
            {
                char c = (value & mask) == mask ? '1' : '0';
                result = c + result;
                value >>= 1;
            }
            return result.ToString();
        }
        public static string BuildString(byte[] value)
        {
            string Print = string.Empty;
            StringBuilder hex = new StringBuilder(value.Length * 2);
            foreach (byte b in value)
                hex.AppendFormat("{0:x2}", b);

            Print = hex.ToString();

            return Print;
        }
        public static string[] BreakInHexWords(byte[] rec, uint N_word2Read)
        {
            int j = 0;
            string[] Value = new string[N_word2Read];
            string Print = BuildString(rec);
            string tobreak = Print.Substring(14 * 2);
            for (int i = 0; i < tobreak.Length; i = i + 4)
            {
                Value[j] = tobreak.Substring(i, 4);
                j++;
            }
            return Value;
        }

        public static byte[] GetStringToInt(string[] Data, int i)
        {
            if (Data != null)
            {
                byte[] value = new byte[4];
                string testes = Data[i].Substring(0, 2);
                string testes1 = Data[i].Substring(2, 2);
                string testes2 = Data[i + 1].Substring(0, 2);
                string testes3 = Data[i + 1].Substring(2, 2);
                value[0] = byte.Parse(testes1, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                value[1] = byte.Parse(testes, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                value[2] = byte.Parse(testes3, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                value[3] = byte.Parse(testes2, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                return value;
            }
            else
                return null;
        }

        public static bool Ticket(string Error)
        {
            bool printStatus = false;
            try
            {
                //Ticket_code = TicketCode_Compile(Ticket_code, Error);

                Socket clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSock.NoDelay = true;
                IPAddress ip = IPAddress.Parse("192.168.1.70");
                IPEndPoint remoteEP = new IPEndPoint(ip, 9100);
                clientSock.Connect(remoteEP);

                Encoding enc = Encoding.ASCII;

                // Line feed hexadecimal values
                byte[] bEsc = new byte[4];
                bEsc[0] = 0x0A;
                bEsc[1] = 0x0A;
                bEsc[2] = 0x0A;
                bEsc[3] = 0x1A;
                // Send the bytes over 
                clientSock.Send(bEsc);

                char[] Labelarray = Error.ToCharArray();
                byte[] LabelbyData = enc.GetBytes(Labelarray);
                clientSock.Send(LabelbyData);

                // Sends an ESC/POS command to the printer to cut the paper
                string Text1 = "\n" + Convert.ToChar(29) + "V" + Convert.ToChar(65) + Convert.ToChar(0);
                char[] array = Text1.ToCharArray();
                byte[] byData = enc.GetBytes(array);
                clientSock.Send(byData);
                clientSock.Close();
                printStatus = true;
            }
            catch
            {
                printStatus = false;
            }
            return printStatus;
        }
        public void WriteToLog(string Data, string Path)
        {
            string folderName = @Path;

            string pathStringY = System.IO.Path.Combine(folderName, DateTime.Now.Year.ToString());
            string pathStringM = System.IO.Path.Combine(pathStringY, DateTime.Now.Month.ToString("00"));

            //create folder to save mounth logs

            System.IO.Directory.CreateDirectory(pathStringM);

            // construct the data to save in file 
            String toSave = DateTime.Now.Hour.ToString("00") + "_" + DateTime.Now.Minute.ToString("00") + "_" + DateTime.Now.Second.ToString("00") + "::" + Data;

            // create the file name and the path where to save it
            string date = DateTime.Now.Day.ToString("00");
            string path = pathStringM + "\\" + date + ".csv";

            try
            {
                //Se o histórico do dia não existir cria o ficheiro com os cabeçalhos
                if (!File.Exists(path))
                {
                    // Cria o ficheiro
                    using (StreamWriter sw = File.CreateText(path))
                    {
                    }
                }

                //Acrescenta os valores ao ficheiro existente
                using (StreamWriter writer = File.AppendText(path))
                {
                    writer.WriteLine(toSave);
                    writer.Flush();
                    writer.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }



        }
        #endregion


    }

}
