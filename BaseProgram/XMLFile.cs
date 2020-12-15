using System.Xml;

namespace BaseProgram
{
    class XMLFile
    {
        private string MyIOFileName = "IO.xml";

        public string[,] ArrayIO()
        {
            string a, b, c, d, e, f;
            int i = 0;

            // Abrir o Ficheiro XML
            XmlDocument doc = new XmlDocument();
            doc.Load(MyIOFileName);

            XmlNodeList xmlIOs = doc.GetElementsByTagName("IO");
            int columns = 6;
            // Numero de elementos IO do ficheiro XML
            int rows = xmlIOs.Count;

            // Declaração do array multi-dimensional
            string[,] ioArray = new string[rows, columns];

            // Percorrer todos os elementos do ficheiro e ler os seus filhos
            foreach (XmlNode xmlIO in xmlIOs)
            {
                a = xmlIO.ChildNodes[0].InnerText;
                b = xmlIO.ChildNodes[1].InnerText;
                c = xmlIO.ChildNodes[2].InnerText;
                d = xmlIO.ChildNodes[3].InnerText;
                e = xmlIO.ChildNodes[4].InnerText;
                f = xmlIO.ChildNodes[5].InnerText;

                // Só aceita se for um tipo de variavel aceitavel
                if (b == "DO" | b == "AO" | b == "DI" | b == "AI")
                {
                    ioArray[i, 0] = a;
                    ioArray[i, 1] = b;
                    ioArray[i, 2] = c;
                    ioArray[i, 3] = d;
                    ioArray[i, 4] = e;
                    ioArray[i, 5] = f;
                    i++;
                }
                // retorna null se o tipo da IO for desconhecido
                else
                {
                    return null;
                }
            }
            return ioArray;
        }

        public int ArrayLength()
        {
            // Abrir o Ficheiro XML
            XmlDocument doc = new XmlDocument();
            doc.Load(MyIOFileName);

            XmlNodeList elementsCount = doc.GetElementsByTagName("IO");
            // numero de elementos IO do ficheiro XML
            int rows = elementsCount.Count;

            // Retorna o numero de elementos (IOs) do ficheiro XML             
            return rows;
        }

        /// <summary>
        /// This function provides an array of rowns for each Table in the file with as many columns as the value inserted in the n_elementos
        /// </summary>
        public string[,] ObjectList(string myFileName, int n_elementos)
        {

            int i = 0, j = 0;

            // Abrir o Ficheiro XML
            XmlDocument doc = new XmlDocument();
            doc.Load(myFileName);

            XmlNodeList xmlIOs = doc.GetElementsByTagName("Table");

            int columns = n_elementos;
            // Numero de elementos IO do ficheiro XML
            int rows = xmlIOs.Count;


            // Declaração do array multi-dimensional
            string[,] ioArray = new string[rows, columns];

            // Percorrer todos os elementos do ficheiro e ler os seus filhos
            foreach (XmlNode xmlIO in xmlIOs)
            {

                for (j = 0; j < n_elementos; j++)
                {
                    ioArray[i, j] = xmlIO.ChildNodes[j].InnerText;
                }
                // genérico retorna todos os dados
                j = 0;
                i++;

            }

            return ioArray;
        }
       
    }
}