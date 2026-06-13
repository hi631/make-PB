namespace pcb2stl
{
    partial class Form1
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
		private void InitializeComponent() {
			button1 = new Button();
			text0 = new TextBox();
			text2 = new TextBox();
			text1 = new TextBox();
			SuspendLayout();
			// 
			// button1
			// 
			button1.Location = new Point(617, 11);
			button1.Name = "button1";
			button1.Size = new Size(75, 33);
			button1.TabIndex = 0;
			button1.Text = "make_stl";
			button1.UseVisualStyleBackColor = true;
			button1.Click += button1_Click;
			// 
			// text0
			// 
			text0.Font = new Font("ＭＳ ゴシック", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
			text0.Location = new Point(12, 17);
			text0.Name = "text0";
			text0.Size = new Size(575, 20);
			text0.TabIndex = 1;
			text0.Text = "C:\\xxxx\\test2\\test.kicad_pcb";
			// 
			// text2
			// 
			text2.Font = new Font("ＭＳ ゴシック", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
			text2.Location = new Point(12, 82);
			text2.Multiline = true;
			text2.Name = "text2";
			text2.ScrollBars = ScrollBars.Vertical;
			text2.Size = new Size(575, 356);
			text2.TabIndex = 2;
			// 
			// text1
			// 
			text1.Font = new Font("ＭＳ ゴシック", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
			text1.Location = new Point(12, 43);
			text1.Name = "text1";
			text1.Size = new Size(575, 20);
			text1.TabIndex = 5;
			text1.Text = "C:\\Users\\xxxx\\Desktop\\pcb.stl";
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(704, 450);
			Controls.Add(text1);
			Controls.Add(text2);
			Controls.Add(text0);
			Controls.Add(button1);
			Name = "Form1";
			Text = "Form1";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button button1;
		private TextBox text0;
		private TextBox text2;
		private TextBox text1;
	}
}
