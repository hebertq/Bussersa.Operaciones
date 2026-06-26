using Microsoft.VisualBasic;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Utilidades.ClasesGenericas
{
    public class DataTableHelper
    {
        public delegate T CreateObjectDelegate<T>();
        private DataTableHelper()
        {
        }

        public static DataTable ConvertTo<T>(List<T> list, string nombre)
        {
            DataTable table = CreateTable<T>(nombre);
            Type entityType = typeof(T);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            object value = null;
            DateTime dte = default(DateTime);

            foreach (T item in list)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    TypeCode typeCode = Type.GetTypeCode(prop.PropertyType);
                    string Name = prop.Name;

                    try
                    {
                        if (prop.PropertyType.BaseType.Name == "Enum")
                            value = Convert.ToInt32(prop.GetValue(item));
                        else
                        {
                            switch (typeCode)
                            {
                                case TypeCode.Int32:
                                    value = Convert.ToInt32(prop.GetValue(item));
                                    break;
                                case TypeCode.Decimal:
                                    value = Convert.ToDecimal(prop.GetValue(item));
                                    break;
                                case TypeCode.Boolean:
                                    value = Convert.ToBoolean(prop.GetValue(item));
                                    break;
                                case TypeCode.DateTime:
                                    value = Convert.ToDateTime(prop.GetValue(item));
                                    break;
                                default:
                                    value = prop.GetValue(item);
                                    break;
                            }
                        }

                        if (value != null)
                        {
                            row[prop.Name] = value;
                        }
                        else
                        {
                            row[prop.Name] = DBNull.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error de conversión de " + Name + " de valor " + prop.GetValue(item).ToString());
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }
        public static List<T> ConvertTo<T>(List<DataRow> rows,
                                           CreateObjectDelegate<T> del)
        {
            List<T> list = null;
            if (rows != null)
            {
                list = new List<T>();
                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row, del);
                    list.Add(item);
                }
            }
            return list;
        }
        public static List<T> ConvertTo<T>(List<DataRow> rows)
        {
            List<T> list = null;
            if (rows != null)
            {
                list = new List<T>();
                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row);
                    list.Add(item);
                }
            }
            return list;
        }
        public static List<T> ConvertTo<T>(DataRow[] rows)
        {
            List<T> list = null;
            if (rows != null)
            {
                list = new List<T>();
                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row);
                    list.Add(item);
                }
            }
            return list;
        }
        public static List<T> ConvertTo<T>(List<DataRow> rows, string sortString)
        {
            List<T> list = null;
            if (rows != null)
            {
                list = new List<T>();
                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row);
                    list.Add(item);
                }
                GenericSorter<T> sorter = new GenericSorter<T>(sortString);
                list.Sort(sorter);
            }
            return list;
        }
        public static List<T> ConvertTo<T>(DataTable dt, string sortString)
        {
            if (dt == null)
            {
                return null;
            }
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow row in dt.Rows)
            {
                rows.Add(row);
            }
            return ConvertTo<T>(rows, sortString);
        }
        public static List<T> ConvertTo<T>(DataTable dt)
        {
            return ConvertTo<T>(dt.Select());
            //List<T> myInstances = new List<T>();
            //T obj = default(T);
            //for (int i = 0; i < dt.Rows.Count; i++)
            //{
            //    obj = Activator.CreateInstance<T>();
            //    foreach (PropertyInfo pInfo in obj.GetType().GetProperties())
            //    {
            //        string Name = pInfo.Name;
            //        if (dt.Columns.Contains(Name))
            //        {
            //            Object val = dt.Rows[i].Field<object>(pInfo.Name);
            //            TypeCode typeCode = Type.GetTypeCode(pInfo.PropertyType);
            //            try
            //            {
            //                switch (typeCode)
            //                {
            //                    case TypeCode.Boolean:
            //                        pInfo.SetValue(obj, val.ToString() == "S" || val.ToString() == "1" ? true : false, null);
            //                        break;
            //                    case TypeCode.DateTime:
            //                        if (val.ToString().Trim() != "")
            //                            pInfo.SetValue(obj, Convert.ToDateTime(val));
            //                        break;
            //                    case TypeCode.Int32:
            //                        if (val == null)
            //                            val = "0";
            //                        pInfo.SetValue(obj, Convert.ChangeType(val, typeCode));
            //                        break;
            //                    case TypeCode.String:
            //                        if (val == null)
            //                            val = "";
            //                        pInfo.SetValue(obj, Convert.ChangeType(val, typeCode));
            //                        break;
            //                    case TypeCode.Object:
            //                        if (pInfo.PropertyType.FullName.Contains("System.Nullable"))
            //                        {
            //                            if (val != null)
            //                            {
            //                                TypeCode TableCode = Type.GetTypeCode(val.GetType());
            //                                if (pInfo.PropertyType.FullName.Contains("DateTime") && val.ToString().Trim() != "")
            //                                {
            //                                    if (TableCode == TypeCode.DateTime)
            //                                        pInfo.SetValue(obj, Convert.ChangeType(val, typeCode), null);
            //                                    else if (!val.ToString().Equals("0/0/0"))
            //                                    {
            //                                        int tamanyo = val.ToString().Length;
            //                                        if (tamanyo < 11 && tamanyo >= 6)
            //                                        {
            //                                            var fecha = FechaUpDate(val.ToString());
            //                                            pInfo.SetValue(obj, Convert.ChangeType(fecha, typeCode), null);
            //                                        }
            //                                    }
            //                                }

            //                                if (pInfo.PropertyType.FullName.Contains("Decimal"))
            //                                    pInfo.SetValue(obj, Convert.ToDecimal(val));

            //                                if (pInfo.PropertyType.FullName.Contains("Int"))
            //                                    pInfo.SetValue(obj, Convert.ToInt32(val));
            //                            }
            //                            else
            //                            {
            //                                if (pInfo.PropertyType.FullName.Contains("Bool"))
            //                                    pInfo.SetValue(obj, false);
            //                            }
            //                        }
            //                        else
            //                            pInfo.SetValue(obj, Convert.ChangeType(val, typeCode));
            //                        break;
            //                    default:
            //                        pInfo.SetValue(obj, Convert.ChangeType(val, typeCode));
            //                        break;
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                throw new Exception("Error de conversión de " + Name + " de valor " + val.ToString() + " a " + pInfo.PropertyType.FullName + " Mensaje: " + ex.Message);
            //            }
            //        }
            //    }
            //    myInstances.Add(obj);
            //}

            //return myInstances;
        }

        private static DateTime FechaUpDate(string fecha)
        {
            string NewFecha = string.Empty;
            if (fecha.Contains("/"))
            {
                var fec = fecha.Split('/');
                string anyo = fec[2].Length < 4 ? Get4LetterYear(int.Parse(fec[2])) : fec[2];
                NewFecha = anyo + "-" + Right(("00" + fec[1]), 2) + "-" + Right(("00" + fec[0]), 2);
            }
            else
                NewFecha = fecha;

            return Convert.ToDateTime(NewFecha);
        }

        private static string Get4LetterYear(int twoLetterYear)
        {
            int TwoDigits = Convert.ToInt32(DateTime.Now.Year.ToString().Substring(2, 2));
            int firstDigits = Convert.ToInt32(DateTime.Now.Year.ToString().Substring(0, 2));
            return Get4LetterYear(twoLetterYear, twoLetterYear <= TwoDigits ? firstDigits : firstDigits - 1).ToString();
        }

        private static int Get4LetterYear(int twoLetterYear, int firstTwoDigits)
        {
            return Convert.ToInt32(firstTwoDigits.ToString() + twoLetterYear.ToString());
        }

        private static int Get2LetterYear(int fourLetterYear)
        {
            return Convert.ToInt32(fourLetterYear.ToString().Substring(2, 2));
        }

        public static string Right(string value, int length)
        {
            return value.Substring(value.Length - length);
        }

        public static List<T> ConvertTo<T>(DataTable dt, CreateObjectDelegate<T> del)
        {
            if (dt == null)
            {
                return null;
            }
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow row in dt.Rows)
            {
                rows.Add(row);
            }
            return ConvertTo<T>(rows, del);
        }
        public static T CreateItem<T>(DataRow dr, CreateObjectDelegate<T> del)
        {
            var obj = default(T);

            if (dr != null)
            {
                if (del != null)
                {
                    obj = del.Invoke();
                }
                else
                {
                    obj = Activator.CreateInstance<T>();
                }
                if (obj != null)
                {
                    foreach (DataColumn column in dr.Table.Columns)
                    {
                        PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName);
                        if (prop != null)
                        {
                            if (!object.ReferenceEquals(dr[column.ColumnName], DBNull.Value))
                            {
                                prop.SetValue(obj, dr[column.ColumnName], null);
                            }
                            else
                            {
                                prop.SetValue(obj, null, null);
                            }
                        }
                    }
                }
            }
            return obj;
        }
        public static T CreateItem<T>(DataRow dr)
        {
            var obj = default(T);

            if (dr != null)
            {
                obj = Activator.CreateInstance<T>();
                if (obj != null)
                {
                    foreach (DataColumn column in dr.Table.Columns)
                    {
                        PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName);
                        if (prop != null)
                        {
                            if (!object.ReferenceEquals(dr[column.ColumnName], DBNull.Value))
                            {
                                try
                                {
                                    var enumdata = prop.PropertyType.BaseType.AssemblyQualifiedName.ToLower().Contains("enum");
                                    if(!enumdata)
                                    {
                                        TypeCode typeCode = Type.GetTypeCode(prop.PropertyType);
                                        switch (typeCode)
                                        {
                                            case TypeCode.Int32:
                                                prop.SetValue(obj, int.Parse(dr[column.ColumnName].ToString()), null);
                                                break;
                                            default:
                                                prop.SetValue(obj, dr[column.ColumnName], null);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        int dato = (int)Enum.Parse(prop.PropertyType, dr[column.ColumnName].ToString());
                                        prop.SetValue(obj, dato, null);
                                    }
                                    
                                }
                                catch{ prop.SetValue(obj, null, null); }
                            }
                            else
                            {
                                prop.SetValue(obj, null, null);
                            }
                        }
                    }
                }
            }
            return obj;
        }

        public static DataTable CreateTable(MemoryStream fs,string Name,string sFileExtension,int numSheet)
        {
            DataTable Tabla = null;
            if (sFileExtension == ".csv")
            {
                Tabla = new DataTable();
                using (StreamReader sr = new StreamReader(fs,Encoding.UTF8))
                {
                    string[] headers = sr.ReadLine().Split(';');
                    foreach (string header in headers)
                    {
                        Tabla.Columns.Add(header.SinTildes().Replace(".", "").Trim().Replace("   ", "_").Replace("  ", "_").Replace(" ", "_"));
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(';');
                        DataRow dr = Tabla.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        Tabla.Rows.Add(dr);
                    }
                }
                return Tabla;
            }

            ISheet sheet;
            if (sFileExtension == ".xls")
            {
                HSSFWorkbook hssfwb = new HSSFWorkbook(fs); //This will read the Excel 97-2000 formats
                sheet = hssfwb.GetSheetAt(numSheet); //get first sheet from workbook
            }
            else
            {
                XSSFWorkbook hssfwb = new XSSFWorkbook(fs); //This will read 2007 Excel format
                sheet = hssfwb.GetSheetAt(numSheet); //get first sheet from workbook
            }

            Tabla = new DataTable(Name);
            Tabla.Rows.Clear();
            Tabla.Columns.Clear();

            for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                DataRow NewReg = null;
                IRow row  = sheet.GetRow(rowIndex);
                IRow row2 = null;
                IRow row3 = null;

                if (rowIndex == 0)
                {
                    row2 = sheet.GetRow(rowIndex + 1); //Si es la Primera fila, obtengo tambien la segunda para saber el tipo de datos
                    row3 = sheet.GetRow(rowIndex + 2); //Y la tercera tambien por las dudas
                }

                if (row != null) //null is when the row only contains empty cells 
                {
                    if (rowIndex > 0) NewReg = Tabla.NewRow();

                    int colIndex = 0;
                    //Leer cada Columna de la fila
                    foreach (ICell cell in row.Cells)
                    {
                        object valorCell = null;
                        string cellType = "";
                        string[] cellType2 = new string[2];

                        if (rowIndex == 0) //Asumo que la primera fila contiene los titlos:
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                ICell cell2 = null;
                                if (i == 0) { cell2 = row2.GetCell(cell.ColumnIndex); }
                                else { cell2 = row3.GetCell(cell.ColumnIndex); }

                                if (cell2 != null)
                                {
                                    switch (cell2.CellType)
                                    {
                                        case CellType.Blank: break;
                                        case CellType.Boolean: cellType2[i] = "System.Boolean"; break;
                                        case CellType.String: cellType2[i] = "System.String"; break;
                                        case CellType.Numeric:
                                            if (HSSFDateUtil.IsCellDateFormatted(cell2)) { cellType2[i] = "System.DateTime"; }
                                            else
                                            {
                                                cellType2[i] = "System.Double";  //valorCell = cell2.NumericCellValue;
                                            }
                                            break;

                                        case CellType.Formula:
                                            bool continuar = true;
                                            switch (cell2.CachedFormulaResultType)
                                            {
                                                case CellType.Boolean: cellType2[i] = "System.Boolean"; break;
                                                case CellType.String: cellType2[i] = "System.String"; break;
                                                case CellType.Numeric:
                                                    if (HSSFDateUtil.IsCellDateFormatted(cell2)) { cellType2[i] = "System.DateTime"; }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            //DETERMINAR SI ES BOOLEANO
                                                            if (cell2.CellFormula == "TRUE()") { cellType2[i] = "System.Boolean"; continuar = false; }
                                                            if (continuar && cell2.CellFormula == "FALSE()") { cellType2[i] = "System.Boolean"; continuar = false; }
                                                            if (continuar) { cellType2[i] = "System.Double"; continuar = false; }
                                                        }
                                                        catch { }
                                                    }
                                                    break;
                                            }
                                            break;
                                        default:
                                            cellType2[i] = "System.String"; break;
                                    }
                                }
                            }

                            //Resolver las diferencias de Tipos
                            if (cellType2[0] == cellType2[1]) { cellType = cellType2[0]; }
                            else
                            {
                                if (cellType2[0] == null) cellType = cellType2[1];
                                if (cellType2[1] == null) cellType = cellType2[0];
                                if (cellType == "") cellType = "System.String";
                            }

                            //Obtener el nombre de la Columna
                            string colName = "Column_{0}";
                            try { colName = cell.StringCellValue; }
                            catch { colName = string.Format(colName, colIndex); }

                            //Verificar que NO se repita el Nombre de la Columna
                            foreach (DataColumn col in Tabla.Columns)
                            {
                                if (col.ColumnName == colName) colName = string.Format("{0}_{1}", colName, colIndex);
                            }

                            //Agregar el campos de la tabla:
                            colName = colName.SinTildes().Replace(".", "").Trim().Replace("   ", "_").Replace("  ", "_").Replace(" ", "_");
                            DataColumn codigo = new DataColumn(colName, System.Type.GetType(cellType));
                            Tabla.Columns.Add(codigo); colIndex++;
                        }
                        else
                        {
                            //Las demas filas son registros:
                            switch (cell.CellType)
                            {
                                case CellType.Blank: valorCell = DBNull.Value; break;
                                case CellType.Boolean: valorCell = cell.BooleanCellValue; break;
                                case CellType.String: valorCell = cell.StringCellValue; break;
                                case CellType.Numeric:
                                    if (HSSFDateUtil.IsCellDateFormatted(cell)) { valorCell = cell.DateCellValue; }
                                    else { valorCell = cell.NumericCellValue; }
                                    break;
                                case CellType.Formula:
                                    switch (cell.CachedFormulaResultType)
                                    {
                                        case CellType.Blank: valorCell = DBNull.Value; break;
                                        case CellType.String: valorCell = cell.StringCellValue; break;
                                        case CellType.Boolean: valorCell = cell.BooleanCellValue; break;
                                        case CellType.Numeric:
                                            if (HSSFDateUtil.IsCellDateFormatted(cell)) { valorCell = cell.DateCellValue; }
                                            else { valorCell = cell.NumericCellValue; }
                                            break;
                                    }
                                    break;
                                default: valorCell = cell.StringCellValue; break;
                            }
                            //Agregar el nuevo Registro
                            if (cell.ColumnIndex <= Tabla.Columns.Count - 1) NewReg[cell.ColumnIndex] = valorCell;
                        }
                    }
                }
                if (rowIndex > 0) Tabla.Rows.Add(NewReg);
            }

            Tabla.AcceptChanges();
            return Tabla;
        }

        private static void AddColumnHeader(DataTable Tabla, ICell cell, IRow row2, IRow row3, string[] cellType2, string cellType, int colIndex)
        {
            for (int i = 0; i < 2; i++)
            {
                ICell cell2 = null;
                if (i == 0) { cell2 = row2.GetCell(cell.ColumnIndex); }
                else { cell2 = row3.GetCell(cell.ColumnIndex); }

                if (cell2 != null)
                {
                    switch (cell2.CellType)
                    {
                        case CellType.Blank: break;
                        case CellType.Boolean: cellType2[i] = "System.Boolean"; break;
                        case CellType.String: cellType2[i] = "System.String"; break;
                        case CellType.Numeric:
                            if (HSSFDateUtil.IsCellDateFormatted(cell2)) { cellType2[i] = "System.DateTime"; }
                            else
                            {
                                cellType2[i] = "System.Double";  //valorCell = cell2.NumericCellValue;
                            }
                            break;

                        case CellType.Formula:
                            bool continuar = true;
                            switch (cell2.CachedFormulaResultType)
                            {
                                case CellType.Boolean: cellType2[i] = "System.Boolean"; break;
                                case CellType.String: cellType2[i] = "System.String"; break;
                                case CellType.Numeric:
                                    if (HSSFDateUtil.IsCellDateFormatted(cell2)) { cellType2[i] = "System.DateTime"; }
                                    else
                                    {
                                        try
                                        {
                                            //DETERMINAR SI ES BOOLEANO
                                            if (cell2.CellFormula == "TRUE()") { cellType2[i] = "System.Boolean"; continuar = false; }
                                            if (continuar && cell2.CellFormula == "FALSE()") { cellType2[i] = "System.Boolean"; continuar = false; }
                                            if (continuar) { cellType2[i] = "System.Double"; continuar = false; }
                                        }
                                        catch { }
                                    }
                                    break;
                            }
                            break;
                        default:
                            cellType2[i] = "System.String"; break;
                    }
                }
            }

            //Resolver las diferencias de Tipos
            if (cellType2[0] == cellType2[1]) { cellType = cellType2[0]; }
            else
            {
                if (cellType2[0] == null) cellType = cellType2[1];
                if (cellType2[1] == null) cellType = cellType2[0];
                if (cellType == "") cellType = "System.String";
            }

            //Obtener el nombre de la Columna
            string colName = "Column_{0}";
            try { colName = cell.StringCellValue; }
            catch { colName = string.Format(colName, colIndex); }

            //Verificar que NO se repita el Nombre de la Columna
            foreach (DataColumn col in Tabla.Columns)
            {
                if (col.ColumnName == colName) colName = string.Format("{0}_{1}", colName, colIndex);
            }

            //Agregar el campos de la tabla:
            colName = colName.SinTildes().Replace(".", "").Trim().Replace("   ", "_").Replace("  ", "_").Replace(" ", "_");
            DataColumn codigo = new DataColumn(colName, System.Type.GetType(cellType));
            Tabla.Columns.Add(codigo); colIndex++;
        }


        public static DataTable CreateTable<T>(string nombre)
        {
            Type entityType = typeof(T);
            DataTable dt = new DataTable(nombre);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            if (properties != null)
            {
                foreach (PropertyDescriptor prop in properties)
                {
                    DataColumn colString = new DataColumn(prop.Name);
                    if (prop.PropertyType.FullName != "System.String")
                    {
                        TypeCode typeCode = Type.GetTypeCode(prop.PropertyType);
                        switch (typeCode)
                        {
                            case TypeCode.Boolean:
                                colString.DataType = System.Type.GetType("System.Boolean");
                                break;
                            case TypeCode.Object:
                                if (prop.PropertyType.FullName.Contains("DateTime"))
                                {
                                    colString.DataType = System.Type.GetType("System.DateTime");
                                }
                                else if (prop.PropertyType.FullName.Contains("Decimal"))
                                {
                                    colString.DataType = System.Type.GetType("System.Decimal");
                                }
                                else if (prop.PropertyType.FullName.Contains("Int"))
                                {
                                    colString.DataType = System.Type.GetType("System.Int");
                                }
                                else if (prop.PropertyType.FullName.Contains("Boolean"))
                                {
                                    colString.DataType = System.Type.GetType("System.Boolean");
                                }
                                break;
                        }
                    }

                    dt.Columns.Add(colString);
                }
            }
            return dt;
        }
    }

    public static class StringExtensions
    {
        public static string SinTildes(this string texto) =>
            new String(
                texto.Replace("�", "o").Replace("�", "e").Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray()
            ).Normalize(NormalizationForm.FormC);
    }
    /// <summary>
    /// Sort order enumeration
    /// </summary>
    public enum SortOrder
    {
        Ascending,
        Descending
    }
    /// <summary>
    /// Sorts a generic collection of some object
    ///
    /// Example:
    ///     Dim MyList As List(Of MyClassType) = 'Populate the list somehow
    ///     Dim Sorter As New Sorter(Of MyClassType)    '''     Sorter.SortString = "Field1 DESC, Field2"
    ///     MyList.Sort(Sorter) 'After this call, the list is sorted
    /// </summary>
    public class GenericSorter<T> : IComparer<T>
    {

        private string _sort;
        /// <summary>
        /// Instantiate the class.
        /// </summary>
        public GenericSorter()
        {
        }
        /// <summary>
        /// Instantiate the class, setting the sort string.
        ///
        /// Example: "LastName DESC, FirstName"
        /// </summary>
        public GenericSorter(string SortString)
        {
            _sort = SortString;
        }
        /// <summary>
        /// The sort string used to perform the sort. Can sort on multiple fields.
        /// Use the property names of the class and basic SQL Syntax.
        ///
        /// Example: "LastName DESC, FirstName"
        /// </summary>
        public string SortString
        {
            get
            {
                if (_sort != null)
                {
                    return _sort.Trim();
                }
                return null;
            }
            set { _sort = value; }
        }
        /// <summary>
        /// This is an implementation of IComparer(Of T).Compare
        /// Can sort on multiple fields, or just one.
        /// </summary>
        public int Compare(T x, T y)
        {
            if (!string.IsNullOrEmpty(this.SortString))
            {
                const string err = "The property \"{0}\" does not exist in type \"{1}\"";
                Type type = typeof(T);
                Comparer comp = Comparer.DefaultInvariant;
                PropertyInfo info = null;
                foreach (string ex in this.SortString.Split(','))
                {
                    SortOrder dir = SortOrder.Ascending;
                    string field = null;
                    string expr = string.Empty;
                    expr = ex.Trim();
                    if (expr.EndsWith(" DESC"))
                    {
                        field = expr.Replace(" DESC", string.Empty).Trim();
                        dir = SortOrder.Descending;
                    }
                    else
                    {
                        field = expr.Replace(" ASC", string.Empty).Trim();
                    }
                    info = type.GetProperty(field);
                    if (info == null)
                    {
                        throw new MissingFieldException(string.Format(err, field, type.ToString()));
                    }
                    else
                    {
                        int Result = comp.Compare(info.GetValue(x, null), info.GetValue(y, null));
                        if (Result != 0)
                        {
                            if (dir == SortOrder.Descending)
                            {
                                return Result * -1;
                            }
                            else
                            {
                                return Result;
                            }
                        }
                    }
                }
            }
            return 0;
        }
    }
}



