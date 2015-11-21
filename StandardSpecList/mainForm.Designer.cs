namespace StandardSpecList
{
    partial class mainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnProcess = new System.Windows.Forms.Button();
            this.specListView = new System.Windows.Forms.ListView();
            this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnLoad = new System.Windows.Forms.Button();
            this.grpListBox = new System.Windows.Forms.GroupBox();
            this.lblSpecListCount = new System.Windows.Forms.Label();
            this.lblFileName = new System.Windows.Forms.Label();
            this.btnExit = new System.Windows.Forms.Button();
            this.workProgressBar = new System.Windows.Forms.ProgressBar();
            this.grpListBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnProcess
            // 
            this.btnProcess.Enabled = false;
            this.btnProcess.ForeColor = System.Drawing.SystemColors.InfoText;
            this.btnProcess.Location = new System.Drawing.Point(18, 391);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(114, 23);
            this.btnProcess.TabIndex = 2;
            this.btnProcess.Text = "Обработать";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // specListView
            // 
            this.specListView.AutoArrange = false;
            this.specListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderStatus});
            this.specListView.FullRowSelect = true;
            this.specListView.GridLines = true;
            this.specListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.specListView.Location = new System.Drawing.Point(6, 19);
            this.specListView.MultiSelect = false;
            this.specListView.Name = "specListView";
            this.specListView.Size = new System.Drawing.Size(538, 300);
            this.specListView.TabIndex = 0;
            this.specListView.UseCompatibleStateImageBehavior = false;
            this.specListView.View = System.Windows.Forms.View.Details;
            this.specListView.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.specListView_ColumnWidthChanging);
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Название";
            this.columnHeaderName.Width = 360;
            // 
            // columnHeaderStatus
            // 
            this.columnHeaderStatus.Text = "Статус";
            this.columnHeaderStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeaderStatus.Width = 140;
            // 
            // btnLoad
            // 
            this.btnLoad.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnLoad.Location = new System.Drawing.Point(18, 362);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(114, 23);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Загрузить список";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // grpListBox
            // 
            this.grpListBox.Controls.Add(this.lblSpecListCount);
            this.grpListBox.Controls.Add(this.specListView);
            this.grpListBox.ForeColor = System.Drawing.Color.SaddleBrown;
            this.grpListBox.Location = new System.Drawing.Point(12, 12);
            this.grpListBox.Name = "grpListBox";
            this.grpListBox.Size = new System.Drawing.Size(550, 344);
            this.grpListBox.TabIndex = 5;
            this.grpListBox.TabStop = false;
            this.grpListBox.Text = "Список ГОСТов";
            // 
            // lblSpecListCount
            // 
            this.lblSpecListCount.AutoSize = true;
            this.lblSpecListCount.ForeColor = System.Drawing.Color.Chocolate;
            this.lblSpecListCount.Location = new System.Drawing.Point(6, 322);
            this.lblSpecListCount.Name = "lblSpecListCount";
            this.lblSpecListCount.Size = new System.Drawing.Size(115, 13);
            this.lblSpecListCount.TabIndex = 2;
            this.lblSpecListCount.Text = "Общее количество: 0";
            // 
            // lblFileName
            // 
            this.lblFileName.AutoSize = true;
            this.lblFileName.ForeColor = System.Drawing.Color.Red;
            this.lblFileName.Location = new System.Drawing.Point(138, 367);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(87, 13);
            this.lblFileName.TabIndex = 3;
            this.lblFileName.Text = "[Укажите файл]";
            // 
            // btnExit
            // 
            this.btnExit.ForeColor = System.Drawing.SystemColors.InfoText;
            this.btnExit.Location = new System.Drawing.Point(442, 391);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(114, 23);
            this.btnExit.TabIndex = 3;
            this.btnExit.Text = "Выход";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // workProgressBar
            // 
            this.workProgressBar.Location = new System.Drawing.Point(141, 391);
            this.workProgressBar.Name = "workProgressBar";
            this.workProgressBar.Size = new System.Drawing.Size(295, 23);
            this.workProgressBar.TabIndex = 6;
            this.workProgressBar.Visible = false;
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 426);
            this.Controls.Add(this.workProgressBar);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.grpListBox);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.btnProcess);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "mainForm";
            this.Text = "StandartSpecList";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainForm_FormClosing);
            this.grpListBox.ResumeLayout(false);
            this.grpListBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.ListView specListView;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.GroupBox grpListBox;
        private System.Windows.Forms.Label lblSpecListCount;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderStatus;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.ProgressBar workProgressBar;
    }
}

