namespace PotPlayerMultiControl;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private ToolTip toolTip;
    private Label statusLabel;
    private Button toggleButton;
    private Button goToStartButton;
    private Button showAllButton;
    private Button minimizeAllButton;
    private Button refreshButton;
    private Button elevateButton;
    private Button pinTopButton;
    private Button rewindButton;
    private Button forwardButton;
    private Label seekSecondsLabel;
    private NumericUpDown seekSecondsUpDown;
    private Button windowListToggle;
    private Button logToggle;
    private ListBox listBox;
    private TextBox logTextBox;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolTip = new ToolTip(components);
        statusLabel = new Label();
        toggleButton = new Button();
        goToStartButton = new Button();
        showAllButton = new Button();
        minimizeAllButton = new Button();
        refreshButton = new Button();
        elevateButton = new Button();
        pinTopButton = new Button();
        rewindButton = new Button();
        forwardButton = new Button();
        seekSecondsLabel = new Label();
        seekSecondsUpDown = new NumericUpDown();
        windowListToggle = new Button();
        logToggle = new Button();
        listBox = new ListBox();
        logTextBox = new TextBox();
        SuspendLayout();
        // 
        // toolTip
        // 
        toolTip.AutoPopDelay = 8000;
        toolTip.InitialDelay = 300;
        toolTip.ReshowDelay = 100;
        toolTip.ShowAlways = true;
        // 
        // statusLabel
        // 
        statusLabel.AutoEllipsis = true;
        statusLabel.Location = new Point(12, 8);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(436, 18);
        statusLabel.TabIndex = 0;
        statusLabel.Text = "检测中...";
        // 
        // goToStartButton
        // 
        goToStartButton.Name = "goToStartButton";
        goToStartButton.TabIndex = 1;
        goToStartButton.Click += GoToStartButton_Click;
        // 
        // rewindButton
        // 
        rewindButton.Name = "rewindButton";
        rewindButton.TabIndex = 2;
        rewindButton.Click += RewindButton_Click;
        // 
        // toggleButton
        // 
        toggleButton.Name = "toggleButton";
        toggleButton.TabIndex = 3;
        toggleButton.Click += ToggleButton_Click;
        // 
        // forwardButton
        // 
        forwardButton.Name = "forwardButton";
        forwardButton.TabIndex = 4;
        forwardButton.Click += ForwardButton_Click;
        // 
        // seekSecondsUpDown
        // 
        seekSecondsUpDown.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
        seekSecondsUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        seekSecondsUpDown.Name = "seekSecondsUpDown";
        seekSecondsUpDown.Size = new Size(48, 23);
        seekSecondsUpDown.TabIndex = 5;
        seekSecondsUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
        seekSecondsUpDown.ValueChanged += SeekSecondsUpDown_ValueChanged;
        // 
        // seekSecondsLabel
        // 
        seekSecondsLabel.AutoSize = true;
        seekSecondsLabel.Name = "seekSecondsLabel";
        seekSecondsLabel.Size = new Size(19, 15);
        seekSecondsLabel.TabIndex = 6;
        seekSecondsLabel.Text = "秒";
        // 
        // showAllButton
        // 
        showAllButton.Name = "showAllButton";
        showAllButton.TabIndex = 7;
        showAllButton.Click += ShowAllButton_Click;
        // 
        // minimizeAllButton
        // 
        minimizeAllButton.Name = "minimizeAllButton";
        minimizeAllButton.TabIndex = 8;
        minimizeAllButton.Click += MinimizeAllButton_Click;
        // 
        // pinTopButton
        // 
        pinTopButton.Name = "pinTopButton";
        pinTopButton.TabIndex = 9;
        pinTopButton.Click += PinTopButton_Click;
        // 
        // refreshButton
        // 
        refreshButton.Name = "refreshButton";
        refreshButton.TabIndex = 10;
        refreshButton.Click += RefreshButton_Click;
        // 
        // elevateButton
        // 
        elevateButton.Name = "elevateButton";
        elevateButton.TabIndex = 11;
        elevateButton.Click += ElevateButton_Click;
        // 
        // windowListToggle
        // 
        windowListToggle.FlatStyle = FlatStyle.Flat;
        windowListToggle.Location = new Point(12, 74);
        windowListToggle.Name = "windowListToggle";
        windowListToggle.Size = new Size(436, 28);
        windowListToggle.TabIndex = 12;
        windowListToggle.Text = "▸  窗口列表";
        windowListToggle.TextAlign = ContentAlignment.MiddleLeft;
        windowListToggle.UseVisualStyleBackColor = true;
        windowListToggle.Click += WindowListToggle_Click;
        // 
        // logToggle
        // 
        logToggle.FlatStyle = FlatStyle.Flat;
        logToggle.Location = new Point(12, 104);
        logToggle.Name = "logToggle";
        logToggle.Size = new Size(436, 28);
        logToggle.TabIndex = 13;
        logToggle.Text = "▸  运行日志";
        logToggle.TextAlign = ContentAlignment.MiddleLeft;
        logToggle.UseVisualStyleBackColor = true;
        logToggle.Click += LogToggle_Click;
        // 
        // listBox
        // 
        listBox.FormattingEnabled = true;
        listBox.ItemHeight = 18;
        listBox.Location = new Point(12, 104);
        listBox.Name = "listBox";
        listBox.IntegralHeight = false;
        listBox.Size = new Size(436, 190);
        listBox.TabIndex = 14;
        listBox.Visible = false;
        // 
        // logTextBox
        // 
        logTextBox.Location = new Point(12, 134);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Size = new Size(436, 236);
        logTextBox.TabIndex = 15;
        logTextBox.Visible = false;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(246, 247, 249);
        ClientSize = new Size(460, 148);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Controls.Add(logTextBox);
        Controls.Add(listBox);
        Controls.Add(logToggle);
        Controls.Add(windowListToggle);
        Controls.Add(seekSecondsUpDown);
        Controls.Add(seekSecondsLabel);
        Controls.Add(elevateButton);
        Controls.Add(refreshButton);
        Controls.Add(pinTopButton);
        Controls.Add(minimizeAllButton);
        Controls.Add(showAllButton);
        Controls.Add(forwardButton);
        Controls.Add(toggleButton);
        Controls.Add(rewindButton);
        Controls.Add(goToStartButton);
        Controls.Add(statusLabel);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "PotPlayer 多窗口控制";
        ResumeLayout(false);
        PerformLayout();
    }
}
