namespace Corecalc.GUI
{
    partial class SdfForm
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
          this.functionsListBox = new System.Windows.Forms.ListBox();
          this.tipLabel = new System.Windows.Forms.Label();
          this.errorLabel = new System.Windows.Forms.Label();
          this.button4 = new System.Windows.Forms.Button();
          this.panel1 = new System.Windows.Forms.Panel();
          this.panel1.SuspendLayout();
          this.SuspendLayout();
          // 
          // functionsListBox
          // 
          this.functionsListBox.Dock = System.Windows.Forms.DockStyle.Top;
          this.functionsListBox.FormattingEnabled = true;
          this.functionsListBox.Location = new System.Drawing.Point(0, 0);
          this.functionsListBox.Name = "functionsListBox";
          this.functionsListBox.Size = new System.Drawing.Size(485, 329);
          this.functionsListBox.TabIndex = 0;
          this.functionsListBox.DoubleClick += new System.EventHandler(this.functionsListbox_DoubleClick);
          // 
          // tipLabel
          // 
          this.tipLabel.Location = new System.Drawing.Point(0, 0);
          this.tipLabel.Name = "tipLabel";
          this.tipLabel.Size = new System.Drawing.Size(100, 23);
          this.tipLabel.TabIndex = 0;
          // 
          // errorLabel
          // 
          this.errorLabel.AutoSize = true;
          this.errorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.errorLabel.ForeColor = System.Drawing.Color.DarkRed;
          this.errorLabel.Location = new System.Drawing.Point(5, 323);
          this.errorLabel.Name = "errorLabel";
          this.errorLabel.Size = new System.Drawing.Size(0, 16);
          this.errorLabel.TabIndex = 12;
          // 
          // button4
          // 
          this.button4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.button4.Location = new System.Drawing.Point(177, 18);
          this.button4.Name = "button4";
          this.button4.Size = new System.Drawing.Size(126, 23);
          this.button4.TabIndex = 14;
          this.button4.Text = "Show bytecode";
          this.button4.UseVisualStyleBackColor = true;
          this.button4.Click += new System.EventHandler(this.ShowBytecode_Click);
          // 
          // panel1
          // 
          this.panel1.Controls.Add(this.button4);
          this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
          this.panel1.Location = new System.Drawing.Point(0, 329);
          this.panel1.Name = "panel1";
          this.panel1.Size = new System.Drawing.Size(485, 55);
          this.panel1.TabIndex = 15;
          // 
          // SDF
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.ClientSize = new System.Drawing.Size(485, 382);
          this.Controls.Add(this.panel1);
          this.Controls.Add(this.errorLabel);
          this.Controls.Add(this.functionsListBox);
          this.Name = "SDF";
          this.Text = "Sheet defined functions";
          this.panel1.ResumeLayout(false);
          this.ResumeLayout(false);
          this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox functionsListBox;
        private System.Windows.Forms.Label tipLabel;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Panel panel1;
    }
}