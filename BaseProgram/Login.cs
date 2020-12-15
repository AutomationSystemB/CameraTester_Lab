using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace BaseProgram
{
    public partial class Login : Form
    {

        public bool getLoggedIn
        {
            get { return LoggedIn; }
        }

        //adicionar um delegate
        public delegate void IdentityUpdateHandler(object sender, IdentityUpdateEventArgs e);

        //adicionar um event do tipo delegate
        public event IdentityUpdateHandler IdentityUpdated;

        //SQLClass MyDB;
        DataSet dsUser = new DataSet("User DataSet");
        DataSet DsLanguage = new DataSet("Language");
        string Language = "";
        bool LoggedIn = false;
        bool READFROMXML = false;
        string StationName = "";

        public Login(ref string Lang, ref DataSet DsLang, bool ReadFromXML, string StationName)
        {
            InitializeComponent();
            DsLanguage = DsLang;
            Language = Lang;
            READFROMXML = ReadFromXML;
            this.StationName = StationName;
        }

        private void Login_Load(object sender, EventArgs e)
        {
            cboUsers.Focus();
            //MyDB = new SQLClass(DBServer, DBName, DBUser, DBUserPsw);
            dsUser = new DataSet("User DataSet");

            try
            {
                if (READFROMXML)
                {
                    dsUser.ReadXml("Users.xml");
                }
                //else {
                //  dsUser.Tables.Add(MyDB.GetAppActiveUsers(StationName));
                //  }
                DataRow Dr2 = dsUser.Tables[0].NewRow();
                Dr2["ID_User"] = "0";
                Dr2["Identification"] = " Escolha ";
                dsUser.Tables[0].Rows.InsertAt(Dr2, 0);
                cboUsers.DataSource = dsUser.Tables[0];
                cboUsers.DisplayMember = "Identification";
                cboUsers.ValueMember = "ID_User";
                UpdateFormLanguage();

            }
            catch (Exception exp)
            {
                MessageBox.Show(TextByTag(1036) + "\r\n" + exp.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private string TextByTag(int intTag)
        {
            try
            {
                return DsLanguage.Tables[0].Select("Tag = '" + intTag + "'")[0][Language].ToString().Replace("|", "\r\n");
            }
            catch (Exception)
            {
                return intTag.ToString();
            }
        }

        private void UpdateFormLanguage()
        {
            dsUser.Tables[0].Rows[0]["Identification"] = TextByTag(1025);
            groupBox10.Text = TextByTag(1022);
            label1.Text = TextByTag(1021);
            buttonLogin.Text = TextByTag(1023);
            btn_Clear.Text = TextByTag(1020);
            button1.Text = TextByTag(1019);
        }

        private void numericButtons_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Control senderNumber;
            senderNumber = (Control)sender;

            //Acrescentar o numero correspondente 
            textBoxPassword.Text += senderNumber.Text;
        }

        private void btn_Clear_Click(object sender, EventArgs e)
        {
            textBoxPassword.Text = "";
        }

        //LOGIN
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            if (cboUsers.SelectedIndex == 0)
            {
                MessageBox.Show(TextByTag(1034), "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (textBoxPassword.Text.Length != 4)
                MessageBox.Show(TextByTag(1035), "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

            else
            {
                //if (!READFROMXML)
                //{
                //    if (MyDB.CheckUserPassword((int)cboUsers.SelectedValue, textBoxPassword.Text))
                //    {
                //        UpdateLoginUser();
                //    }

                //    else
                //    {
                //        MessageBox.Show(TextByTag(1033), "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //        textBoxPassword.Clear();
                //    }
                //}
                //else
                //{
                if (GetUserInfo(Encripta(textBoxPassword.Text)))
                    UpdateLoginUser();

                else
                {
                    MessageBox.Show(TextByTag(1033), "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    textBoxPassword.Clear();
                }
                //}
            }
        }


        //TEXTBOX PASSWORD SELECCIONADA POR ULTIMO
        private void textBoxPassword_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //Tecla Cr foi Pressionada
            if (e.KeyValue == 13)
                buttonLogin_Click(new object(), new EventArgs());
        }

        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {
            if (textBoxPassword.Text.Length > 3) buttonLogin.Focus();
        }

        private string Encripta(string Psw)
        {
            if (READFROMXML) return Psw;

            string s = "";
            char[] chr = Psw.ToCharArray();

            Encoding Enc = Encoding.GetEncoding(1252);
            byte[] byt = Enc.GetBytes(chr);

            for (int i = 0; i < byt.Length; i++)
                byt[i] = (byte)(byt[i] + 80);

            chr = Enc.GetChars(byt);

            for (int i = 0; i < chr.Length; i++)
                s = s + chr[i].ToString();
            return s;
        }

        private bool GetUserInfo(string Psw)
        {
            DataRow row = dsUser.Tables[0].Rows[cboUsers.SelectedIndex];
            if (Psw.Equals(row["Psw"].ToString())) { return true; }
            else { return false; }
        }

        private void UpdateLoginUser()
        {
            DataRow row = dsUser.Tables[0].Rows[cboUsers.SelectedIndex];

            string sID_User = dsUser.Tables[0].Rows[cboUsers.SelectedIndex]["ID_User"].ToString();
            string sIdentification = dsUser.Tables[0].Rows[cboUsers.SelectedIndex]["Identification"].ToString();
            string sUsername = dsUser.Tables[0].Rows[cboUsers.SelectedIndex]["Username"].ToString();
            string sUser_Level = dsUser.Tables[0].Rows[cboUsers.SelectedIndex]["AccessMask"].ToString();

            IdentityUpdateEventArgs args = new IdentityUpdateEventArgs(sID_User, sIdentification, sUsername, sUser_Level);

            IdentityUpdated(this, args);
            LoggedIn = true;
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(TextByTag(1024), TextByTag(1019), MessageBoxButtons.OKCancel,
                  MessageBoxIcon.Question) == DialogResult.OK)

                Application.Exit();
        }

        private void cboUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DataRow Drow = dsUser.Tables[0].Rows[cboUsers.SelectedIndex];
                lblNome.Text = Drow["Username"].ToString();
                buttonLogin.Enabled = true;
                textBoxPassword.Focus();
            }
            catch
            {
                MessageBox.Show("Erro na selecção do operador. O Programa irá encerrar", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Dispose();
                GC.Collect();
                Application.ExitThread();
                Application.Exit();
            }
        }        
    }

    public class IdentityUpdateEventArgs : System.EventArgs
    {
        //add local member variable to hold text
        private string mID_User;
        private string mIdentification;
        private string mUsername;
        //private string mPsw;
        private string mUser_Level;

        //constructor da classe
        public IdentityUpdateEventArgs(string sID_User, string sIdentification, string sUsername, string sUser_Level)
        {
            this.mID_User = sID_User;
            this.mIdentification = sIdentification;
            this.mUsername = sUsername;
            this.mUser_Level = sUser_Level;
        }

        //Properties - Accessible by the listener
        public string ID_User
        {
            get { return mID_User; }
        }

        public string Identification
        {
            get { return mIdentification; }
        }

        public string Username
        {
            get { return mUsername; }
        }

        //public string Psw
        //{
        //    get { return mPsw; }
        //}

        public string User_Level
        {
            get { return mUser_Level; }
        }
    }
}
