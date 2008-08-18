using System;
using System.Windows.Forms;
using System.Drawing;

using JetBrains.CommonControls.Validation;
using JetBrains.UI.Options;

namespace ReSharper.Scout
{
	using Properties;
	using Validation;

	[
		OptionsPage(PageID, AssemblyInfo.Product,
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

			setFontStyle(_usePdbFilesCheckBox,      FontStyle.Bold);
			setFontStyle(_useReflectorCheckBox,     FontStyle.Bold);
			setFontStyle(_reflectorConfigLabel,     FontStyle.Bold);
			setFontStyle(_debuggerOptionsHintLabel, FontStyle.Italic);

			_usePdbFilesCheckBox.Checked            = Options.UsePdbFiles;
			_useDebuggerSettingsRadioButton.Checked = Options.UseDebuggerSettings;
			_useCustomSettingsRadioButton.Checked   = !_useDebuggerSettingsRadioButton.Checked;
			_symbolServersTextBox.Text              = Options.SymbolPath;
			_symbolCacheFolderTextBox.Text          = Options.SymbolCacheDir;

			_useReflectorCheckBox.Checked           = Options.UseReflector;

			if (_useReflectorCheckBox.Checked)
				_reflectorPathTextBox.Text = Options.ReflectorPath;

			_customConfigTextBox.Text = Options.ReflectorCustomConfiguration;
			_reuseAnyReflectorCheckBox.Checked = Options.ReuseAnyReflectorInstance;

			switch (Options.ReflectorConfiguration)
			{
				case ReflectorConfiguration.Default:
					_useDefaultConfigRadioButton.Checked = true;
					break;
				case ReflectorConfiguration.PerSolution:
					_usePerSolutionConfigRadioButton.Checked = true;
					break;
				case ReflectorConfiguration.Custom:
					_useCustomConfigRadioButton.Checked = true;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void setFontStyle(Control ctl, FontStyle style)
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
			Options.UsePdbFiles = _usePdbFilesCheckBox.Checked;

			if (_usePdbFilesCheckBox.Checked)
			{
				Options.UseDebuggerSettings = _useDebuggerSettingsRadioButton.Checked;
				Options.SymbolPath          = _symbolServersTextBox.Text;
				Options.SymbolCacheDir      = _symbolCacheFolderTextBox.Text;
			}

			Options.UseReflector  = _useReflectorCheckBox.Checked;
			if (_useReflectorCheckBox.Checked)
			{
				Options.ReflectorPath             = _reflectorPathTextBox.Text;
				Options.ReuseAnyReflectorInstance = _reuseAnyReflectorCheckBox.Checked;

				if (!_reuseAnyReflectorCheckBox.Checked)
				{
					if (_useCustomConfigRadioButton.Checked)
					{
						Options.ReflectorConfiguration = ReflectorConfiguration.Custom;
						Options.ReflectorCustomConfiguration = _customConfigTextBox.Text;
					}
					else
						Options.ReflectorConfiguration = _useDefaultConfigRadioButton.Checked?
							ReflectorConfiguration.Default: ReflectorConfiguration.PerSolution;
				}
			}

			return true;
		}

		public bool ValidatePage()
		{
			return (!_useReflectorCheckBox.Checked || _reflectorPathTextBox.TextLength > 0) &&
				(!_useCustomSettingsRadioButton.Checked || _symbolCacheFolderTextBox.TextLength > 0) &&
				(!_useCustomConfigRadioButton.Checked || _customConfigTextBox.TextLength > 0);
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
			this._reuseAnyReflectorCheckBox = new System.Windows.Forms.CheckBox();
			this._useDefaultConfigRadioButton = new System.Windows.Forms.RadioButton();
			this._usePerSolutionConfigRadioButton = new System.Windows.Forms.RadioButton();
			this._reflectorConfigLabel = new System.Windows.Forms.Label();
			this._useCustomConfigRadioButton = new System.Windows.Forms.RadioButton();
			this._customConfigBrowseButton = new System.Windows.Forms.Button();
			this._customConfigTextBox = new System.Windows.Forms.TextBox();
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
			reflectorExecutableLabel.TabIndex = 10;
			reflectorExecutableLabel.Text = "Path to the &executable file:";
			// 
			// _reflectorHomepageLinkLabel
			// 
			this._reflectorHomepageLinkLabel.AutoSize = true;
			this._reflectorHomepageLinkLabel.Location = new System.Drawing.Point(34, 456);
			this._reflectorHomepageLinkLabel.Name = "_reflectorHomepageLinkLabel";
			this._reflectorHomepageLinkLabel.Size = new System.Drawing.Size(130, 13);
			this._reflectorHomepageLinkLabel.TabIndex = 20;
			this._reflectorHomepageLinkLabel.TabStop = true;
			this._reflectorHomepageLinkLabel.Text = "Open reflector home page";
			this._reflectorHomepageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.handleOpenReflectorSite);
			// 
			// _symbolCacheBrowseButton
			// 
			this._symbolCacheBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._symbolCacheBrowseButton.Enabled = false;
			this._symbolCacheBrowseButton.Location = new System.Drawing.Point(365, 179);
			this._symbolCacheBrowseButton.Name = "_symbolCacheBrowseButton";
			this._symbolCacheBrowseButton.Size = new System.Drawing.Size(75, 23);
			this._symbolCacheBrowseButton.TabIndex = 8;
			this._symbolCacheBrowseButton.Text = "&Browse…";
			this._symbolCacheBrowseButton.UseVisualStyleBackColor = true;
			this._symbolCacheBrowseButton.Click += new System.EventHandler(this.handleCacheBrowseClick);
			// 
			// _reflectorPathBrowseButton
			// 
			this._reflectorPathBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._reflectorPathBrowseButton.Enabled = false;
			this._reflectorPathBrowseButton.Location = new System.Drawing.Point(365, 267);
			this._reflectorPathBrowseButton.Name = "_reflectorPathBrowseButton";
			this._reflectorPathBrowseButton.Size = new System.Drawing.Size(75, 23);
			this._reflectorPathBrowseButton.TabIndex = 12;
			this._reflectorPathBrowseButton.Text = "Bro&wse…";
			this._reflectorPathBrowseButton.UseVisualStyleBackColor = true;
			this._reflectorPathBrowseButton.Click += new System.EventHandler(this.handleReflectorBrowseClick);
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
			this._usePdbFilesCheckBox.CheckedChanged += new System.EventHandler(this.handlePdbGroupCheckedChanged);
			this._usePdbFilesCheckBox.EnabledChanged += new System.EventHandler(this.handlePdbGroupCheckedChanged);
			// 
			// _useDebuggerSettingsRadioButton
			// 
			this._useDebuggerSettingsRadioButton.AutoCheck = false;
			this._useDebuggerSettingsRadioButton.AutoSize = true;
			this._useDebuggerSettingsRadioButton.Enabled = false;
			this._useDebuggerSettingsRadioButton.Location = new System.Drawing.Point(37, 37);
			this._useDebuggerSettingsRadioButton.Name = "_useDebuggerSettingsRadioButton";
			this._useDebuggerSettingsRadioButton.Size = new System.Drawing.Size(131, 17);
			this._useDebuggerSettingsRadioButton.TabIndex = 1;
			this._useDebuggerSettingsRadioButton.TabStop = true;
			this._useDebuggerSettingsRadioButton.Text = "Use &debugger settings";
			this._useDebuggerSettingsRadioButton.UseVisualStyleBackColor = true;
			this._useDebuggerSettingsRadioButton.Click += new System.EventHandler(this.handlePdbRadioButtonClicked);
			// 
			// _debuggerOptionsHintLabel
			// 
			this._debuggerOptionsHintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._debuggerOptionsHintLabel.Location = new System.Drawing.Point(57, 57);
			this._debuggerOptionsHintLabel.Name = "_debuggerOptionsHintLabel";
			this._debuggerOptionsHintLabel.Size = new System.Drawing.Size(383, 35);
			this._debuggerOptionsHintLabel.TabIndex = 2;
			this._debuggerOptionsHintLabel.Text = "Please go to Visual Studio Options -> Debugging -> Symbols to set options.";
			// 
			// _useCustomSettingsRadioButton
			// 
			this._useCustomSettingsRadioButton.AutoCheck = false;
			this._useCustomSettingsRadioButton.AutoSize = true;
			this._useCustomSettingsRadioButton.Enabled = false;
			this._useCustomSettingsRadioButton.Location = new System.Drawing.Point(37, 95);
			this._useCustomSettingsRadioButton.Name = "_useCustomSettingsRadioButton";
			this._useCustomSettingsRadioButton.Size = new System.Drawing.Size(120, 17);
			this._useCustomSettingsRadioButton.TabIndex = 3;
			this._useCustomSettingsRadioButton.TabStop = true;
			this._useCustomSettingsRadioButton.Text = "Use c&ustom settings";
			this._useCustomSettingsRadioButton.UseVisualStyleBackColor = true;
			this._useCustomSettingsRadioButton.Click += new System.EventHandler(this.handlePdbRadioButtonClicked);
			this._useCustomSettingsRadioButton.CheckedChanged += new System.EventHandler(this.handleCustomPdbGroupCheckedChanged);
			this._useCustomSettingsRadioButton.EnabledChanged += new System.EventHandler(this.handleCustomPdbGroupCheckedChanged);
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
			this._symbolServersTextBox.Size = new System.Drawing.Size(380, 20);
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
			this._symbolCacheFolderTextBox.Size = new System.Drawing.Size(299, 20);
			this._symbolCacheFolderTextBox.TabIndex = 7;
			// 
			// _useReflectorCheckBox
			// 
			this._useReflectorCheckBox.AutoSize = true;
			this._useReflectorCheckBox.Location = new System.Drawing.Point(17, 233);
			this._useReflectorCheckBox.Name = "_useReflectorCheckBox";
			this._useReflectorCheckBox.Size = new System.Drawing.Size(187, 17);
			this._useReflectorCheckBox.TabIndex = 9;
			this._useReflectorCheckBox.Text = "Use Lutz Roeder\'s .NET &Reflector";
			this._useReflectorCheckBox.UseVisualStyleBackColor = true;
			this._useReflectorCheckBox.CheckedChanged += new System.EventHandler(this.handleReflectorGroupCheckedChanged);
			this._useReflectorCheckBox.EnabledChanged += new System.EventHandler(this.handleReflectorGroupCheckedChanged);
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
			this._reflectorPathTextBox.Size = new System.Drawing.Size(322, 20);
			this._reflectorPathTextBox.TabIndex = 11;
			// 
			// _reuseAnyReflectorCheckBox
			// 
			this._reuseAnyReflectorCheckBox.AutoSize = true;
			this._reuseAnyReflectorCheckBox.Location = new System.Drawing.Point(37, 306);
			this._reuseAnyReflectorCheckBox.Name = "_reuseAnyReflectorCheckBox";
			this._reuseAnyReflectorCheckBox.Size = new System.Drawing.Size(166, 17);
			this._reuseAnyReflectorCheckBox.TabIndex = 19;
			this._reuseAnyReflectorCheckBox.Text = "Reuse &any Reflector instance";
			this._reuseAnyReflectorCheckBox.UseVisualStyleBackColor = true;
			this._reuseAnyReflectorCheckBox.CheckedChanged += new System.EventHandler(this.handleReuseReflectorCheckedChanged);
			this._reuseAnyReflectorCheckBox.EnabledChanged += new System.EventHandler(this.handleReuseReflectorCheckedChanged);
			// 
			// _useDefaultConfigRadioButton
			// 
			this._useDefaultConfigRadioButton.AutoCheck = false;
			this._useDefaultConfigRadioButton.AutoSize = true;
			this._useDefaultConfigRadioButton.Location = new System.Drawing.Point(60, 357);
			this._useDefaultConfigRadioButton.Name = "_useDefaultConfigRadioButton";
			this._useDefaultConfigRadioButton.Size = new System.Drawing.Size(59, 17);
			this._useDefaultConfigRadioButton.TabIndex = 14;
			this._useDefaultConfigRadioButton.TabStop = true;
			this._useDefaultConfigRadioButton.Text = "Defaul&t";
			this._useDefaultConfigRadioButton.UseVisualStyleBackColor = true;
			this._useDefaultConfigRadioButton.Click += new System.EventHandler(this.handleReflectorRadionButtonClicked);
			// 
			// _usePerSolutionConfigRadioButton
			// 
			this._usePerSolutionConfigRadioButton.AutoCheck = false;
			this._usePerSolutionConfigRadioButton.AutoSize = true;
			this._usePerSolutionConfigRadioButton.Location = new System.Drawing.Point(60, 380);
			this._usePerSolutionConfigRadioButton.Name = "_usePerSolutionConfigRadioButton";
			this._usePerSolutionConfigRadioButton.Size = new System.Drawing.Size(80, 17);
			this._usePerSolutionConfigRadioButton.TabIndex = 15;
			this._usePerSolutionConfigRadioButton.TabStop = true;
			this._usePerSolutionConfigRadioButton.Text = "Per solutio&n";
			this._usePerSolutionConfigRadioButton.UseVisualStyleBackColor = true;
			this._usePerSolutionConfigRadioButton.Click += new System.EventHandler(this.handleReflectorRadionButtonClicked);
			// 
			// _reflectorConfigLabel
			// 
			this._reflectorConfigLabel.AutoSize = true;
			this._reflectorConfigLabel.Location = new System.Drawing.Point(34, 341);
			this._reflectorConfigLabel.Name = "_reflectorConfigLabel";
			this._reflectorConfigLabel.Size = new System.Drawing.Size(117, 13);
			this._reflectorConfigLabel.TabIndex = 13;
			this._reflectorConfigLabel.Text = "Reflector &configuration:";
			// 
			// _useCustomConfigRadioButton
			// 
			this._useCustomConfigRadioButton.AutoCheck = false;
			this._useCustomConfigRadioButton.AutoSize = true;
			this._useCustomConfigRadioButton.Location = new System.Drawing.Point(60, 403);
			this._useCustomConfigRadioButton.Name = "_useCustomConfigRadioButton";
			this._useCustomConfigRadioButton.Size = new System.Drawing.Size(63, 17);
			this._useCustomConfigRadioButton.TabIndex = 16;
			this._useCustomConfigRadioButton.TabStop = true;
			this._useCustomConfigRadioButton.Text = "Custo&m:";
			this._useCustomConfigRadioButton.UseVisualStyleBackColor = true;
			this._useCustomConfigRadioButton.Click += new System.EventHandler(this.handleReflectorRadionButtonClicked);
			this._useCustomConfigRadioButton.CheckedChanged += new System.EventHandler(this.handleCustomReflectorConfigGroupCheckedChanged);
			this._useCustomConfigRadioButton.EnabledChanged += new System.EventHandler(this.handleCustomReflectorConfigGroupCheckedChanged);
			// 
			// _customConfigBrowseButton
			// 
			this._customConfigBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._customConfigBrowseButton.Enabled = false;
			this._customConfigBrowseButton.Location = new System.Drawing.Point(365, 424);
			this._customConfigBrowseButton.Name = "_customConfigBrowseButton";
			this._customConfigBrowseButton.Size = new System.Drawing.Size(75, 23);
			this._customConfigBrowseButton.TabIndex = 18;
			this._customConfigBrowseButton.Text = "Br&owse…";
			this._customConfigBrowseButton.UseVisualStyleBackColor = true;
			this._customConfigBrowseButton.Click += new System.EventHandler(this.handleCustomReflectorConfigBrowseClick);
			// 
			// _customConfigTextBox
			// 
			this._customConfigTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._customConfigTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this._customConfigTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
			this._customConfigTextBox.Enabled = false;
			this._customConfigTextBox.Location = new System.Drawing.Point(78, 426);
			this._customConfigTextBox.Name = "_customConfigTextBox";
			this._customConfigTextBox.Size = new System.Drawing.Size(281, 20);
			this._customConfigTextBox.TabIndex = 17;
			// 
			// OptionsPage
			// 
			this.AutoSize = true;
			this.Controls.Add(this._customConfigBrowseButton);
			this.Controls.Add(this._customConfigTextBox);
			this.Controls.Add(this._useCustomConfigRadioButton);
			this.Controls.Add(this._reflectorConfigLabel);
			this.Controls.Add(this._usePerSolutionConfigRadioButton);
			this.Controls.Add(this._useDefaultConfigRadioButton);
			this.Controls.Add(this._reuseAnyReflectorCheckBox);
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
			this.Size = new System.Drawing.Size(451, 482);
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
		private CheckBox _reuseAnyReflectorCheckBox;
		private RadioButton _useDefaultConfigRadioButton;
		private RadioButton _usePerSolutionConfigRadioButton;
		private Label _reflectorConfigLabel;
		private RadioButton _useCustomConfigRadioButton;
		private Button _customConfigBrowseButton;
		[TextIsValidPath("Path must be valid", ValidatorSeverity.Error)]
		private TextBox _customConfigTextBox;
		private IFormValidator _formValidator;

		#endregion

		#region Event handlers

		private void handleOpenReflectorSite(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(Resources.Reflector_HomePage);
		}

		private void handlePdbRadioButtonClicked(object sender, EventArgs e)
		{
			RadioButton[] group = new RadioButton[]
				{ _useDebuggerSettingsRadioButton, _useCustomSettingsRadioButton };

			foreach (RadioButton button in group)
			{
				button.Checked = button == sender;
			}
		}

		private void handleReflectorRadionButtonClicked(object sender, EventArgs e)
		{
			RadioButton[] group = new RadioButton[]
				{ _useDefaultConfigRadioButton, _usePerSolutionConfigRadioButton, _useCustomConfigRadioButton};

			foreach (RadioButton button in group)
			{
				button.Checked = button == sender;
			}
		}

		private void handlePdbGroupCheckedChanged(object sender, EventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;
			_useDebuggerSettingsRadioButton.Enabled =
				_useCustomSettingsRadioButton.Enabled = checkBox.Checked && checkBox.Enabled;
		}

		private void handleReflectorGroupCheckedChanged(object sender, EventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;
			_reflectorPathTextBox.Enabled =
				_reuseAnyReflectorCheckBox.Enabled =
				_reflectorPathBrowseButton.Enabled = checkBox.Checked && checkBox.Enabled;

			if (_reflectorPathTextBox.Enabled && _reflectorPathTextBox.TextLength == 0)
				_reflectorPathTextBox.Text = Options.ReflectorPath;
		}

		private void handleCustomPdbGroupCheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			_symbolCacheFolderTextBox.Enabled =
				_symbolServersTextBox.Enabled =
				_symbolCacheBrowseButton.Enabled = radioButton.Checked && radioButton.Enabled;
		}

		private void handleReuseReflectorCheckedChanged(object sender, EventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;

			_useDefaultConfigRadioButton.Enabled =
			_usePerSolutionConfigRadioButton.Enabled =
			_useCustomConfigRadioButton.Enabled = !checkBox.Checked && checkBox.Enabled;
		}

		private void handleCustomReflectorConfigGroupCheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			_customConfigTextBox.Enabled =
			_customConfigBrowseButton.Enabled = radioButton.Checked && radioButton.Enabled;
		}

		private void handleReflectorBrowseClick(object sender, EventArgs e)
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

		private void handleCacheBrowseClick(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				dlg.SelectedPath = _symbolCacheFolderTextBox.Text;
				if (dlg.ShowDialog(this) == DialogResult.OK)
					_symbolCacheFolderTextBox.Text = dlg.SelectedPath;
			}
		}

		private void handleCustomReflectorConfigBrowseClick(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.FileName        = _reflectorPathTextBox.Text;
				dlg.CheckFileExists = false;
				dlg.Filter          = Resources.ConfigFilesFilter;

				if (DialogResult.OK == dlg.ShowDialog(this))
				{
					_customConfigTextBox.Text = dlg.FileName;
				}
			}
		}

		#endregion
	}
}
