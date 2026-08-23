using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace chat_service
{
    /// <summary>
    /// 现代化进度条单元格：圆角进度条 + 百分比文字。
    /// 会解析单元格值（可为 int 或类似 "100"/"100%" 的字符串）。
    /// </summary>
    public class ModernProgressBarCell : DataGridViewTextBoxCell
    {
        public ModernProgressBarCell()
        {
        }

        public override object Clone()
        {
            return (ModernProgressBarCell)base.Clone();
        }

        private static int ParsePercent(object value)
        {
            if (value == null) return 0;
            string s = Convert.ToString(value).Trim();
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Replace("%", "").Trim();
            int pct;
            if (!int.TryParse(s, out pct)) return 0;
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 2)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            bool selected = (cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            Color back = selected ? cellStyle.SelectionBackColor : cellStyle.BackColor;

            if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background)
            {
                using (SolidBrush brush = new SolidBrush(back)) graphics.FillRectangle(brush, cellBounds);
            }
            if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border)
            {
                this.PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
            }

            int pct = ParsePercent(value);
            int pad = 5;
            int trackH = 10;
            Rectangle inner = new Rectangle(
                cellBounds.X + pad,
                cellBounds.Y + (cellBounds.Height - trackH) / 2,
                cellBounds.Width - pad * 2,
                trackH);
            if (inner.Width < 12) return;

            // 轨道
            using (GraphicsPath track = RoundedRect(inner, trackH / 2))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(235, 238, 245)))
            {
                graphics.FillPath(trackBrush, track);
            }

            // 填充
            int fillW = (int)Math.Round(inner.Width * pct / 100.0);
            if (fillW > 4)
            {
                Rectangle fill = new Rectangle(inner.X, inner.Y, fillW, inner.Height);
                using (GraphicsPath fillPath = RoundedRect(fill, trackH / 2))
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    fill, Color.FromArgb(64, 158, 255), Color.FromArgb(36, 107, 253), LinearGradientMode.Horizontal))
                {
                    graphics.FillPath(fillBrush, fillPath);
                }
            }

            // 百分比文字（覆盖轨道超过一半时用白色）
            if ((paintParts & DataGridViewPaintParts.ContentForeground) == DataGridViewPaintParts.ContentForeground)
            {
                string text = pct + "%";
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                Color textColor = fillW >= inner.Width * 0.5 ? Color.White : Color.FromArgb(48, 49, 51);
                TextRenderer.DrawText(graphics, text, cellStyle.Font, cellBounds, textColor, flags);
            }
        }
    }

    /// <summary>现代化进度条列。</summary>
    public class ModernProgressBarColumn : DataGridViewColumn
    {
        public ModernProgressBarColumn() : base(new ModernProgressBarCell())
        {
        }
    }

    /// <summary>
    /// 状态图标单元格：按状态文字绘制彩色图标 + 状态文字（成功绿 / 进行中蓝转圈 / 失败红 / 等待灰）。
    /// </summary>
    public class StatusIconCell : DataGridViewTextBoxCell
    {
        public StatusIconCell()
        {
        }

        public override object Clone()
        {
            return (StatusIconCell)base.Clone();
        }

        private enum StatusKind { Success, Running, Failed, Pending, Unknown }

        private static StatusKind Classify(string status, out Color color)
        {
            status = status ?? string.Empty;
            if (status.Contains("成功") || status.Contains("完成") || status.Contains("已上传") || status.Contains("已下载"))
            {
                color = Color.FromArgb(22, 149, 91); // 成功绿
                return StatusKind.Success;
            }
            if (status.Contains("失败") || status.Contains("错误") || status.Contains("异常"))
            {
                color = Color.FromArgb(224, 74, 74); // 失败红
                return StatusKind.Failed;
            }
            if (status.Contains("中...") || status.Contains("中…") || status.Contains("进行") || status.Contains("上传中") || status.Contains("下载中"))
            {
                color = Color.FromArgb(36, 107, 253); // 进行中蓝
                return StatusKind.Running;
            }
            if (status.Contains("待") || status.Contains("排队") || status.Contains("未上传"))
            {
                color = Color.FromArgb(144, 147, 153); // 等待灰
                return StatusKind.Pending;
            }
            color = Color.FromArgb(144, 147, 153);
            return StatusKind.Unknown;
        }

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            bool selected = (cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            Color back = selected ? cellStyle.SelectionBackColor : cellStyle.BackColor;

            if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background)
            {
                using (SolidBrush brush = new SolidBrush(back)) graphics.FillRectangle(brush, cellBounds);
            }
            if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border)
            {
                this.PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
            }

            string status = Convert.ToString(value) ?? "";
            Color color;
            StatusKind kind = Classify(status, out color);

            if ((paintParts & DataGridViewPaintParts.ContentForeground) == DataGridViewPaintParts.ContentForeground)
            {
                int iconSize = 14;
                int iconX = cellBounds.X + 8;
                int iconY = cellBounds.Y + (cellBounds.Height - iconSize) / 2;
                Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);

                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                switch (kind)
                {
                    case StatusKind.Success:
                        using (SolidBrush b = new SolidBrush(color)) graphics.FillEllipse(b, iconRect);
                        using (Pen p = new Pen(Color.White, 2f))
                        {
                            Point[] pts = { new Point(iconRect.X + 3, iconRect.Y + 7), new Point(iconRect.X + 6, iconRect.Y + 10), new Point(iconRect.X + 11, iconRect.Y + 4) };
                            graphics.DrawLines(p, pts);
                        }
                        break;
                    case StatusKind.Failed:
                        using (SolidBrush b = new SolidBrush(color)) graphics.FillEllipse(b, iconRect);
                        using (Pen p = new Pen(Color.White, 2f))
                        {
                            graphics.DrawLine(p, iconRect.X + 4, iconRect.Y + 4, iconRect.X + 10, iconRect.Y + 10);
                            graphics.DrawLine(p, iconRect.X + 10, iconRect.Y + 4, iconRect.X + 4, iconRect.Y + 10);
                        }
                        break;
                    case StatusKind.Running:
                        using (Pen p = new Pen(color, 2f))
                            graphics.DrawArc(p, iconRect.X + 2, iconRect.Y + 2, iconSize - 4, iconSize - 4, -90, 270);
                        break;
                    case StatusKind.Pending:
                    case StatusKind.Unknown:
                    default:
                        using (SolidBrush b = new SolidBrush(color)) graphics.FillEllipse(b, iconRect);
                        using (Pen p = new Pen(Color.White, 2f))
                            graphics.DrawLine(p, iconRect.X + 4, iconRect.Y + 7, iconRect.X + 10, iconRect.Y + 7);
                        break;
                }

                Rectangle textRect = new Rectangle(
                    iconRect.Right + 4, cellBounds.Y,
                    Math.Max(0, cellBounds.Right - iconRect.Right - 4), cellBounds.Height);
                TextRenderer.DrawText(graphics, status, cellStyle.Font, textRect, color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
        }
    }

    /// <summary>状态图标列。</summary>
    public class StatusIconColumn : DataGridViewColumn
    {
        public StatusIconColumn() : base(new StatusIconCell())
        {
        }
    }
}
