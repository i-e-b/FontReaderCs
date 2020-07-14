using System.ComponentModel;

namespace FontReader
{
    partial class GlyphView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.glyphDisplay = new System.Windows.Forms.PictureBox();
            this.characterBox = new System.Windows.Forms.TextBox();
            this.pickButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.glyphDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // glyphDisplay
            // 
            this.glyphDisplay.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.glyphDisplay.Location = new System.Drawing.Point(12, 12);
            this.glyphDisplay.Name = "glyphDisplay";
            this.glyphDisplay.Size = new System.Drawing.Size(463, 314);
            this.glyphDisplay.TabIndex = 0;
            this.glyphDisplay.TabStop = false;
            // 
            // characterBox
            // 
            this.characterBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.characterBox.Location = new System.Drawing.Point(79, 345);
            this.characterBox.Name = "characterBox";
            this.characterBox.Size = new System.Drawing.Size(312, 20);
            this.characterBox.TabIndex = 1;
            // 
            // pickButton
            // 
            this.pickButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pickButton.Location = new System.Drawing.Point(397, 337);
            this.pickButton.Name = "pickButton";
            this.pickButton.Size = new System.Drawing.Size(78, 29);
            this.pickButton.TabIndex = 2;
            this.pickButton.Text = "Pick";
            this.pickButton.UseVisualStyleBackColor = true;
            this.pickButton.Click += new System.EventHandler(this.pickButton_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(12, 348);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 21);
            this.label1.TabIndex = 3;
            this.label1.Text = "Character";
            // 
            // GlyphView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 378);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pickButton);
            this.Controls.Add(this.characterBox);
            this.Controls.Add(this.glyphDisplay);
            this.Name = "GlyphView";
            this.Text = "GlyphView";
            ((System.ComponentModel.ISupportInitialize) (this.glyphDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox characterBox;
        private System.Windows.Forms.PictureBox glyphDisplay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button pickButton;

        #endregion
    }
}