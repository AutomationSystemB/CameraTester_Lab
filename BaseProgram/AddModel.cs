using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Dispositivo_1
{
    public partial class AddModel : Form
    {
        // adicionar um delegate
        public delegate void IdentityUpdateHandler(object sender, IdentityUpdateEventArgs e);

        // adicionar um event do tipo delegate
        public event IdentityUpdateHandler IdentityUpdated;

        public AddModel()
        {
            InitializeComponent();
        }

        private void buttonCancelModel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void buttonOKModel_Click(object sender, EventArgs e)
        {
            // this button click event handler will raise
            // the event which can then intercepted by any
            // listeners
            // read the textboxes and set the variables

            string sNewProduct = textBoxProduct.Text;
            string sNewLed = comboBoxLed.Text;
            decimal sNewMIN1 = numericUpDownMIN1.Value;
            decimal sNewMAX1 = numericUpDownMAX1.Value;
            decimal sNewMIN2 = numericUpDownMIN2.Value;
            decimal sNewMAX2 = numericUpDownMAX2.Value;
            decimal sNewMIN3 = numericUpDownMIN3.Value;
            decimal sNewMAX3 = numericUpDownMIN3.Value;
            decimal sNewXTampa = numericUpDownXTampa.Value;
            decimal sNewZTampa = numericUpDownZTampa.Value;
            decimal sNewXTecla = numericUpDownXTecla.Value;
            decimal sNewZTecla = numericUpDownZTecla.Value;

            // instance the event args and pass it each
            // value

            if (textBoxProduct.Text == "")
            {
                MessageBox.Show("Falta o nome do produto");
                return;
            }

            if (numericUpDownMIN1.Value <= numericUpDownMAX1.Value && textBoxProduct.Text != "")
            {

                IdentityUpdateEventArgs args = new IdentityUpdateEventArgs(sNewProduct, sNewLed, sNewMIN1, sNewMAX1, sNewMIN2, sNewMAX2, sNewMIN3, sNewMAX3, sNewXTampa, sNewZTampa, sNewXTecla, sNewZTecla);

                // raise the event with the updated arguments

                IdentityUpdated(this, args);
                this.Dispose();
            }
            else
            {
                MessageBox.Show("MIN tem que ser maior que MAX");
            }
        }

        private void FormModel_Load(object sender, EventArgs e)
        {
            comboBoxLed.SelectedIndex = 1;
        }
    }

  
    public class IdentityUpdateEventArgs : System.EventArgs
    {
        // add local member variable to hold text
        private string mProduct;
        private string mLed;
        private decimal mMIN1;
        private decimal mMAX1;
        private decimal mMIN2;
        private decimal mMAX2;
        private decimal mMIN3;
        private decimal mMAX3;
        private decimal mXTampa;
        private decimal mZTampa;
        private decimal mXTecla;
        private decimal mZTecla;
       
        // constructor da classe

        public IdentityUpdateEventArgs(string sNewProduct, string sNewLed, decimal sNewMIN1, decimal sNewMAX1, decimal sNewMIN2, decimal sNewMAX2, decimal sNewMIN3, decimal sNewMAX3, decimal sNewXTampa, decimal sNewZTampa, decimal sNewXTecla, decimal sNewZTecla)
        {
            this.mProduct = sNewProduct;
            this.mLed = sNewLed;
            this.mMIN1 = sNewMIN1;
            this.mMAX1 = sNewMAX1;
            this.mMIN2 = sNewMIN2;
            this.mMAX2 = sNewMAX2;
            this.mMIN3 = sNewMIN3;
            this.mMAX3 = sNewMAX3;
            this.mXTampa = sNewXTampa;
            this.mZTampa = sNewZTampa;
            this.mXTecla = sNewXTecla;
            this.mZTecla = sNewZTecla;
        }

        // Properties - Accessible by the listener

        public string Product
        {
            get { return mProduct; }
        }

        public string Led
        {
            get { return mLed; }
        }

        public decimal MIN1
        {
            get { return mMIN1; }
        }

        public decimal MAX1
        {
            get { return mMAX1; }
        }

        public decimal MIN2
        {
            get { return mMIN2; }
        }

        public decimal MAX2
        {
            get { return mMAX2; }
        }

        public decimal MIN3
        {
            get { return mMIN3; }
        }

        public decimal MAX3
        {
            get { return mMAX3; }
        }

        public decimal XTampa
        {
            get { return mXTampa; }
        }

        public decimal ZTampa
        {
            get { return mZTampa; }
        }

        public decimal XTecla
        {
            get { return mXTecla; }
        }

        public decimal ZTecla
        {
            get { return mZTecla; }
        }
    }
}


