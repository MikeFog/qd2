namespace FogSoft.WinForm.Forms
{
	partial class MessageBox
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
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.rtbinformation = new System.Windows.Forms.RichTextBox();
            this.btnOne = new System.Windows.Forms.Button();
            this.btnTwo = new System.Windows.Forms.Button();
            this.lblText = new System.Windows.Forms.Label();
            this.llMoreInfo = new System.Windows.Forms.LinkLabel();
            this.llCopy = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Image = global::FogSoft.WinForm.Properties.Resources.Information;
            this.pictureBox.Location = new System.Drawing.Point(14, 14);
            this.pictureBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(62, 62);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // rtbinformation
            // 
            this.rtbinformation.BackColor = System.Drawing.SystemColors.Control;
            this.rtbinformation.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.rtbinformation.Location = new System.Drawing.Point(14, 163);
            this.rtbinformation.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.rtbinformation.Name = "rtbinformation";
            this.rtbinformation.ReadOnly = true;
            this.rtbinformation.Size = new System.Drawing.Size(598, 126);
            this.rtbinformation.TabIndex = 0;
            this.rtbinformation.Text = "";
            // 
            // btnOne
            // 
            this.btnOne.Location = new System.Drawing.Point(430, 113);
            this.btnOne.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOne.Name = "btnOne";
            this.btnOne.Size = new System.Drawing.Size(88, 27);
            this.btnOne.TabIndex = 1;
            this.btnOne.Text = "btnOne";
            this.btnOne.UseVisualStyleBackColor = true;
            this.btnOne.Click += new System.EventHandler(this.btnOne_Click);
            // 
            // btnTwo
            // 
            this.btnTwo.Location = new System.Drawing.Point(525, 113);
            this.btnTwo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnTwo.Name = "btnTwo";
            this.btnTwo.Size = new System.Drawing.Size(88, 27);
            this.btnTwo.TabIndex = 2;
            this.btnTwo.Text = "btnTwo";
            this.btnTwo.UseVisualStyleBackColor = true;
            this.btnTwo.Click += new System.EventHandler(this.btnTwo_Click);
            // 
            // lblText
            // 
            this.lblText.Location = new System.Drawing.Point(93, 43);
            this.lblText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(519, 55);
            this.lblText.TabIndex = 3;
            this.lblText.Text = "Текст сообщения";
            // 
            // llMoreInfo
            // 
            this.llMoreInfo.AutoSize = true;
            this.llMoreInfo.Location = new System.Drawing.Point(14, 119);
            this.llMoreInfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.llMoreInfo.Name = "llMoreInfo";
            this.llMoreInfo.Size = new System.Drawing.Size(88, 15);
            this.llMoreInfo.TabIndex = 4;
            this.llMoreInfo.TabStop = true;
            this.llMoreInfo.Text = "Подробнее >>";
            this.llMoreInfo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llMoreInfo_LinkClicked);
            // 
            // llCopy
            // 
            this.llCopy.AutoSize = true;
            this.llCopy.Location = new System.Drawing.Point(429, 10);
            this.llCopy.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.llCopy.Name = "llCopy";
            this.llCopy.Size = new System.Drawing.Size(171, 15);
            this.llCopy.TabIndex = 5;
            this.llCopy.TabStop = true;
            this.llCopy.Text = "Скопировать в буфер обмена";
            this.llCopy.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llCopy_LinkClicked);
            // 
            // MessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(626, 153);
            this.Controls.Add(this.llCopy);
            this.Controls.Add(this.llMoreInfo);
            this.Controls.Add(this.lblText);
            this.Controls.Add(this.btnTwo);
            this.Controls.Add(this.btnOne);
            this.Controls.Add(this.rtbinformation);
            this.Controls.Add(this.pictureBox);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MessageBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Сообщение";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.RichTextBox rtbinformation;
		private System.Windows.Forms.Button btnOne;
		private System.Windows.Forms.Button btnTwo;
		private System.Windows.Forms.Label lblText;
		private System.Windows.Forms.LinkLabel llMoreInfo;
		private System.Windows.Forms.LinkLabel llCopy;
	}
}