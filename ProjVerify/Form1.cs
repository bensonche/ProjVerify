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
            lblMessage.Text = "";
            SaveSettings();

            try
            {
                ToggleControls(false);

                FileInfo csproj = new FileInfo(txtCsproj.Text);
                if (!csproj.Exists)
                {
                    lblMessage.Text = String.Format("Cannot find {0}", csproj.Name);
                    return;
                }

                Dictionary<string, string> csprojD;

                using (FileStream fs = csproj.OpenRead())
                using (StreamReader sr = new StreamReader(fs))
                {
                    string csprojString = sr.ReadToEnd();

                    XElement element = XElement.Parse(csprojString);
                    XNamespace ns = element.GetDefaultNamespace();

                    csprojD = (from field in element.Descendants(ns + "Compile")
                               select Path.Combine(csproj.DirectoryName, field.Attribute("Include").Value))
                                            .Union(from field in element.Descendants(ns + "Content")
                                                   select Path.Combine(csproj.DirectoryName, field.Attribute("Include").Value)).ToDictionary(x => x, x => "");
                }

                Dictionary<string, string> paths = new Dictionary<string, string>();
                ProcessDir(paths, txtDir.Text);

                List<string> notInFileSystem = new List<string>();
                List<string> notInCsproj = new List<string>();

                foreach(var a in csprojD.Keys)
                {
                    if (!paths.Keys.Contains(a))
                        notInFileSystem.Add(a);
                }

                foreach (var a in paths.Keys)
                {
                    if (!csprojD.Keys.Contains(a))
                        notInCsproj.Add(a);
                }
            }
            finally
            {
                ToggleControls(true);
            }
        }

        private void ToggleControls(bool enabled)
        {
            txtCsproj.Enabled = enabled;
            txtDir.Enabled = enabled;
            button1.Enabled = enabled;
            button2.Enabled = enabled;
        }

        private void ProcessDir(Dictionary<string,string> dict, string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            foreach (var f in d.GetFiles())
                dict.Add(f.FullName, "");

            foreach (var subdir in d.GetDirectories())
                ProcessDir(dict, subdir.FullName);
        }
    }
}
