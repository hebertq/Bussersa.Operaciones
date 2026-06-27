using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Modelo.Validaciones
{
    public static class Util
    {
        public static void LogToFile(string path, string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                File.AppendAllText(path, "\r\n\r\n" + System.DateTime.Now.ToString() + "\r\n" + logText);
            }
        }
        /// <summary>
        /// Convertir una cadena de texto xml a xml node
        /// </summary>
        /// <param name="pCadenaXml">cadena xml</param>
        /// <returns>Ojeto XmlNode</returns>
        public static XmlNode ToXmlNode(string pCadenaXml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(pCadenaXml);
            XmlNode newNode = doc.DocumentElement;
            return newNode;
        }
        public static string AnyoMesLetras(DateTime fecha)
        {
            return MesLetras(fecha.Month) + " " + fecha.Year.ToString();
        }
        public static string MesLetras(int mes)
        {
            string stringmes = ((Mes)mes).ToString();
            return stringmes;
        }
        public static string UpdString(string pNombre)
        {
            byte[] tempBytes;
            tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(pNombre);
            string asciiStr = System.Text.Encoding.UTF8.GetString(tempBytes);

            return asciiStr;
        }

        public static DateTime IsDateValid(string fechaTemp)
        {
            DateTimeFormatInfo DateTimeFormatGB = new CultureInfo("en-US").DateTimeFormat; // new CultureInfo("en-US").DateTimeFormat;
            return IsDate(fechaTemp, DateTimeFormatGB);
        }

        private static List<Object> AcceptableDateFormats = new List<Object>(72);
        private static DateTime IsDate(string value, DateTimeFormatInfo formatInfo)
        {
            DateTime fecha = new DateTime();
            if (AcceptableDateFormats.Count == 0)
            {
                foreach (var dateFormat in new[] { "yy", "yyyy" })
                {
                    foreach (var monthFormat in new[] { "M", "MM", "MMM" })
                    {
                        foreach (var yearFormat in new[] { "d", "dd" })
                        {
                            foreach (var separator in new[] { "-", "/" }) // formatInfo.DateSeparator ?
                            {
                                String shortDateFormat;
                                shortDateFormat = dateFormat + separator + monthFormat + separator + yearFormat;
                                AcceptableDateFormats.Add(shortDateFormat);
                            }
                        }
                    }
                }

                foreach (var dateFormat in new[] { "M", "MM", "MMM" })
                {
                    foreach (var monthFormat in new[] { "d", "dd" })
                    {
                        foreach (var yearFormat in new[] { "yy", "yyyy" })
                        {
                            foreach (var separator in new[] { "-", "/" }) // formatInfo.DateSeparator ?
                            {
                                String shortDateFormat;
                                shortDateFormat = dateFormat + separator + monthFormat + separator + yearFormat;
                                AcceptableDateFormats.Add(shortDateFormat);
                            }
                        }
                    }
                }

                foreach (var dateFormat in new[] { "d", "dd" })
                {
                    foreach (var monthFormat in new[] { "M", "MM", "MMM" })
                    {
                        foreach (var yearFormat in new[] { "yy", "yyyy" })
                        {
                            foreach (var separator in new[] { "-", "/" }) // formatInfo.DateSeparator ?
                            {
                                String shortDateFormat;
                                shortDateFormat = dateFormat + separator + monthFormat + separator + yearFormat;
                                AcceptableDateFormats.Add(shortDateFormat);
                            }
                        }
                    }
                }
            }

            DateTime unused;
            String sValue = mesnumero(value.ToString().Trim());
            CultureInfo es = new CultureInfo("es-NI");
            CultureInfo us = new CultureInfo("en-US");

            if (DateTime.TryParse(sValue, out unused) == true) return unused;
            else if (DateTime.TryParse(sValue, new DateTimeFormatInfo() { MonthDayPattern = "YYYY-MM-DD" }, DateTimeStyles.None, out unused) == true) return unused;
            else if (DateTime.TryParse(sValue, es, DateTimeStyles.None, out unused) == true) return unused;
            else if (DateTime.TryParse(sValue.Replace(" ", ""), us, DateTimeStyles.None, out unused) == true) return unused;
            else
            {
                foreach (String format in AcceptableDateFormats)
                {
                    if (DateTime.TryParseExact(sValue, format, formatInfo, DateTimeStyles.None, out unused) == true) return unused;
                }
            }
            return fecha;
        }

        private static string mesnumero(string fecha)
        {
            string fchanueva = fecha;
            string mesnumero = "";

            string mesletra = fecha.Split('-')[1].ToLower();
            switch (mesletra)
            {
                case "jan.":
                    mesnumero = "01";
                    break;
                case "feb.":
                    mesnumero = "02";
                    break;
                case "abr.":
                    mesnumero = "04";
                    break;
                case "mar.":
                    mesnumero = "04";
                    break;
                case "may.":
                    mesnumero = "05";
                    break;
                case "jun.":
                    mesnumero = "06";
                    break;
                case "ful.":
                    mesnumero = "07";
                    break;
                case "aug.":
                    mesnumero = "08";
                    break;
                case "sep.":
                    mesnumero = "09";
                    break;
                case "oct.":
                    mesnumero = "10";
                    break;
                case "nov.":
                    mesnumero = "11";
                    break;
                case "dec.":
                    mesnumero = "12";
                    break;
                default:
                    mesletra = "";
                    break;
            }

            if (mesletra != "")
                fchanueva = fecha.Replace(mesletra, mesnumero);

            return fchanueva;
        }
        public static int getEdad(DateTime? dt)
        {
            if (dt == null) return 0;

            var today = DateTime.Today;
            var dt2 = Convert.ToDateTime(dt);

            var a = (today.Year * 100 + today.Month) * 100 + today.Day;
            var b = (dt2.Year * 100 + dt2.Month) * 100 + dt2.Day;

            return (a - b) / 10000;
        }

        public static string GetValueFromNvpFile(string path, string name)
        {
            string rtnValue = string.Empty;
            foreach (string line in File.ReadLines(path))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (key == name)
                {
                    rtnValue = value;
                    break;
                }
            }
            return rtnValue;
        }

        public static byte[] ToByteArray(Stream stream)
        {
            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < stream.Length;)
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            return buffer;
        }

        public static void SaveValueToNvpFile(string path, string name, string value)
        {
            string item = string.Empty;
            List<string> lines = new List<string>();
            foreach (string line in File.ReadLines(path))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts[0].Trim() == name)
                {
                    parts[1] = value;
                    item = parts[0] + "=" + parts[1];
                }
                else
                {
                    item = line;
                }
                lines.Add(item);
            }
            File.WriteAllLines(path, lines);
        }

        public static Dictionary<string, string> GetAllFromNvpFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var dict = lines.Select(l => l.Split(new[] { '=' }, 2)).ToDictionary(a => a[0].Trim(), a => a[1].Trim());
            return dict;

            //Get items
            //var item = dict["item1"];
        }

        public static void SaveAllToNvpFile(string path, Dictionary<string, string> dict)
        {
            //Data entries
            //var dict = new Dictionary<string, string>
            //{
            //    { "name1", "value1" },
            //    { "name2", "value2" },
            //    // etc
            //}
            string[] lines = dict.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
            File.WriteAllLines(path, lines);
        }
      
        public static bool IsFileReady(String sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static readonly Regex _isNumericRegex = new Regex("^(" +
            /*Hex*/ @"0x[0-9a-f]+" + "|" +
            /*Bin*/ @"0b[01]+" + "|" +
            /*Oct*/ @"0[0-7]*" + "|" +
            /*Dec*/ @"((?!0)|[-+]|(?=0+\.))(\d*\.)?\d+(e\d+)?" +
            ")$");

        //String extension        
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static int CalculateTotalPages(long numberOfRecords, Int32 pageSize)
        {
            long result;
            int totalPages;

            Math.DivRem(numberOfRecords, pageSize, out result);

            if (result > 0)
                totalPages = (int)((numberOfRecords / pageSize)) + 1;
            else
                totalPages = (int)(numberOfRecords / pageSize);

            return totalPages;

        }

        /// <summary>
        /// Generate Random Number
        /// </summary>
        /// <returns></returns>
        public static int GenerateRandomNumber(int max)
        {
            Random rnd = new Random();
            int randomNumber = rnd.Next(max);
            return randomNumber;
        }

        /// <summary>
        /// Fold first letter of every word to uppercase
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UppercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            StringBuilder output = new StringBuilder();
            string[] words = s.Split(' ');
            foreach (string word in words)
            {
                char[] a = word.ToCharArray();
                a[0] = char.ToUpper(a[0]);
                string b = new string(a);
                output.Append(b + " ");
            }

            return output.ToString().Trim();
        }

        public static Boolean UppercasetLetter(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            string opertname = data.ToUpperInvariant();
            if (data == opertname)
                return true;
            else
                return false;
        }


        /// <summary>
        /// Check if date is a valid format
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Boolean IsDate(string date)
        {
            DateTime dateTime;
            return DateTime.TryParse(date, out dateTime);
        }

        /// <summary>
        /// IsNumeric
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Boolean IsNumeric(object entity)
        {
            if (entity == null) return false;

            int result;
            return int.TryParse(entity.ToString(), out result);
        }

        /// <summary>
        /// IsDouble
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Boolean IsDouble(object entity)
        {
            if (entity == null) return false;

            string e = entity.ToString();

            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = e.IndexOf(".", i)) != -1)
            {
                i += ".".Length;
                count++;
            }
            if (count > 1) return false;

            e = e.Replace(".", "");

            int result;
            return int.TryParse(e, out result);
        }

        /// <summary>
        /// Add a generic message string
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static List<String> Message(string message)
        {
            List<String> returnMessage = new List<String>();
            returnMessage.Add(message);
            return returnMessage;
        }


        /// <summary>
        /// Get String
        /// </summary>
        /// <param name="inValue"></param>
        /// <returns></returns>
        public static string GetString(string inValue)
        {
            return (inValue != null) ? (inValue) : String.Empty;
        }

        public static void SetDefaultValue(object clase)
        {
            foreach (PropertyInfo propertyInfo in clase.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType == typeof(string))
                {
                    if (propertyInfo.GetValue(clase, null) == null || propertyInfo.GetValue(clase, null).ToString() == string.Empty)
                        propertyInfo.SetValue(clase, "N/A");
                }
            }
        }

        public static string ConcatenarStr(this List<string> data)
        {
            var str = string.Empty;
            foreach (var item in data)
            {
                str += item + ",";
            }
            str = str.Remove(str.Length - 1, 1);
            return str;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        /// <summary>
        /// Quitar los dobles espacios de una cadena de texto		
        /// </summary>
        /// <param name="pCadenaTexto">Cadena de texto que contiene dobles espacios</param>
        /// <returns>Cadena de Texto o nulo de recibir cadena nula</returns>
        public static string QuitarDoblesEspacios(string pCadenaTexto)
        {
            if (string.IsNullOrEmpty(pCadenaTexto))
                return pCadenaTexto;

            while (pCadenaTexto.Contains("  "))
            {
                pCadenaTexto = pCadenaTexto.Replace("  ", " ");

            }
            return pCadenaTexto.Trim();
        }

        //public static byte[] ToByteArray(Stream stream)
        //{
        //    stream.Position = 0;
        //    byte[] buffer = new byte[stream.Length];
        //    for (int totalBytesCopied = 0; totalBytesCopied < stream.Length; )
        //        totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
        //    return buffer;
        //}
        public static XmlDocument CrearRespuestaErrorXML(string pMsgError, string pCodError, string pMsgExcepcion)
        {

            XmlDocument vXmlResultado = new XmlDocument();
            StringBuilder vStringBuilder = new StringBuilder();
            int vRespuestaError = 0;
            try
            {
                if (pCodError != "" && pCodError != "0000")
                {
                    vRespuestaError = 1;
                }
                vStringBuilder.Append("<respuesta>");
                vStringBuilder.Append("<respuesta_error>");
                vStringBuilder.Append("<cod_error>").Append(pCodError).Append("</cod_error>");
                vStringBuilder.Append("<mensaje_error>").Append(pMsgError).Append("</mensaje_error>");
                vStringBuilder.Append("<mensaje_tecnico_error>").Append(pMsgExcepcion).Append("</mensaje_tecnico_error>");
                vStringBuilder.Append("<existe_error>").Append(vRespuestaError.ToString()).Append("</existe_error>");
                vStringBuilder.Append("</respuesta_error>");
                vStringBuilder.Append("</respuesta>");
                vXmlResultado.LoadXml(vStringBuilder.ToString());
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Format("Capa:[UT] Metodo:[CrearXmlRespuestaError] pMsgError:{0} \n", pMsgError));
                sb.Append(ex.Message);
                sb.Append("\n ");

                throw ex;
            }
            return vXmlResultado;
        }

        /// <summary>
        /// Escribir un archivo en disco hacia c:\TestReports
        /// </summary>
        /// <param name="datos">objetos de datos a exportar</param>
        /// <param name="nombreArchivo">nombre deseado del arhcivo sin extensión</param>
        /// <param name="tipo">enviar xml/txt</param>
        public static void ResultToDisk(object datos, string nombreArchivo = "test", string tipo = "xml")
        {
            var fn = nombreArchivo + "_" + DateTime.Now.ToString("yyyyMMdd") + DateTime.Now.ToString("HH:mm:ss tt zz").Replace(":", "") + ".txt";
            fn = fn.Replace(" ", "");
            System.IO.File.WriteAllText(@"C:\TestReports\" + fn, tipo == "xml" ? ToXml(datos) : datos.ToString());
        }

        /// <summary>
        /// Convertir objeto a xml
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tipo"></param>
        /// <returns></returns>
        public static string ToXml<T>(T tipo)
        {
            string xmlTmp = string.Empty;
            StringWriter writer = new StringWriter();
            XmlSerializer ser = new XmlSerializer(tipo.GetType());

            ser.Serialize(writer, tipo);
            xmlTmp = writer.ToString();

            writer.Close();

            return xmlTmp;
        }

        /// <summary>
        ///  Genera reporte a traves de docentric
        /// </summary>
        /// <param name="inputPath">path relativo de la ruta de la plantilla</param>
        /// <param name="dataSource">Objeto datasource</param>
        /// <returns>Archivo en byte</returns>
        //public static Byte[] GeneraDocumento(string inputPath, object dataSource, _FORMATO formato)
        //{

        //    var opcion = GetFormatoDocumento(formato);
        //    using (var inputFileStream = new FileStream(inputPath, FileMode.Open))
        //    using (var outputFileStream = new MemoryStream())
        //    {
        //        var documentGenerator = new DocumentGenerator(dataSource);
        //        var result = documentGenerator.GenerateDocument(inputFileStream, outputFileStream, opcion);

        //        if (result.HasErrors)
        //        {
        //            var sb = new StringBuilder();
        //            foreach (var error in result.Errors) sb.Append(error.Message);
        //            throw new Exception(sb.ToString());
        //        }

        //        return outputFileStream.ToArray();
        //    }

        //    //return null;
        //}

        /// <summary>
        ///  Genera reporte a traves de docentric
        /// </summary>
        /// <param name="oPlantilla">Plantilla cargarda por recurso embebido</param>
        /// <param name="dataSource">Objeto datasource</param>
        /// <param name="formato">Formato de transformacion</param>
        /// <returns>Archivo en byte</returns>
        /*public static byte[] GeneraDocumento(Stream oPlantilla, object dataSource, _FORMATO formato)
        {
            var opcion = GetFormatoDocumento(formato);
            using (Stream reportDocumentStream = new MemoryStream())
            {
                DocumentGenerator dg = new DocumentGenerator(dataSource);
                DocumentGenerationResult result = dg.GenerateDocument(oPlantilla, reportDocumentStream, opcion);

                if (result.HasErrors)
                {
                    var sb = new StringBuilder();
                    foreach (var error in result.Errors) sb.Append(error.Message);
                    throw new Exception(sb.ToString());
                }

                return reportDocumentStream.ConfigureAwait();
            }
        }*/

        //public static SaveOptions GetFormatoDocumento(_FORMATO formato)
        //{
        //    SaveOptions opcion;
        //    switch (formato)
        //    {
        //        case _FORMATO.WORD:
        //            opcion = SaveOptions.Word;
        //            break;
        //        case _FORMATO.PDF:
        //            opcion = SaveOptions.Pdf;
        //            break;
        //        case _FORMATO.XPS:
        //            opcion = SaveOptions.Xps;
        //            break;
        //        default:
        //            opcion = SaveOptions.Word;
        //            break;
        //    }
        //    return opcion;
        //}

        public static XmlDocument ToXmldoc<T>(T tipo)
        {
            string XmlNode = Util.ToXml(tipo);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(XmlNode);
            return xml;
        }

        public static T XmlToObjet<T>(string testData)
        {
            T result = default(T);
            if (string.IsNullOrEmpty(testData)) return default(T);

            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(testData))
            {
                result = (T)ser.Deserialize(sr);
            }

            return result;
        }
        /// <summary>
        /// Returns the left part of this string instance.
        /// </summary>
        /// <param name="count">Number of characters to return.</param>
        public static string Left(string input, int count)
        {
            return input.Substring(0, Math.Min(input.Length, count));
        }
        /// <summary>
        /// Returns the right part of the string instance.
        /// </summary>
        /// <param name="count">Number of characters to return.</param>
        public static string Right(string input, int count)
        {
            return input.Substring(Math.Max(input.Length - count, 0), Math.Min(count, input.Length));
        }

        public static List<Dictionary<string, object>> ToDictionaryList<T>(IEnumerable<T> items)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(items);
            return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
        }
    }

    public enum _FORMATO
    {
        WORD,
        PDF,
        XPS
    }

    public enum Mes
    {
        Enero = 1,
        Febrero = 2,
        Marzo = 3,
        Abril = 4,
        Mayo = 5,
        Junio = 6,
        Julio = 7,
        Agosto = 8,
        Septiembre = 9,
        Octubre = 10,
        Noviembre = 11,
        Diciembre = 12
    }
}
