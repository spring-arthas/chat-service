using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace chat_service
{
    internal class ConversationListCanvas : Panel
    {
        private readonly List<string> contacts = new List<string>();
        private string selectedContact = string.Empty;

        public event EventHandler<string> ContactSelected;

        public ConversationListCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(248, 252, 255);
            Cursor = Cursors.Hand;
        }

        public void SetContacts(IEnumerable<string> values, string selected)
        {
            contacts.Clear();
            if (values != null)
            {
                foreach (string value in values)
                {
                    if (!string.IsNullOrWhiteSpace(value) && !contacts.Contains(value)) contacts.Add(value);
                }
            }
            selectedContact = selected ?? string.Empty;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (contacts.Count == 0)
            {
                TextRenderer.DrawText(e.Graphics, "暂无在线好友", new Font("微软雅黑", 9F), new Rectangle(0, 26, Width, 28), Color.FromArgb(140, 157, 177), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            for (int i = 0; i < contacts.Count; i++)
            {
                int top = i * 66;
                Rectangle row = new Rectangle(0, top, Width - 2, 60);
                bool selected = contacts[i] == selectedContact;
                if (selected)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(218, 238, 255)))
                        e.Graphics.FillRectangle(brush, row);
                }

                DrawAvatar(e.Graphics, new Rectangle(12, top + 10, 40, 40), contacts[i], i);
                TextRenderer.DrawText(e.Graphics, contacts[i], new Font("微软雅黑", 10.5F, FontStyle.Bold), new Rectangle(66, top + 9, Width - 128, 22), Color.FromArgb(30, 47, 70), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, selected ? "正在聊天" : "在线", new Font("微软雅黑", 8.5F), new Rectangle(66, top + 32, Width - 132, 18), Color.FromArgb(113, 136, 161), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, selected ? "刚刚" : "", new Font("Segoe UI", 8F), new Rectangle(Width - 54, top + 11, 44, 18), Color.FromArgb(136, 153, 173), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = e.Y / 66;
            if (index < 0 || index >= contacts.Count) return;
            selectedContact = contacts[index];
            Invalidate();
            EventHandler<string> handler = ContactSelected;
            if (handler != null) handler(this, selectedContact);
        }

        private static void DrawAvatar(Graphics graphics, Rectangle bounds, string name, int index)
        {
            Color[] colors = { Color.FromArgb(246, 164, 37), Color.FromArgb(74, 150, 244), Color.FromArgb(103, 183, 139), Color.FromArgb(173, 125, 222) };
            using (SolidBrush brush = new SolidBrush(colors[index % colors.Length]))
                graphics.FillEllipse(brush, bounds);
            string initials = string.IsNullOrEmpty(name) ? "友" : name.Substring(0, 1);
            TextRenderer.DrawText(graphics, initials, new Font("微软雅黑", 13F, FontStyle.Bold), bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal class ChatMessageCanvas : Panel
    {
        private readonly List<string> messages = new List<string>();

        public ChatMessageCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(244, 249, 254);
        }

        public void SetTranscript(string text)
        {
            messages.Clear();
            string[] lines = (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string value = line.Trim();
                if (!string.IsNullOrEmpty(value)) messages.Add(value);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawBackdrop(e.Graphics);
            int top = 36;
            foreach (string message in messages)
            {
                bool outgoing = message.Contains("向 [") || message.Contains("发送:");
                DrawBubble(e.Graphics, message, outgoing, top);
                top += 66;
                if (top > Height - 20) break;
            }
        }

        private void DrawBackdrop(Graphics graphics)
        {
            using (Pen pen = new Pen(Color.FromArgb(226, 239, 251), 2F))
            {
                graphics.DrawArc(pen, -Width / 4, Height / 4, Width + 220, Height, 195, 135);
                graphics.DrawArc(pen, Width / 4, -Height / 3, Width, Height + 180, 28, 120);
            }
        }

        private void DrawBubble(Graphics graphics, string text, bool outgoing, int top)
        {
            int maxWidth = Math.Max(180, Width * 2 / 5);
            Size measured = TextRenderer.MeasureText(text, new Font("微软雅黑", 9.5F), new Size(maxWidth - 28, 120), TextFormatFlags.WordBreak);
            int bubbleWidth = Math.Min(maxWidth, Math.Max(92, measured.Width + 28));
            int bubbleHeight = Math.Max(38, measured.Height + 16);
            int left = outgoing ? Width - bubbleWidth - 64 : 60;
            Rectangle bubble = new Rectangle(left, top, bubbleWidth, bubbleHeight);
            Rectangle avatar = outgoing ? new Rectangle(Width - 48, top, 34, 34) : new Rectangle(14, top, 34, 34);
            using (GraphicsPath path = CreateRoundPath(bubble, 12))
            using (SolidBrush brush = new SolidBrush(outgoing ? Color.FromArgb(22, 132, 245) : Color.White))
            {
                graphics.FillPath(brush, path);
            }
            using (SolidBrush brush = new SolidBrush(outgoing ? Color.FromArgb(55, 135, 240) : Color.FromArgb(93, 128, 166)))
                graphics.FillEllipse(brush, avatar);
            TextRenderer.DrawText(graphics, outgoing ? "我" : "友", new Font("微软雅黑", 10F, FontStyle.Bold), avatar, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(graphics, text, new Font("微软雅黑", 9.5F), new Rectangle(bubble.Left + 14, bubble.Top + 8, bubble.Width - 28, bubble.Height - 12), outgoing ? Color.White : Color.FromArgb(40, 55, 75), TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath CreateRoundPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int size = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, size, size, 180, 90);
            path.AddArc(bounds.Right - size, bounds.Top, size, size, 270, 90);
            path.AddArc(bounds.Right - size, bounds.Bottom - size, size, size, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - size, size, size, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
