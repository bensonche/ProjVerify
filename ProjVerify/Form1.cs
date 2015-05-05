using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Linq;

namespace ProjVerify
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            LoadSettings();
        }

        #region Save/Load Settings

        private const string settingsFileName = "settings.xml";

        private void LoadSettings()
        {
            try
            {
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

                if (!isoStore.FileExists(settingsFileName))
                    return;

                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(settingsFileName, FileMode.Open, FileAccess.Read, isoStore))
                using (StreamReader sr = new StreamReader(isoStream))
                {
                    string settings = sr.ReadToEnd();

                    XElement element = XElement.Parse(settings);

                    txtCsproj.Text = (from field in element.Elements("appSettings").Elements("csproj")
                                      select field.Value).FirstOrDefault() ?? "";
                    txtDir.Text = (from field in element.Elements("appSettings").Elements("directory")
                                      select field.Value).FirstOrDefault() ?? "";
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(settingsFileName, FileMode.OpenOrCreate, FileAccess.Write, isoStore))
                using(StreamWriter sw = new StreamWriter(isoStream))
                {
                    XElement element =
                        new XElement("config",
                            new XElement("appSettings",
                                new XElement("csproj", txtCsproj.Text),
                                new XElement("directory", txtDir.Text)
                            )
                        );

                    sw.Write(element.ToString());
                }
            }
            catch { }
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            if(result == DialogResult.OK)
            {
                txtCsproj.Text = dialog.FileName;

                txtDir.Text = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtDir.Text = dialog.SelectedPath;
            }

        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
