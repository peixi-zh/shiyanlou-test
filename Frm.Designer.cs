namespace 包装队列显示
{
    partial class Frm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnClearOne = new System.Windows.Forms.Button();
            this.ChkAT500 = new System.Windows.Forms.CheckBox();
            this.btnUnlock = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 19;
            this.listBox1.Location = new System.Drawing.Point(3, 4);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(473, 194);
            this.listBox1.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.Location = new System.Drawing.Point(419, 143);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(35, 35);
            this.button1.TabIndex = 2;
            this.button1.Text = "单";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(328, 154);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "包装方式:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(295, 103);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(119, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "手动移除队列:";
            // 
            // btnClearOne
            // 
            this.btnClearOne.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClearOne.Location = new System.Drawing.Point(419, 92);
            this.btnClearOne.Name = "btnClearOne";
            this.btnClearOne.Size = new System.Drawing.Size(35, 35);
            this.btnClearOne.TabIndex = 6;
            this.btnClearOne.Text = "删";
            this.btnClearOne.UseVisualStyleBackColor = true;
            this.btnClearOne.Click += new System.EventHandler(this.btnClearOne_Click);
            // 
            // ChkAT500
            // 
            this.ChkAT500.AutoSize = true;
            this.ChkAT500.Location = new System.Drawing.Point(12, 154);
            this.ChkAT500.Name = "ChkAT500";
            this.ChkAT500.Size = new System.Drawing.Size(48, 16);
            this.ChkAT500.TabIndex = 8;
            this.ChkAT500.Text = "Pick";
            this.ChkAT500.UseVisualStyleBackColor = true;
            this.ChkAT500.CheckedChanged += new System.EventHandler(this.ChkAT500_CheckedChanged);
            // 
            // btnUnlock
            // 
            this.btnUnlock.Location = new System.Drawing.Point(81, 150);
            this.btnUnlock.Name = "btnUnlock";
            this.btnUnlock.Size = new System.Drawing.Size(75, 23);
            this.btnUnlock.TabIndex = 9;
            this.btnUnlock.Text = "解除报警";
            this.btnUnlock.UseVisualStyleBackColor = true;
            this.btnUnlock.Click += new System.EventHandler(this.btnUnlock_Click);
            // 
            // Frm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(476, 203);
            this.Controls.Add(this.btnUnlock);
            this.Controls.Add(this.ChkAT500);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnClearOne);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listBox1);
            this.Name = "Frm";
            this.Text = "包装队列信息";
            this.Load += new System.EventHandler(this.Frm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnClearOne;
        private System.Windows.Forms.CheckBox ChkAT500;
        private System.Windows.Forms.Button btnUnlock;
        private System.Windows.Forms.Timer timer1;
    }
}

