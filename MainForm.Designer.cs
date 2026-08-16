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
        // listBox
        // 
        listBox.FormattingEnabled = true;
        listBox.ItemHeight = 15;
        listBox.Location = new Point(16, 132);
        listBox.Name = "listBox";
        listBox.Size = new Size(568, 190);
        listBox.TabIndex = 7;
        // 
        // logTextBox
        // 
        logTextBox.Location = new Point(16, 332);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Size = new Size(568, 236);
        logTextBox.TabIndex = 8;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 588);
        Controls.Add(logTextBox);
        Controls.Add(listBox);
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
