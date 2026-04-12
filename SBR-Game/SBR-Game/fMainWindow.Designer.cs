using OpenTK.GLControl;

namespace SBR_Game
{
    partial class fMainWindow
    {
        private System.ComponentModel.IContainer components = null!;
        private GLControl glControl = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(fMainWindow));
            glControl = new GLControl();
            _updateTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // glControl
            // 
            glControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            glControl.APIVersion = new Version(3, 3);
            glControl.Dock = DockStyle.Fill;
            glControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            glControl.IsEventDriven = true;
            glControl.Location = new Point(0, 0);
            glControl.Name = "glControl";
            glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            glControl.SharedContext = null;
            glControl.Size = new Size(1280, 720);
            glControl.TabIndex = 0;
            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            // 
            // fMainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 720);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            Name = "fMainWindow";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SBR Game";
            WindowState = FormWindowState.Maximized;
            ResumeLayout(false);
        }
    }
}