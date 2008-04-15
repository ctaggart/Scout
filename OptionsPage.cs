using System.Windows.Forms;
using System.Drawing;

using JetBrains.CommonControls.Validation;
using JetBrains.UI.Options;

namespace ReSharper.Scout
{
	using Properties;
	using Validation;

	[
		OptionsPage(OptionsPage.PageID, AssemblyInfo.Product,
			"ReSharper.Scout.OptionsPage.png",
			ParentId = "SearchAndNavigation",
			Sequence = 1)
	]
	public class OptionsPage : UserControl, IOptionsPage
	{
		private const string PageID = "Scout.GotoDeclarationOptionsPage";

		public OptionsPage()
		{
			InitializeComponent();

			SetFontStyle(_usePdbFilesCheckBox,      FontStyle.Bold);
			SetFontStyle(_useReflectorCheckBox,     FontStyle.Bold);
			SetFontStyle(_debuggerOptionsHintLabel, FontStyle.Italic);

			_usePdbFilesCheckBox.Checked            = Options.UsePdbFiles;
			_useDebuggerSettingsRadioButton.Checked = Options.UseDebuggerSettings;
			_useCustomSettingsRadioButton.Checked   = !_useDebuggerSettingsRadioButton.Checked;
			_symbolServersTextBox.Text              = Options.SymbolPath;
			_symbolCacheFolderTextBox.Text          = Options.SymbolCacheDir;

			_useReflectorCheckBox.Checked           = Options.UseReflector;

			if (_useReflectorCheckBox.Checked)
				_reflectorPathTextBox.Text = Options.ReflectorPath;
		}

		private static void SetFontStyle(Control ctl, FontStyle style)
		{
			ctl.Font = new Font(ctl.Font, style);
		}

		#region IOptionsPage Members

		public void OnActivated(bool activated)
		{
			_formValidator = FormValidator.GetInstance(this);
		}

		public bool OnOk()
		{
			Options.UsePdbFiles         = _usePdbFilesCheckBox.Checked;
			Options.UseDebuggerSettings = _useDebuggerSettingsRadioButton.Checked;
			Options.SymbolPath          = _symbolServersTextBox.Text;
			Options.SymbolCacheDir      = _symbolCacheFolderTextBox.Text;

			Options.UseReflector  = _useReflectorCheckBox.Checked;
			Options.ReflectorPath = _reflectorPathTextBox.Text;

			return true;
		}

		public bool ValidatePage()
		{
			return (!_useReflectorCheckBox.Checked || _reflectorPathTextBox.TextLength > 0) &&
				(!_useCustomSettingsRadioButton.Checked || _symbolCacheFolderTextBox.TextLength > 0);
		}

		public Control Control
		{
			get { return this; }
		}

		public string Id
		{
			get { return PageID; }
		}

		#endregion

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			System.Windows.Forms.Label symbolServersLabel;
			System.Windows.Forms.Label symbolCacheFolder;
			System.Windows.Forms.Label reflectorExecutableLabel;
			this._reflectorHomepageLinkLabel = new System.Windows.Forms.LinkLabel();
			this._symbolCacheBrowseButton = new System.Windows.Forms.Button();
			this._reflectorPathBrowseButton = new System.Windows.Forms.Button();
			this._usePdbFilesCheckBox = new System.Windows.Forms.CheckBox();
			this._useDebuggerSettingsRadioButton = new System.Windows.Forms.RadioButton();
			this._debuggerOptionsHintLabel = new System.Windows.Forms.Label();
			this._useCustomSettingsRadioButton = new System.Windows.Forms.RadioButton();
			this._symbolServersTextBox = new System.Windows.Forms.TextBox();
			this._symbolCacheFolderTextBox = new System.Windows.Forms.TextBox();
			this._useReflectorCheckBox = new System.Windows.Forms.CheckBox();
			this._reflectorPathTextBox = new System.Windows.Forms.TextBox();
			symbolServersLabel = new System.Windows.Forms.Label();
			symbolCacheFolder = new System.Windows.Forms.Label();
			reflectorExecutableLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// symbolServersLabel
			// 
			symbolServersLabel.AutoSize = true;
			symbolServersLabel.Location = new System.Drawing.Point(57, 115);
			symbolServersLabel.Name = "symbolServersLabel";
			symbolServersLabel.Size = new System.Drawing.Size(81, 13);
			symbolServersLabel.TabIndex = 4;
			symbolServersLabel.Text = "Symbol &servers:";
			// 
			// symbolCacheFolder
			// 
			symbolCacheFolder.AutoSize = true;
			symbolCacheFolder.Location = new System.Drawing.Point(57, 165);
			symbolCacheFolder.Name = "symbolCacheFolder";
			symbolCacheFolder.Size = new System.Drawing.Size(106, 13);
			symbolCacheFolder.TabIndex = 6;
			symbolCacheFolder.Text = "Symbol cache &folder:";
			// 
			// reflectorExecutableLabel
			// 
			reflectorExecutableLabel.AutoSize = true;
			reflectorExecutableLabel.Location = new System.Drawing.Point(34, 253);
			reflectorExecutableLabel.Name = "reflectorExecutableLabel";
			reflectorExecutableLabel.Size = new System.Drawing.Size(133, 13);
			reflectorExecutableLabel.TabIndex = 11;
			reflectorExecutableLabel.Text = "Path to the &executable file:";
			// 
			// _reflectorHomepageLinkLabel
			// 
			this._reflectorHomepageLinkLabel.AutoSize = true;
			this._reflectorHomepageLinkLabel.Location = new System.Drawing.Point(37, 296);
			this._reflectorHomepageLinkLabel.Name = "_reflectorHomepageLinkLabel";
			this._reflectorHomepageLinkLabel.Size = new System.Drawing.Size(130, 13);
			this._reflectorHomepageLinkLabel.TabIndex = 14;
			this._reflectorHomepageLinkLabel.TabStop = true;
			this._reflectorHomepageLinkLabel.Text = "Open reflector home page";
			this._reflectorHomepageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.HandleOpenReflectorSite);
			// 
			// _symbolCacheBrowseButton
			// 
			this._symbolCacheBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._symbolCacheBrowseButton.Enabled = false;
			this._symbolCacheBrowseButton.Location = new System.Drawing.Point(343, 179);
			this._symbolCacheBrowseButton.Name = "_symbolCacheBrowseButton";
			this._symbolCacheBrowseButton.Size = new System.Drawing.Size(75, 23);
			this._symbolCacheBrowseButton.TabIndex = 9;
			this._symbolCacheBrowseButton.Text = "&Browse…";
			this._symbolCacheBrowseButton.UseVisualStyleBackColor = true;
			this._symbolCacheBrowseButton.Click += new System.EventHandler(this.HandleCacheBrowseClick);
			// 
			// _reflectorPathBrowseButton
			// 
			this._reflectorPathBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._reflectorPathBrowseButton.Enabled = false;
			this._reflectorPathBrowseButton.Location = new System.Drawing.Point(343, 267);
			this._reflectorPathBrowseButton.Name = "_reflectorPathBrowseButton";
			this._reflectorPathBrowseButton.Size = new System.Drawing.Size(75, 23);
			this._reflectorPathBrowseButton.TabIndex = 13;
			this._reflectorPathBrowseButton.Text = "Bro&wse…";
			this._reflectorPathBrowseButton.UseVisualStyleBackColor = true;
			this._reflectorPathBrowseButton.Click += new System.EventHandler(this.HandleReflectorBrowseClick);
			// 
			// _usePdbFilesCheckBox
			// 
			this._usePdbFilesCheckBox.AutoSize = true;
			this._usePdbFilesCheckBox.Location = new System.Drawing.Point(17, 14);
			this._usePdbFilesCheckBox.Name = "_usePdbFilesCheckBox";
			this._usePdbFilesCheckBox.Size = new System.Drawing.Size(339, 17);
			this._usePdbFilesCheckBox.TabIndex = 0;
			this._usePdbFilesCheckBox.Text = "Recover source locations from program debug database (&pdb) files";
			this._usePdbFilesCheckBox.UseVisualStyleBackColor = true;
			this._usePdbFilesCheckBox.CheckedChanged += new System.EventHandler(this.HandlePdbGroupCheckedChanged);
			this._usePdbFilesCheckBox.EnabledChanged += new System.EventHandler(this.HandlePdbGroupCheckedChanged);
			// 
			// _useDebuggerSettingsRadioButton
			// 
			this._useDebuggerSettingsRadioButton.AutoSize = true;
			this._useDebuggerSettingsRadioButton.Enabled = false;
			this._useDebuggerSettingsRadioButton.Location = new System.Drawing.Point(37, 37);
			this._useDebuggerSettingsRadioButton.Name = "_useDebuggerSettingsRadioButton";
			this._useDebuggerSettingsRadioButton.Size = new System.Drawing.Size(131, 17);
			this._useDebuggerSettingsRadioButton.TabIndex = 1;
			this._useDebuggerSettingsRadioButton.TabStop = true;
			this._useDebuggerSettingsRadioButton.Text = "Use &debugger settings";
			this._useDebuggerSettingsRadioButton.UseVisualStyleBackColor = true;
			// 
			// _debuggerOptionsHintLabel
			// 
			this._debuggerOptionsHintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._debuggerOptionsHintLabel.Location = new System.Drawing.Point(57, 57);
			this._debuggerOptionsHintLabel.Name = "_debuggerOptionsHintLabel";
			this._debuggerOptionsHintLabel.Size = new System.Drawing.Size(361, 35);
			this._debuggerOptionsHintLabel.TabIndex = 2;
			this._debuggerOptionsHintLabel.Text = "Please go to Visual Studio Options -> Debugging -> Symbols to set options.";
			// 
			// _useCustomSettingsRadioButton
			// 
			this._useCustomSettingsRadioButton.AutoSize = true;
			this._useCustomSettingsRadioButton.Enabled = false;
			this._useCustomSettingsRadioButton.Location = new System.Drawing.Point(37, 95);
			this._useCustomSettingsRadioButton.Name = "_useCustomSettingsRadioButton";
			this._useCustomSettingsRadioButton.Size = new System.Drawing.Size(120, 17);
			this._useCustomSettingsRadioButton.TabIndex = 3;
			this._useCustomSettingsRadioButton.TabStop = true;
			this._useCustomSettingsRadioButton.Text = "Use &custom settings";
			this._useCustomSettingsRadioButton.UseVisualStyleBackColor = true;
			this._useCustomSettingsRadioButton.CheckedChanged += new System.EventHandler(this.HandleCustomPdbGroupCheckedChanged);
			this._useCustomSettingsRadioButton.EnabledChanged += new System.EventHandler(this.HandleCustomPdbGroupCheckedChanged);
			// 
			// _symbolServersTextBox
			// 
			this._symbolServersTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._symbolServersTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this._symbolServersTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.AllUrl;
			this._symbolServersTextBox.Enabled = false;
			this._symbolServersTextBox.Location = new System.Drawing.Point(60, 131);
			this._symbolServersTextBox.Name = "_symbolServersTextBox";
			this._symbolServersTextBox.Size = new System.Drawing.Size(358, 20);
			this._symbolServersTextBox.TabIndex = 5;
			// 
			// _symbolCacheFolderTextBox
			// 
			this._symbolCacheFolderTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._symbolCacheFolderTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this._symbolCacheFolderTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
			this._symbolCacheFolderTextBox.Enabled = false;
			this._symbolCacheFolderTextBox.Location = new System.Drawing.Point(60, 181);
			this._symbolCacheFolderTextBox.Name = "_symbolCacheFolderTextBox";
			this._symbolCacheFolderTextBox.Size = new System.Drawing.Size(277, 20);
			this._symbolCacheFolderTextBox.TabIndex = 7;
			// 
			// _useReflectorCheckBox
			// 
			this._useReflectorCheckBox.AutoSize = true;
			this._useReflectorCheckBox.Location = new System.Drawing.Point(17, 233);
			this._useReflectorCheckBox.Name = "_useReflectorCheckBox";
			this._useReflectorCheckBox.Size = new System.Drawing.Size(187, 17);
			this._useReflectorCheckBox.TabIndex = 10;
			this._useReflectorCheckBox.Text = "Use Lutz Roeder\'s .NET &Reflector";
			this._useReflectorCheckBox.UseVisualStyleBackColor = true;
			this._useReflectorCheckBox.CheckedChanged += new System.EventHandler(this.HandleReflectorGroupCheckedChanged);
			this._useReflectorCheckBox.EnabledChanged += new System.EventHandler(this.HandleReflectorGroupCheckedChanged);
			// 
			// _reflectorPathTextBox
			// 
			this._reflectorPathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._reflectorPathTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this._reflectorPathTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
			this._reflectorPathTextBox.Enabled = false;
			this._reflectorPathTextBox.Location = new System.Drawing.Point(37, 269);
			this._reflectorPathTextBox.Name = "_reflectorPathTextBox";
			this._reflectorPathTextBox.Size = new System.Drawing.Size(300, 20);
			this._reflectorPathTextBox.TabIndex = 12;
			// 
			// OptionsPage
			// 
			this.AutoSize = true;
			this.Controls.Add(this._reflectorHomepageLinkLabel);
			this.Controls.Add(this._reflectorPathBrowseButton);
			this.Controls.Add(this._reflectorPathTextBox);
			this.Controls.Add(reflectorExecutableLabel);
			this.Controls.Add(this._useReflectorCheckBox);
			this.Controls.Add(this._symbolCacheBrowseButton);
			this.Controls.Add(this._symbolCacheFolderTextBox);
			this.Controls.Add(symbolCacheFolder);
			this.Controls.Add(this._symbolServersTextBox);
			this.Controls.Add(symbolServersLabel);
			this.Controls.Add(this._useCustomSettingsRadioButton);
			this.Controls.Add(this._debuggerOptionsHintLabel);
			this.Controls.Add(this._useDebuggerSettingsRadioButton);
			this.Controls.Add(this._usePdbFilesCheckBox);
			this.Name = "OptionsPage";
			this.Size = new System.Drawing.Size(429, 319);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private RadioButton _useDebuggerSettingsRadioButton;
		private Label _debuggerOptionsHintLabel;
		private RadioButton _useCustomSettingsRadioButton;
		[TextNotEmpty("At least one symbol server should be specified", ValidatorSeverity.Error)]
		private TextBox _symbolServersTextBox;
		[DirectoryExists("Directory not exists", ValidatorSeverity.Error)]
		private TextBox _symbolCacheFolderTextBox;
		private CheckBox _useReflectorCheckBox;
		[FileExists("File not exists", ValidatorSeverity.Error)]
		private TextBox _reflectorPathTextBox;
		private LinkLabel _reflectorHomepageLinkLabel;
		private Button _reflectorPathBrowseButton;
		private Button _symbolCacheBrowseButton;

		private CheckBox _usePdbFilesCheckBox;
		private IFormValidator _formValidator;

		#endregion

		#region Event handlers

		private void HandleOpenReflectorSite(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(Settings.Default.ReflectorHomePage);
		}

		private void HandlePdbGroupCheckedChanged(object sender, System.EventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;
			_useDebuggerSettingsRadioButton.Enabled =
				_useCustomSettingsRadioButton.Enabled = checkBox.Checked && checkBox.Enabled;
		}

		private void HandleReflectorGroupCheckedChanged(object sender, System.EventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;
			_reflectorPathTextBox.Enabled =
				_reflectorPathBrowseButton.Enabled = checkBox.Checked && checkBox.Enabled;

			if (_reflectorPathTextBox.Enabled && _reflectorPathTextBox.TextLength == 0)
				_reflectorPathTextBox.Text = Options.ReflectorPath;
		}

		private void HandleCustomPdbGroupCheckedChanged(object sender, System.EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			_symbolCacheFolderTextBox.Enabled =
				_symbolServersTextBox.Enabled =
				_symbolCacheBrowseButton.Enabled = radioButton.Checked && radioButton.Enabled;
		}

		private void HandleReflectorBrowseClick(object sender, System.EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.FileName        = _reflectorPathTextBox.Text;
				dlg.CheckFileExists = true;
				dlg.Filter          = Resources.ExecutableFilesFilter;

				if (DialogResult.OK == dlg.ShowDialog(this))
				{
					_reflectorPathTextBox.Text = dlg.FileName;
				}
			}
		}

		private void HandleCacheBrowseClick(object sender, System.EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				dlg.SelectedPath = _symbolCacheFolderTextBox.Text;
				if (dlg.ShowDialog(this) == DialogResult.OK)
					_symbolCacheFolderTextBox.Text = dlg.SelectedPath;
			}
		}

		#endregion
	}
}
