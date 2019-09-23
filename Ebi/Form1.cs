using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.IO;
using NPOI.SS.UserModel;

namespace Ebi
{
    public partial class MainForm : Form
    {
        private Image ebi = null;
        private Image a4img = null;
        private int printcnt = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            // エビを読み込んでおく
            ebi = Image.FromFile("./resources/ebi.png");

            calcSize();
            createBuffer();
            updateImage();
            loadExcel(null);
        }


        private void loadExcel(String filename)
        {
            var dt = dataGrid.DataSource as DataTable;
            if(dt != null)
            {
                dataGrid.DataSource = null;
                dt.Dispose();
                dt = null;
            }

            DataColumn[] cols = new DataColumn[]
            {
                 new DataColumn("no", typeof(int))
                ,new DataColumn("target", typeof(bool))
                ,new DataColumn("name1", typeof(String))
                ,new DataColumn("name2", typeof(String))
                ,new DataColumn("price_jp", typeof(String))
            };

            dt = new DataTable();
            foreach(DataColumn col in cols)
            {
                dt.Columns.Add(col);
            }

            if(File.Exists(filename) == true)
            {
                // Excelを読み込んでDataTableに設定する
                IWorkbook book = WorkbookFactory.Create(filename);

                // 最初のシートが対象のシートとする
                ISheet sheet = book.GetSheetAt(0);

                IRow row = sheet.GetRow(0);
                var printIdxPos = -1;
                var printNamePos = -1;
                var printPricePos = -1;

                foreach (ICell cell in row.Cells)
                {
                    if (cell.StringCellValue == "張出" || cell.StringCellValue == "貼出")
                    {
                        printIdxPos = cell.ColumnIndex;
                    }
                    if (cell.StringCellValue == "貼出用店名")
                    {
                        printNamePos = cell.ColumnIndex;
                    }
                    if (cell.StringCellValue == "張出金額" || cell.StringCellValue == "貼出金額")
                    {
                        printPricePos = cell.ColumnIndex;
                    }
                }

                var cnt = 1;
                for (int i = 1; i < sheet.LastRowNum; i++)
                {
                    row = sheet.GetRow(i);
                    String targetFlag = "";
                    String name1 = "";
                    String name2 = "";
                    String price = "";
                    if (row.Cells[printIdxPos].CellType == CellType.String)
                    {
                        targetFlag = row.Cells[printIdxPos].StringCellValue;
                    }
                    if (row.Cells[printNamePos].CellType == CellType.String)
                    {
                        name1 = row.Cells[printNamePos].StringCellValue;
                    }
                    if (row.Cells[printNamePos+1].CellType == CellType.String)
                    {
                        name2 = row.Cells[printNamePos + 1].StringCellValue;
                    }
                    if (row.Cells[printPricePos].CellType == CellType.String)
                    {
                        price = row.Cells[printPricePos].StringCellValue;
                    }
                    if (row.Cells[printPricePos].CellType == CellType.Formula)
                    {
                        switch (row.Cells[printPricePos].CachedFormulaResultType)
                        {
                            case CellType.String:
                                price = row.Cells[printPricePos].StringCellValue;
                                break;
                            case CellType.Numeric:
                                break;
                            case CellType.Boolean:
                                break;
                            case CellType.Blank:
                                break;
                            case CellType.Error:
                                break;
                            case CellType.Unknown:
                                break;
                            default:
                                break;
                        }
                    }

                    if (name1 == "" && name2 == "")
                    {
                        // 名前な脚は無視
                        continue;
                    }

                    var newRow = dt.NewRow();
                    newRow["no"] = cnt;
                    newRow["target"] = (targetFlag == "〇" || targetFlag == "○") && price != "" ? true : false;
                    newRow["price_jp"] = price;
                    newRow["name1"] = name1;
                    newRow["name2"] = name2;
                    dt.Rows.Add(newRow);
                    cnt++;
                }
                book.Close();
                //book.Dispose();
            }


            var col1 = new DataGridViewTextBoxColumn();
            col1.DataPropertyName = "no";
            col1.Name = "NO";
            col1.ReadOnly = true;

            var col2 = new DataGridViewCheckBoxColumn();
            col2.DataPropertyName = "target";
            col2.Name = "対象";

            var col3 = new DataGridViewTextBoxColumn();
            col3.DataPropertyName = "price_jp";
            col3.Name = "金額(漢字)";
            col3.ReadOnly = true;

            var col4 = new DataGridViewTextBoxColumn();
            col4.DataPropertyName = "name1";
            col4.Name = "名前1";
            col4.ReadOnly = true;

            var col5 = new DataGridViewTextBoxColumn();
            col5.DataPropertyName = "name2";
            col5.Name = "名前2";
            col5.ReadOnly = true;

            dataGrid.Columns.Add(col1);
            dataGrid.Columns.Add(col2);
            dataGrid.Columns.Add(col3);
            dataGrid.Columns.Add(col4);
            dataGrid.Columns.Add(col5);

            dataGrid.DataSource = dt;
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGrid.AutoResizeColumns();
        }

        /// <summary>
        /// ウィンドウのサイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            calcSize();
        }

        private void calcSize()
        {
            var clientHeight = this.ClientSize.Height - mainMenu.Height - 8 - mainStatusBar.Height;
            var width = (int)(Math.Round(clientHeight * 0.707071, 0));
            previewBox.Width = width;
            previewBox.Height = clientHeight;

            rightPanel.Width = this.ClientSize.Width - width - 8;
            rightPanel.Height = clientHeight;
            rightPanel.Left = width + 8;

        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
        }


        private void createBuffer()
        {
            if(a4img != null)
            {
                a4img.Dispose();
                a4img = null;
            }
            a4img = new Bitmap(ebi.Width, ebi.Height) as Image;
        }

        private void updateImage(int targetRow = -1)
        {
            StringFormat sf = new StringFormat(StringFormatFlags.DirectionVertical);
            using (Graphics graph = Graphics.FromImage(a4img))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            using (Font priceFont = new Font("HGP行書体", 250))
            using (Font nameFont = new Font("HGP行書体", 250))
            {
                graph.DrawImage(ebi, 0, 0, ebi.Width, ebi.Height);

                if (dataGrid.Rows.Count > 0)
                {
                    var rowIdx = 0;
                    if(targetRow == -1)
                    {
                        rowIdx = dataGrid.SelectedCells[0].RowIndex;

                    }
                    else
                    {
                        rowIdx = targetRow;

                    }
                    bool isTarget = (bool)dataGrid.Rows[rowIdx].Cells["対象"].Value;
                    String price = (String)dataGrid.Rows[rowIdx].Cells["金額(漢字)"].Value;
                    if(price != "")
                    {
//                        price = "一、" + price + "圓也";
                        price = price + "圓也";
                    }
                    String name1 = (String)dataGrid.Rows[rowIdx].Cells["名前1"].Value;
                    String name2 = (String)dataGrid.Rows[rowIdx].Cells["名前2"].Value;

                    // 金額の描画
                    graph.DrawString(price, priceFont, brush, 1900, 64, sf);

                    if(name2 == "")
                    {
                        // name1の描画
                        graph.DrawString(name1, nameFont, brush, 1000, 64 + 200, sf);

                    }
                    else
                    {
                        // name1の描画
                        graph.DrawString(name1, nameFont, brush, 1200, 64 + 200, sf);


                        SizeF name2Size = graph.MeasureString(name2, nameFont, new PointF(0, 0), sf);
                        //var name2Top = a4img.Height - (name2Size.Height + 80);
                        var name2Top = 80 + 600;

                        // name2の描画
                        graph.DrawString(name2, nameFont, brush, 600, name2Top, sf);

                    }
                    // 様の描画が必要
                    graph.DrawString("様", nameFont, brush, 60, 3000, sf);
                }

            }


            // 描画結果を転送
            previewBox.Image = a4img;
            //previewBox.Update();
        }

        private void MenuPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(printing);

            var dlg = new PrintDialog();

            dlg.Document = pd;

            if(dlg.ShowDialog() == DialogResult.OK)
            {
                printcnt = 1;
                pd.Print();
            }
        }

        private void printing(Object sender, PrintPageEventArgs e)
        {
            
            while( printcnt < dataGrid.RowCount && ( (bool) dataGrid.Rows[printcnt].Cells["対象"].Value ) == false)
            {
                printcnt++;
            } 
            if(printcnt >= dataGrid.RowCount)
            {
                e.HasMorePages = false;
            }
            else
            {
                updateImage(printcnt);
                e.Graphics.DrawImage(a4img, e.MarginBounds);
                e.HasMorePages = true;

            }
            printcnt++;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewBox_Click(object sender, EventArgs e)
        {
                
        }

        /// <summary>
        /// 終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExit_Click(object sender, EventArgs e)
        {
            if(a4img!= null)
            {
                a4img.Dispose();
                a4img = null;
            }

            this.Close();
        }

        /// <summary>
        /// Excelのインポート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuImportExcel_Click(object sender, EventArgs e)
        {
            if(xlsOpenDlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            try
            {
                loadExcel(xlsOpenDlg.FileName);
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(this, "読み込もうとしたファイルが既にExcelで開かれている、または読み込もうとしたファイルに誤りがあります。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void DataGrid_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRowIndex = this.dataGrid.SelectedCells[0].RowIndex;
            updateImage();

        }

        private void BtnAllSelect_Click(object sender, EventArgs e)
        {
            for(int i = 0; i <  dataGrid.RowCount; i++)
            {
                if((String)(dataGrid.Rows[i].Cells["名前1"].Value) != "" && (String)(dataGrid.Rows[i].Cells["名前2"].Value) != "")
                {
                    dataGrid.Rows[i].Cells["対象"].Value = true;

                }
            }
        }

        private void BtnAllClear_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGrid.RowCount; i++)
            {
                dataGrid.Rows[i].Cells["対象"].Value = false;
            }

        }
    }
}
