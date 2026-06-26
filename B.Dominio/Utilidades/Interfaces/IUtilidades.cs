using Modelo.ClasesGenericas;
using Modelo.Validaciones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using static Modelo.ClasesGenericas.AppConfig;

namespace Utilidades.Interfaces
{
    public interface IUtilidades
    {
        List<T> ObtenerDato<T>(string dato);
        string CambiarLetras(string str);
        string FormaDatos<T>(List<T> dato);
        T ObtenerRegistro<T>(string dato);
        TOut MapDatos<TOut, TIn>(TIn fromRecord) where TOut : new();
        HttpContent CreateHttpContent<T>(T content);
        List<InfoContex> ConnectionStrings { get; }
        List<InfoiSoap> SoapConfig { get; }
        AppReportInfo Inforeport { get; }
        AppSettingsSW ConfigService { get; }
        int SistemaId { get; }
        string BaseUrlApiLog { get; }
        string BaseUrlRedmine { get; }
        string ApiKeyRedmine { get; }
        IQueryable<T> MapToListReader<T>(DbDataReader dr);
        DataTable ConvertTo<T>(List<T> list, string nombre);
        List<T> ConvertTo<T>(DataTable dt);
        XmlNode ToXmlNode(string pCadenaXml);
        string ToXml<T>(T tipo);
        T XmlToObjet<T>(string testData);
        void MapTUpDList(object Out, object parameters);
        XmlDocument ToXmldoc<T>(T tipo);
        AppConfig Options { set; get; }
        string Encrypt(string password);
        string Decrypt(string cipherText);
        string AnyoMesLetras(DateTime fecha);
        DateTime DateFormat(string fecha);
        string Right(string value, int length);
        DataTable Excel_To_DataTable(MemoryStream fs, string Name, string sFileExtension, int numSheet);
    }
}
