namespace Client
{
    partial class Roll
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
            this.rollButton1 = new System.Windows.Forms.Button();
            this.rollButton2 = new System.Windows.Forms.Button();
            this.dicePictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.dicePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // rollButton1
            // 
            this.rollButton1.Location = new System.Drawing.Point(23, 128);
            this.rollButton1.Name = "rollButton1";
            this.rollButton1.Size = new System.Drawing.Size(94, 31);
            this.rollButton1.TabIndex = 0;
            this.rollButton1.Text = "小心微摇";
            this.rollButton1.UseVisualStyleBackColor = true;
            this.rollButton1.Click += new System.EventHandler(this.rollButton1_Click);
            // 
            // rollButton2
            // 
            this.rollButton2.Location = new System.Drawing.Point(141, 128);
            this.rollButton2.Name = "rollButton2";
            this.rollButton2.Size = new System.Drawing.Size(99, 31);
            this.rollButton2.TabIndex = 1;
            this.rollButton2.Text = "大力猛摇";
            this.rollButton2.UseVisualStyleBackColor = true;
            this.rollButton2.Click += new System.EventHandler(this.rollButton2_Click);
            // 
            // dicePictureBox
            // 
            this.dicePictureBox.Location = new System.Drawing.Point(67, 25);
            this.dicePictureBox.Name = "dicePictureBox";
            this.dicePictureBox.Size = new System.Drawing.Size(115, 83);
            this.dicePictureBox.TabIndex = 2;
            this.dicePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.dicePictureBox.TabStop = false;
            // 
            // Roll
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(252, 195);
            this.Controls.Add(this.dicePictureBox);
            this.Controls.Add(this.rollButton2);
            this.Controls.Add(this.rollButton1);
            this.Name = "Roll";
            this.Text = "Roll";
            ((System.ComponentModel.ISupportInitialize)(this.dicePictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button rollButton1;
        private System.Windows.Forms.Button rollButton2;
        private System.Windows.Forms.PictureBox dicePictureBox;
    }
}