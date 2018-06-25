using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;

namespace WindowsFormsApp14
{
    public partial class Form1 : Form
    {
        private bool _fileSaved;
        private DirectoryInfo _openedProject;
        private List<FastColoredTextBox> listColoredTextBoxes;
        Style GreenStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
        AutocompleteMenu autocomplete;
        private int _extFilesCount;

        public Form1()
        {
            InitializeComponent();

            listColoredTextBoxes = new List<FastColoredTextBox>();
            Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Projects");

            splitContainer1.Panel1.Controls.Add(treeView1);
            splitContainer1.Panel2.Controls.Add(tabControl1);

            splitContainer2.Panel1.Controls.Add(splitContainer1);
            splitContainer2.Panel2.Controls.Add(textBox1);

            treeView1.ImageList = new ImageList();
            treeView1.ImageList.Images.Add("folderOpen", Resource.folderOpen);
            treeView1.ImageList.Images.Add("folderClose", Resource.folderClose);
            treeView1.ImageList.ColorDepth = ColorDepth.Depth32Bit;

            toolStripComboBoxTheme.Items.Add("Beige");
            toolStripComboBoxTheme.Items.Add("Dark");
            toolStripComboBoxTheme.Items.Add("Light");
            toolStripComboBoxTheme.SelectedItem = "Light";
            Clipboard.Clear();

            autocomplete = null;
            _fileSaved = true;
            _extFilesCount = 0;
        }

        // formResize
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyIcon1.Visible = true;
            }
        }

        // notify
        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        // build project
        private void ToolStripButtonBuild_Click(object sender, EventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 20;
            Compiling();
        }

        // run
        private void ToolStripButtonRun_Click(object sender, EventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 20;
            if (Compiling())
            {
                Process.Start(($"{_openedProject.FullName}\\bin\\Debug\\{_openedProject.Name}.exe"));
            }
        }

        // компиляция
        private bool Compiling()
        {
            if (_openedProject == null)
            {
                MessageBox.Show("_openedProject == null");
                return false;
            }
            CompilerParameters cp = new CompilerParameters();
            cp.GenerateExecutable = true;

            DirectoryInfo d = Directory.CreateDirectory($"{_openedProject.FullName}\\bin\\Debug");
            cp.OutputAssembly = ($"{d.FullName}\\{_openedProject.Name}.exe");
            cp.GenerateInMemory = false;
            cp.WarningLevel = 3;
            cp.CompilerOptions = "/optimize";

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults crRes = null;

            FileInfo[] filesCs = _openedProject.GetFiles();
            string[] toCompile = new string[filesCs.Count()];
            for (int i = 0; i < listColoredTextBoxes.Count; ++i)
            {
                toCompile[i] = listColoredTextBoxes[i].Text;
            }

            if (toCompile.Count() > listColoredTextBoxes.Count)
            {
                for (int i = 0, j = 0; i < filesCs.Count(); ++i)
                {
                    bool isHere = false;
                    foreach (FastColoredTextBox colBox in listColoredTextBoxes)
                    {

                        if (filesCs[i].Name == colBox.Name)
                        {
                            isHere = true;
                        }
                    }
                    if (isHere)
                    {
                        continue;
                    }
                    StreamReader sReader = new StreamReader(filesCs[i].FullName);
                    toCompile[listColoredTextBoxes.Count + j] = sReader.ReadToEnd();
                    sReader.Close();
                    ++j;
                }
            }

            crRes = provider.CompileAssemblyFromSource(cp, toCompile);
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;

            var errors = crRes.Errors;
            if (errors.Count > 0)
            {
                splitContainer2.Panel2Collapsed = false;
                if (textBox1.Text.Length > 0)
                {
                    textBox1.Clear();
                }
                textBox1.Focus();
                clearToolStripMenuItem.Enabled = true;
                foreach (var item in errors)
                {
                    if (textBox1.Text.Length == 0)
                    {
                        textBox1.Text = item.ToString();
                    }
                    else
                    {
                        textBox1.Text = $"{textBox1.Text}\r\n{item.ToString()}";
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        // OpenProject
        private void ToolStripButtonOpen_Click(object sender, EventArgs e)
        {
            if (!_fileSaved)
            {
                DialogResult res;
                if (_openedProject != null)
                {
                    if (listColoredTextBoxes.Count > 0)
                    {
                        res = MessageBox.Show($"Save changes in {_openedProject.Name}?", "VS", MessageBoxButtons.YesNoCancel);
                        if (res == DialogResult.Yes)
                        {
                            foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
                            {
                                colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
                            }
                            _fileSaved = true;
                        }
                        else if (res == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                }
            }

            OpenFileDialog openDialog = new OpenFileDialog()
            {
                Filter = "Project FIles(*.sln)|*.sln"
            };
            openDialog.InitialDirectory = $"{Directory.GetCurrentDirectory()}\\Projects";
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(openDialog.FileName))
                {
                    return;
                }

                foreach (TabPage page in tabControl1.TabPages)
                {
                    tabControl1.TabPages.Remove(page);
                }
                windowToolStripMenuItem.DropDownItems.Clear();
                this.Text = $"{openDialog.FileName.Substring(openDialog.FileName.LastIndexOf('\\') + 1)} - VS";
                _fileSaved = true;

                treeView1.Nodes.Clear();
                DirectoryInfo dir = new FileInfo(openDialog.FileName).Directory;
                toolStripButtonBuild.Enabled = true;
                toolStripButtonRun.Enabled = true;
                toolStripMenuItemBuild.Enabled = true;
                toolStripMenuItemRun.Enabled = true;
                try
                {
                    var subdir = dir.GetDirectories();
                    if (subdir != null && subdir.Count() > 0)
                    {
                        foreach (var item in subdir)
                        {
                            if (item as DirectoryInfo != null)
                            {
                                if (item.FullName == openDialog.FileName.Substring(0, openDialog.FileName.LastIndexOf('.')))
                                {
                                    _openedProject = item;
                                }
                                TreeNode tNode = treeView1.Nodes.Add(item.Name);
                                tNode.ImageKey = "folderClose";
                                tNode.SelectedImageKey = "folderClose";
                                tNode.Tag = item;
                                tNode.ContextMenuStrip = contextMenuOpenExplorer;
                                if (item.GetDirectories() != null && item.GetDirectories().Count() > 0)
                                {
                                    tNode.Nodes.Add("");
                                }
                                else
                                {
                                    if (item.GetFiles() != null && item.GetFiles().Count() > 0)
                                    {
                                        tNode.Nodes.Add("");
                                    }
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }

        }

        // ProjectSave
        private void FileSave(FastColoredTextBox colTextBox)
        {
            try
            {
                colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AddFiles
        private void AddFiles(TreeNode treeNode, FileInfo[] files)
        {
            if (files == null || files.Count() == 0)
            {
                return;
            }
            foreach (FileInfo fInfo in files)
            {
                treeView1.ImageList.Images.Add(fInfo.Extension, Icon.ExtractAssociatedIcon(fInfo.FullName));
                TreeNode tNode = treeNode.Nodes.Add(fInfo.Name);
                tNode.Tag = fInfo;
                tNode.ImageKey = fInfo.Extension;
                tNode.SelectedImageKey = fInfo.Extension;
                if (fInfo.Extension == ".cs")
                {
                    tNode.ContextMenuStrip = contextMenuStripFile;
                }
            }
        }

        // AddDirectories
        private void AddDirectories(TreeNode treeNode, DirectoryInfo[] subDir)
        {
            if (subDir == null || subDir.Count() == 0)
            {
                return;
            }

            foreach (DirectoryInfo d in subDir)
            {
                TreeNode tNode = treeNode.Nodes.Add(d.Name);
                tNode.Tag = d;
                tNode.ImageKey = "folderClose";
                tNode.SelectedImageKey = "folderClose";
                try
                {
                    if (d.GetDirectories() != null && d.GetDirectories().Count() > 0)
                    {
                        tNode.Nodes.Add("");
                    }
                    else if (d.GetFiles() != null && d.GetFiles().Count() > 0)
                    {
                        tNode.Nodes.Add("");
                    }
                }
                catch (Exception)
                {
                    tNode.Nodes.Add("");
                }
            }
        }

        // treeView BeforeExpand
        private void TreeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null)
            {
                e.Node.Nodes.Clear();
                DirectoryInfo selectedElement = e.Node.Tag as DirectoryInfo;
                if (selectedElement != null)
                {
                    e.Node.Nodes.Clear();
                    try
                    {
                        DirectoryInfo[] subDirectories = selectedElement.GetDirectories();
                        if (subDirectories != null && subDirectories.Count() > 0)
                        {
                            AddDirectories(e.Node, subDirectories);
                        }
                        FileInfo[] files = selectedElement.GetFiles();
                        if (files != null && files.Count() > 0)
                        {
                            AddFiles(e.Node, files);
                        }
                        if (selectedElement.Parent != null)
                        {
                            e.Node.ImageKey = e.Node.SelectedImageKey = "folderOpen";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // treeView AfterCollapse
        private void TreeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageKey = e.Node.SelectedImageKey = "folderClose";
        }

        // treeView DoubleClick
        private void TreeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                DirectoryInfo selectedElement = treeView1.SelectedNode.Tag as DirectoryInfo;
                if (selectedElement != null)
                {
                    if (!treeView1.SelectedNode.IsExpanded)
                    {
                        treeView1.SelectedNode.Collapse();
                        return;
                    }
                    treeView1.SelectedNode.Nodes.Clear();

                    try
                    {
                        DirectoryInfo[] subDirectories = selectedElement.GetDirectories();
                        if (subDirectories != null && subDirectories.Count() > 0)
                        {
                            AddDirectories(treeView1.SelectedNode, subDirectories);
                        }
                        FileInfo[] files = selectedElement.GetFiles();
                        if (files != null && files.Count() > 0)
                        {
                            AddFiles(treeView1.SelectedNode, files);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    tabControl1.Visible = true;
                    FileInfo selectedFile = treeView1.SelectedNode.Tag as FileInfo;
                    if (selectedFile != null)
                    {
                        if (selectedFile.Extension == ".cs")
                        {
                            for (int i = 0; i < tabControl1.TabPages.Count; ++i)
                            {
                                if (tabControl1.TabPages[i].Name == selectedFile.Name)
                                {
                                    tabControl1.SelectTab(tabControl1.TabPages[i]);
                                    return;
                                }
                            }
                            TabPage page = new TabPage(selectedFile.Name) { BackColor = System.Drawing.Color.White };
                            page.Name = selectedFile.Name;
                            tabControl1.Controls.Add(page);
                            FastColoredTextBox fastColoredTextBox = new FastColoredTextBox() { Name = selectedFile.Name };
                            fastColoredTextBox.Size = new Size(tabControl1.Width, tabControl1.Height);
                            fastColoredTextBox.SelectionChanged += FastColoredTextBox_SelectionChanged;
                            fastColoredTextBox.AutoCompleteBrackets = true;
                            fastColoredTextBox.TextChanged += FastColoredTextBox_TextChanged;
                            fastColoredTextBox.ContextMenuStrip = contextMenuStripColoredBox;

                            fastColoredTextBox.Dock = DockStyle.Fill;
                            fastColoredTextBox.Language = Language.CSharp;
                            fastColoredTextBox.Tag = selectedFile;
                            switch (toolStripComboBoxTheme.SelectedItem)
                            {
                                case "Beige":
                                    {
                                        fastColoredTextBox.BackColor = Color.SeaShell;
                                        fastColoredTextBox.ForeColor = Color.SaddleBrown;
                                        break;
                                    }
                                case "Dark":
                                    {
                                        fastColoredTextBox.BackColor = Color.DimGray;
                                        fastColoredTextBox.ForeColor = Color.White;
                                        break;
                                    }
                                case "Light":
                                    {
                                        fastColoredTextBox.BackColor = Color.White;
                                        fastColoredTextBox.ForeColor = Color.Black;
                                        break;
                                    }
                            }
                            if (autocomplete == null)
                            {
                                SetAutocomplete(fastColoredTextBox);
                            }

                            foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
                            {
                                if (item.Checked)
                                {
                                    item.Checked = false;
                                }
                            }

                            ToolStripMenuItem stripItem = new ToolStripMenuItem()
                            {
                                Name = selectedFile.Name,
                                Text = selectedFile.Name,
                                CheckOnClick = true,
                                Checked = true,
                            };

                            windowToolStripMenuItem.DropDownItems.Add(stripItem);
                            page.Controls.Add(fastColoredTextBox);
                            tabControl1.SelectTab(page);
                            using (StreamReader sReader = new StreamReader(selectedFile.FullName))
                            {
                                fastColoredTextBox.Text = sReader.ReadToEnd();
                                sReader.Close();
                            }
                            listColoredTextBoxes.Add(fastColoredTextBox);

                            if (Clipboard.ContainsText())
                            {
                                toolStripButtonPaste.Enabled = true;
                                toolStripMenuItemPaste.Enabled = true;
                                pasteToolStripMenuItem.Enabled = true;
                            }
                            else
                            {
                                toolStripButtonPaste.Enabled = false;
                                toolStripMenuItemPaste.Enabled = false;
                                pasteToolStripMenuItem.Enabled = false;
                            }
                            if (!string.IsNullOrEmpty(fastColoredTextBox.Text))
                            {
                                toolStripMenuItemSelectAll.Enabled = true;
                                selectAllToolStripMenuItem.Enabled = true;
                            }
                        }
                    }
                }
            }
        }



        // новый проект
        private void ToolStripButtonNew_Click(object sender, EventArgs e)
        {
            if (!_fileSaved)
            {
                DialogResult res;
                if (_openedProject != null)
                {
                    if (listColoredTextBoxes.Count > 0)
                    {
                        res = MessageBox.Show($"Save changes in {_openedProject.Name}?", "VS", MessageBoxButtons.YesNoCancel);
                        if (res == DialogResult.Yes)
                        {
                            foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
                            {
                                colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
                            }
                            _fileSaved = true;
                        }
                        else if (res == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                }
            }

            FolderBrowserDialog browserDialog = new FolderBrowserDialog();
            string projectName = "ConsApp1";
            browserDialog.Description = "Select directory for the project";
            browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            DirectoryInfo rootDir = new DirectoryInfo($"{Directory.GetCurrentDirectory()}\\Projects");
            browserDialog.SelectedPath = rootDir.FullName;
            if (browserDialog.ShowDialog() == DialogResult.OK)
            {
                if (_openedProject != null)
                {
                    foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
                    {
                        colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
                    }
                    foreach (TabPage page in tabControl1.TabPages)
                    {
                        tabControl1.TabPages.Remove(page);
                    }
                    windowToolStripMenuItem.DropDownItems.Clear();
                }
                tabControl1.Visible = true;
                DirectoryInfo selectedDir = new DirectoryInfo(browserDialog.SelectedPath);
                try
                {
                    var subdirs = selectedDir.GetDirectories();
                    if (subdirs != null && subdirs.Count() > 0)
                    {
                        List<int> numbers = new List<int>();
                        foreach (var dir in subdirs)
                        {
                            if (Regex.Match(dir.Name, "^ConsApp[\\d]+$").Success)
                            {
                                if (int.TryParse(dir.Name.Replace("ConsApp", ""), out int num))
                                {
                                    numbers.Add(num);
                                }
                            }
                        }
                        if (numbers.Count > 0)
                        {
                            projectName = $"ConsApp{numbers.Max() + 1}";
                        }
                    }

                    DirectoryInfo projDir = Directory.CreateDirectory($"{browserDialog.SelectedPath}\\{projectName}");
                    treeView1.Nodes.Clear();
                    File.Create($"{projDir.FullName}\\{projectName}.sln").Close();
                    DirectoryInfo csDir = Directory.CreateDirectory($"{projDir.FullName}\\{projDir.Name}");
                    TreeNode tNode1 = treeView1.Nodes.Add(csDir.Name);
                    tNode1.SelectedImageKey = tNode1.ImageKey = "folderClose";
                    tNode1.Tag = csDir;
                    _openedProject = csDir;

                    File.Create($"{csDir.FullName}\\Program.cs").Close();

                    FileInfo programFile = new FileInfo($"{csDir.FullName}\\Program.cs");
                    TreeNode tNode2 = tNode1.Nodes.Add(programFile.Name);
                    treeView1.ImageList.Images.Add(programFile.Extension, Icon.ExtractAssociatedIcon(programFile.FullName));
                    tNode2.ImageKey = tNode2.SelectedImageKey = programFile.Extension;
                    tNode2.Tag = programFile;
                    treeView1.ExpandAll();
                    treeView1.Nodes[0].ContextMenuStrip = contextMenuOpenExplorer;

                    treeView1.SelectedNode = treeView1.Nodes[0].Nodes[0];

                    TabPage page = new TabPage("Program.cs") { BackColor = System.Drawing.Color.White };
                    page.Name = "Program.cs";
                    tabControl1.Controls.Add(page);
                    FastColoredTextBox fastColoredTextBox = new FastColoredTextBox();
                    fastColoredTextBox.Size = new Size(tabControl1.Width, tabControl1.Height);
                    fastColoredTextBox.Tag = programFile;
                    fastColoredTextBox.AutoCompleteBrackets = true;
                    ToolStripMenuItem stripItem = new ToolStripMenuItem()
                    {
                        Text = "Program.cs",
                        Name = "Program.cs",
                        CheckOnClick = true,
                        Checked = true,
                    };

                    windowToolStripMenuItem.DropDownItems.Add(stripItem);
                    switch (toolStripComboBoxTheme.SelectedItem)
                    {
                        case "Beige":
                            {
                                fastColoredTextBox.BackColor = Color.SeaShell;
                                fastColoredTextBox.ForeColor = Color.SaddleBrown;
                                fastColoredTextBox.IndentBackColor = Color.SeaShell;
                                page.BackColor = Color.SeaShell;
                                page.ForeColor = Color.SaddleBrown;

                                break;
                            }
                        case "Dark":
                            {
                                fastColoredTextBox.BackColor = Color.DimGray;
                                fastColoredTextBox.ForeColor = Color.White;
                                fastColoredTextBox.IndentBackColor = Color.DimGray;
                                page.BackColor = Color.DimGray;
                                page.ForeColor = Color.White;
                                break;
                            }
                        case "Light":
                            {
                                fastColoredTextBox.BackColor = Color.White;
                                fastColoredTextBox.ForeColor = Color.Black;
                                fastColoredTextBox.IndentBackColor = Color.White;
                                page.BackColor = Color.White;
                                page.ForeColor = Color.Black;
                                break;
                            }
                    }
                    fastColoredTextBox.Dock = DockStyle.Fill;
                    fastColoredTextBox.Language = Language.CSharp;
                    fastColoredTextBox.Text = Resource.StartProject;
                    fastColoredTextBox.SelectionChanged += FastColoredTextBox_SelectionChanged;
                    fastColoredTextBox.TextChanged += FastColoredTextBox_TextChanged;
                    fastColoredTextBox.ContextMenuStrip = contextMenuStripColoredBox;
                    if (autocomplete == null)
                    {
                        SetAutocomplete(fastColoredTextBox);
                    }
                    page.Controls.Add(fastColoredTextBox);
                    tabControl1.SelectTab(page);
                    listColoredTextBoxes.Add(fastColoredTextBox);
                    _fileSaved = false;

                    if (Clipboard.ContainsText())
                    {
                        toolStripButtonPaste.Enabled = true;
                        toolStripMenuItemPaste.Enabled = true;
                        pasteToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        toolStripButtonPaste.Enabled = false;
                        toolStripMenuItemPaste.Enabled = false;
                        pasteToolStripMenuItem.Enabled = false;
                    }
                    if (!string.IsNullOrEmpty(fastColoredTextBox.Text))
                    {
                        toolStripMenuItemSelectAll.Enabled = true;
                        selectAllToolStripMenuItem.Enabled = true;
                    }

                    toolStripButtonBuild.Enabled = true;
                    toolStripButtonRun.Enabled = true;
                    toolStripMenuItemBuild.Enabled = true;
                    toolStripMenuItemRun.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
                }
            }
        }

        private void FastColoredTextBox_SelectionChanged(object sender, EventArgs e)
        {
            FastColoredTextBox coloredTextBox = sender as FastColoredTextBox;
            if (coloredTextBox == null)
            {
                return;
            }
            if (coloredTextBox.SelectedText.Length > 0)
            {
                toolStripButtonCut.Enabled = true;
                toolStripButtonCopy.Enabled = true;
                toolStripMenuItemCut.Enabled = true;
                toolStripMenuItemCopy.Enabled = true;
                cutToolStripMenuItem.Enabled = true;
                copyToolStripMenuItem.Enabled = true;
                delToolStripMenuItem.Enabled = true;
                toolStripMenuItemDel.Enabled = true;
                toolStripButtonComment.Enabled = true;
                toolStripButtonUncomment.Enabled = true;
            }
            else
            {
                toolStripButtonCut.Enabled = false;
                toolStripButtonCopy.Enabled = false;
                toolStripMenuItemCut.Enabled = false;
                toolStripMenuItemCopy.Enabled = false;
                cutToolStripMenuItem.Enabled = false;
                copyToolStripMenuItem.Enabled = false;
                delToolStripMenuItem.Enabled = false;
                toolStripMenuItemDel.Enabled = false;
                toolStripButtonComment.Enabled = false;
                toolStripButtonUncomment.Enabled = false;
            }
            if (coloredTextBox.SelectedText.Length == coloredTextBox.Text.Length)
            {
                selectAllToolStripMenuItem.Enabled = false;
                toolStripMenuItemSelectAll.Enabled = false;
            }
            else
            {
                selectAllToolStripMenuItem.Enabled = true;
                toolStripMenuItemSelectAll.Enabled = true;
            }
        }

        private void SetAutocomplete(FastColoredTextBox fastColoredTextBox)
        {
            if (autocomplete == null)
            {
                this.autocomplete = new AutocompleteMenu(fastColoredTextBox);
                switch (toolStripComboBoxTheme.SelectedItem)
                {
                    case "Beige":
                        {
                            autocomplete.BackColor = Color.SeaShell;
                            autocomplete.ForeColor = Color.SaddleBrown;
                            break;
                        }
                    case "Dark":
                        {
                            autocomplete.BackColor = Color.DimGray;
                            autocomplete.ForeColor = Color.White;
                            break;
                        }
                    case "Light":
                        {
                            autocomplete.BackColor = Color.White;
                            autocomplete.ForeColor = Color.Black;
                            break;
                        }
                }
                autocomplete.SelectedColor = Color.Turquoise;
                autocomplete.SearchPattern = @"[\w\.]";
                autocomplete.AllowTabKey = true;
                autocomplete.Items.SetAutocompleteItems(new string[] { "heeello", "hiiii" });
            }
        }

        // text changed
        private void FastColoredTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Range range = (sender as FastColoredTextBox).VisibleRange;
            range.ClearStyle(GreenStyle);
            range.SetStyle(GreenStyle, @"//.*$", RegexOptions.Multiline);
            range.SetStyle(GreenStyle, @"(/\*.*?\*/)|(/\*.*)", RegexOptions.Singleline);
            range.SetStyle(GreenStyle, @"(/\*.*?\*/)|(.*\*/)",
                RegexOptions.Singleline | RegexOptions.RightToLeft);
            _fileSaved = false;
        }

        // open in fileExplorer
        private void OpenFolderInFileExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DirectoryInfo openedFolder = treeView1.SelectedNode.Tag as DirectoryInfo;
            if (openedFolder == null)
            {
                return;
            }
            try
            {
                Process.Start("explorer.exe", openedFolder.FullName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        // о программе
        private void ToolStripMenuItemAbout_Click(object sender, EventArgs e)
        {
            string info = "Visual Studio 2018\nVersion 1.0\n2017";
            MessageBox.Show(info, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // новый файл
        private void ToolStripMenuItemNewFile_Click(object sender, EventArgs e)
        {
            if (_openedProject == null)
            {
                return;
            }
            try
            {
                string fileName = "New file 1";
                FileInfo[] files = (new DirectoryInfo($"{_openedProject.FullName}").GetFiles());
                if (files != null && files.Count() > 0)
                {
                    List<int> numbers = new List<int>();
                    foreach (var dir in files)
                    {
                        if (Regex.Match(dir.Name, "^New file [\\d]+.cs").Success)
                        {
                            string tmp = dir.Name.Replace("New file ", "");
                            if (int.TryParse(tmp.Replace(".cs", ""), out int num))
                            {
                                numbers.Add(num);
                            }
                        }
                    }
                    if (numbers.Count > 0)
                    {
                        fileName = $"New file {numbers.Max() + 1}";
                    }
                }
                FileStream f = File.Create($"{_openedProject.FullName}\\{fileName}.cs");
                f.Close();
                FileInfo newFile = new FileInfo($"{_openedProject.FullName}\\{fileName}.cs");
                TreeNode toAdd = null;
                foreach (TreeNode node in treeView1.Nodes)
                {
                    if (node.Tag as DirectoryInfo != null)
                    {
                        if (node.Tag as DirectoryInfo == _openedProject)
                        {
                            toAdd = node;

                        }
                    }
                }
                if (toAdd != null)
                {
                    TreeNode newNode = toAdd.Nodes.Add(newFile.Name);
                    treeView1.ImageList.Images.Add(newFile.Extension, Icon.ExtractAssociatedIcon(newFile.FullName));
                    newNode.Tag = newFile;
                    newNode.ImageKey = newNode.SelectedImageKey = newFile.Extension;
                    newNode.ContextMenuStrip = contextMenuStripFile;
                    toAdd.Nodes.Clear();
                    DirectoryInfo[] subDirs = (toAdd.Tag as DirectoryInfo).GetDirectories();
                    if (subDirs != null && subDirs.Count() > 0)
                    {
                        AddDirectories(toAdd, subDirs);
                    }
                    FileInfo[] fs = (toAdd.Tag as DirectoryInfo).GetFiles();
                    AddFiles(toAdd, fs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // удаление файла
        private void ToolStripMenuItemDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete?", "Deleting", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            FileInfo selectedFile = treeView1.SelectedNode.Tag as FileInfo;
            if (selectedFile == null)
            {
                MessageBox.Show("Select a file", "VS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                selectedFile.Delete();
                TreeNode toDelete = null;
                foreach (TreeNode node in treeView1.Nodes)
                {
                    if (node.Tag as DirectoryInfo != null)
                    {
                        if (node.Tag as DirectoryInfo == _openedProject)
                        {
                            toDelete = node;

                        }
                    }
                }
                if (toDelete != null)
                {
                    toDelete.Nodes.Clear();
                    DirectoryInfo[] subDirs = (toDelete.Tag as DirectoryInfo).GetDirectories();
                    if (subDirs != null && subDirs.Count() > 0)
                    {
                        AddDirectories(toDelete, subDirs);
                    }
                    FileInfo[] fs = (toDelete.Tag as DirectoryInfo).GetFiles();
                    AddFiles(toDelete, fs);
                }

                foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
                {
                    if (colTextBox.Name == selectedFile.Name)
                    {
                        listColoredTextBoxes.Remove(colTextBox);
                        break;
                    }
                }
                foreach (TabPage page in tabControl1.TabPages)
                {
                    if (page.Name == selectedFile.Name)
                    {
                        tabControl1.Controls.Remove(page);
                        break;
                    }
                }
                foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
                {
                    if (item.Name == selectedFile.Name)
                    {
                        windowToolStripMenuItem.DropDownItems.Remove(item);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }










        // File -> Exit
        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            if (!_fileSaved)
            {
                DialogResult res;
                if (_openedProject != null)
                {
                    res = MessageBox.Show($"Save changes in {_openedProject.Name}?", "VS", MessageBoxButtons.YesNoCancel);
                    if (res == DialogResult.Yes)
                    {
                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            FileSave(page.Controls[0] as FastColoredTextBox);
                        }
                        _fileSaved = true;
                        this.Close();
                    }
                    else if (res == DialogResult.No)
                    {
                        _fileSaved = true;
                        this.Close();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    _fileSaved = true;
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        // при закрытии формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_fileSaved)
            {
                DialogResult res;
                if (_openedProject != null)
                {
                    res = MessageBox.Show($"Save changes in {_openedProject.Name}?", "VS", MessageBoxButtons.YesNoCancel);
                    if (res == DialogResult.Yes)
                    {
                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            FileSave(page.Controls[0] as FastColoredTextBox);
                        }
                        e.Cancel = false;
                    }
                    else if (res == DialogResult.No)
                    {
                        e.Cancel = false;
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    _fileSaved = true;
                    this.Close();
                }
            }
            else
            {
                e.Cancel = false;
            }
        }

        // cut
        private void ToolStripButtonCut_Click(object sender, EventArgs e)
        {
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).Cut();
            if (!toolStripButtonPaste.Enabled)
            {
                toolStripButtonPaste.Enabled = true;
                toolStripMenuItemPaste.Enabled = true;
                pasteToolStripMenuItem.Enabled = true;
            }
            _fileSaved = false;
        }

        // copy
        private void ToolStripButtonCopy_Click(object sender, EventArgs e)
        {
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).Copy();
            tabControl1.SelectedTab.Controls[0].Focus();
            if (!toolStripButtonPaste.Enabled)
            {
                toolStripButtonPaste.Enabled = true;
                toolStripMenuItemPaste.Enabled = true;
                pasteToolStripMenuItem.Enabled = true;
            }
        }

        // paste
        private void ToolStripButtonPaste_Click(object sender, EventArgs e)
        {
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).Paste();
            _fileSaved = false;
        }

        // delete
        private void ToolStripMenuItemDel_Click(object sender, EventArgs e)
        {
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).SelectedText = "";
            _fileSaved = false;
        }

        // select all
        private void ToolStripMenuItemSelectAll_Click(object sender, EventArgs e)
        {
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).SelectAll();
            (tabControl1.SelectedTab.Controls[0] as FastColoredTextBox).Focus();

            selectAllToolStripMenuItem.Enabled = false;
            toolStripMenuItemSelectAll.Enabled = false;
            toolStripButtonCut.Enabled = true;
            toolStripButtonCopy.Enabled = true;
            toolStripMenuItemCut.Enabled = true;
            toolStripMenuItemCopy.Enabled = true;
            cutToolStripMenuItem.Enabled = true;
            copyToolStripMenuItem.Enabled = true;
            delToolStripMenuItem.Enabled = true;
            toolStripMenuItemDel.Enabled = true;
            toolStripButtonComment.Enabled = true;
            toolStripButtonUncomment.Enabled = true;
        }

        // закрытие вкладки
        private void ToolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            FastColoredTextBox colBoxSelected = tabControl1.SelectedTab.Controls[0] as FastColoredTextBox;
            if (colBoxSelected == null)
            {
                return;
            }
            FileSave(colBoxSelected);
            foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
            {
                if (item.Tag as FileInfo == colBoxSelected.Tag as FileInfo)
                {
                    windowToolStripMenuItem.DropDownItems.Remove(item);
                    break;
                }
            }
            if (tabControl1.SelectedTab.Tag == null)
            {
                foreach (FastColoredTextBox colBox in listColoredTextBoxes)
                {
                    if (colBoxSelected == colBox)
                    {
                        listColoredTextBoxes.Remove(colBox);
                        break;
                    }
                }
            }
            else
            {
                --_extFilesCount;
            }
        }

        // сохранение вкладки
        private void ToolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabCount == 0)
            {
                return;
            }
            FileSave(tabControl1.SelectedTab.Controls[0] as FastColoredTextBox);
        }

        // конт.меню вкладки
        private void ContextMenuStripClose_Opening(object sender, CancelEventArgs e)
        {
            for (int i = 0; i < tabControl1.Controls.Count; ++i)
            {
                if (tabControl1.GetTabRect(i).Contains(tabControl1.PointToClient(Cursor.Position)))
                {
                    tabControl1.SelectTab(i);
                    e.Cancel = false;
                    return;
                }
            }
            e.Cancel = true;
        }

        private List<ToolStripItem> GetAllChildren(ToolStripItem item)
        {
            List<ToolStripItem> Items = new List<ToolStripItem> { item };
            if (item is ToolStripMenuItem)
                foreach (ToolStripItem i in ((ToolStripMenuItem)item).DropDownItems)
                    Items.AddRange(GetAllChildren(i));
            else if (item is ToolStripSplitButton)
                foreach (ToolStripItem i in ((ToolStripSplitButton)item).DropDownItems)
                    Items.AddRange(GetAllChildren(i));
            else if (item is ToolStripDropDownButton)
                foreach (ToolStripItem i in ((ToolStripDropDownButton)item).DropDownItems)
                    Items.AddRange(GetAllChildren(i));
            return Items;
        }

        // смена Theme
        private void ToolStripComboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ToolStripItem> itemsMenu = new List<ToolStripItem>();
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                List<ToolStripItem> tmp = GetAllChildren(item);
                itemsMenu.AddRange(tmp);
            }

            switch (toolStripComboBoxTheme.SelectedItem)
            {
                case "Beige":
                    {
                        this.BackColor = Color.SeaShell;
                        this.ForeColor = Color.SaddleBrown;
                        toolStrip1.BackColor = Color.SeaShell;
                        toolStrip1.ForeColor = Color.SaddleBrown;
                        menuStrip1.BackColor = Color.SeaShell;
                        menuStrip1.ForeColor = Color.SaddleBrown;
                        foreach (ToolStripItem it in itemsMenu)
                        {

                            it.BackColor = Color.SeaShell;
                            it.ForeColor = Color.SaddleBrown;
                        }
                        treeView1.BackColor = Color.SeaShell;
                        treeView1.ForeColor = Color.SaddleBrown;
                        treeView1.LineColor = Color.SaddleBrown;
                        statusStrip1.BackColor = Color.SeaShell;
                        statusStrip1.ForeColor = Color.SaddleBrown;
                        contextMenuStripColoredBox.BackColor = Color.SeaShell;
                        contextMenuStripColoredBox.ForeColor = Color.SaddleBrown;
                        contextMenuOpenExplorer.BackColor = Color.SeaShell;
                        contextMenuOpenExplorer.ForeColor = Color.SaddleBrown;
                        contextMenuStripFile.BackColor = Color.SeaShell;
                        contextMenuStripFile.ForeColor = Color.SaddleBrown;
                        contextMenuStripClose.BackColor = Color.SeaShell;
                        contextMenuStripClose.ForeColor = Color.SaddleBrown;
                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            page.ForeColor = Color.SaddleBrown;
                            page.BackColor = Color.SeaShell;
                        }
                        textBox1.BackColor = Color.SeaShell;
                        textBox1.ForeColor = Color.SaddleBrown;
                        showToolStripMenuItem.BackColor = Color.SeaShell;
                        showToolStripMenuItem.ForeColor = Color.SaddleBrown;
                        clearToolStripMenuItem.BackColor = Color.SeaShell;
                        clearToolStripMenuItem.ForeColor = Color.SaddleBrown;
                        break;
                    }
                case "Dark":
                    {
                        this.BackColor = Color.DimGray;
                        this.ForeColor = Color.White;
                        toolStrip1.BackColor = Color.DimGray;
                        toolStrip1.ForeColor = Color.White;
                        menuStrip1.BackColor = Color.DimGray;
                        foreach (ToolStripItem it in itemsMenu)
                        {
                            it.BackColor = Color.DimGray;
                            it.ForeColor = Color.White;
                        }
                        menuStrip1.ForeColor = Color.White;
                        treeView1.BackColor = Color.DimGray;
                        treeView1.ForeColor = Color.White;
                        treeView1.LineColor = Color.White;
                        statusStrip1.BackColor = Color.DimGray;
                        statusStrip1.ForeColor = Color.White;
                        contextMenuStripColoredBox.BackColor = Color.DimGray;
                        contextMenuStripColoredBox.ForeColor = Color.White;
                        contextMenuOpenExplorer.BackColor = Color.DimGray;
                        contextMenuOpenExplorer.ForeColor = Color.White;
                        contextMenuStripFile.BackColor = Color.DimGray;
                        contextMenuStripFile.ForeColor = Color.White;
                        contextMenuStripClose.BackColor = Color.DimGray;
                        contextMenuStripClose.ForeColor = Color.White;
                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            page.ForeColor = Color.White;
                            page.BackColor = Color.DimGray;
                        }
                        textBox1.BackColor = Color.DimGray;
                        textBox1.ForeColor = Color.White;
                        showToolStripMenuItem.BackColor = Color.DimGray;
                        showToolStripMenuItem.ForeColor = Color.White;
                        clearToolStripMenuItem.BackColor = Color.DimGray;
                        clearToolStripMenuItem.ForeColor = Color.White;
                        break;
                    }
                case "Light":
                    {
                        this.BackColor = Color.White;
                        this.ForeColor = Color.Black;
                        toolStrip1.BackColor = Color.White;
                        toolStrip1.ForeColor = Color.Black;
                        menuStrip1.BackColor = Color.White;
                        menuStrip1.ForeColor = Color.Black;
                        foreach (ToolStripItem it in itemsMenu)
                        {
                            it.BackColor = Color.White;
                            it.ForeColor = Color.Black;
                        }
                        treeView1.BackColor = Color.White;
                        treeView1.ForeColor = Color.Black;
                        treeView1.LineColor = Color.Black;
                        statusStrip1.BackColor = Color.White;
                        statusStrip1.ForeColor = Color.Black;
                        contextMenuStripColoredBox.BackColor = Color.White;
                        contextMenuStripColoredBox.ForeColor = Color.Black;
                        contextMenuOpenExplorer.BackColor = Color.White;
                        contextMenuOpenExplorer.ForeColor = Color.Black;
                        contextMenuStripFile.BackColor = Color.White;
                        contextMenuStripFile.ForeColor = Color.Black;
                        contextMenuStripClose.BackColor = Color.White;
                        contextMenuStripClose.ForeColor = Color.Black;
                        foreach (TabPage page in tabControl1.TabPages)
                        {
                            page.ForeColor = Color.Black;
                            page.BackColor = Color.White;
                        }
                        textBox1.BackColor = Color.White;
                        textBox1.ForeColor = Color.Black;
                        showToolStripMenuItem.BackColor = Color.White;
                        showToolStripMenuItem.ForeColor = Color.Black;
                        clearToolStripMenuItem.BackColor = Color.White;
                        clearToolStripMenuItem.ForeColor = Color.Black;
                        break;
                    }
            }
            foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
            {
                switch (toolStripComboBoxTheme.SelectedItem)
                {
                    case "Beige":
                        {
                            colTextBox.BackColor = Color.SeaShell;
                            colTextBox.ForeColor = Color.SaddleBrown;
                            colTextBox.IndentBackColor = Color.SeaShell;
                            break;
                        }
                    case "Dark":
                        {
                            colTextBox.BackColor = Color.DimGray;
                            colTextBox.ForeColor = Color.White;
                            colTextBox.IndentBackColor = Color.DimGray;
                            colTextBox.LineNumberColor = Color.Blue;
                            break;
                        }
                    case "Light":
                        {
                            colTextBox.BackColor = Color.White;
                            colTextBox.ForeColor = Color.Black;
                            colTextBox.IndentBackColor = Color.White;
                            break;
                        }
                }
            }
        }

        private void tabControl1_Enter(object sender, EventArgs e)
        {
            splitContainer2.Panel2Collapsed = true;
        }

        private void treeView1_Enter(object sender, EventArgs e)
        {
            splitContainer2.Panel2Collapsed = true;
        }

        // control removed (stop timer)
        private void tabControl1_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (tabControl1.TabPages.Count == 1)
            {
                tabControl1.Visible = false;
                if (toolStripMenuItemAutoSave.Checked)
                {
                    timerAutoSave.Stop();
                }
                toolStripMenuItemSelectAll.Enabled = false;
                selectAllToolStripMenuItem.Enabled = false;
                _fileSaved = true;
            }
        }

        // autosave timer
        private void timerAutoSave_Tick(object sender, EventArgs e)
        {
            foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
            {
                colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
            }
            _fileSaved = true;
        }

        // control added (start timer)
        private void tabControl1_ControlAdded(object sender, ControlEventArgs e)
        {
            if (tabControl1.TabPages.Count == 1)
            {
                if (toolStripMenuItemAutoSave.Checked)
                {
                    timerAutoSave.Start();
                }
            }

        }

        // auto save click
        private void toolStripMenuItemAutoSave_Click(object sender, EventArgs e)
        {
            if (toolStripMenuItemAutoSave.Checked)
            {
                timerAutoSave.Start();
            }
            else
            {
                timerAutoSave.Stop();
            }
        }

        // save click
        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            foreach (FastColoredTextBox colTextBox in listColoredTextBoxes)
            {
                colTextBox.SaveToFile((colTextBox.Tag as FileInfo).FullName, Encoding.Default);
            }
            _fileSaved = true;
        }

        // tab index changed
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count > 0)
            {
                foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
                {
                    if (item.Name == tabControl1.SelectedTab.Name)
                    {
                        item.Checked = true;
                    }
                    else
                    {
                        item.Checked = false;
                    }
                }
            }
        }

        // dropDownClosed
        private void windowToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            List<ToolStripMenuItem> selectedItems = new List<ToolStripMenuItem>();
            foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
            {
                if (item.Checked)
                {
                    selectedItems.Add(item);
                }
            }
            if (selectedItems.Count == 0)
            {
                foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
                {
                    if (item.Name == tabControl1.SelectedTab.Name)
                    {
                        item.Checked = true;
                    }
                }
            }
            else if (selectedItems.Count == 2)
            {
                if (selectedItems.First().Name == tabControl1.SelectedTab.Name)
                {
                    selectedItems.First().Checked = false;
                    foreach (TabPage page in tabControl1.TabPages)
                    {
                        if (page.Name == selectedItems.Last().Name)
                        {
                            tabControl1.SelectedTab = page;
                        }
                    }
                }
            }
        }
        
        // переименование файла
        private void ToolStripMenuItemRename_Click(object sender, EventArgs e)
        {
            FileInfo selectedFile = treeView1.SelectedNode.Tag as FileInfo;
            if (selectedFile == null)
            {
                return;
            }
            RenameForm rForm = new RenameForm(selectedFile.Name);
            switch (toolStripComboBoxTheme.SelectedItem)
            {
                case "Beige":
                    {
                        rForm.BackColor = Color.SeaShell;
                        rForm.ForeColor = Color.SaddleBrown;
                        break;
                    }
                case "Dark":
                    {
                        rForm.BackColor = Color.DimGray;
                        rForm.ForeColor = Color.White;
                        break;
                    }
                case "Light":
                    {
                        rForm.BackColor = Color.White;
                        rForm.ForeColor = Color.Black;
                        break;
                    }
            }

            if (rForm.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.Move(selectedFile.FullName, $"{selectedFile.DirectoryName}\\{rForm.FileName}");
                    DirectoryInfo[] subDirs = (treeView1.SelectedNode.Parent.Tag as DirectoryInfo).GetDirectories();
                    TreeNode parentNode = treeView1.SelectedNode.Parent;
                    parentNode.Nodes.Clear();
                    if (subDirs != null && subDirs.Count() > 0)
                    {
                        AddDirectories(parentNode, subDirs);
                    }
                    FileInfo[] files = (parentNode.Tag as DirectoryInfo).GetFiles();
                    if (files != null && files.Count() > 0)
                    {
                        AddFiles(parentNode, files);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // comment
        private void toolStripButtonComment_Click(object sender, EventArgs e)
        {
            FastColoredTextBox colTextBox = tabControl1.SelectedTab.Controls[0] as FastColoredTextBox;
            if (colTextBox == null)
            {
                return;
            }
            int start = colTextBox.SelectionStart;
            int finish = start + colTextBox.SelectionLength;
            colTextBox.Text = colTextBox.Text.Insert(start, "/*");
            colTextBox.Text = colTextBox.Text.Insert(finish + 2, "*/");
            colTextBox.SelectionStart = 0;
            colTextBox.SelectionLength = 0;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

        }
        
        // show errors
        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (splitContainer2.Panel2Collapsed)
            {
                splitContainer2.Panel2Collapsed = false;
                textBox1.Focus();
            }
            else
            {
                splitContainer2.Panel2Collapsed = true;
            }
        }

        // clear errors
        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            clearToolStripMenuItem.Enabled = false;
        }

        // uncomment
        private void toolStripButtonUncomment_Click(object sender, EventArgs e)
        {
            FastColoredTextBox colTextBox = tabControl1.SelectedTab.Controls[0] as FastColoredTextBox;
            if (colTextBox == null)
            {
                return;
            }
            int start = colTextBox.SelectionStart;
            int finish = start + colTextBox.SelectionLength - 2;
            if (colTextBox.Text.Substring(start, 2) == "/*" &&
                colTextBox.Text.Substring(finish, 2) == "*/")
            {
                colTextBox.Text = colTextBox.Text.Remove(start, 2);
                colTextBox.Text = colTextBox.Text.Remove(finish - 2, 2);
            }
            colTextBox.SelectionStart = 0;
            colTextBox.SelectionLength = 0;
        }

        private void toolStripMenuItemOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog()
            {
                Filter = "C# Files(*.cs)|*.cs"
            };
            openDialog.InitialDirectory = $"{Directory.GetCurrentDirectory()}\\Projects";
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(openDialog.FileName))
                {
                    return;
                }

                FileInfo openedFile = new FileInfo(openDialog.FileName);
                try
                {
                    
                    tabControl1.Visible = true;
                    if (openedFile != null)
                    {
                        {
                            for (int i = 0; i < tabControl1.TabPages.Count; ++i)
                            {
                                if ((tabControl1.TabPages[i].Tag as FileInfo) == openedFile)
                                {
                                    tabControl1.SelectTab(tabControl1.TabPages[i]);
                                    return;
                                }
                            }
                            ++_extFilesCount;
                            TabPage page = new TabPage($"{openedFile.Name}(ext{_extFilesCount})") { BackColor = System.Drawing.Color.White };
                            page.Name = $"{openedFile.Name}(ext{_extFilesCount})";
                            page.Tag = openedFile;
                            tabControl1.Controls.Add(page);
                            FastColoredTextBox fastColoredTextBox = new FastColoredTextBox() { Name = openedFile.Name };
                            fastColoredTextBox.Size = new Size(tabControl1.Width, tabControl1.Height);
                            fastColoredTextBox.SelectionChanged += FastColoredTextBox_SelectionChanged;

                            fastColoredTextBox.TextChanged += FastColoredTextBox_TextChanged;
                            fastColoredTextBox.ContextMenuStrip = contextMenuStripColoredBox;

                            fastColoredTextBox.Dock = DockStyle.Fill;
                            fastColoredTextBox.Language = Language.CSharp;
                            fastColoredTextBox.Tag = openedFile;
                            fastColoredTextBox.AutoCompleteBrackets = true;
                            switch (toolStripComboBoxTheme.SelectedItem)
                            {
                                case "Beige":
                                    {
                                        fastColoredTextBox.BackColor = Color.SeaShell;
                                        fastColoredTextBox.ForeColor = Color.SaddleBrown;
                                        break;
                                    }
                                case "Dark":
                                    {
                                        fastColoredTextBox.BackColor = Color.DimGray;
                                        fastColoredTextBox.ForeColor = Color.White;
                                        break;
                                    }
                                case "Light":
                                    {
                                        fastColoredTextBox.BackColor = Color.White;
                                        fastColoredTextBox.ForeColor = Color.Black;
                                        break;
                                    }
                            }
                            if (autocomplete == null)
                            {
                                SetAutocomplete(fastColoredTextBox);
                            }

                            foreach (ToolStripMenuItem item in windowToolStripMenuItem.DropDownItems)
                            {
                                if (item.Checked)
                                {
                                    item.Checked = false;
                                }
                            }

                            ToolStripMenuItem stripItem = new ToolStripMenuItem()
                            {
                                Name = $"{openedFile.Name}(ext{_extFilesCount})",
                                Text = $"{openedFile.Name}(ext{_extFilesCount})",
                                CheckOnClick = true,
                                Checked = true,
                                Tag = openedFile
                            };

                            windowToolStripMenuItem.DropDownItems.Add(stripItem);
                            page.Controls.Add(fastColoredTextBox);
                            tabControl1.SelectTab(page);
                            using (StreamReader sReader = new StreamReader(openedFile.FullName))
                            {
                                fastColoredTextBox.Text = sReader.ReadToEnd();
                                sReader.Close();
                            }

                            if (Clipboard.ContainsText())
                            {
                                toolStripButtonPaste.Enabled = true;
                                toolStripMenuItemPaste.Enabled = true;
                                pasteToolStripMenuItem.Enabled = true;
                            }
                            else
                            {
                                toolStripButtonPaste.Enabled = false;
                                toolStripMenuItemPaste.Enabled = false;
                                pasteToolStripMenuItem.Enabled = false;
                            }
                            if (!string.IsNullOrEmpty(fastColoredTextBox.Text))
                            {
                                toolStripMenuItemSelectAll.Enabled = true;
                                selectAllToolStripMenuItem.Enabled = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }
    }
}
