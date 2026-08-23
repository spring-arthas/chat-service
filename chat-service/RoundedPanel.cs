using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace chat_service
{
    /// <summary>用于主界面卡片的轻量圆角容器，不改变内部控件的行为。</summary>
    internal class RoundedPanel : Panel
    {
        private int cornerRadius = 18;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = value; }
        }

        public RoundedPanel()
        {
            DoubleBuffered = true;
            Resize += delegate { UpdateRoundedRegion(); };
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRoundedRegion();
        }

        private void UpdateRoundedRegion()
        {
            if (Width < 2 || Height < 2) return;
            int diameter = System.Math.Min(CornerRadius * 2, System.Math.Min(Width, Height));
            using (GraphicsPath path = new GraphicsPath())
            {
                Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                Region = new Region(path);
            }
        }
    }
}
