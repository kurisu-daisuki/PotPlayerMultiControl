namespace PotPlayerMultiControl;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
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
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(16, 16);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(68, 15);
        statusLabel.TabIndex = 0;
        statusLabel.Text = "检测中...";
        // 
        // toggleButton
        // 
        toggleButton.Location = new Point(16, 44);
        toggleButton.Name = "toggleButton";
        toggleButton.Size = new Size(220, 36);
        toggleButton.TabIndex = 1;
        toggleButton.Text = "播放/暂停全部 (Ctrl+Alt+Space)";
        toggleButton.UseVisualStyleBackColor = true;
        toggleButton.Click += ToggleButton_Click;
        // 
        // goToStartButton
        // 
        goToStartButton.Location = new Point(16, 86);
        goToStartButton.Name = "goToStartButton";
        goToStartButton.Size = new Size(220, 36);
        goToStartButton.TabIndex = 2;
        goToStartButton.Text = "回到起始点 (Ctrl+Alt+Home)";
        goToStartButton.UseVisualStyleBackColor = true;
        goToStartButton.Click += GoToStartButton_Click;
        // 
        // showAllButton
        // 
        showAllButton.Location = new Point(246, 86);
        showAllButton.Name = "showAllButton";
        showAllButton.Size = new Size(162, 36);
        showAllButton.TabIndex = 3;
        showAllButton.Text = "显示全部 (Ctrl+Alt+↑)";
        showAllButton.UseVisualStyleBackColor = true;
        showAllButton.Click += ShowAllButton_Click;
        // 
        // minimizeAllButton
        // 
        minimizeAllButton.Location = new Point(418, 86);
        minimizeAllButton.Name = "minimizeAllButton";
        minimizeAllButton.Size = new Size(166, 36);
        minimizeAllButton.TabIndex = 4;
        minimizeAllButton.Text = "最小化全部 (Ctrl+Alt+↓)";
        minimizeAllButton.UseVisualStyleBackColor = true;
        minimizeAllButton.Click += MinimizeAllButton_Click;
        // 
        // refreshButton
        // 
        refreshButton.Location = new Point(246, 44);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(110, 36);
        refreshButton.TabIndex = 5;
        refreshButton.Text = "刷新列表";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += RefreshButton_Click;
        // 
        // elevateButton
        // 
        elevateButton.Location = new Point(366, 44);
        elevateButton.Name = "elevateButton";
        elevateButton.Size = new Size(170, 36);
        elevateButton.TabIndex = 6;
        elevateButton.Text = "以管理员身份重启";
        elevateButton.UseVisualStyleBackColor = true;
        elevateButton.Click += ElevateButton_Click;
        // 
        // pinTopButton
        // 
        pinTopButton.Location = new Point(16, 128);
        pinTopButton.Name = "pinTopButton";
        pinTopButton.Size = new Size(220, 32);
        pinTopButton.TabIndex = 7;
        pinTopButton.Text = "置顶控制窗口";
        pinTopButton.UseVisualStyleBackColor = true;
        pinTopButton.Click += PinTopButton_Click;
        // 
        // rewindButton
        // 
        rewindButton.Location = new Point(246, 128);
        rewindButton.Name = "rewindButton";
        rewindButton.Size = new Size(162, 32);
        rewindButton.TabIndex = 8;
        rewindButton.Text = "后退 5秒 (Ctrl+Alt+←)";
        rewindButton.UseVisualStyleBackColor = true;
        rewindButton.Click += RewindButton_Click;
        // 
        // forwardButton
        // 
        forwardButton.Location = new Point(418, 128);
        forwardButton.Name = "forwardButton";
        forwardButton.Size = new Size(166, 32);
        forwardButton.TabIndex = 9;
        forwardButton.Text = "快进 5秒 (Ctrl+Alt+→)";
        forwardButton.UseVisualStyleBackColor = true;
        forwardButton.Click += ForwardButton_Click;
        // 
        // seekSecondsLabel
        // 
        seekSecondsLabel.AutoSize = true;
        seekSecondsLabel.Location = new Point(246, 174);
        seekSecondsLabel.Name = "seekSecondsLabel";
        seekSecondsLabel.Size = new Size(79, 15);
        seekSecondsLabel.TabIndex = 10;
        seekSecondsLabel.Text = "时间跨度(秒)";
        // 
        // seekSecondsUpDown
        // 
        seekSecondsUpDown.Location = new Point(338, 170);
        seekSecondsUpDown.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
        seekSecondsUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        seekSecondsUpDown.Name = "seekSecondsUpDown";
        seekSecondsUpDown.Size = new Size(70, 23);
        seekSecondsUpDown.TabIndex = 11;
        seekSecondsUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
        seekSecondsUpDown.ValueChanged += SeekSecondsUpDown_ValueChanged;
        // 
        // windowListToggle
        // 
        windowListToggle.FlatStyle = FlatStyle.Flat;
        windowListToggle.Location = new Point(16, 208);
        windowListToggle.Name = "windowListToggle";
        windowListToggle.Size = new Size(568, 28);
        windowListToggle.TabIndex = 12;
        windowListToggle.Text = "▶ 窗口列表";
        windowListToggle.TextAlign = ContentAlignment.MiddleLeft;
        windowListToggle.UseVisualStyleBackColor = true;
        windowListToggle.Click += WindowListToggle_Click;
        // 
        // logToggle
        // 
        logToggle.FlatStyle = FlatStyle.Flat;
        logToggle.Location = new Point(16, 242);
        logToggle.Name = "logToggle";
        logToggle.Size = new Size(568, 28);
        logToggle.TabIndex = 13;
        logToggle.Text = "▶ 命令栏";
        logToggle.TextAlign = ContentAlignment.MiddleLeft;
        logToggle.UseVisualStyleBackColor = true;
        logToggle.Click += LogToggle_Click;
        // 
        // listBox
        // 
        listBox.FormattingEnabled = true;
        listBox.ItemHeight = 15;
        listBox.Location = new Point(16, 242);
        listBox.Name = "listBox";
        listBox.Size = new Size(568, 190);
        listBox.TabIndex = 14;
        listBox.Visible = false;
        // 
        // logTextBox
        // 
        logTextBox.Location = new Point(16, 276);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Size = new Size(568, 236);
        logTextBox.TabIndex = 15;
        logTextBox.Visible = false;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 286);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Controls.Add(logTextBox);
        Controls.Add(listBox);
        Controls.Add(logToggle);
        Controls.Add(windowListToggle);
        Controls.Add(seekSecondsUpDown);
        Controls.Add(seekSecondsLabel);
        Controls.Add(forwardButton);
        Controls.Add(rewindButton);
        Controls.Add(pinTopButton);
        Controls.Add(elevateButton);
        Controls.Add(refreshButton);
        Controls.Add(minimizeAllButton);
        Controls.Add(showAllButton);
        Controls.Add(goToStartButton);
        Controls.Add(toggleButton);
        Controls.Add(statusLabel);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "PotPlayer 多窗口控制";
        ResumeLayout(false);
        PerformLayout();
    }
}
