using System.Drawing;
using System.Windows.Forms;

namespace chat_service
{
    /// <summary>
    /// 全局 UI 主题助手：浅色现代化风格（Element 配色）。
    /// 仅调整控件外观，不影响任何业务逻辑；在 Main_Form 构造函数中调用 ApplyTheme 即可。
    /// </summary>
    public static class UiTheme
    {
        // ==================== 配色 ====================
        /// <summary>主色（蓝）</summary>
        public static readonly Color Primary = Color.FromArgb(36, 107, 253);
        /// <summary>主色 - 按下态（深蓝）</summary>
        public static readonly Color PrimaryDown = Color.FromArgb(28, 83, 202);
        /// <summary>主色 - 悬停态（亮蓝）</summary>
        public static readonly Color PrimaryHover = Color.FromArgb(57, 124, 255);
        /// <summary>成功（绿）</summary>
        public static readonly Color Success = Color.FromArgb(22, 149, 91);
        /// <summary>成功 - 悬停</summary>
        public static readonly Color SuccessHover = Color.FromArgb(125, 206, 80);
        /// <summary>危险（红）</summary>
        public static readonly Color Danger = Color.FromArgb(224, 74, 74);
        /// <summary>危险 - 悬停</summary>
        public static readonly Color DangerHover = Color.FromArgb(247, 130, 130);
        /// <summary>主文字</summary>
        public static readonly Color TextMain = Color.FromArgb(23, 32, 51);
        /// <summary>次要文字</summary>
        public static readonly Color TextSecondary = Color.FromArgb(120, 132, 153);
        /// <summary>边框</summary>
        public static readonly Color Border = Color.FromArgb(222, 228, 237);
        /// <summary>面板底色（窗体背景）</summary>
        public static readonly Color Panel = Color.FromArgb(246, 248, 251);
        /// <summary>表格表头底色</summary>
        public static readonly Color TableHeader = Color.FromArgb(248, 250, 253);
        /// <summary>表格斑马纹</summary>
        public static readonly Color RowAlt = Color.FromArgb(250, 251, 252);
        /// <summary>表格网格线</summary>
        public static readonly Color GridLine = Color.FromArgb(235, 238, 245);
        /// <summary>表格选中底色（浅蓝）</summary>
        public static readonly Color SelectionLight = Color.FromArgb(236, 245, 255);
        /// <summary>默认按钮底色</summary>
        public static readonly Color ButtonDefault = Color.FromArgb(244, 244, 245);
        /// <summary>默认按钮悬停</summary>
        public static readonly Color ButtonDefaultHover = Color.FromArgb(235, 238, 245);

        // ==================== 字体 ====================
        public static readonly Font FontBody = new Font("微软雅黑", 10.5F);
        public static readonly Font FontTitle = new Font("微软雅黑", 12F, FontStyle.Bold);

        /// <summary>按钮风格</summary>
        public enum Kind
        {
            /// <summary>主操作（蓝底白字）</summary>
            Primary,
            /// <summary>成功类操作（绿底白字）</summary>
            Success,
            /// <summary>危险类操作（红底白字）</summary>
            Danger,
            /// <summary>普通按钮（浅灰底深灰字）</summary>
            Default
        }

        /// <summary>按风格美化一个按钮</summary>
        public static void StyleButton(Button button, Kind kind = Kind.Default)
        {
            if (button == null) return;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Font = FontBody;

            switch (kind)
            {
                case Kind.Primary:
                    button.BackColor = Primary;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.BorderColor = Primary;
                    button.FlatAppearance.MouseOverBackColor = PrimaryHover;
                    button.FlatAppearance.MouseDownBackColor = PrimaryDown;
                    break;
                case Kind.Success:
                    button.BackColor = Success;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.BorderColor = Success;
                    button.FlatAppearance.MouseOverBackColor = SuccessHover;
                    button.FlatAppearance.MouseDownBackColor = Success;
                    break;
                case Kind.Danger:
                    button.BackColor = Danger;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.BorderColor = Danger;
                    button.FlatAppearance.MouseOverBackColor = DangerHover;
                    button.FlatAppearance.MouseDownBackColor = Danger;
                    break;
                default:
                    button.BackColor = ButtonDefault;
                    button.ForeColor = TextMain;
                    button.FlatAppearance.BorderColor = Border;
                    button.FlatAppearance.MouseOverBackColor = ButtonDefaultHover;
                    button.FlatAppearance.MouseDownBackColor = Border;
                    break;
            }
        }

        /// <summary>美化一个 DataGridView：白底、蓝色表头、斑马纹、浅色选中</summary>
        public static void StyleGrid(DataGridView grid)
        {
            if (grid == null) return;
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = GridLine;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = TableHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBody;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = TextMain;
            grid.DefaultCellStyle.Font = FontBody;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = SelectionLight;
            grid.DefaultCellStyle.SelectionForeColor = TextMain;
            grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
            grid.RowHeadersVisible = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowTemplate.Height = 42;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        /// <summary>
        /// 递归美化整个窗体（Tab 页白底、表格、输入控件字体、树控件等）。
        /// 按钮请在外部按名称调用 StyleButton 指定风格。
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null) return;

            if (root is TabPage)
            {
                root.BackColor = Color.White;
            }
            else if (root is DataGridView)
            {
                StyleGrid((DataGridView)root);
            }
            else if (root is TextBox)
            {
                root.Font = FontBody;
                ((TextBox)root).BorderStyle = BorderStyle.FixedSingle;
            }
            else if (root is RichTextBox)
            {
                root.Font = FontBody;
            }
            else if (root is TreeView)
            {
                root.Font = FontBody;
                root.BackColor = Color.White;
                ((TreeView)root).LineColor = Border;
            }
            else if (root is GroupBox)
            {
                ((GroupBox)root).FlatStyle = FlatStyle.Flat;
            }

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }
        }
    }
}
