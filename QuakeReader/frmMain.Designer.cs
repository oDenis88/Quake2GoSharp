namespace QuakeReader
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnLoadPak = new Button();
            txtPath = new TextBox();
            btnLoadBsp = new Button();
            lstBsps = new ListBox();
            SuspendLayout();
            // 
            // btnLoadPak
            // 
            btnLoadPak.Location = new Point(450, 32);
            btnLoadPak.Name = "btnLoadPak";
            btnLoadPak.Size = new Size(116, 23);
            btnLoadPak.TabIndex = 0;
            btnLoadPak.Text = "Load PAK";
            btnLoadPak.UseVisualStyleBackColor = true;
            btnLoadPak.Click += btnLoadPak_Click;
            // 
            // txtPath
            // 
            txtPath.Location = new Point(28, 32);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(416, 23);
            txtPath.TabIndex = 1;
            // 
            // btnLoadBsp
            // 
            btnLoadBsp.Location = new Point(450, 257);
            btnLoadBsp.Name = "btnLoadBsp";
            btnLoadBsp.Size = new Size(116, 38);
            btnLoadBsp.TabIndex = 3;
            btnLoadBsp.Text = "Load Map";
            btnLoadBsp.UseVisualStyleBackColor = true;
            btnLoadBsp.Click += btnLoadBsp_Click;
            // 
            // lstBsps
            // 
            lstBsps.FormattingEnabled = true;
            lstBsps.Location = new Point(41, 66);
            lstBsps.Name = "lstBsps";
            lstBsps.Size = new Size(403, 229);
            lstBsps.TabIndex = 4;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(578, 311);
            Controls.Add(lstBsps);
            Controls.Add(btnLoadBsp);
            Controls.Add(txtPath);
            Controls.Add(btnLoadPak);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Quake 2 Map Loader";
            Load += frmMain_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnLoadPak;
        private TextBox txtPath;
        private Button btnLoadBsp;
        private ListBox lstBsps;
    }
}
