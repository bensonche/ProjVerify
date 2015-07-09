using System;
using System.Collections;
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
using bche.SettingsManager;

namespace ProjVerify
{
    public partial class Form1 : Form
    {
        private readonly string[] ExcludeList = {"obj", "bin"};

        private readonly SettingsManager Settings;

        public Form1()
        {
            Settings = new SettingsManager();

            InitializeComponent();

            LoadSettings();
        }

        #region Save/Load Settings

        private void LoadSettings()
        {
            Settings.LoadSettings();
            txtCsproj.Text = Settings["csproj"];
            txtDir.Text = Settings["directory"];
        }

        private void SaveSettings()
        {
            Settings["csproj"] = txtCsproj.Text;
            Settings["directory"] = txtDir.Text;
            Settings.SaveSettings();
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
            if(result == DialogResult.OK)
            {
                txtDir.Text = dialog.SelectedPath;
            }

        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            txtResult.Text = "";

            SaveSettings();

            try
            {
                ToggleControls(false);

                FileInfo csproj = new FileInfo(txtCsproj.Text);
                if(!csproj.Exists)
                {
                    txtResult.Text = String.Format("Cannot find {0}", csproj.Name);
                    return;
                }

                Dictionary<string, int> csprojD = new Dictionary<string, int>();

                using(FileStream fs = csproj.OpenRead())
                using(StreamReader sr = new StreamReader(fs))
                {
                    string csprojString = sr.ReadToEnd();

                    XElement element = XElement.Parse(csprojString);
                    XNamespace ns = element.GetDefaultNamespace();

                    var compile = from field in element.Descendants(ns + "Compile")
                                  select Path.Combine(csproj.DirectoryName, field.Attribute("Include").Value);
                    var content = from field in element.Descendants(ns + "Content")
                                  select Path.Combine(csproj.DirectoryName, field.Attribute("Include").Value);

                    csprojD.Add(compile);
                    csprojD.Add(content);
                }

                Dictionary<string, string> paths = new Dictionary<string, string>();
                ProcessDir(paths, txtDir.Text, true);

                List<string> notInFileSystem = new List<string>();
                List<string> notInCsproj = new List<string>();

                foreach(var a in csprojD.Keys)
                {
                    if(!paths.Keys.Contains(a))
                        notInFileSystem.Add(a);
                }

                foreach(var a in paths.Keys)
                {
                    if(!csprojD.Keys.Contains(a))
                        notInCsproj.Add(a);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Files in csproj but not in file system:");
                sb.AppendLine();
                foreach(var a in notInFileSystem.OrderBy(x => x))
                    sb.AppendLine(a);

                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine("Files in file system but not in csproj:");
                sb.AppendLine();
                foreach(var a in notInCsproj.OrderBy(x => x))
                    sb.AppendLine(a);


                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine("Duplicates in csproj:");
                var duplicates = from p in csprojD.AsEnumerable()
                                 where p.Value > 1
                                 orderby p.Key
                                 select p;

                foreach(var proj in duplicates)
                {
                    sb.AppendLine(proj.Key);
                }

                txtResult.Text = sb.ToString();
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

        private void ProcessDir(Dictionary<string, string> dict, string path, bool topLevel = false)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            foreach(var f in d.GetFiles())
                dict.Add(f.FullName, "");

            foreach(var subdir in d.GetDirectories())
            {
                if(!topLevel || !ExcludeList.Contains(subdir.Name.ToLower()))
                    ProcessDir(dict, subdir.FullName);
            }
        }
    }

    internal static class Extensions
    {
        public static void Add(this Dictionary<string, int> dict, IEnumerable<string> e)
        {
            foreach (var a in e)
            {
                if (dict.ContainsKey(a))
                    dict[a]++;
                else
                    dict[a] = 1;
            }
        }
    }
}
