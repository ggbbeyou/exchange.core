using System.Data;
using System.Security.Cryptography;
using System.Text;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Com.Bll.Util;

public class ExcelHelper
{

    /// <summary>
    /// 读取excel文件
    /// </summary>
    /// <param name="FileName"></param>
    /// <returns></returns>
    public static DataTable? OpenExcel(Stream ms, string fileExt)
    {
        DataTable? dt = null;
        // FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        bool end = false;
        try
        {
            IWorkbook? book;
            // string fileExt = Path.GetExtension(FileName).ToLower();
            if (fileExt == ".xlsx")
            {
                book = new XSSFWorkbook(ms);
            }
            else if (fileExt == ".xls")
            {
                book = new HSSFWorkbook(ms);
            }
            else
            {
                book = null;
                throw new Exception("不认识此Excel文件。");
            }
            int sheetCount = book.NumberOfSheets;
            for (int sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                NPOI.SS.UserModel.ISheet sheet = book.GetSheetAt(sheetIndex);
                if (sheet == null)
                {
                    continue;
                }
                NPOI.SS.UserModel.IRow row = sheet.GetRow(0);
                if (row == null)
                {
                    continue;
                }
                int firstCellNum = row.FirstCellNum;
                int lastCellNum = row.LastCellNum;
                if (firstCellNum == lastCellNum)
                {
                    continue;
                }
                dt = new DataTable(sheet.SheetName);
                for (int i = firstCellNum; i < lastCellNum; i++)
                {
                    dt.Columns.Add(row.GetCell(i).StringCellValue, typeof(string));
                }
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    DataRow newRow = dt.Rows.Add();
                    for (int j = firstCellNum; j < lastCellNum; j++)
                    {
                        if (sheet.GetRow(i).GetCell(j) != null)
                        {
                            try
                            {
                                if (sheet.GetRow(i).GetCell(j).CellType == CellType.String)
                                {
                                    newRow[j] = sheet.GetRow(i).GetCell(j).StringCellValue;
                                    // newRow[j] = newRow[j].ToString().Trim('\ud8e9').Trim().Replace("\r", "").Replace("\n", "").Replace("\f", "").Replace("\t", "");
                                }
                                else if (sheet.GetRow(i).GetCell(j).CellType == CellType.Blank)
                                {
                                    newRow[j] = "";
                                }
                                else if (sheet.GetRow(i).GetCell(j).CellType == CellType.Numeric)
                                {
                                    newRow[j] = sheet.GetRow(i).GetCell(j).NumericCellValue;
                                }
                                else if (sheet.GetRow(i).GetCell(j).CellType == CellType.Formula)
                                {
                                    newRow[j] = sheet.GetRow(i).GetCell(j).NumericCellValue;//  .getNumericCellValue();
                                }
                                else if (sheet.GetRow(i).GetCell(j).CellType == CellType.Boolean)
                                {
                                    newRow[j] = sheet.GetRow(i).GetCell(j).BooleanCellValue;
                                }
                                else
                                {
                                    newRow[j] = sheet.GetRow(i).GetCell(j).ToString();
                                }
                                if (j == 1 && newRow[j].ToString() == "以下空白")
                                {
                                    end = true;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                    if (end)
                    {
                        break;
                    }
                }
                if (end)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        finally
        {
            // fs.Close();
        }
        return dt;
    }

}
