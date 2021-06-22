using System.ComponentModel;

namespace FontReader
{
    sealed partial class GlyphView
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
            this.characterBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.collapseButton = new System.Windows.Forms.Button();
            this.gravityButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // characterBox
            // 
            this.characterBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.characterBox.Location = new System.Drawing.Point(88, 342);
            this.characterBox.Name = "characterBox";
            this.characterBox.Size = new System.Drawing.Size(150, 20);
            this.characterBox.TabIndex = 1;
            this.characterBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.characterBox_PreviewKeyDown);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label1.Location = new System.Drawing.Point(12, 345);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 21);
            this.label1.TabIndex = 3;
            this.label1.Text = "Character";
            // 
            // collapseButton
            // 
            this.collapseButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.collapseButton.Location = new System.Drawing.Point(400, 340);
            this.collapseButton.Name = "collapseButton";
            this.collapseButton.Size = new System.Drawing.Size(75, 23);
            this.collapseButton.TabIndex = 4;
            this.collapseButton.Text = "Collapse";
            this.collapseButton.UseVisualStyleBackColor = true;
            this.collapseButton.Click += new System.EventHandler(this.collapseButton_Click);
            // 
            // gravityButton
            // 
            this.gravityButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.gravityButton.Location = new System.Drawing.Point(319, 340);
            this.gravityButton.Name = "gravityButton";
            this.gravityButton.Size = new System.Drawing.Size(75, 23);
            this.gravityButton.TabIndex = 5;
            this.gravityButton.Text = "Gravity";
            this.gravityButton.UseVisualStyleBackColor = true;
            this.gravityButton.Click += new System.EventHandler(this.gravityButton_Click);
            // 
            // GlyphView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 378);
            this.Controls.Add(this.gravityButton);
            this.Controls.Add(this.collapseButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.characterBox);
            this.Name = "GlyphView";
            this.Text = "GlyphView";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button gravityButton;

        private System.Windows.Forms.Button collapseButton;

        private System.Windows.Forms.Button button1;

        private System.Windows.Forms.TextBox characterBox;
        private System.Windows.Forms.Label label1;

        #endregion
    }
}