using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service.file
{
    public class DataGridViewProgressBarColumn : DataGridViewTextBoxColumn
    {
        public DataGridViewProgressBarColumn()
        {
            this.CellTemplate = new DataGridViewProgressBarCell();
        }
        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                if (!(value is DataGridViewProgressBarCell))
                {
                    throw new InvalidCastException("DataGridViewProgressBarCell" + "指定。");
                }
                base.CellTemplate = value;
            }
        }

        /// <summary>
        /// ProgressBarの最大値
        /// </summary>
        public int Maximum
        {
            get
            {
                return ((DataGridViewProgressBarCell)this.CellTemplate).Maximum;
            }
            set
            {
                if (this.Maximum == value)
                    return;
                ((DataGridViewProgressBarCell)this.CellTemplate).Maximum = value;

                if (this.DataGridView == null)
                    return;
                int rowCount = this.DataGridView.RowCount;
                for (int i = 0; i < rowCount; i++)
                {
                    DataGridViewRow r = this.DataGridView.Rows.SharedRow(i);
                    ((DataGridViewProgressBarCell)r.Cells[this.Index]).Maximum = value;
                }
            }
        }
        /// <summary>
        /// ProgressBarの最小値
        /// </summary>
        public int Mimimum
        {
            get
            {
                return ((DataGridViewProgressBarCell)this.CellTemplate).Mimimum;
            }
            set
            {
                if (this.Mimimum == value)
                    return;
                ((DataGridViewProgressBarCell)this.CellTemplate).Mimimum = value;
                if (this.DataGridView == null)
                    return;
                int rowCount = this.DataGridView.RowCount;
                for (int i = 0; i < rowCount; i++)
                {
                    DataGridViewRow r = this.DataGridView.Rows.SharedRow(i);
                    ((DataGridViewProgressBarCell)r.Cells[this.Index]).Mimimum = value;
                }
            }
        }

    }
}
