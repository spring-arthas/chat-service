using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service.file
{
    public class DataGridViewProgressBarCell : DataGridViewTextBoxCell
    {
        private int mimimumValue;

        private int maximumValue;

        public DataGridViewProgressBarCell()
        {
            this.maximumValue = 100;
            this.mimimumValue = 0;
        }

        public int Maximum
        {
            get
            {
                return this.maximumValue;
            }
            set
            {
                this.maximumValue = value;
            }
        }

        public int Mimimum
        {
            get
            {
                return this.mimimumValue;
            }
            set
            {
                this.mimimumValue = value;
            }
        }

        public override Type ValueType
        {
            get
            {
                return typeof(int);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                return 0;
            }
        }

        public override object Clone()
        {
            DataGridViewProgressBarCell cell = (DataGridViewProgressBarCell)base.Clone();
            cell.Maximum = this.Maximum;
            cell.Mimimum = this.Mimimum;
            return cell;
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
            int intValue = 0;
            if (value is int)
            {
                intValue = (int)value;
            }

            if (intValue < this.mimimumValue)
            {
                intValue = this.mimimumValue;
            }

            if (intValue > this.maximumValue)
            {
                intValue = this.maximumValue;
            }
                
            double rate = (double)(intValue - this.mimimumValue) / (this.maximumValue - this.mimimumValue);
            if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border)
            {
                this.PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
            }

            Rectangle borderRect = this.BorderWidths(advancedBorderStyle);
            Rectangle paintRect = new Rectangle( cellBounds.Left + borderRect.Left,
                cellBounds.Top + borderRect.Top,
                cellBounds.Width - borderRect.Right,
                cellBounds.Height - borderRect.Bottom);
            bool isSelected = (cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            Color bkColor;
            if (isSelected && (paintParts & DataGridViewPaintParts.SelectionBackground) == DataGridViewPaintParts.SelectionBackground)
            {
                bkColor = cellStyle.SelectionBackColor;
            }
            else
            {
                bkColor = cellStyle.BackColor;
            }
            if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background)
            {
                using (SolidBrush backBrush = new SolidBrush(bkColor))
                {
                    graphics.FillRectangle(backBrush, paintRect);
                }
            }
            paintRect.Offset(cellStyle.Padding.Right, cellStyle.Padding.Top);
            paintRect.Width -= cellStyle.Padding.Horizontal;
            paintRect.Height -= cellStyle.Padding.Vertical;
            if ((paintParts & DataGridViewPaintParts.ContentForeground) == DataGridViewPaintParts.ContentForeground)
            {
                if (ProgressBarRenderer.IsSupported)
                {
                    ProgressBarRenderer.DrawHorizontalBar(graphics, paintRect);
                    Rectangle barBounds = new Rectangle(
                        paintRect.Left + 3, paintRect.Top + 3,
                        paintRect.Width - 4, paintRect.Height - 6);
                    barBounds.Width = (int)Math.Round(barBounds.Width * rate);
                    ProgressBarRenderer.DrawHorizontalChunks(graphics, barBounds);
                }
                else
                {
                    graphics.FillRectangle(Brushes.White, paintRect);
                    graphics.DrawRectangle(Pens.Black, paintRect);
                    Rectangle barBounds = new Rectangle(
                        paintRect.Left + 1, paintRect.Top + 1,
                        paintRect.Width - 1, paintRect.Height - 1);
                    barBounds.Width = (int)Math.Round(barBounds.Width * rate);
                    graphics.FillRectangle(Brushes.Blue, barBounds);
                }
            }
            if (this.DataGridView.CurrentCellAddress.X == this.ColumnIndex &&
                    this.DataGridView.CurrentCellAddress.Y == this.RowIndex &&
                    (paintParts & DataGridViewPaintParts.Focus) ==
                        DataGridViewPaintParts.Focus &&
                    this.DataGridView.Focused)
            {

                Rectangle focusRect = paintRect;
                focusRect.Inflate(-3, -3);
                ControlPaint.DrawFocusRectangle(graphics, focusRect);
            }
            if ((paintParts & DataGridViewPaintParts.ContentForeground) == DataGridViewPaintParts.ContentForeground)
            {

                string txt = string.Format("{0}%", Math.Round(rate * 100));

                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                Color fColor = cellStyle.ForeColor;
                paintRect.Inflate(-2, -2);
                TextRenderer.DrawText(graphics, txt, cellStyle.Font,
                    paintRect, fColor, flags);
            }
            if ((paintParts & DataGridViewPaintParts.ErrorIcon) == DataGridViewPaintParts.ErrorIcon
                && this.DataGridView.ShowCellErrors
                && !string.IsNullOrEmpty(errorText))
            {
                Rectangle iconBounds = this.GetErrorIconBounds(graphics, cellStyle, rowIndex);
                iconBounds.Offset(cellBounds.X, cellBounds.Y);
                this.PaintErrorIcon(graphics, iconBounds, cellBounds, errorText);
            }
        }

    }
}
