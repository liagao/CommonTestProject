namespace CommonTestProject
{
    using System;
    using System.Data;
    using System.Configuration;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using Microsoft.Office.Interop;
    using Microsoft.Office.Core;

    using Microsoft.Office.Interop.Excel;

    /// <SUMMARY>
    /// ExcelEdit 的摘要说明
    /// </SUMMARY>
    public class ExcelHelper
    {
        public string mFilename;
        public Application app;
        public Workbooks wbs;
        public Workbook wb;
        public Worksheets wss;
        public Worksheet ws;
        public ExcelHelper()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }
        public void Create()//创建一个Excel对象
        {
            app = new Application();
            wbs = app.Workbooks;
            wb = wbs.Add(true);
        }
        public void Open(string FileName)//打开一个Excel文件
        {
            app = new Application();
            wbs = app.Workbooks;
            wb = wbs.Add(FileName);
            //wb = wbs.Open(FileName, 0, true, 5,"", "", true, XlPlatform.xlWindows, "t", false, false, 0, true,Type.Missing,Type.Missing);
            //wb = wbs.Open(FileName,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing,XlPlatform.xlWindows,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing);
            mFilename = FileName;
        }
        public Worksheet GetSheet(string SheetName)
        //获取一个工作表
        {
            Worksheet s = (Worksheet)wb.Worksheets[SheetName];
            return s;
        }
        public Worksheet AddSheet(string SheetName)
        //添加一个工作表
        {
            Worksheet s = (Worksheet)wb.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            s.Name = SheetName;
            return s;
        }

        public void DelSheet(string SheetName)//删除一个工作表
        {
            ((Worksheet)wb.Worksheets[SheetName]).Delete();
        }
        public Worksheet ReNameSheet(string OldSheetName, string NewSheetName)//重命名一个工作表一
        {
            Worksheet s = (Worksheet)wb.Worksheets[OldSheetName];
            s.Name = NewSheetName;
            return s;
        }

        public Worksheet ReNameSheet(Worksheet Sheet, string NewSheetName)//重命名一个工作表二
        {

            Sheet.Name = NewSheetName;

            return Sheet;
        }

        public void SetCellValue(Worksheet ws, int x, int y, object value)
        //ws：要设值的工作表     X行Y列     value   值
        {
            ws.Cells[x, y] = value;
        }
        public void SetCellValue(string ws, int x, int y, object value)
        //ws：要设值的工作表的名称 X行Y列 value 值
        {

            GetSheet(ws).Cells[x, y] = value;
        }

        public void SetCellProperty(Worksheet ws, int Startx, int Starty, int Endx, int Endy, int size, string name, Constants color, Constants HorizontalAlignment)
        //设置一个单元格的属性   字体，   大小，颜色   ，对齐方式
        {
            name = "宋体";
            size = 12;
            color = Constants.xlAutomatic;
            HorizontalAlignment = Constants.xlRight;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Name = name;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Size = size;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Color = color;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).HorizontalAlignment = HorizontalAlignment;
        }

        public void SetCellProperty(string wsn, int Startx, int Starty, int Endx, int Endy, int size, string name, Constants color, Constants HorizontalAlignment)
        {
            //name = "宋体";
            //size = 12;
            //color = Constants.xlAutomatic;
            //HorizontalAlignment = Constants.xlRight;

            Worksheet ws = GetSheet(wsn);
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Name = name;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Size = size;
            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).Font.Color = color;

            ws.get_Range(ws.Cells[Startx, Starty], ws.Cells[Endx, Endy]).HorizontalAlignment = HorizontalAlignment;
        }


        public void UniteCells(Worksheet ws, int x1, int y1, int x2, int y2)
        //合并单元格
        {
            ws.get_Range(ws.Cells[x1, y1], ws.Cells[x2, y2]).Merge(Type.Missing);
        }

        public void UniteCells(string ws, int x1, int y1, int x2, int y2)
        //合并单元格
        {
            GetSheet(ws).get_Range(GetSheet(ws).Cells[x1, y1], GetSheet(ws).Cells[x2, y2]).Merge(Type.Missing);

        }

        public bool Save()
        //保存文档
        {
            if (mFilename == "")
            {
                return false;
            }
            else
            {
                wb.Save();
                return true;
            }
        }
        public bool SaveAs(object FileName)
        //文档另存为
        {
            wb.SaveAs(FileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            return true;
        }
        public void Close()
        //关闭一个Excel对象，销毁对象
        {
            //wb.Save();
            wb.Close(Type.Missing, Type.Missing, Type.Missing);
            wbs.Close();
            app.Quit();
            wb = null;
            wbs = null;
            app = null;
            GC.Collect();
        }
    }
}
